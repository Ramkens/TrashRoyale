using UnityEngine;
using TrashRoyale.Util;

namespace TrashRoyale.Combat
{
    /// <summary>
    /// Cheap reusable particle bursts for combat feedback. Every effect is one-shot,
    /// has a hard maxParticles cap, ParticleSystemStopAction.Destroy AND a backup
    /// Destroy(go, lifetime) call so nothing can ever leak/loop into a "permanent
    /// explosion" mess on screen.
    /// </summary>
    public static class FxFactory
    {
        public static void SpawnHit(Vector3 pos)
        {
            var go = new GameObject("HitFx");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.3f;
            main.startLifetime = 0.25f;
            main.startSpeed = 3.5f;
            main.startSize = 0.13f;
            main.startColor = new Color(1f, 0.95f, 0.4f);
            main.maxParticles = 12;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.material = SafeShader.NewSpriteMaterial();
            Object.Destroy(go, 1.5f);
        }

        public static void SpawnPoof(Vector3 pos)
        {
            var go = new GameObject("PoofFx");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.55f;
            main.startSpeed = 1.5f;
            main.startSize = 0.4f;
            main.startColor = new Color(0.85f, 0.82f, 0.7f, 0.85f);
            main.maxParticles = 14;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.material = SafeShader.NewSpriteMaterial();
            Object.Destroy(go, 2f);
        }

        public static void SpawnExplosion(Vector3 pos, float radius)
        {
            // Layered fireball: (1) bright orange burst (2) expanding ring (3) sparks
            // (4) lingering smoke. Each layer is one-shot with a hard destroy timer.
            var root = new GameObject("ExplosionFx");
            root.transform.position = pos;

            // 1. Core flash (yellow→orange)
            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(root.transform, false);
            var flash = flashGo.AddComponent<ParticleSystem>();
            var fmain = flash.main;
            fmain.duration = 0.4f;
            fmain.startLifetime = 0.4f;
            fmain.startSpeed = radius * 2.5f;
            fmain.startSize = radius * 0.55f;
            fmain.startColor = new Color(1f, 0.85f, 0.3f);
            fmain.maxParticles = 30;
            fmain.loop = false;
            fmain.stopAction = ParticleSystemStopAction.Destroy;
            var femit = flash.emission;
            femit.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
            var fshape = flash.shape;
            fshape.shapeType = ParticleSystemShapeType.Sphere;
            fshape.radius = radius * 0.15f;
            var fcolor = flash.colorOverLifetime;
            fcolor.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.95f, 0.55f), 0f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.05f), 0.5f),
                    new GradientColorKey(new Color(0.5f, 0.15f, 0.05f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fcolor.color = grad;
            flash.GetComponent<ParticleSystemRenderer>().material = SafeShader.NewSpriteMaterial();

            // 2. Sparks (tiny yellow dots flying outward)
            var sparksGo = new GameObject("Sparks");
            sparksGo.transform.SetParent(root.transform, false);
            var sparks = sparksGo.AddComponent<ParticleSystem>();
            var smain = sparks.main;
            smain.duration = 0.6f;
            smain.startLifetime = 0.7f;
            smain.startSpeed = radius * 4f;
            smain.startSize = 0.12f;
            smain.startColor = new Color(1f, 0.95f, 0.45f);
            smain.maxParticles = 24;
            smain.loop = false;
            smain.stopAction = ParticleSystemStopAction.Destroy;
            smain.gravityModifier = 0.6f;
            sparks.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var sshape = sparks.shape;
            sshape.shapeType = ParticleSystemShapeType.Sphere;
            sshape.radius = radius * 0.1f;
            sparks.GetComponent<ParticleSystemRenderer>().material = SafeShader.NewSpriteMaterial();

            // 3. Lingering smoke (dark gray, slow)
            var smokeGo = new GameObject("Smoke");
            smokeGo.transform.SetParent(root.transform, false);
            var smoke = smokeGo.AddComponent<ParticleSystem>();
            var mmain = smoke.main;
            mmain.duration = 1.0f;
            mmain.startLifetime = 1.2f;
            mmain.startSpeed = radius * 0.4f;
            mmain.startSize = radius * 0.7f;
            mmain.startColor = new Color(0.25f, 0.25f, 0.28f, 0.55f);
            mmain.maxParticles = 16;
            mmain.loop = false;
            mmain.stopAction = ParticleSystemStopAction.Destroy;
            mmain.gravityModifier = -0.15f;
            smoke.emission.SetBursts(new[] { new ParticleSystem.Burst(0.05f, 12) });
            var mshape = smoke.shape;
            mshape.shapeType = ParticleSystemShapeType.Sphere;
            mshape.radius = radius * 0.25f;
            var mcol = smoke.colorOverLifetime;
            mcol.enabled = true;
            var sgrad = new Gradient();
            sgrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.4f, 0.35f, 0.3f), 0f), new GradientColorKey(new Color(0.2f, 0.2f, 0.22f), 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            mcol.color = sgrad;
            smoke.GetComponent<ParticleSystemRenderer>().material = SafeShader.NewSpriteMaterial();

            // 4. Shockwave ring on the ground (expanding cylinder)
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Shockwave";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0, 0.05f, 0);
            ring.transform.localScale = new Vector3(0.2f, 0.02f, 0.2f);
            Object.Destroy(ring.GetComponent<Collider>());
            var rmat = SafeShader.NewOpaqueMaterial(new Color(1f, 0.55f, 0.15f));
            ring.GetComponent<MeshRenderer>().sharedMaterial = rmat;
            var rs = ring.AddComponent<RingExpand>();
            rs.targetRadius = radius * 1.4f;
            rs.duration = 0.45f;

            // Belt-and-suspenders: kill the whole hierarchy after 2s no matter what.
            Object.Destroy(root, 2.5f);
        }
    }

    /// <summary>Animates a unit cylinder to expand outward and fade as a shockwave ring.</summary>
    public class RingExpand : MonoBehaviour
    {
        public float targetRadius = 2f;
        public float duration = 0.45f;
        float _t;
        Material _mat;

        void Awake()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null) _mat = mr.material;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / Mathf.Max(0.0001f, duration));
            float r = Mathf.Lerp(0.2f, targetRadius, k);
            transform.localScale = new Vector3(r, 0.02f, r);
            if (_mat != null)
            {
                var c = _mat.color;
                c.a = 1f - k;
                _mat.color = c;
            }
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
