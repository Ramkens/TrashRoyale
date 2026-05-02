using UnityEngine;
using TrashRoyale.Match;
using TrashRoyale.Util;

namespace TrashRoyale.Combat
{
    public class Projectile : MonoBehaviour
    {
        Damageable _target;
        Vector3 _lastTargetPos;
        float _damage;
        float _speed = 9f;
        Team _team;

        Transform _bodyT;

        public static Projectile Spawn(Vector3 from, Damageable target, float damage, Team team)
        {
            var go = new GameObject("Projectile");
            go.transform.position = from;
            // Stretch the bullet along its travel direction so it reads as an arrow/bolt
            // rather than a generic ball. Scaling Z gives a capsule-like silhouette.
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(go.transform, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.16f, 0.32f, 0.16f);
            var col = body.GetComponent<Collider>(); if (col != null) Destroy(col);
            var color = team == Team.Player ? new Color(0.65f, 0.95f, 1f) : new Color(1f, 0.45f, 0.25f);
            body.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewUnlitColorMaterial(color);

            // Trail behind the bullet so it draws a glowing streak in the air.
            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(go.transform, false);
            var tr = trailGo.AddComponent<TrailRenderer>();
            tr.time = 0.18f;
            tr.startWidth = 0.18f;
            tr.endWidth = 0.02f;
            tr.material = SafeShader.NewSpriteMaterial();
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(new Color(color.r * 0.4f, color.g * 0.4f, color.b * 0.4f), 1f)
                },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = grad;

            var p = go.AddComponent<Projectile>();
            p._target = target;
            p._damage = damage;
            p._team = team;
            p._lastTargetPos = target != null ? target.AimPos : from + Vector3.forward;
            p._bodyT = body.transform;
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 dest = _target != null && !_target.isDead ? _target.AimPos : _lastTargetPos;
            _lastTargetPos = dest;
            var diff = dest - transform.position;
            float step = _speed * dt;
            if (diff.magnitude <= step)
            {
                if (_target != null && !_target.isDead) _target.TakeDamage(_damage);
                FxFactory.SpawnSparkBurst(dest,
                    _team == Team.Player ? new Color(0.8f, 0.95f, 1f) : new Color(1f, 0.6f, 0.3f),
                    8, 0.3f);
                Destroy(gameObject);
                return;
            }
            var dir = diff.normalized;
            transform.position += dir * step;
            // Aim the capsule along its flight direction so it reads as an arrow.
            if (_bodyT != null)
            {
                var look = Quaternion.LookRotation(dir);
                transform.rotation = look;
                _bodyT.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
    }
}
