using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Audio;

namespace TrashRoyale.Combat
{
    public class Unit : Damageable
    {
        public CardData card;
        Damageable _target;
        float _attackCd;
        float _retargetCd;
        Vector3 _moveDir;
        HpBar _hpBar;
        float _deployTimer;

        public void Init(CardData data, Team t)
        {
            card = data;
            base.Init(t, data.hp);
            isBuilding = false;
            isAir = data.isAir;
            _attackCd = 0f;
            _retargetCd = 0f;
            _deployTimer = data.deployTime;
            CombatRegistry.Register(this);
            _hpBar = HpBar.Create(this, t == Team.Player ? new Color(0.2f, 0.5f, 1f) : new Color(1f, 0.3f, 0.2f));
            AudioManager.PlayOneShot(card.voiceLine, transform.position);
        }

        void OnDestroy() => CombatRegistry.Unregister(this);

        void Update()
        {
            if (isDead) return;
            float dt = Time.deltaTime;

            if (_deployTimer > 0f)
            {
                _deployTimer -= dt;
                return;
            }

            if (_attackCd > 0f) _attackCd -= dt;
            if (_retargetCd > 0f) _retargetCd -= dt;

            if (_target == null || _target.isDead || _retargetCd <= 0f)
            {
                AcquireTarget();
                _retargetCd = 0.4f;
            }

            if (_target == null)
            {
                MoveTowardEnemySide(dt);
                return;
            }

            Vector3 toTarget = _target.transform.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            float effectiveRange = card.range + (_target.isBuilding ? 0.5f : 0.3f);
            if (dist <= effectiveRange)
            {
                FaceTarget(toTarget);
                if (_attackCd <= 0f)
                {
                    DoAttack();
                    _attackCd = card.attackInterval;
                }
            }
            else
            {
                Move(toTarget.normalized, dt);
            }
        }

        void AcquireTarget()
        {
            float searchRange = card.targetMode == "BuildingsOnly" ? 9999f : 5.5f;
            bool buildingsOnly = card.Targets == TargetMode.BuildingsOnly;
            _target = CombatRegistry.FindClosestEnemy(transform.position, team, searchRange, buildingsOnly, card.targetsAir);
            if (_target == null && !buildingsOnly)
            {
                _target = CombatRegistry.FindClosestEnemy(transform.position, team, 9999f, true, card.targetsAir);
            }
        }

        void MoveTowardEnemySide(float dt)
        {
            float dir = team == Team.Player ? 1f : -1f;
            Move(new Vector3(0f, 0f, dir), dt);
        }

        void Move(Vector3 dir, float dt)
        {
            transform.position += dir * card.moveSpeed * dt;
            if (dir.sqrMagnitude > 0.001f)
            {
                var look = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 720f * dt);
            }
        }

        void FaceTarget(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            var look = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 720f * Time.deltaTime);
        }

        void DoAttack()
        {
            if (_target == null || _target.isDead) return;
            AudioManager.PlayOneShot("attack_swing", transform.position);
            if (card.range > 1.6f)
            {
                Projectile.Spawn(transform.position + Vector3.up * 0.6f, _target, card.damage, team);
            }
            else
            {
                _target.TakeDamage(card.damage, this);
                FxFactory.SpawnHit(_target.AimPos);
            }
        }

        protected override void OnDeath()
        {
            AudioManager.PlayOneShot("unit_death", transform.position);
            FxFactory.SpawnPoof(transform.position + Vector3.up * 0.5f);
            if (_hpBar != null) Destroy(_hpBar.gameObject);
            Destroy(gameObject);
        }
    }
}
