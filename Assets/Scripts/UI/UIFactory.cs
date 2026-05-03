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
            t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            // Heavy black outline for readability
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 1f);
            outline.effectDistance = new Vector2(3, -3);
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
            img.raycastTarget = true;
            // All buttons use the yellow sliced sprite — ignore tint
            var btnSprite = LoadSprite("UI/btn_gold");
            if (btnSprite != null)
            {
                img.sprite = btnSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(1f, 0.78f, 0.15f, 1f);
            }
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            btn.colors = colors;

            var txt = MakeText(go.transform, "Label", label, 56, TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.raycastTarget = false;
            var outline = txt.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0f, 0f, 0f, 1f);
                outline.effectDistance = new Vector2(4, -4);
            }
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            // shift label up a bit because the bottom shadow on the sprite makes it look offset
            trt.offsetMin = new Vector2(0, 8);
            trt.offsetMax = new Vector2(0, 0);
            return btn;
        }

        static System.Collections.Generic.Dictionary<string, Sprite> _spriteCache = new System.Collections.Generic.Dictionary<string, Sprite>();
        public static Sprite LoadSprite(string resourcePath)
        {
            if (_spriteCache.TryGetValue(resourcePath, out var s)) return s;
            // First try as native Sprite asset (preserves 9-slice border from .meta)
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null) { _spriteCache[resourcePath] = sprite; return sprite; }
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) { _spriteCache[resourcePath] = null; return null; }
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(60, 60, 60, 60));
            _spriteCache[resourcePath] = sprite;
            return sprite;
        }

        public static Image MakeIcon(Transform parent, string resourcePath, Vector2 size)
        {
            return MakeIcon(parent, "Icon_" + resourcePath, resourcePath, size);
        }

        // Named overload — keeps debugging easier when the same parent
        // hosts several icons. Used by the medal strip / achievements
        // grid where every slot needs a unique name.
        public static Image MakeIcon(Transform parent, string name, string resourcePath, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            var sp = LoadSprite(resourcePath);
            if (sp != null) { img.sprite = sp; img.preserveAspect = true; }
            else img.color = new Color(1, 1, 1, 0.3f);
            return img;
        }

        public static Image MakeCardArt(Transform parent, string cardId)
        {
            var go = new GameObject("Art_" + cardId);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            var sp = LoadSprite("CardArt/" + cardId);
            if (sp != null) { img.sprite = sp; img.preserveAspect = true; }
            else img.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            return img;
        }

        public static InputField MakeInputField(Transform parent, string name, string startValue, int size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.05f, 0.07f, 0.18f, 0.85f);
            img.raycastTarget = true;
            var input = go.AddComponent<InputField>();
            input.targetGraphic = img;

            var text = MakeText(go.transform, "Text", startValue, size, TextAnchor.MiddleLeft);
            text.raycastTarget = false;
            text.color = Color.white;
            text.supportRichText = false;
            var trt = text.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(20, 4); trt.offsetMax = new Vector2(-20, -4);

            var placeholder = MakeText(go.transform, "Placeholder", "Введи ник", size, TextAnchor.MiddleLeft);
            placeholder.raycastTarget = false;
            placeholder.color = new Color(1, 1, 1, 0.4f);
            placeholder.fontStyle = FontStyle.Italic;
            var prt = placeholder.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(20, 4); prt.offsetMax = new Vector2(-20, -4);

            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = startValue;
            input.characterLimit = 16;
            return input;
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
