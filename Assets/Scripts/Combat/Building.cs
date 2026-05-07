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
        // Spawner-on-tick state for huts (Imposter Hut, Furnace).
        // When card.spawnOnTickInterval > 0 and card.spawnOnTickCardId is
        // non-empty, the building emits one unit at that cadence. The
        // first emission fires after the FIRST interval (not on deploy)
        // so the player gets value from the building rather than instantly.
        float _spawnTickCd;
        // Mini bar shown above the HP bar for spawner huts so players can
        // see when the next imposter / unit will pop out. Only created
        // for cards that actually tick on a spawn interval.
        SpawnTickBar _spawnBar;

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
            _spawnTickCd = data.spawnOnTickInterval;
            CombatRegistry.Register(this);
            _hpBar = HpBar.Create(this,
                t == Team.Player ? new Color(0.18f, 0.6f, 1f) : new Color(1f, 0.3f, 0.25f),
                2.6f, 1.6f, /*showNumber*/ true);
            // Show a yellow countdown bar above the HP bar for huts so
            // players can time their pushes around imposter spawns.
            if (data.spawnOnTickInterval > 0f && !string.IsNullOrEmpty(data.spawnOnTickCardId))
            {
                _spawnBar = SpawnTickBar.Create(transform, new Color(1f, 0.85f, 0.2f), 2.95f, 1.4f);
            }
            if (_deployTimer > 0f) BuildDeployRing();
            AudioManager.PlayOneShot(card.voiceLine, transform.position, card.sfxVolume);
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

            // Without ticking these, a building frozen by Freeze /
            // Lightning would stay stunRemaining > 0 forever and never
            // attack or spawn again.
            if (stunRemaining > 0f) stunRemaining -= dt;
            if (rageRemaining > 0f) rageRemaining -= dt;
            // Visual feedback (freeze tint, lightning flash, rage aura).
            UpdateStatusFx();

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

            // While frozen, the building can't attack OR spawn its
            // hut tick. Cooldowns still tick (above) so it doesn't
            // catch up on missed attacks the instant the stun ends.
            if (stunRemaining > 0f) return;

            // Spawner huts (e.g. Imposter Hut): emit a fresh unit every
            // `spawnOnTickInterval` seconds, indefinitely while we're
            // alive. Independent of attack logic — a hut can both spawn
            // imposters AND optionally have its own attack (for combo
            // huts). Most huts have card.range = 0 so the target search
            // below returns null and they simply spawn without firing.
            if (card.spawnOnTickInterval > 0f && !string.IsNullOrEmpty(card.spawnOnTickCardId))
            {
                _spawnTickCd -= dt;
                if (_spawnTickCd <= 0f)
                {
                    SpawnTickedUnit();
                    _spawnTickCd = card.spawnOnTickInterval;
                }
                if (_spawnBar != null)
                {
                    _spawnBar.SetProgress(_spawnTickCd / card.spawnOnTickInterval);
                }
            }

            // Pure spawner buildings (Хата Sus etc.) have card.range == 0
            // and card.damage == 0 — they should NEVER fire a projectile.
            // The previous implementation only checked _target != null, which
            // combined with attackInterval == 0 caused the hut to spit a
            // projectile every frame (looked exactly like an inferno tower).
            // We now gate the entire attack pipeline on having a sane
            // weapon definition.
            bool hasWeapon = card.range > 0f && card.attackInterval > 0f && card.damage > 0f;
            if (hasWeapon)
            {
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
        }

        /// <summary>
        /// Spawns ONE instance of the spawn-on-tick child unit just in
        /// front of the hut, biased toward enemy territory so units
        /// immediately walk down the lane instead of milling around the
        /// hut. Spawning a single unit (instead of respecting the source
        /// card's <c>spawnCount</c>) is intentional: e.g. the Хата Sus
        /// reuses the existing 4-imposter <c>amongus</c> card but should
        /// only emit 1 imposter per 5s — otherwise the hut would dump
        /// 4×8=32 imposters over its lifetime, which is way past CR
        /// balance for a 3-elixir building.
        /// </summary>
        void SpawnTickedUnit()
        {
            var data = TrashRoyale.Core.CardDatabase.Get(card.spawnOnTickCardId);
            if (data == null) return;
            // Push the spawn one tile toward the enemy so the new unit
            // doesn't intersect the building collider on the first frame.
            float dir = team == Team.Player ? 1f : -1f;
            Vector3 pos = transform.position + new Vector3(0f, 0f, dir * 0.6f);
            // SpawnUnitsExact lets us force count=1 even though the
            // source card (e.g. amongus) normally spawns 4.
            UnitFactory.SpawnUnitsExact(data, team, pos, 1);
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
            // "spawn_on_death" mechanic for buildings (e.g. Imposter Hut
            // dropping a fresh batch of imposters when it falls). Mirrors
            // the Unit.cs implementation so designers can chain death
            // payloads off any card type.
            if (card.HasMechanic("spawn_on_death") &&
                !string.IsNullOrEmpty(card.spawnOnDeathCardId) &&
                card.spawnOnDeathCount > 0)
            {
                var child = TrashRoyale.Core.CardDatabase.Get(card.spawnOnDeathCardId);
                if (child != null)
                {
                    UnitFactory.SpawnUnitsExact(child, team, transform.position, card.spawnOnDeathCount);
                }
            }
            if (_hpBar != null) Destroy(_hpBar.gameObject);
            if (_spawnBar != null) Destroy(_spawnBar.gameObject);
            Destroy(gameObject);
        }
    }
}
