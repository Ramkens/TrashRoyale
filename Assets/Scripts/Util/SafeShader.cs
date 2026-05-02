using UnityEngine;

namespace TrashRoyale.Util
{
    /// <summary>
    /// Wraps Shader.Find with guaranteed non-null fallbacks. On Android,
    /// Unity strips shaders that aren't in GraphicsSettings.alwaysIncludedShaders,
    /// so Shader.Find("Standard") and friends often return null at runtime.
    /// We fall through several built-in shader names and finally to Hidden/InternalErrorShader,
    /// which Unity always ships.
    /// </summary>
    public static class SafeShader
    {
        // Cache to avoid repeated Shader.Find calls.
        static Shader _opaque;     // 3D, colored or textured
        static Shader _unlitColor; // 3D unlit / HP bars
        static Shader _sprite;     // 2D / particles fallback

        public static Shader Opaque
        {
            get
            {
                if (_opaque == null)
                    _opaque = First("Standard", "Universal Render Pipeline/Lit",
                        "Legacy Shaders/Diffuse", "Sprites/Default", "Hidden/InternalErrorShader");
                return _opaque;
            }
        }

        public static Shader UnlitColor
        {
            get
            {
                if (_unlitColor == null)
                    _unlitColor = First("Unlit/Color", "Universal Render Pipeline/Unlit",
                        "Legacy Shaders/Diffuse", "Sprites/Default", "Hidden/InternalErrorShader");
                return _unlitColor;
            }
        }

        public static Shader Sprite
        {
            get
            {
                if (_sprite == null)
                    _sprite = First("Sprites/Default", "UI/Default",
                        "Particles/Standard Unlit", "Hidden/InternalErrorShader");
                return _sprite;
            }
        }

        public static Material NewOpaqueMaterial(Color color)
        {
            var m = new Material(Opaque);
            m.color = color;
            return m;
        }

        public static Material NewUnlitColorMaterial(Color color)
        {
            var m = new Material(UnlitColor);
            m.color = color;
            return m;
        }

        public static Material NewSpriteMaterial()
        {
            return new Material(Sprite);
        }

        /// <summary>
        /// Translucent material for floor overlays / drag-preview rings. Uses the
        /// sprite shader so it respects per-vertex alpha, and falls back gracefully
        /// when Standard isn't available on Android.
        /// </summary>
        public static Material NewTransparentMaterial(Color color)
        {
            var m = new Material(Sprite);
            if (m.HasProperty("_Color")) m.color = color;
            else m.color = color;
            // Sprites/Default already does proper alpha blending; no extra setup
            // needed beyond color. The renderer also has to be set to no-cast
            // shadows by the caller.
            return m;
        }

        public static Material NewTexturedOpaqueMaterial(Texture tex)
        {
            var m = new Material(Opaque);
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        public static Material NewTexturedSpriteMaterial(Texture tex)
        {
            var m = new Material(Sprite);
            if (tex != null) m.mainTexture = tex;
            // Sprites/Default uses _Color tint; default to white so the texture is unmodulated.
            if (m.HasProperty("_Color")) m.color = Color.white;
            return m;
        }

        static Shader First(params string[] names)
        {
            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s != null) return s;
            }
            // Last resort: Hidden/InternalErrorShader is shipped in every Unity build.
            return Shader.Find("Hidden/InternalErrorShader");
        }
    }
}
