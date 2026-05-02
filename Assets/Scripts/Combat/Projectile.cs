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

        public static Projectile Spawn(Vector3 from, Damageable target, float damage, Team team)
        {
            var go = new GameObject("Projectile");
            go.transform.position = from;
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(go.transform, false);
            sphere.transform.localScale = Vector3.one * 0.18f;
            var col = sphere.GetComponent<Collider>(); if (col != null) Destroy(col);
            var mat = SafeShader.NewUnlitColorMaterial(team == Team.Player ? new Color(0.5f, 0.9f, 1f) : new Color(1f, 0.5f, 0.3f));
            sphere.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var p = go.AddComponent<Projectile>();
            p._target = target;
            p._damage = damage;
            p._team = team;
            p._lastTargetPos = target != null ? target.AimPos : from + Vector3.forward;
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
                FxFactory.SpawnHit(dest);
                Destroy(gameObject);
                return;
            }
            transform.position += diff.normalized * step;
        }
    }
}
