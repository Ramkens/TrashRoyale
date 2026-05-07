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
            // 2.5x reduction from the legacy 5.5 / 7.0 spread per user
            // request (5.5 / 2.5 ≈ 2.2, 7.0 / 2.5 ≈ 2.8). Defensive
            // buildings (cannon/tesla/totem) sit at 2.8-3.0 so towers
            // now feel proportional, not oppressive.
            range = king ? 2.8f : 2.2f;
            isBuilding = true;
            isActive = !king;
            CombatRegistry.Register(this);
            float hpY = king ? 4.4f : 3.4f;
            float hpW = king ? 2.4f : 1.7f;
            _hpBar = HpBar.Create(this,
                t == Team.Player ? new Color(0.18f, 0.55f, 1f) : new Color(1f, 0.25f, 0.2f),
                hpY, hpW, /*showNumber*/ true);
        }

        void OnDestroy() => CombatRegistry.Unregister(this);

        void Update()
        {
            if (isDead || !isActive) return;
            float dt = Time.deltaTime;
            // Without ticking these, a tower frozen by Freeze / Lightning
            // would stay stunRemaining > 0 forever, never re-attack.
            if (stunRemaining > 0f) stunRemaining -= dt;
            if (rageRemaining > 0f) rageRemaining -= dt;
            UpdateStatusFx();
            if (_attackCd > 0f) _attackCd -= dt;
            if (_retargetCd > 0f) _retargetCd -= dt;
            // Freeze / Lightning lock the tower out of attacking but
            // still let cooldowns tick so it doesn't fire instantly the
            // moment the stun ends.
            if (stunRemaining > 0f) return;
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

        /// <summary>
        /// CR rule: the king tower wakes up the moment it takes any
        /// damage, not only when an adjacent crown tower falls. Without
        /// this override, players can chip the enemy king with spells
        /// (fireball / lightning / poison) while both crowns are still
        /// alive, and the king never shoots back.
        /// </summary>
        public override void TakeDamage(float dmg, Damageable source = null)
        {
            base.TakeDamage(dmg, source);
            if (isKing && !isActive && !isDead)
            {
                Activate();
            }
        }

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
