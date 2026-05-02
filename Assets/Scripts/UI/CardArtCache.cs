using System.Collections.Generic;
using UnityEngine;

namespace TrashRoyale.UI
{
    public static class CardArtCache
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_cache.TryGetValue(id, out var s)) return s;
            var tex = Resources.Load<Texture2D>("CardArt/" + id);
            if (tex == null) { _cache[id] = null; return null; }
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            _cache[id] = sprite;
            return sprite;
        }
    }
}
