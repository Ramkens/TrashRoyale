using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEditor;
using UnityEngine;

namespace TrashRoyale.EditorTools
{
    /// <summary>
    /// Imports raw glTF model folders from <c>_assets/models/raw/&lt;name&gt;/scene.gltf</c>
    /// into <c>Resources/UnitPrefabs/&lt;name&gt;.prefab</c> so they can be loaded at
    /// runtime by ModelLoader without shipping loose glTF files. Run from
    /// the menu (TrashRoyale → Import GLTF Models → Prefabs) or via
    /// <c>-executeMethod TrashRoyale.EditorTools.GltfImporter.ImportAll</c>
    /// during CI / local builds.
    /// </summary>
    public static class GltfImporter
    {
        [MenuItem("TrashRoyale/Import GLTF Models -> Prefabs")]
        public static void ImportAll()
        {
            string projRoot = Path.GetDirectoryName(Application.dataPath) ?? "";
            string rawDir = Path.Combine(projRoot, "_assets/models/raw");
            string prefabDir = "Assets/Resources/UnitPrefabs";
            Directory.CreateDirectory(Path.Combine(projRoot, prefabDir));

            if (!Directory.Exists(rawDir))
            {
                Debug.LogWarning("[Gltf] no raw models dir: " + rawDir);
                return;
            }

            int ok = 0, fail = 0;
            foreach (var dir in Directory.GetDirectories(rawDir))
            {
                string name = Path.GetFileName(dir);
                string gltfPath = Path.Combine(dir, "scene.gltf");
                if (!File.Exists(gltfPath))
                {
                    Debug.LogWarning($"[Gltf] no scene.gltf in {dir}");
                    continue;
                }
                if (ImportOne(name, gltfPath, prefabDir)) ok++; else fail++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Gltf] import done: {ok} ok, {fail} failed");
        }

        static bool ImportOne(string name, string gltfPath, string prefabDir)
        {
            var go = new GameObject(name);
            try
            {
                var task = ImportAsync(go, gltfPath);
                while (!task.IsCompleted) System.Threading.Thread.Sleep(5);
                if (!task.Result)
                {
                    Debug.LogError($"[Gltf] import failed for {name}");
                    UnityEngine.Object.DestroyImmediate(go);
                    return false;
                }
                string prefabPath = $"{prefabDir}/{name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                UnityEngine.Object.DestroyImmediate(go);
                Debug.Log($"[Gltf] saved {prefabPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Gltf] exception importing {name}: {ex.Message}");
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                return false;
            }
        }

        static async Task<bool> ImportAsync(GameObject parent, string gltfPath)
        {
            // Use UninterruptedDeferAgent so glTFast doesn't try to call
            // DontDestroyOnLoad / coroutines while running inside the editor (the
            // default agent does and crashes during a batch import).
            var deferAgent = new UninterruptedDeferAgent();
            var importer = new GltfImport(deferAgent: deferAgent);
            bool success = await importer.Load(new Uri("file://" + gltfPath));
            if (!success) return false;
            await importer.InstantiateMainSceneAsync(parent.transform);
            return true;
        }
    }
}
