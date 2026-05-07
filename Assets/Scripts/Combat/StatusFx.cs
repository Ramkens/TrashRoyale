using System.Collections.Generic;
using UnityEngine;

namespace TrashRoyale.Combat
{
    /// <summary>
    /// Visual feedback overlay for status effects (freeze / lightning /
    /// rage). Sits next to the entity's existing renderers and tints
    /// them via a per-frame property block — no shader changes required
    /// and no permanent material allocation. The component is added on
    /// demand by Damageable.UpdateStatusFx the first frame any status
    /// timer goes positive, then removed when nothing is active.
    /// </summary>
    public class StatusFx : MonoBehaviour
    {
        struct CapturedRenderer
        {
            public Renderer renderer;
            public Color baseColor;
        }

        readonly List<CapturedRenderer> _renderers = new();
        MaterialPropertyBlock _block;

        // Lightning bolt overlay: a thin yellow capsule shown for ~0.18s
        // when the lightning spell zaps this target. Pooled so retriggers
        // restart the timer instead of stacking objects.
        GameObject _boltGo;
        float _boltRemaining;

        // Rage aura: red ring sphere drawn under the unit. Persists for
        // the life of the rage timer.
        GameObject _rageRing;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            CaptureRenderers();
        }

        void CaptureRenderers()
        {
            _renderers.Clear();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (r is ParticleSystemRenderer) continue;
                if (r is TrailRenderer) continue;
                if (r is LineRenderer) continue;
                Color baseCol = Color.white;
                var mat = r.sharedMaterial;
                if (mat != null && mat.HasProperty("_Color"))
                {
                    baseCol = mat.color;
                }
                _renderers.Add(new CapturedRenderer { renderer = r, baseColor = baseCol });
            }
        }

        public void Refresh(Damageable d)
        {
            if (d == null) { Disable(); return; }

            // Pick the dominant tint by priority: lightning flash > freeze > rage.
            Color tint = Color.white;
            bool hasTint = false;
            if (_boltRemaining > 0f)
            {
                _boltRemaining -= Time.deltaTime;
                tint = new Color(1.6f, 1.6f, 0.8f); // bleached yellow flash
                hasTint = true;
            }
            else if (d.stunRemaining > 0f)
            {
                tint = new Color(0.55f, 0.78f, 1.05f); // icy blue
                hasTint = true;
            }
            else if (d.rageRemaining > 0f)
            {
                tint = new Color(1.25f, 0.65f, 0.55f); // warm red
                hasTint = true;
            }

            ApplyTint(hasTint, tint);
            UpdateBolt(d);
            UpdateRageRing(d);

            // Self-cleanup: nothing active and bolt timer drained -> remove.
            bool nothingActive =
                d.stunRemaining <= 0f &&
                d.rageRemaining <= 0f &&
                _boltRemaining <= 0f;
            if (nothingActive)
            {
                Disable();
            }
        }

        void ApplyTint(bool active, Color tint)
        {
            for (int i = _renderers.Count - 1; i >= 0; i--)
            {
                var cr = _renderers[i];
                if (cr.renderer == null)
                {
                    _renderers.RemoveAt(i);
                    continue;
                }
                cr.renderer.GetPropertyBlock(_block);
                if (active)
                {
                    var c = cr.baseColor * tint;
                    c.a = cr.baseColor.a;
                    _block.SetColor("_Color", c);
                    _block.SetColor("_BaseColor", c);
                }
                else
                {
                    _block.SetColor("_Color", cr.baseColor);
                    _block.SetColor("_BaseColor", cr.baseColor);
                }
                cr.renderer.SetPropertyBlock(_block);
            }
        }

        public void TriggerLightning()
        {
            _boltRemaining = 0.22f;
            if (_boltGo == null)
            {
                _boltGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _boltGo.name = "LightningBolt";
                var col = _boltGo.GetComponent<Collider>();
                if (col != null) Destroy(col);
                _boltGo.transform.SetParent(transform, false);
                _boltGo.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                _boltGo.transform.localScale = new Vector3(0.18f, 2.4f, 0.18f);
                var mr = _boltGo.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = new Material(Shader.Find("Standard"))
                    {
                        color = new Color(1f, 0.95f, 0.35f),
                    };
                }
            }
            _boltGo.SetActive(true);
        }

        void UpdateBolt(Damageable d)
        {
            if (_boltGo == null) return;
            if (_boltRemaining <= 0f)
            {
                _boltGo.SetActive(false);
            }
        }

        void UpdateRageRing(Damageable d)
        {
            if (d.rageRemaining > 0f)
            {
                if (_rageRing == null)
                {
                    _rageRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    _rageRing.name = "RageAura";
                    var col = _rageRing.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                    _rageRing.transform.SetParent(transform, false);
                    _rageRing.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                    _rageRing.transform.localScale = new Vector3(1.4f, 0.04f, 1.4f);
                    var mr = _rageRing.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        var mat = new Material(Shader.Find("Standard"));
                        mat.color = new Color(1f, 0.3f, 0.2f, 0.55f);
                        // Make it semi-transparent.
                        mat.SetFloat("_Mode", 3);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = 3000;
                        mr.sharedMaterial = mat;
                    }
                }
                _rageRing.SetActive(true);
            }
            else if (_rageRing != null)
            {
                _rageRing.SetActive(false);
            }
        }

        void Disable()
        {
            ApplyTint(false, Color.white);
            if (_boltGo != null) _boltGo.SetActive(false);
            if (_rageRing != null) _rageRing.SetActive(false);
            // Destroy ourselves so we don't poll forever.
            Destroy(this);
        }

        void OnDestroy()
        {
            // Clean up any spawned visuals.
            if (_boltGo != null) Destroy(_boltGo);
            if (_rageRing != null) Destroy(_rageRing);
            ApplyTint(false, Color.white);
        }
    }
}
