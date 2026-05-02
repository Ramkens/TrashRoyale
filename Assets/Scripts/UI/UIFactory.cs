using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TrashRoyale.UI
{
    public enum TextAlign { Left, Center, Right }

    public static class UIFactory
    {
        public static Font DefaultFont
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }
        static Font _font;

        public static Image MakePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Image MakePanelWithSprite(Transform parent, string name, Sprite sprite, Color tint)
        {
            var img = MakePanel(parent, name, tint);
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            return img;
        }

        public static Text MakeText(Transform parent, string name, string content, int size, TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var t = go.AddComponent<Text>();
            t.text = content;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.font = DefaultFont;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            // Add outline by default for readability
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2, -2);
            return t;
        }

        public static Text MakeText(Transform parent, string name, string content, int size, TextAlign align)
        {
            var anchor = align == TextAlign.Left ? TextAnchor.MiddleLeft :
                         align == TextAlign.Right ? TextAnchor.MiddleRight :
                         TextAnchor.MiddleCenter;
            return MakeText(parent, name, content, size, anchor);
        }

        public static Button MakeButton(Transform parent, string name, string label, UnityAction onClick)
        {
            return MakeButton(parent, name, label, onClick, new Color(0.95f, 0.7f, 0.15f));
        }

        public static Button MakeButton(Transform parent, string name, string label, UnityAction onClick, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = tint;
            // Try to apply a stylized button sprite if we have one
            var btnSprite = LoadSprite("UI/btn_gold");
            if (btnSprite != null)
            {
                img.sprite = btnSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            var btn = go.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            var txt = MakeText(go.transform, "Label", label, 44, TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold;
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            txt.color = Color.white;
            return btn;
        }

        static System.Collections.Generic.Dictionary<string, Sprite> _spriteCache = new System.Collections.Generic.Dictionary<string, Sprite>();
        public static Sprite LoadSprite(string resourcePath)
        {
            if (_spriteCache.TryGetValue(resourcePath, out var s)) return s;
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) { _spriteCache[resourcePath] = null; return null; }
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(40, 40, 40, 40));
            _spriteCache[resourcePath] = sprite;
            return sprite;
        }

        public static Slider MakeSlider(Transform parent, string name, Color fillColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.6f);
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(go.transform, false);
            var fart = fillArea.AddComponent<RectTransform>();
            fart.anchorMin = Vector2.zero;
            fart.anchorMax = Vector2.one;
            fart.offsetMin = new Vector2(2, 2);
            fart.offsetMax = new Vector2(-2, -2);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var frt = fill.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            var fimg = fill.AddComponent<Image>();
            fimg.color = fillColor;

            slider.fillRect = frt;
            slider.targetGraphic = bg;
            slider.handleRect = null;
            slider.transition = Selectable.Transition.None;
            return slider;
        }
    }
}
