using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Util;

namespace TrashRoyale.Match
{
    /// <summary>
    /// Spawns 3D character models for cards. Tries the imported glTF prefab
    /// from Resources/UnitGltf/&lt;name&gt;/scene first; if that's missing it
    /// falls back to a chunky primitive built out of Unity capsules/spheres so
    /// a missing model never crashes the match.
    ///
    /// Sketchfab models come in wildly different native scales (some are 1cm
    /// tall, others 15m tall) and orientations (Y-up vs Z-up). To keep every
    /// unit roughly the same on-arena size we measure the combined renderer
    /// bounds at runtime and uniformly scale to <see cref="TargetHeight"/>,
    /// then shift by Y so the feet sit on the ground. Per-card overrides in
    /// <see cref="OrientationOverride"/> rotate the source model when the
    /// glTF was authored lying on its side or facing the wrong way.
    /// </summary>
    public static class ModelLoader
    {
        // Map card id -> prefab name in Resources/UnitGltf. Some folders use
        // the sketchfab-suffixed name (skibidi_cameraman).
        static readonly Dictionary<string, string> PrefabName = new()
        {
            { "knight",   "knight"   },
            { "pig",      "pig"      },
            { "skibidi",  "skibidi_cameraman" },
            { "pocoyo",   "pocoyo"   },
            { "amongus",  "amongus"  },
            { "cheems",   "cheems"   },
            { "shrek",    "shrek"    },
            { "gigachad", "gigachad" },
            { "nyancat",  "nyancat"  },
        };

        // Target world-space height in arena units (~meters). Towers are ~3.2u
        // tall and the arena is 16u long, so 1.8u puts every fighter well
        // below tower height while staying readable from the CR-style camera.
        const float TargetHeight = 1.8f;

        // Per-card orientation correction applied BEFORE measuring bounds.
        // Sketchfab models authored Z-up or facing -Z look like they're lying
        // on their belly in Unity (Y-up, +Z forward); rotating fixes that.
        // NOTE: pig is intentionally NOT here — pigs walk horizontally on 4
        // legs, so the native orientation is correct and rotating would put
        // the pig on its back.
        static readonly Dictionary<string, Quaternion> OrientationOverride = new()
        {
            { "pocoyo",  Quaternion.Euler(-90f, 0f, 0f) },
            { "shrek",   Quaternion.Euler(-90f, 0f, 0f) },
        };

        // Per-card additional fudge multiplier on top of auto-fit (e.g. tank
        // units a touch taller than swarm). Defaults to 1.0 if not listed.
        static readonly Dictionary<string, float> SizeMultiplier = new()
        {
            { "shrek",    1.25f },  // shrek is a big ogre
            { "gigachad", 1.20f },  // gigachad is buff
            { "amongus",  0.85f },  // imposters are smaller
            { "cheems",   0.9f  },
        };

        public static GameObject InstantiateUnit(CardData card)
        {
            // 1) Special-case Nyan Cat: it's a flat 2D meme — even the glTF
            //    is a single-side plane that disappears from one half of the
            //    arena. Always render Nyan Cat as a two-sided billboard so it
            //    looks correct from both teams' camera angles.
            if (card.id == "nyancat")
            {
                return BuildNyanCatBillboard(card);
            }

            // 2) Try a real glTF-imported prefab. glTFast registers a
            //    ScriptedImporter that produces a GameObject for every
            //    .gltf under Assets/, so Resources.Load picks it up.
            if (PrefabName.TryGetValue(card.id, out var pname))
            {
                var prefab = Resources.Load<GameObject>("UnitGltf/" + pname + "/scene");
                if (prefab == null)
                    prefab = Resources.Load<GameObject>("UnitPrefabs/" + pname);
                if (prefab != null)
                {
                    return BuildFromPrefab(card, prefab);
                }
            }

            // 3) Fallback: chunky primitives so a missing prefab doesn't crash.
            var go = new GameObject(card.id);
            switch (card.id)
            {
                case "knight":   BuildKnight(go); break;
                case "pig":      BuildPig(go); break;
                case "skibidi":  BuildSkibidi(go); break;
                case "pocoyo":   BuildPocoyo(go); break;
                case "amongus":  BuildAmongUs(go); break;
                case "cheems":   BuildCheems(go); break;
                case "shrek":    BuildShrek(go); break;
                case "gigachad": BuildGigachad(go); break;
                case "nyancat":  BuildNyanCat(go); break;
                case "bomber":   BuildBomber(go); break;
                case "doge_mage": BuildDogeMage(go); break;
                default:         BuildGeneric(go, card); break;
            }
            return go;
        }

