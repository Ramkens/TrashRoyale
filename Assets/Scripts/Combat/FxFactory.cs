using UnityEngine;
using TrashRoyale.Util;

namespace TrashRoyale.Combat
{
    /// <summary>
    /// Visual effects for combat. Every effect is one-shot, hard-capped in particle
    /// count, and self-destructs after a set lifetime — there's no way for a stray
    /// PS to loop forever and turn the screen into a flicker fest.
    ///
    /// Themed entrypoints (SpawnSwordSlash, SpawnRainbowBeam, SpawnEarthquake, ...)
    /// are picked by Unit/Tower based on card.id so each character has its own
    /// signature attack feedback rather than every melee just being a yellow puff.
    /// </summary>
    public static class FxFactory
    {
        // ---------- Generic ----------

        public static void SpawnHit(Vector3 pos)
        {
            SpawnSparkBurst(pos, new Color(1f, 0.95f, 0.4f), 6, 0.25f);
        }

        public static void SpawnPoof(Vector3 pos)
        {
            var go = new GameObject("PoofFx");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.55f;
            main.startSpeed = 1.4f;
            main.startSize = 0.4f;
            main.startColor = new Color(0.85f, 0.82f, 0.7f, 0.85f);
            main.maxParticles = 14;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            ps.GetComponent<ParticleSystemRenderer>().material = SafeShader.NewSpriteMaterial();
            Object.Destroy(go, 2f);
        }

        // ---------- Themed melee ----------

        /// <summary>White slash arc + 5 sparks. Used by knight & gigachad swings.</summary>
        public static void SpawnSwordSlash(Vector3 attacker, Vector3 target)
        {
            // Slash quad: thin white rectangle stretched between attacker and target,
            // pitched 45° so it reads as a diagonal slash; fades out fast.
            var go = new GameObject("Slash");
            var dir = target - attacker;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist < 0.01f) { Object.Destroy(go); return; }
            go.transform.position = (attacker + target) * 0.5f + Vector3.up * 0.9f;
            go.transform.rotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0f, 0f, 35f);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(go.transform, false);
            quad.transform.localScale = new Vector3(dist * 1.1f, 0.18f, 1f);
            Object.Destroy(quad.GetComponent<Collider>());
            var mat = SafeShader.NewTransparentMaterial(new Color(1f, 1f, 1f, 0.9f));
            quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var fade = quad.AddComponent<FadeAndDestroy>();
            fade.duration = 0.18f;

