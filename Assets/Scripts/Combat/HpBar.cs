using UnityEngine;

namespace TrashRoyale.Combat
{
    public class HpBar : MonoBehaviour
    {
        Damageable _owner;
        Transform _fill;
        float _yOffset = 1.4f;
        Camera _cam;

        public static HpBar Create(Damageable owner, Color color, float yOffset = 1.4f)
        {
            var root = new GameObject("HpBar");
            root.transform.SetParent(owner.transform, false);
            root.transform.localPosition = new Vector3(0, yOffset, 0);

            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.transform.SetParent(root.transform, false);
            bg.transform.localScale = new Vector3(0.7f, 0.1f, 1f);
            DestroyImmediate(bg.GetComponent<Collider>());
            var bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            bgMat.color = new Color(0, 0, 0, 0.6f);
            bg.GetComponent<MeshRenderer>().sharedMaterial = bgMat;

            var fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.transform.SetParent(root.transform, false);
            fill.transform.localScale = new Vector3(0.66f, 0.07f, 1f);
            fill.transform.localPosition = new Vector3(0, 0, -0.001f);
            DestroyImmediate(fill.GetComponent<Collider>());
            var fillMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            fillMat.color = color;
            fill.GetComponent<MeshRenderer>().sharedMaterial = fillMat;

            var bar = root.AddComponent<HpBar>();
            bar._owner = owner;
            bar._fill = fill.transform;
            bar._yOffset = yOffset;
            bar._cam = Camera.main;
            return bar;
        }

        void LateUpdate()
        {
            if (_owner == null || _owner.isDead) return;
            float ratio = Mathf.Clamp01(_owner.hp / Mathf.Max(1f, _owner.maxHp));
            _fill.localScale = new Vector3(0.66f * ratio, 0.07f, 1f);
            _fill.localPosition = new Vector3(-0.33f * (1f - ratio), 0f, -0.001f);
            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);
            }
        }
    }
}
