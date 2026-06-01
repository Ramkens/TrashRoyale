using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Audio;
using TrashRoyale.Util;

namespace TrashRoyale.Combat
{
    /// <summary>
    /// Stationary defensive structure spawned from a Building card (cannon,
    /// tesla tower, etc.). Behaves like a hybrid of <see cref="Tower"/> (no
    /// movement, counts as a building) and <see cref="Unit"/> (limited HP,
    /// configurable card stats, lifetime timer that ticks down to self-
    /// destruct after <c>card.lifetime</c> seconds).
    ///
    /// Buildings are valid targets for <c>BuildingsOnly</c> attackers (Pig,
    /// Bomber, etc.) — that's the whole point of this card type: drop a
    /// cannon on the lane, the building-only chargers run at the cannon
    /// instead of your tower, and the cannon shreds them.
    /// </summary>
    public class Building : Damageable
    {
        public CardData card;
        Damageable _target;
        float _attackCd;
        float _retargetCd;
        float _lifetime;
        HpBar _hpBar;
        float _deployTimer;
        Transform _deployRing;

        public void Init(CardData data, Team t)
        {
            card = data;
            base.Init(t, data.hp);
            isBuilding = true;
            isAir = false;
            _attackCd = 0f;
            _retargetCd = 0f;
            _lifetime = data.lifetime > 0f ? data.lifetime : 30f;
            _deployTimer = data.deployTime;
            CombatRegistry.Register(this);
            _hpBar = HpBar.Create(this,
                t == Team.Player ? new Color(0.18f, 0.6f, 1f) : new Color(1f, 0.3f, 0.25f),
                2.6f, 1.6f, /*showNumber*/ true);
            if (_deployTimer > 0f) BuildDeployRing();
            AudioManager.PlayOneShot(card.voiceLine, transform.position);
        }

        void BuildDeployRing()
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "DeployRing";
            DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(transform, false);
            ring.transform.localScale = new Vector3(1.6f, 0.02f, 1.6f);
            ring.transform.localPosition = new Vector3(0, 0.05f, 0);
            ring.GetComponent<MeshRenderer>().sharedMaterial =
                SafeShader.NewOpaqueMaterial(team == Team.Player
                    ? new Color(0.4f, 0.7f, 1f, 0.8f)
                    : new Color(1f, 0.4f, 0.4f, 0.8f));
            _deployRing = ring.transform;
        }

        void OnDestroy() => CombatRegistry.Unregister(this);

        void Update()
        {
            if (isDead) return;
            float dt = Time.deltaTime;

            // Lifetime ticks regardless of deploy state — same as CR.
            _lifetime -= dt;
            if (_lifetime <= 0f)
            {
                // Self-destruct, no XP awarded to enemy.
                TakeDamage(hp + 1f, this);
                return;
            }

            if (_deployTimer > 0f)
            {
                _deployTimer -= dt;
                if (_deployTimer <= 0f && _deployRing != null)
                {
                    Destroy(_deployRing.gameObject);
                    _deployRing = null;
                }
                else
                {
                    return;
                }
            }

            if (_attackCd > 0f) _attackCd -= dt;
            if (_retargetCd > 0f) _retargetCd -= dt;

            if (_target == null || _target.isDead || _retargetCd <= 0f)
            {
                _target = CombatRegistry.FindClosestEnemy(transform.position, team,
                    card.range, /*buildingsOnly*/ false, card.targetsAir);
                _retargetCd = 0.3f;
            }

            if (_target != null && _attackCd <= 0f)
            {
                DoAttack();
                _attackCd = card.attackInterval;
            }
        }

        void DoAttack()
        {
            if (_target == null || _target.isDead) return;
            AudioManager.PlayOneShot("tower_shoot", transform.position);
            // Buildings are always ranged: spawn a projectile aimed at target.
            Projectile.Spawn(transform.position + Vector3.up * 1.0f, _target, card.damage, team);
        }

        protected override void OnDeath()
        {
            AudioManager.PlayOneShot("tower_destroyed", transform.position);
            FxFactory.SpawnExplosion(transform.position + Vector3.up * 0.5f, 1.0f);
            if (_hpBar != null) Destroy(_hpBar.gameObject);
            Destroy(gameObject);
        }
    }
}
