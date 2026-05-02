using UnityEngine;
using TrashRoyale.Match;
using TrashRoyale.Audio;

namespace TrashRoyale.Combat
{
    public class Tower : Damageable
    {
        public bool isKing = false;
        public float damage = 90f;
        public float attackInterval = 0.8f;
        public float range = 5.5f;
        public bool isActive = true;
        Damageable _target;
        float _attackCd;
        float _retargetCd;
        HpBar _hpBar;

        public void InitTower(Team t, bool king)
        {
            isKing = king;
            base.Init(t, king ? 4824f : 3052f);
            damage = king ? 109f : 90f;
            attackInterval = king ? 1.0f : 0.8f;
            range = king ? 7.0f : 5.5f;
            isBuilding = true;
            isActive = !king;
            CombatRegistry.Register(this);
            _hpBar = HpBar.Create(this, t == Team.Player ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.3f, 0.2f), 1.4f);
        }

        void OnDestroy() => CombatRegistry.Unregister(this);

        void Update()
        {
            if (isDead || !isActive) return;
            float dt = Time.deltaTime;
            if (_attackCd > 0f) _attackCd -= dt;
            if (_retargetCd > 0f) _retargetCd -= dt;
            if (_target == null || _target.isDead || _retargetCd <= 0f)
            {
                _target = CombatRegistry.FindClosestEnemy(transform.position, team, range, false, true);
                _retargetCd = 0.3f;
            }
            if (_target != null && _attackCd <= 0f)
            {
                Projectile.Spawn(transform.position + Vector3.up * 1.2f, _target, damage, team);
                _attackCd = attackInterval;
                AudioManager.PlayOneShot("tower_shoot", transform.position);
            }
        }

        public void Activate() => isActive = true;

        protected override void OnDeath()
        {
            AudioManager.PlayOneShot("tower_destroyed", transform.position);
            FxFactory.SpawnExplosion(transform.position + Vector3.up * 1f, 1.5f);
            MatchManager.I?.OnTowerDestroyed(this);
            if (_hpBar != null) Destroy(_hpBar.gameObject);
            Destroy(gameObject);
        }
    }
}