        /// <summary>
        /// Builds visual for stationary defensive buildings (cannon, tesla
        /// tower, etc.). Uses primitives only — buildings don't need realistic
        /// 3D meshes and primitives composite cleanly with the existing card
        /// art on the deck.
        /// </summary>
        public static GameObject InstantiateBuilding(CardData card)
        {
            var go = new GameObject(card.id);
            switch (card.id)
            {
                case "cannon": BuildCannon(go); break;
                case "tesla":  BuildTesla(go); break;
                case "totem":  BuildTotem(go); break;
                default:       BuildGenericBuilding(go); break;
            }
            return go;
        }

        // ---------- glTF prefab post-processing ----------

        /// <summary>
        /// Wraps the imported model in an outer GameObject so we can rotate
        /// the model child without touching the unit-control transform, then
        /// uniformly scales to <see cref="TargetHeight"/> and lifts to the
        /// floor. This fixes "model is huge / tiny / lying down" complaints
        /// uniformly without per-card hardcoded magic numbers.
        /// </summary>
        static GameObject BuildFromPrefab(CardData card, GameObject prefab)
        {
            var root = new GameObject(card.id);
            var inst = Object.Instantiate(prefab, root.transform);
            inst.name = "Model";

            // Strip imported colliders so navigation/aim raycasts ignore them.
            foreach (var col in inst.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(col);
            }

            // 1) Per-card orientation correction so Z-up source models stand
            //    up. Applied to the model child; the outer root keeps a clean
            //    transform that Unit/Tower can rotate to face targets.
            if (OrientationOverride.TryGetValue(card.id, out var rot))
            {
                inst.transform.localRotation = rot;
            }

            // 2) Compute combined renderer bounds AFTER rotation so the auto-
            //    fit scale is computed against the *visible* upright model.
            var bounds = ComputeWorldBounds(inst);
            float h = Mathf.Max(0.001f, bounds.size.y);
            float mult = SizeMultiplier.TryGetValue(card.id, out var m) ? m : 1f;
            float cardScale = card.modelScale > 0f ? card.modelScale : 1f;
            float uniformScale = (TargetHeight / h) * mult * cardScale;
            inst.transform.localScale = Vector3.one * uniformScale;

            // 3) Drop the model so its lowest renderer point sits at y=0.
            //    Recompute bounds after scaling for accuracy.
            var scaledBounds = ComputeWorldBounds(inst);
            float feetOffset = -(scaledBounds.min.y - root.transform.position.y);
            inst.transform.localPosition = new Vector3(0f, feetOffset, 0f);

            return root;
        }