            SpawnSparkBurst(target + Vector3.up * 0.4f, new Color(1f, 1f, 0.95f), 5, 0.3f);
            Object.Destroy(go, 0.4f);
        }

        /// <summary>Cartoon star-pow burst: 6 four-pointed stars. Used by pig/cheems/amongus.</summary>
        public static void SpawnStarPow(Vector3 pos, Color tint)
        {
            var go = new GameObject("StarPow");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f;
            main.startLifetime = 0.5f;
            main.startSpeed = 2.5f;
            main.startSize = 0.4f;
            main.startColor = tint;
            main.maxParticles = 12;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.gravityModifier = 0.4f;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(360f);
            ps.GetComponent<ParticleSystemRenderer>().material = SafeShader.NewSpriteMaterial();
            Object.Destroy(go, 1.5f);
        }

        /// <summary>Ground-based shockwave ring expanding outward. Used by gigachad/shrek.</summary>
        public static void SpawnShockwave(Vector3 groundPos, Color color, float maxRadius)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Shockwave";
            ring.transform.position = new Vector3(groundPos.x, 0.06f, groundPos.z);
            ring.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
            Object.Destroy(ring.GetComponent<Collider>());
            var mat = SafeShader.NewTransparentMaterial(new Color(color.r, color.g, color.b, 0.85f));
            ring.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var rs = ring.AddComponent<RingExpand>();
            rs.targetRadius = maxRadius;
            rs.duration = 0.5f;
        }

        /// <summary>Brown earth chunks flying up + dust ring. Used by shrek slam.</summary>
        public static void SpawnEarthquake(Vector3 pos)
        {
            var go = new GameObject("EarthquakeFx");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.6f;
            main.startLifetime = 0.7f;
            main.startSpeed = 3.5f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.45f, 0.3f, 0.2f);
            main.maxParticles = 18;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.gravityModifier = 1.2f;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 35f;
            sh.radius = 0.15f;
            ps.GetComponent<ParticleSystemRenderer>().material = SafeShader.NewSpriteMaterial();
            SpawnShockwave(pos, new Color(0.5f, 0.35f, 0.2f), 1.6f);
            Object.Destroy(go, 1.5f);
        }

        /// <summary>Bright rainbow beam from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static void SpawnRainbowBeam(Vector3 from, Vector3 to)
        {
            var go = new GameObject("RainbowBeam");
            var dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.01f) { Object.Destroy(go); return; }
            go.transform.position = (from + to) * 0.5f + Vector3.up * 0.4f;
            go.transform.rotation = Quaternion.LookRotation(dir.normalized);

            // Six thin quads stacked vertically with rainbow colors.
            Color[] colors = {
                new Color(1f, 0.2f, 0.2f), new Color(1f, 0.55f, 0.1f),
                new Color(1f, 0.95f, 0.2f), new Color(0.3f, 0.85f, 0.3f),
                new Color(0.25f, 0.55f, 1f), new Color(0.7f, 0.3f, 0.95f)
            };
            for (int i = 0; i < colors.Length; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.transform.SetParent(go.transform, false);
                q.transform.localPosition = new Vector3(0, (i - colors.Length * 0.5f) * 0.07f, 0);
                q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                q.transform.localScale = new Vector3(0.16f, dist * 1.05f, 1f);
                Object.Destroy(q.GetComponent<Collider>());
                var mat = SafeShader.NewTransparentMaterial(new Color(colors[i].r, colors[i].g, colors[i].b, 0.9f));
                q.GetComponent<MeshRenderer>().sharedMaterial = mat;
                var fade = q.AddComponent<FadeAndDestroy>();
                fade.duration = 0.3f;
            }
            Object.Destroy(go, 0.5f);
        }

        /// <summary>Magic spiral: ring + sparkles in a tinted color. Used by pocoyo/skibidi.</summary>
        public static void SpawnMagicRing(Vector3 pos, Color color)
        {
            SpawnShockwave(pos, color, 1.2f);
            var go = new GameObject("MagicSparkles");
            go.transform.position = pos + Vector3.up * 0.6f;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.6f;
            main.startSpeed = 2.0f;
            main.startSize = 0.18f;
            main.startColor = color;
            main.maxParticles = 14;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Donut;
            shape.radius = 0.5f;
            ps.GetComponent<ParticleSystemRenderer>().material = SafeShader.NewSpriteMaterial();
            Object.Destroy(go, 1.5f);
        }

        // ---------- Spell explosion (fireball impact) ----------

        public static void SpawnExplosion(Vector3 pos, float radius)
        {
            var root = new GameObject("ExplosionFx");
            root.transform.position = pos;

            // 1. Bright flash core.
            var flash = NewParticleChild(root, "Flash",
                duration: 0.4f, lifetime: 0.4f,
                speed: radius * 2.5f, size: radius * 0.55f,
                color: new Color(1f, 0.85f, 0.3f),
                maxParticles: 30, burstCount: 22);
            var flashColor = flash.colorOverLifetime;
            flashColor.enabled = true;
            flashColor.color = new ParticleSystem.MinMaxGradient(BuildFireGradient());
            var fshape = flash.shape;
            fshape.shapeType = ParticleSystemShapeType.Sphere;
            fshape.radius = radius * 0.15f;

            // 2. Sparks fly outward + gravity.
            var sparks = NewParticleChild(root, "Sparks",
                duration: 0.6f, lifetime: 0.7f,
                speed: radius * 4f, size: 0.12f,
                color: new Color(1f, 0.95f, 0.45f),
                maxParticles: 24, burstCount: 18);
            var smain = sparks.main; smain.gravityModifier = 0.6f;

            // 3. Smoke that lingers.
            var smoke = NewParticleChild(root, "Smoke",
                duration: 1.0f, lifetime: 1.2f,
                speed: radius * 0.4f, size: radius * 0.7f,
                color: new Color(0.25f, 0.25f, 0.28f, 0.55f),
                maxParticles: 16, burstCount: 12, burstTime: 0.05f);
            var mmain = smoke.main; mmain.gravityModifier = -0.15f;
            var mcol = smoke.colorOverLifetime; mcol.enabled = true;
            var sgrad = new Gradient();
            sgrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.4f, 0.35f, 0.3f), 0f),
                        new GradientColorKey(new Color(0.2f, 0.2f, 0.22f), 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            mcol.color = sgrad;

            // 4. Expanding shockwave ring on the ground.
            SpawnShockwave(pos, new Color(1f, 0.55f, 0.15f), radius * 1.4f);

            Object.Destroy(root, 2.5f);
        }

        /// <summary>Flying flame missile + trail; ignites a real explosion at <paramref name="target"/>.</summary>
        public static void LaunchFireballMissile(Vector3 from, Vector3 target, float radius, System.Action onImpact)
        {
            var go = new GameObject("FireballMissile");
            // Visible orange sphere body.
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.SetParent(go.transform, false);
            ball.transform.localScale = Vector3.one * 0.55f;
            Object.Destroy(ball.GetComponent<Collider>());
            ball.GetComponent<MeshRenderer>().sharedMaterial =
                SafeShader.NewUnlitColorMaterial(new Color(1f, 0.55f, 0.1f));

            // Trail of sparks from the missile.
            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(go.transform, false);
            var tr = trailGo.AddComponent<TrailRenderer>();
            tr.time = 0.45f;
            tr.startWidth = 0.5f;
            tr.endWidth = 0.05f;
            tr.material = SafeShader.NewSpriteMaterial();
            var tg = new Gradient();
            tg.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0f),
                        new GradientColorKey(new Color(1f, 0.3f, 0.05f), 0.5f),
                        new GradientColorKey(new Color(0.3f, 0.05f, 0f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = tg;

            var arc = go.AddComponent<MissileArc>();
            arc.start = from;
            arc.end = target;
            arc.duration = 0.65f;
            arc.peakHeight = 2.2f;
            arc.onImpact = () =>
            {
                SpawnExplosion(target, radius);
                onImpact?.Invoke();
            };
        }

        // ---------- Helpers ----------

        static ParticleSystem NewParticleChild(GameObject root, string name,
            float duration, float lifetime, float speed, float size, Color color,
            int maxParticles, int burstCount, float burstTime = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = duration;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = maxParticles;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(burstTime, (short)burstCount) });
            ps.GetComponent<ParticleSystemRenderer>().material = SafeShader.NewSpriteMaterial();
            return ps;
        }

        public static void SpawnSparkBurst(Vector3 pos, Color color, int count, float lifetime)
        {
            var go = new GameObject("Sparks");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.3f;
            main.startLifetime = lifetime;
            main.startSpeed = 3.5f;
            main.startSize = 0.13f;
            main.startColor = color;
            main.maxParticles = Mathf.Max(count, 4);
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.gravityModifier = 0.3f;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            ps.GetComponent<ParticleSystemRenderer>().material = SafeShader.NewSpriteMaterial();
            Object.Destroy(go, 1.5f);
        }

        static Gradient BuildFireGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.95f, 0.55f), 0f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.05f), 0.5f),
                    new GradientColorKey(new Color(0.5f, 0.15f, 0.05f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.9f, 0.5f),
                        new GradientAlphaKey(0f, 1f) });
            return g;
        }
    }

    // ---------- Helper components ----------

    /// <summary>Animates a flat cylinder outward as a shockwave ring and fades alpha.</summary>
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
            float r = Mathf.Lerp(0.3f, targetRadius, k);
            transform.localScale = new Vector3(r, 0.02f, r);
            if (_mat != null)
            {
                var c = _mat.color; c.a = 1f - k; _mat.color = c;
            }
            if (k >= 1f) Destroy(gameObject);
        }
    }

    /// <summary>Fades transform's MeshRenderer material alpha to zero, then destroys.</summary>
    public class FadeAndDestroy : MonoBehaviour
    {
        public float duration = 0.3f;
        float _t;
        Material[] _mats;
        Color[] _start;

        void Awake()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                _mats = new[] { mr.material };
                _start = new[] { _mats[0].color };
            }
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / Mathf.Max(0.0001f, duration));
            if (_mats != null)
            {
                for (int i = 0; i < _mats.Length; i++)
                {
                    var c = _start[i]; c.a = _start[i].a * (1f - k); _mats[i].color = c;
                }
            }
            if (k >= 1f) Destroy(gameObject);
        }
    }

    /// <summary>Flies an attached object along a parabolic arc to <c>end</c> then destroys + invokes <c>onImpact</c>.</summary>
    public class MissileArc : MonoBehaviour
    {
        public Vector3 start;
        public Vector3 end;
        public float duration = 0.6f;
        public float peakHeight = 2f;
        public System.Action onImpact;
        float _t;

        void Start() { transform.position = start; }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / Mathf.Max(0.0001f, duration));
            var pos = Vector3.Lerp(start, end, k);
            pos.y += Mathf.Sin(k * Mathf.PI) * peakHeight;
            transform.position = pos;
            if (k >= 1f)
            {
                onImpact?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
