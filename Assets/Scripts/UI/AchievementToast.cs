using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Persistence;
using TrashRoyale.Audio;

namespace TrashRoyale.UI
{
    /// <summary>
    /// Slides a "MEDAL UNLOCKED" toast in from the right when an
    /// achievement is unlocked, then auto-dismisses after a few
    /// seconds. Multiple toasts queue and play one after another so a
    /// single match that unlocks several medals doesn't pile them on
    /// top of each other.
    /// </summary>
    public class AchievementToast : MonoBehaviour
    {
        static readonly Queue<AchievementDef> _queue = new Queue<AchievementDef>();
        static AchievementToast _active;

        Canvas _canvas;
        RectTransform _panel;
        Image _medalIcon;
        Text _titleText;
        Text _descText;
        float _t;
        AchievementDef _def;

        public static void ShowQueue(List<AchievementDef> defs)
        {
            foreach (var d in defs) _queue.Enqueue(d);
            TryShowNext();
        }

        static void TryShowNext()
        {
            if (_active != null) return;
            if (_queue.Count == 0) return;
            var def = _queue.Dequeue();
            var go = new GameObject("AchievementToast");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var sc = go.GetComponent<CanvasScaler>();
            sc.referenceResolution = new Vector2(1080, 1920);
            sc.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            var t = go.AddComponent<AchievementToast>();
            t._canvas = canvas;
            t._def = def;
            t.BuildContent();
            _active = t;
            AudioManager.PlayOneShot("victory", Vector3.zero);
        }

        void BuildContent()
        {
            var bg = UIFactory.MakePanel(transform, "Toast", new Color(0.05f, 0.07f, 0.18f, 0.95f));
            var btn = UIFactory.LoadSprite("UI/btn_gold");
            if (btn != null) { bg.sprite = btn; bg.type = Image.Type.Sliced; bg.color = _def.medalColor; }
            _panel = bg.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(1f, 1f);
            _panel.anchorMax = new Vector2(1f, 1f);
            _panel.pivot = new Vector2(1f, 1f);
            _panel.sizeDelta = new Vector2(640, 200);
            _panel.anchoredPosition = new Vector2(700f, -180f); // start off-screen right

            // Big medal icon on the left.
            _medalIcon = UIFactory.MakeIcon(_panel, "Medal", "Icons/" + _def.medalIconKey, new Vector2(160, 160));
            var mrt = _medalIcon.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0f, 0.5f);
            mrt.anchorMax = new Vector2(0f, 0.5f);
            mrt.pivot = new Vector2(0f, 0.5f);
            mrt.anchoredPosition = new Vector2(20f, 0f);
            _medalIcon.color = Color.white;

            _titleText = UIFactory.MakeText(_panel, "Title", "МЕДАЛЬ: " + _def.title, 44, TextAnchor.UpperLeft);
            _titleText.color = Color.white;
            var trt = _titleText.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(200f, 80f);
            trt.offsetMax = new Vector2(-30f, -10f);

            _descText = UIFactory.MakeText(_panel, "Desc", _def.description, 28, TextAnchor.UpperLeft);
            _descText.color = new Color(1f, 1f, 1f, 0.85f);
            var drt = _descText.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(1f, 1f);
            drt.offsetMin = new Vector2(200f, 10f);
            drt.offsetMax = new Vector2(-30f, -90f);
        }

        void Update()
        {
            _t += Time.deltaTime;
            float t = _t;
            // Slide-in 0.0-0.5s, hold 0.5-3.5s, slide-out 3.5-4.0s.
            float x;
            if (t < 0.5f) x = Mathf.Lerp(700f, -30f, EaseOutBack(t / 0.5f));
            else if (t < 3.5f) x = -30f;
            else if (t < 4.0f) x = Mathf.Lerp(-30f, 700f, (t - 3.5f) / 0.5f);
            else x = 700f;
            _panel.anchoredPosition = new Vector2(x, -180f);

            if (t >= 4.0f)
            {
                Destroy(gameObject);
                _active = null;
                TryShowNext();
            }
        }

        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
