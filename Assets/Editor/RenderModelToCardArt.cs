using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TrashRoyale.Core;

namespace TrashRoyale.EditorTools
{
    /// <summary>
    /// Editor utility that renders each unit/building glTF model to a
    /// transparent 1024x1024 PNG into tools/art_raw/<id>.png so that
    /// gen_card_art.py can wrap a CR-style frame around it. Lets us ship
    /// real character portraits on cards without HF inference credits.
    ///
    /// Run from CI:
    ///   Unity -batchmode -nographics -projectPath . \
    ///         -executeMethod TrashRoyale.EditorTools.RenderModelToCardArt.RenderAll \
    ///         -quit
    /// </summary>
    public static class RenderModelToCardArt
    {
        // Map card id -> prefab folder under Resources/UnitGltf. Mirrors
        // ModelLoader.PrefabName but is intentionally duplicated so this
        // editor tool stays standalone (no Match/ModelLoader dependency).
        static readonly Dictionary<string, string> Prefabs = new()
        {
            { "knight",          "knight"            },
            { "pig",             "pig"               },
            { "skibidi",         "skibidi_cameraman" },
            { "pocoyo",          "pocoyo"            },
            { "amongus",         "amongus"           },
            { "cheems",          "cheems"            },
            { "shrek",           "shrek"             },
            { "gigachad",        "gigachad"          },
            { "nyancat",         "nyancat"           },
            { "capybara",        "capybara"          },
            { "slender",         "slender"           },
            { "streamer_girl",   "streamer_girl"     },
            { "john_pork",       "john_pork"         },
            { "pepe_mage",       "pepe_mage"         },
            { "putin_cat",       "putin_cat"         },
            { "ronald_siu",      "ronald_siu"        },
            { "imposter_hut",    "imposter_hut"      },
        };

        const int RenderSize = 1024;

        [MenuItem("Tools/TrashRoyale/Render Models to Card Art")]
        public static void RenderAll()
        {
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../tools/art_raw"));
            Directory.CreateDirectory(outDir);

            int success = 0, failed = 0;
            foreach (var kv in Prefabs)
            {
                string cardId = kv.Key;
                string prefabName = kv.Value;
                try
                {
                    var prefab = Resources.Load<GameObject>("UnitGltf/" + prefabName + "/scene");
                    if (prefab == null)
                    {
                        Debug.LogWarning($"[RenderCardArt] missing prefab UnitGltf/{prefabName}/scene for card {cardId}");
                        failed++;
                        continue;
                    }
                    string outPath = Path.Combine(outDir, cardId + ".png");
                    RenderOne(prefab, cardId, outPath);
                    success++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[RenderCardArt] {cardId} failed: {ex.Message}");
                    failed++;
                }
            }
            Debug.Log($"[RenderCardArt] DONE: {success} ok, {failed} failed -> {outDir}");
        }

        static void RenderOne(GameObject prefab, string cardId, string outPath)
        {
            // Spawn an isolated camera + light + the model under a temp parent
            // so we don't pollute the open scene.
            var temp = new GameObject("__render_card_root__");
            try
            {
                // Strong 3-point lighting so PBR materials read brightly on
                // the rendered card. Default scene ambient in batch mode is
                // basically zero so we have to overpower it ourselves.
                var lightGo = new GameObject("KeyLight");
                lightGo.transform.SetParent(temp.transform, false);
                var key = lightGo.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 2.8f;
                key.color = Color.white;
                lightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

                var fillGo = new GameObject("FillLight");
                fillGo.transform.SetParent(temp.transform, false);
                var fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 1.4f;
                fill.color = new Color(0.85f, 0.9f, 1f);
                fillGo.transform.rotation = Quaternion.Euler(20f, 150f, 0f);

                var rimGo = new GameObject("RimLight");
                rimGo.transform.SetParent(temp.transform, false);
                var rim = rimGo.AddComponent<Light>();
                rim.type = LightType.Directional;
                rim.intensity = 1.0f;
                rim.color = new Color(1f, 0.95f, 0.85f);
                rimGo.transform.rotation = Quaternion.Euler(-25f, 60f, 0f);

                // Bump RenderSettings ambient to compensate for batchmode's
                // dark default and so dark-textured models still register
                // visible surface detail.
                var prevAmb = RenderSettings.ambientLight;
                var prevAmbMode = RenderSettings.ambientMode;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

                var modelGo = Object.Instantiate(prefab);
                modelGo.transform.SetParent(temp.transform, false);
                modelGo.transform.localPosition = Vector3.zero;
                modelGo.transform.localRotation = Quaternion.identity;

                // Apply the same Z-up to Y-up rotation used by the in-game
                // ModelLoader for these problematic Sketchfab uploads so the
                // rendered card matches the in-arena pose.
                if (cardId == "pocoyo" || cardId == "shrek")
                {
                    modelGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                }

                // Compute combined renderer bounds and frame the camera.
                Bounds bounds = ComputeBounds(modelGo);
                if (bounds.size == Vector3.zero)
                {
                    Object.DestroyImmediate(temp);
                    Debug.LogWarning($"[RenderCardArt] {cardId} has no renderers, skipping");
                    return;
                }

                var camGo = new GameObject("RenderCam");
                camGo.transform.SetParent(temp.transform, false);
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // transparent
                cam.orthographic = false;
                cam.fieldOfView = 25f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 100f;

                // Frame the model: position the cam in front-and-slightly-above
                // the bounds center, distance proportional to bounds size so
                // the model fits the viewport.
                Vector3 center = bounds.center;
                float size = Mathf.Max(bounds.size.x, bounds.size.y) * 1.1f;
                float dist = size / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
                Vector3 dir = (Quaternion.Euler(15f, 25f, 0f) * Vector3.forward);
                cam.transform.position = center - dir * dist;
                cam.transform.LookAt(center);

                // RenderTexture with alpha so the background stays transparent.
                var rt = new RenderTexture(RenderSize, RenderSize, 24, RenderTextureFormat.ARGB32);
                rt.antiAliasing = 8;
                cam.targetTexture = rt;
                cam.Render();

                var prevActive = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(RenderSize, RenderSize, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, RenderSize, RenderSize), 0, 0);
                tex.Apply();
                RenderTexture.active = prevActive;
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);

                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Debug.Log($"[RenderCardArt] {cardId} -> {outPath}");
            }
            finally
            {
                if (temp != null) Object.DestroyImmediate(temp);
            }
        }

        static Bounds ComputeBounds(GameObject root)
        {
            var rends = root.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }
    }
}
