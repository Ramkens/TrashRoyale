using UnityEngine;
using TrashRoyale.Util;

namespace TrashRoyale.Combat
{
    public class HpBar : MonoBehaviour
    {
        Damageable _owner;
        Transform _fill;
        Transform _bg;
        float _yOffset = 1.4f;
        float _width = 0.7f;
        float _height = 0.1f;
        Camera _cam;
        TextMesh _hpText;
        bool _showNumber;
        int _lastHp = -1;

        public static HpBar Create(Damageable owner, Color color, float yOffset = 1.4f, float width = 0.7f, bool showNumber = false)
        {
            var root = new GameObject("HpBar");
            root.transform.SetParent(owner.transform, false);
            root.transform.localPosition = new Vector3(0, yOffset, 0);

            float h = 0.18f;
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.transform.SetParent(root.transform, false);
            bg.transform.localScale = new Vector3(width, h, 1f);
            DestroyImmediate(bg.GetComponent<Collider>());
            var bgMat = SafeShader.NewUnlitColorMaterial(new Color(0, 0, 0, 0.9f));
            bg.GetComponent<MeshRenderer>().sharedMaterial = bgMat;

            float fillW = width * 0.94f;
            float fillH = h * 0.72f;
            var fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.transform.SetParent(root.transform, false);
            fill.transform.localScale = new Vector3(fillW, fillH, 1f);
            fill.transform.localPosition = new Vector3(0, 0, -0.001f);
            DestroyImmediate(fill.GetComponent<Collider>());
            var fillMat = SafeShader.NewUnlitColorMaterial(color);
            fill.GetComponent<MeshRenderer>().sharedMaterial = fillMat;

            TextMesh hpTm = null;
            if (showNumber)
            {
                var txtGo = new GameObject("HpText");
                txtGo.transform.SetParent(root.transform, false);
                txtGo.transform.localPosition = new Vector3(0, 0, -0.01f);
                hpTm = txtGo.AddComponent<TextMesh>();
                hpTm.text = ((int)owner.hp).ToString();
                hpTm.fontSize = 64;
                hpTm.characterSize = 0.012f;
                hpTm.color = Color.white;
                hpTm.alignment = TextAlignment.Center;
                hpTm.anchor = TextAnchor.MiddleCenter;
                hpTm.fontStyle = FontStyle.Bold;
                var mr = txtGo.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = hpTm.font.material;
                    mr.sortingOrder = 5;
                }
            }

            var bar = root.AddComponent<HpBar>();
            bar._owner = owner;
            bar._bg = bg.transform;
            bar._fill = fill.transform;
            bar._yOffset = yOffset;
            bar._width = fillW;
            bar._height = fillH;
            bar._cam = Camera.main;
            bar._hpText = hpTm;
            bar._showNumber = showNumber;
            return bar;
        }

        void LateUpdate()
        {
            if (_owner == null || _owner.isDead) return;
            float ratio = Mathf.Clamp01(_owner.hp / Mathf.Max(1f, _owner.maxHp));
            _fill.localScale = new Vector3(_width * ratio, _height, 1f);
            _fill.localPosition = new Vector3(-_width * 0.5f * (1f - ratio), 0f, -0.001f);
            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);
            }
            if (_showNumber && _hpText != null)
            {
                int v = Mathf.Max(0, Mathf.CeilToInt(_owner.hp));
                if (v != _lastHp) { _hpText.text = v.ToString(); _lastHp = v; }
            }
        }
    }
}
