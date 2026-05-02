using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Util;

namespace TrashRoyale.Match
{
    /// <summary>
    /// Builds units as billboarded quads textured with the card art. This is way
    /// more readable than colored primitives and matches the "meme card" theme of
    /// the game (Skibidi/Cheems/Gigachad/etc are immediately recognizable).
    ///
    /// At runtime each unit is:
    ///   - a small base disk on the ground (so it isn't floating)
    ///   - a billboarded quad above it that always faces the camera
    /// Air units skip the base disk and use a "shadow" disk projected on the ground.
    /// </summary>
    public static class ModelLoader
    {
        static readonly Dictionary<string, Texture2D> _texCache = new Dictionary<string, Texture2D>();

        public static GameObject InstantiateUnit(CardData card)
        {
            var go = new GameObject(card.id);

            // Base / shadow disk on ground
            var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disk.name = "Base";
            disk.transform.SetParent(go.transform, false);
            disk.transform.localScale = new Vector3(0.9f, 0.04f, 0.9f);
            disk.transform.localPosition = new Vector3(0, 0.02f, 0);
            var diskCol = disk.GetComponent<Collider>(); if (diskCol != null) Object.Destroy(diskCol);
            var diskRend = disk.GetComponent<MeshRenderer>();
            if (diskRend != null)
            {
                int idx = Mathf.Abs(card.id.GetHashCode()) % _palette.Length;
                var c = _palette[idx];
                diskRend.sharedMaterial = SafeShader.NewOpaqueMaterial(card.isAir
                    ? new Color(0, 0, 0, 0.4f)
                    : new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, 1f));
            }

            // Billboard art quad
            var billboard = GameObject.CreatePrimitive(PrimitiveType.Quad);
            billboard.name = "Art";
            billboard.transform.SetParent(go.transform, false);
            // Shrink width slightly so the silhouette doesn't look square
            billboard.transform.localScale = new Vector3(1.6f, 1.8f, 1f);
            billboard.transform.localPosition = new Vector3(0, 0.95f, 0);
            var bbCol = billboard.GetComponent<Collider>(); if (bbCol != null) Object.Destroy(bbCol);
            var rend = billboard.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                Texture tex = LoadTex(card.id);
                if (tex != null)
                {
                    rend.sharedMaterial = SafeShader.NewTexturedSpriteMaterial(tex);
                }
                else
                {
                    int idx = Mathf.Abs(card.id.GetHashCode()) % _palette.Length;
                    rend.sharedMaterial = SafeShader.NewOpaqueMaterial(_palette[idx]);
                }
            }
            billboard.AddComponent<Billboard>();

            return go;
        }

        static Texture2D LoadTex(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_texCache.TryGetValue(key, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>("CardArt/" + key);
            _texCache[key] = tex;
            return tex;
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
    }

    /// <summary>
    /// Rotates the GO so the local +Z always points away from the main camera.
    /// Used by ModelLoader for billboarded card art.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        Camera _cam;
        void Start() { _cam = Camera.main; }
        void LateUpdate()
        {
            if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }
            // Face camera: align our forward with the camera's forward (so the front
            // of the quad points at the camera).
            transform.rotation = Quaternion.LookRotation(_cam.transform.forward, Vector3.up);
        }
    }
}