        /// <summary>
        /// Combined world-space AABB of every Renderer under <paramref name="go"/>.
        /// Returns a zero-size box at the origin if there are no renderers, so
        /// callers can divide safely.
        /// </summary>
        static Bounds ComputeWorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(go.transform.position, Vector3.one * 0.001f);
            }
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }

        /// <summary>
        /// Two back-to-back textured quads showing CardArt/nyancat. Replaces
        /// the literally-flat glTF, which read as a single-side 2D plane and
        /// disappeared when viewed from the other lane. Includes a bobbing
        /// rainbow trail so it still looks like Nyan Cat in motion.
        /// </summary>
        static GameObject BuildNyanCatBillboard(CardData card)
        {
            var root = new GameObject(card.id);
            var sprite = Resources.Load<Texture2D>("CardArt/nyancat");

            // Two quads facing opposite directions so we can see Nyan Cat from
            // both halves of the arena without relying on a two-sided shader.
            BuildBillboardQuad(root.transform, sprite, 0f);
            BuildBillboardQuad(root.transform, sprite, 180f);

            // Rainbow trail floating behind the cat regardless of facing.
            var trailGo = new GameObject("RainbowTrail");
            trailGo.transform.SetParent(root.transform, false);
            trailGo.transform.localPosition = new Vector3(0f, 1.0f, -0.4f);
            Color[] rainbow = {
                new Color(1f, 0.2f, 0.2f),
                new Color(1f, 0.6f, 0.2f),
                new Color(1f, 1f, 0.2f),
                new Color(0.2f, 1f, 0.3f),
                new Color(0.3f, 0.6f, 1f),
                new Color(0.6f, 0.3f, 1f),
            };
            for (int i = 0; i < rainbow.Length; i++)
            {
                Cube(trailGo, rainbow[i], new Vector3(0.5f, 0.08f, 0.4f),
                    new Vector3(0f, 0.05f * (i - rainbow.Length * 0.5f), -0.3f * i));
            }

            return root;
        }

        /// <summary>One textured quad sized 1.6m tall, 2.4m wide, lifted 1m off the ground.</summary>
        static void BuildBillboardQuad(Transform parent, Texture2D tex, float yawDeg)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Billboard_" + Mathf.RoundToInt(yawDeg);
            q.transform.SetParent(parent, false);
            q.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
            q.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            q.transform.localScale = new Vector3(2.4f, 1.6f, 1f);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            var mat = tex != null
                ? SafeShader.NewTexturedSpriteMaterial(tex)
                : SafeShader.NewSpriteMaterial();
            // Sprite shader respects per-vertex alpha so the texture's
            // transparent edges blend cleanly against the arena.
            q.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // ---------- Primitive fallback builders ----------

        static void BuildKnight(GameObject go)
        {
            // Body capsule (armor), head sphere (helmet), tiny sword
            Body(go, new Color(0.85f, 0.85f, 0.9f), 0.85f);
            Head(go, new Color(0.7f, 0.7f, 0.75f), 1.55f, 0.55f);
            // Sword
            Cube(go, new Color(0.6f, 0.6f, 0.7f), new Vector3(0.12f, 0.7f, 0.12f),
                new Vector3(0.45f, 0.95f, 0.0f));
            // Cape
            Cube(go, new Color(0.85f, 0.2f, 0.25f), new Vector3(0.7f, 0.7f, 0.05f),
                new Vector3(0, 0.95f, -0.35f));
        }

        static void BuildPig(GameObject go)
        {
            // Pink chunky body, snout box on front, two tiny ears
            Cube(go, new Color(1f, 0.65f, 0.7f), new Vector3(1.05f, 0.85f, 1.4f), new Vector3(0, 0.6f, 0));
            // Snout
            Cube(go, new Color(1f, 0.55f, 0.6f), new Vector3(0.45f, 0.4f, 0.3f), new Vector3(0, 0.7f, 0.78f));
            // Ears
            Cube(go, new Color(1f, 0.55f, 0.6f), new Vector3(0.18f, 0.25f, 0.1f), new Vector3(-0.3f, 1.1f, 0.4f));
            Cube(go, new Color(1f, 0.55f, 0.6f), new Vector3(0.18f, 0.25f, 0.1f), new Vector3(0.3f, 1.1f, 0.4f));
            // Eyes
            Sphere(go, Color.black, 0.08f, new Vector3(-0.18f, 0.85f, 0.65f));
            Sphere(go, Color.black, 0.08f, new Vector3(0.18f, 0.85f, 0.65f));
        }

        static void BuildSkibidi(GameObject go)
        {
            // White toilet base + bowl, brown head sticking out, tiny eyes
            Cube(go, new Color(0.95f, 0.95f, 0.97f), new Vector3(0.85f, 0.55f, 0.6f), new Vector3(0, 0.3f, 0));
            // Toilet bowl rim
            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.transform.SetParent(go.transform, false);
            bowl.transform.localScale = new Vector3(0.85f, 0.07f, 0.65f);
            bowl.transform.localPosition = new Vector3(0, 0.66f, 0);
            Object.DestroyImmediate(bowl.GetComponent<Collider>());
            bowl.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(new Color(0.95f, 0.95f, 0.97f));
            // Skibidi head
            Sphere(go, new Color(0.92f, 0.78f, 0.65f), 0.42f, new Vector3(0, 1.1f, 0.05f));
            // Hair tuft (black)
            Sphere(go, new Color(0.15f, 0.1f, 0.07f), 0.32f, new Vector3(0, 1.45f, -0.05f));
            // Eyes
            Sphere(go, Color.white, 0.13f, new Vector3(-0.16f, 1.15f, 0.4f));
            Sphere(go, Color.white, 0.13f, new Vector3(0.16f, 1.15f, 0.4f));
            Sphere(go, Color.black, 0.06f, new Vector3(-0.16f, 1.15f, 0.5f));
            Sphere(go, Color.black, 0.06f, new Vector3(0.16f, 1.15f, 0.5f));
        }

        static void BuildPocoyo(GameObject go)
        {
            // Blue jumpsuit body, big round head, blue hat (everything blue)
            Cube(go, new Color(0.25f, 0.5f, 0.95f), new Vector3(0.7f, 0.85f, 0.5f), new Vector3(0, 0.55f, 0));
            Sphere(go, new Color(0.95f, 0.75f, 0.6f), 0.55f, new Vector3(0, 1.4f, 0));
            // Hat (cylinder)
            var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.transform.SetParent(go.transform, false);
            hat.transform.localScale = new Vector3(0.55f, 0.18f, 0.55f);
            hat.transform.localPosition = new Vector3(0, 1.85f, 0);
            Object.DestroyImmediate(hat.GetComponent<Collider>());
            hat.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(new Color(0.25f, 0.5f, 0.95f));
            // Eyes
            Sphere(go, Color.white, 0.13f, new Vector3(-0.18f, 1.45f, 0.45f));
            Sphere(go, Color.white, 0.13f, new Vector3(0.18f, 1.45f, 0.45f));
            Sphere(go, Color.black, 0.06f, new Vector3(-0.18f, 1.45f, 0.55f));
            Sphere(go, Color.black, 0.06f, new Vector3(0.18f, 1.45f, 0.55f));
        }

        static void BuildAmongUs(GameObject go)
        {
            // Red bean: capsule body, sphere visor on front
            Body(go, new Color(0.95f, 0.2f, 0.2f), 0.95f, 0.55f);
            // Backpack
            Cube(go, new Color(0.7f, 0.15f, 0.15f), new Vector3(0.5f, 0.65f, 0.3f), new Vector3(0, 0.95f, -0.45f));
            // Visor (cyan glossy oval)
            var visor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visor.transform.SetParent(go.transform, false);
            visor.transform.localScale = new Vector3(0.55f, 0.32f, 0.15f);
            visor.transform.localPosition = new Vector3(0, 1.2f, 0.45f);
            Object.DestroyImmediate(visor.GetComponent<Collider>());
            visor.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(new Color(0.55f, 0.85f, 1f));
        }

        static void BuildCheems(GameObject go)
        {
            // Tan dog: chubby box body, big round head, ears, snout
            Cube(go, new Color(0.92f, 0.78f, 0.55f), new Vector3(0.85f, 0.7f, 1.1f), new Vector3(0, 0.5f, 0));
            // Head
            Sphere(go, new Color(0.95f, 0.82f, 0.6f), 0.55f, new Vector3(0, 1.1f, 0.4f));
            // Ears
            Cube(go, new Color(0.85f, 0.7f, 0.45f), new Vector3(0.18f, 0.35f, 0.1f), new Vector3(-0.3f, 1.45f, 0.35f));
            Cube(go, new Color(0.85f, 0.7f, 0.45f), new Vector3(0.18f, 0.35f, 0.1f), new Vector3(0.3f, 1.45f, 0.35f));
            // Snout
            Sphere(go, new Color(0.95f, 0.85f, 0.65f), 0.22f, new Vector3(0, 1.0f, 0.85f));
            Sphere(go, Color.black, 0.08f, new Vector3(0, 1.05f, 1.0f));
            // Eyes
            Sphere(go, Color.white, 0.10f, new Vector3(-0.18f, 1.2f, 0.85f));
            Sphere(go, Color.white, 0.10f, new Vector3(0.18f, 1.2f, 0.85f));
            Sphere(go, Color.black, 0.05f, new Vector3(-0.18f, 1.2f, 0.92f));
            Sphere(go, Color.black, 0.05f, new Vector3(0.18f, 1.2f, 0.92f));
        }

        static void BuildShrek(GameObject go)
        {
            // Big green ogre: chunky body, big round head with ear-trumpets
            Cube(go, new Color(0.35f, 0.7f, 0.3f), new Vector3(1.3f, 1.2f, 1.0f), new Vector3(0, 0.7f, 0));
            // Head
            Sphere(go, new Color(0.55f, 0.8f, 0.4f), 0.6f, new Vector3(0, 1.65f, 0));
            // Ear trumpets
            var earL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            earL.transform.SetParent(go.transform, false);
            earL.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            earL.transform.localPosition = new Vector3(-0.55f, 1.65f, 0);
            earL.transform.localRotation = Quaternion.Euler(0, 0, 90);
            Object.DestroyImmediate(earL.GetComponent<Collider>());
            earL.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(new Color(0.55f, 0.8f, 0.4f));
            var earR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            earR.transform.SetParent(go.transform, false);
            earR.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            earR.transform.localPosition = new Vector3(0.55f, 1.65f, 0);
            earR.transform.localRotation = Quaternion.Euler(0, 0, 90);
            Object.DestroyImmediate(earR.GetComponent<Collider>());
            earR.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(new Color(0.55f, 0.8f, 0.4f));
            // Eyes
            Sphere(go, Color.white, 0.13f, new Vector3(-0.18f, 1.7f, 0.45f));
            Sphere(go, Color.white, 0.13f, new Vector3(0.18f, 1.7f, 0.45f));
            Sphere(go, Color.black, 0.06f, new Vector3(-0.18f, 1.7f, 0.55f));
            Sphere(go, Color.black, 0.06f, new Vector3(0.18f, 1.7f, 0.55f));
        }

        static void BuildGigachad(GameObject go)
        {
            // Bulky tan torso (huge), small head, big jaw
            Cube(go, new Color(0.85f, 0.65f, 0.5f), new Vector3(1.6f, 1.1f, 0.7f), new Vector3(0, 0.7f, 0));
            // Arms
            Cube(go, new Color(0.85f, 0.65f, 0.5f), new Vector3(0.35f, 1.0f, 0.35f), new Vector3(-0.95f, 0.6f, 0));
            Cube(go, new Color(0.85f, 0.65f, 0.5f), new Vector3(0.35f, 1.0f, 0.35f), new Vector3(0.95f, 0.6f, 0));
            // Head
            Sphere(go, new Color(0.92f, 0.78f, 0.62f), 0.4f, new Vector3(0, 1.55f, 0));
            // Hair (black blob)
            Sphere(go, new Color(0.1f, 0.08f, 0.06f), 0.42f, new Vector3(0, 1.78f, -0.05f));
            // Jaw (small cube)
            Cube(go, new Color(0.92f, 0.78f, 0.62f), new Vector3(0.35f, 0.18f, 0.3f), new Vector3(0, 1.3f, 0.2f));
            // Eyes (slits — small black cubes)
            Cube(go, Color.black, new Vector3(0.12f, 0.03f, 0.04f), new Vector3(-0.15f, 1.6f, 0.32f));
            Cube(go, Color.black, new Vector3(0.12f, 0.03f, 0.04f), new Vector3(0.15f, 1.6f, 0.32f));
        }

        static void BuildNyanCat(GameObject go)
        {
            // Pop-tart rectangle body (pink + sprinkles) + cat head, rainbow trail behind
            Cube(go, new Color(1f, 0.7f, 0.85f), new Vector3(1.05f, 0.55f, 0.65f), new Vector3(0, 1.4f, 0));
            // Cat head
            Sphere(go, new Color(0.65f, 0.65f, 0.65f), 0.4f, new Vector3(0, 1.8f, 0.4f));
            // Cat ears (cubes tilted)
            Cube(go, new Color(0.65f, 0.65f, 0.65f), new Vector3(0.18f, 0.2f, 0.05f), new Vector3(-0.18f, 2.05f, 0.4f));
            Cube(go, new Color(0.65f, 0.65f, 0.65f), new Vector3(0.18f, 0.2f, 0.05f), new Vector3(0.18f, 2.05f, 0.4f));
            // Eyes
            Sphere(go, Color.black, 0.06f, new Vector3(-0.13f, 1.85f, 0.7f));
            Sphere(go, Color.black, 0.06f, new Vector3(0.13f, 1.85f, 0.7f));
            // Rainbow trail: stack of colored quads behind
            Color[] rainbow = {
                new Color(1f, 0.2f, 0.2f),
                new Color(1f, 0.6f, 0.2f),
                new Color(1f, 1f, 0.2f),
                new Color(0.2f, 1f, 0.3f),
                new Color(0.3f, 0.6f, 1f),
                new Color(0.6f, 0.3f, 1f),
            };
            for (int i = 0; i < rainbow.Length; i++)
            {
                Cube(go, rainbow[i], new Vector3(0.6f, 0.1f, 0.5f),
                    new Vector3(0, 1.3f + i * 0.1f, -0.55f));
            }
        }

        static void BuildGeneric(GameObject go, CardData card)
        {
            int idx = Mathf.Abs(card.id.GetHashCode()) % 6;
            Color[] colors = {
                new Color(0.95f, 0.85f, 0.4f),
                new Color(1f, 0.5f, 0.5f),
                new Color(0.4f, 0.7f, 1f),
                new Color(0.5f, 0.9f, 0.4f),
                new Color(0.95f, 0.5f, 0.95f),
                new Color(1f, 0.7f, 0.3f),
            };
            Body(go, colors[idx], 0.9f);
            Head(go, colors[idx], 1.55f, 0.55f);
        }

        // ---------- Primitive helpers ----------

        static void Body(GameObject parent, Color c, float h, float w = 0.7f)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            p.transform.SetParent(parent.transform, false);
            p.transform.localScale = new Vector3(w, h * 0.5f, w);
            p.transform.localPosition = new Vector3(0, h * 0.5f, 0);
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(c);
        }

        static void Head(GameObject parent, Color c, float y, float r)
        {
            Sphere(parent, c, r, new Vector3(0, y, 0));
        }

        static GameObject Cube(GameObject parent, Color c, Vector3 size, Vector3 pos)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.transform.SetParent(parent.transform, false);
            p.transform.localScale = size;
            p.transform.localPosition = pos;
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(c);
            return p;
        }

        static GameObject Sphere(GameObject parent, Color c, float r, Vector3 pos)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.transform.SetParent(parent.transform, false);
            p.transform.localScale = new Vector3(r * 2, r * 2, r * 2);
            p.transform.localPosition = pos;
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(c);
            return p;
        }

        // ---------- New units (PR2) ----------

        // Bomber: cute cartoon goblin lobbing a bomb. Splash damage.
        static void BuildBomber(GameObject go)
        {
            Body(go, new Color(0.45f, 0.85f, 0.45f), 1.0f, 0.55f); // green body
            Head(go, new Color(0.6f, 0.95f, 0.55f), 1.5f, 0.45f);
            // Bomb in hand.
            Sphere(go, new Color(0.1f, 0.1f, 0.1f), 0.18f, new Vector3(0.55f, 1.0f, 0.0f));
            // Fuse.
            Cube(go, new Color(1f, 0.7f, 0.2f), new Vector3(0.05f, 0.18f, 0.05f),
                new Vector3(0.55f, 1.22f, 0.0f));
            // Tiny eyes.
            Sphere(go, new Color(0.05f, 0.05f, 0.05f), 0.06f, new Vector3(0.18f, 1.55f, 0.38f));
            Sphere(go, new Color(0.05f, 0.05f, 0.05f), 0.06f, new Vector3(-0.18f, 1.55f, 0.38f));
        }

        // Doge Mage: blocky shiba in a wizard hat shooting splashy spells.
        static void BuildDogeMage(GameObject go)
        {
            // Body — fluffy tan torso.
            Body(go, new Color(0.95f, 0.78f, 0.5f), 0.9f, 0.65f);
            // Head with snout.
            Head(go, new Color(0.95f, 0.78f, 0.5f), 1.4f, 0.42f);
            Cube(go, new Color(1f, 0.95f, 0.85f), new Vector3(0.35f, 0.25f, 0.45f),
                new Vector3(0f, 1.32f, 0.45f));
            // Wizard hat (purple cone is too fancy → stacked cubes).
            Cube(go, new Color(0.4f, 0.2f, 0.6f), new Vector3(0.7f, 0.12f, 0.7f),
                new Vector3(0f, 1.78f, 0f));
            Cube(go, new Color(0.4f, 0.2f, 0.6f), new Vector3(0.4f, 0.45f, 0.4f),
                new Vector3(0f, 2.05f, 0f));
            // Star on hat.
            Sphere(go, new Color(1f, 0.95f, 0.4f), 0.08f, new Vector3(0f, 2.32f, 0f));
            // Floating staff orb (pre-shot).
            Sphere(go, new Color(0.6f, 0.4f, 1f), 0.16f, new Vector3(0.55f, 1.0f, 0.0f));
        }

        // ---------- Buildings (PR2) ----------

        static void BuildCannon(GameObject go)
        {
            // Stone base — wide cylinder.
            var basePart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePart.transform.SetParent(go.transform, false);
            basePart.transform.localScale = new Vector3(1.4f, 0.25f, 1.4f);
            basePart.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            Object.DestroyImmediate(basePart.GetComponent<Collider>());
            basePart.GetComponent<MeshRenderer>().sharedMaterial =
                SafeShader.NewOpaqueMaterial(new Color(0.45f, 0.45f, 0.5f));

            // Pivot block.
            Cube(go, new Color(0.3f, 0.3f, 0.35f), new Vector3(0.6f, 0.5f, 0.6f),
                new Vector3(0f, 0.75f, 0f));

            // Barrel — long cylinder pointing forward.
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.transform.SetParent(go.transform, false);
            barrel.transform.localScale = new Vector3(0.35f, 0.7f, 0.35f);
            barrel.transform.localPosition = new Vector3(0f, 0.95f, 0.55f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(barrel.GetComponent<Collider>());
            barrel.GetComponent<MeshRenderer>().sharedMaterial =
                SafeShader.NewOpaqueMaterial(new Color(0.2f, 0.2f, 0.25f));
        }

        static void BuildTesla(GameObject go)
        {
            // Stone base.
            var basePart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePart.transform.SetParent(go.transform, false);
            basePart.transform.localScale = new Vector3(1.2f, 0.2f, 1.2f);
            basePart.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            Object.DestroyImmediate(basePart.GetComponent<Collider>());
            basePart.GetComponent<MeshRenderer>().sharedMaterial =
                SafeShader.NewOpaqueMaterial(new Color(0.4f, 0.4f, 0.45f));

            // Insulator stack.
            Cube(go, new Color(0.85f, 0.85f, 0.85f), new Vector3(0.35f, 0.4f, 0.35f),
                new Vector3(0f, 0.6f, 0f));
            Cube(go, new Color(0.95f, 0.95f, 0.95f), new Vector3(0.5f, 0.1f, 0.5f),
                new Vector3(0f, 0.85f, 0f));

            // Coil — stacked rings.
            for (int i = 0; i < 4; i++)
            {
                Cube(go, new Color(0.55f, 0.4f, 0.2f),
                    new Vector3(0.55f - i * 0.05f, 0.06f, 0.55f - i * 0.05f),
                    new Vector3(0f, 0.95f + i * 0.18f, 0f));
            }

            // Glowing cap — bright cyan sphere.
            Sphere(go, new Color(0.4f, 0.95f, 1f), 0.18f, new Vector3(0f, 1.85f, 0f));
            Sphere(go, new Color(0.85f, 0.95f, 1f), 0.08f, new Vector3(0f, 2.05f, 0f));
        }

        static void BuildTotem(GameObject go)
        {
            // Cursed totem: stacked cubes with painted faces.
            Cube(go, new Color(0.45f, 0.3f, 0.2f), new Vector3(1.0f, 0.6f, 1.0f),
                new Vector3(0f, 0.3f, 0f));
            Cube(go, new Color(0.95f, 0.85f, 0.6f), new Vector3(0.85f, 0.55f, 0.85f),
                new Vector3(0f, 0.9f, 0f));
            // Eyes.
            Sphere(go, new Color(0.95f, 0.2f, 0.2f), 0.08f, new Vector3(0.2f, 1.0f, 0.45f));
            Sphere(go, new Color(0.95f, 0.2f, 0.2f), 0.08f, new Vector3(-0.2f, 1.0f, 0.45f));
            Cube(go, new Color(0.6f, 0.3f, 0.2f), new Vector3(0.7f, 0.5f, 0.7f),
                new Vector3(0f, 1.45f, 0f));
            Cube(go, new Color(0.95f, 0.85f, 0.5f), new Vector3(0.55f, 0.4f, 0.55f),
                new Vector3(0f, 1.85f, 0f));
        }

        static void BuildGenericBuilding(GameObject go)
        {
            Cube(go, new Color(0.5f, 0.5f, 0.55f), new Vector3(1.2f, 0.3f, 1.2f),
                new Vector3(0f, 0.15f, 0f));
            Cube(go, new Color(0.7f, 0.7f, 0.75f), new Vector3(0.8f, 1.2f, 0.8f),
                new Vector3(0f, 0.9f, 0f));
        }
    }
}
