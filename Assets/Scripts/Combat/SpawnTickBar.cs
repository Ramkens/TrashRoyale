using UnityEngine;
using TrashRoyale.Util;

namespace TrashRoyale.Combat
{
    /// <summary>
    /// Sibling of <see cref="HpBar"/> that shows the spawn-tick countdown
    /// for huts (Imposter Hut) above the HP bar — players asked for a
    /// "next imposter in N" indicator. Driven by <see cref="Building"/>
    /// via <see cref="SetProgress"/>: 0..1 where 1 = full bar (cooldown
    /// just started), 0 = empty (about to spawn).
    /// </summary>
    public class SpawnTickBar : MonoBehaviour
    {
        Transform _owner;
        Transform _fill;
        float _yOffset = 1.65f;
        float _width = 0.6f;
        float _height = 0.06f;
        Camera _cam;
        float _ratio = 1f;

        public static SpawnTickBar Create(Transform owner, Color color, float yOffset = 1.65f, float width = 0.6f)
        {
            var root = new GameObject("SpawnTickBar");
            root.transform.SetParent(owner, false);
            root.transform.localPosition = new Vector3(0, yOffset, 0);

            float h = 0.1f;
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.transform.SetParent(root.transform, false);
            bg.transform.localScale = new Vector3(width, h, 1f);
            DestroyImmediate(bg.GetComponent<Collider>());
            var bgMat = SafeShader.NewUnlitColorMaterial(new Color(0f, 0f, 0f, 0.85f));
            bg.GetComponent<MeshRenderer>().sharedMaterial = bgMat;

            float fillW = width * 0.94f;
            float fillH = h * 0.65f;
            var fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.transform.SetParent(root.transform, false);
            fill.transform.localScale = new Vector3(fillW, fillH, 1f);
            fill.transform.localPosition = new Vector3(0, 0, -0.001f);
            DestroyImmediate(fill.GetComponent<Collider>());
            var fillMat = SafeShader.NewUnlitColorMaterial(color);
            fill.GetComponent<MeshRenderer>().sharedMaterial = fillMat;

            var bar = root.AddComponent<SpawnTickBar>();
            bar._owner = owner;
            bar._fill = fill.transform;
            bar._yOffset = yOffset;
            bar._width = fillW;
            bar._height = fillH;
            bar._cam = Camera.main;
            return bar;
        }

        public void SetProgress(float ratio)
        {
            _ratio = Mathf.Clamp01(ratio);
        }

        void LateUpdate()
        {
            if (_owner == null) return;
            // ratio interpreted as "remaining fraction of cooldown" so the
            // bar empties as the spawn approaches; flip if you want the
            // opposite. Caller picks the convention via SetProgress.
            float r = _ratio;
            _fill.localScale = new Vector3(_width * r, _height, 1f);
            _fill.localPosition = new Vector3(-_width * 0.5f * (1f - r), 0f, -0.001f);
            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);
            }
        }
    }
}
