using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Persistence;

namespace TrashRoyale.UI
{
    public class NicknamePopup : MonoBehaviour
    {
        public static NicknamePopup Open(Transform canvas, PlayerProfile profile, System.Action onSaved)
        {
            var go = new GameObject("NicknamePopup");
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.85f);
            dim.raycastTarget = true;

            var pop = go.AddComponent<NicknamePopup>();
            pop._profile = profile;
            pop._onSaved = onSaved;
            pop.Build();
            return pop;
        }

        PlayerProfile _profile;
        System.Action _onSaved;
        InputField _input;

        void Build()
        {
            var panel = UIFactory.MakePanel(transform, "Panel", new Color(0.07f, 0.13f, 0.28f, 1f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.08f, 0.36f);
            prt.anchorMax = new Vector2(0.92f, 0.64f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var bg = UIFactory.LoadSprite("UI/menu_bg");
            if (bg != null) { panel.sprite = bg; panel.color = new Color(1f, 1f, 1f, 0.6f); }

            var title = UIFactory.MakeText(panel.transform, "Title", "СМЕНА НИКА", 60, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.7f);
            trt.anchorMax = new Vector2(1, 0.95f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = Color.white;

            _input = UIFactory.MakeInputField(panel.transform, "Input", _profile.playerName, 50);
            var irt = _input.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.08f, 0.42f);
            irt.anchorMax = new Vector2(0.92f, 0.62f);
            irt.offsetMin = irt.offsetMax = Vector2.zero;

            var btnSave = UIFactory.MakeButton(panel.transform, "Save", "СОХРАНИТЬ", () =>
            {
                var v = (_input.text ?? "").Trim();
                if (string.IsNullOrEmpty(v)) v = _profile.playerName;
                _profile.playerName = v;
                _profile.Save();
                _onSaved?.Invoke();
                Destroy(gameObject);
            });
            var srt = btnSave.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.08f, 0.1f);
            srt.anchorMax = new Vector2(0.5f, 0.32f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            var btnCancel = UIFactory.MakeButton(panel.transform, "Cancel", "ОТМЕНА", () =>
            {
                Destroy(gameObject);
            });
            var crt = btnCancel.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.52f, 0.1f);
            crt.anchorMax = new Vector2(0.92f, 0.32f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
        }
    }
}
