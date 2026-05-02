using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Core;

namespace TrashRoyale.Match
{
    public static class ModelLoader
    {
        static readonly Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();

        public static GameObject InstantiateUnit(CardData card)
        {
            GameObject src = LoadPrefab(card.modelKey);
            GameObject go;
            if (src != null)
            {
                go = Object.Instantiate(src);
            }
            else
            {
                go = BuildFallbackPrimitive(card);
            }
            go.name = card.id;
            go.transform.localScale = Vector3.one * Mathf.Max(0.01f, card.modelScale);
            return go;
        }

        static GameObject LoadPrefab(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_cache.TryGetValue(key, out var cached)) return cached;
            var prefab = Resources.Load<GameObject>("UnitPrefabs/" + key);
            if (prefab != null)
            {
                _cache[key] = prefab;
                return prefab;
            }
            return null;
        }

        static readonly Color[] _palette = {
            new Color(0.95f, 0.85f, 0.4f), // knight gold
            new Color(1f, 0.7f, 0.7f),     // pig pink
            new Color(0.6f, 0.3f, 0.1f),   // skibidi brown
            new Color(0.4f, 0.7f, 1f),     // pocoyo blue
            new Color(0.95f, 0.2f, 0.2f),  // amongus red
            new Color(0.95f, 0.85f, 0.5f), // cheems
            new Color(0.5f, 0.9f, 0.4f),   // shrek green
            new Color(0.85f, 0.6f, 0.45f), // gigachad
            new Color(0.95f, 0.5f, 0.95f), // nyancat
            new Color(1f, 0.5f, 0.1f),     // fireball
        };

        static GameObject BuildFallbackPrimitive(CardData card)
        {
            var go = new GameObject(card.id + "_fallback");
            var shape = card.isAir ? PrimitiveType.Sphere : PrimitiveType.Capsule;
            var prim = GameObject.CreatePrimitive(shape);
            prim.transform.SetParent(go.transform, false);
            var col = prim.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            var rend = prim.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                int idx = Mathf.Abs(card.id.GetHashCode()) % _palette.Length;
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = _palette[idx];
                rend.sharedMaterial = mat;
            }
            return go;
        }
    }
}
