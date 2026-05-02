using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
#if HAS_GLTFAST
using GLTFast;
#endif

namespace TrashRoyale.EditorTools
{
    /// <summary>
    /// Imports raw GLTF model folders from _assets/models/raw/<name>/scene.gltf
    /// into Resources/UnitPrefabs/<name>.prefab so they can be loaded at runtime
    /// by ModelLoader without needing to ship loose GLTF files.
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

            foreach (var dir in Directory.GetDirectories(rawDir))
            {
                string name = Path.GetFileName(dir);
                string gltfPath = Path.Combine(dir, "scene.gltf");
                if (!File.Exists(gltfPath))
                {
                    Debug.LogWarning($"[Gltf] no scene.gltf in {dir}");
                    continue;
                }
                ImportOne(name, gltfPath, prefabDir);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void ImportOne(string name, string gltfPath, string prefabDir)
        {
#if HAS_GLTFAST
            var go = new GameObject(name);
            var task = ImportAsync(go, gltfPath);
            while (!task.IsCompleted) { System.Threading.Thread.Sleep(5); }
            if (!task.Result)
            {
                Debug.LogError($"[Gltf] import failed for {name}");
                UnityEngine.Object.DestroyImmediate(go);
                return;
            }
            string prefabPath = $"{prefabDir}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log($"[Gltf] saved {prefabPath}");
#else
            Debug.LogWarning($"[Gltf] HAS_GLTFAST not defined; skipping {name}");
#endif
        }

#if HAS_GLTFAST
        static async Task<bool> ImportAsync(GameObject parent, string gltfPath)
        {
            var importer = new GltfImport();
            bool success = await importer.Load(new Uri("file://" + gltfPath));
            if (!success) return false;
            await importer.InstantiateMainSceneAsync(parent.transform);
            return true;
        }
#endif
    }
}
