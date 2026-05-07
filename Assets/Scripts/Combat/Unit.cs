using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Audio;
using TrashRoyale.Util;

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
        float _deployTotal;
        Transform _deployRing;
        Renderer[] _renderers;
        // "charge" mechanic: counts seconds the unit has been moving
        // toward an enemy without combat. Reaches card.chargeBuildSeconds
        // → next attack deals card.chargeMultiplier× damage. Resets on
        // every successful attack.
        float _chargeBuiltSeconds;
        bool _chargeReady;
        // "inferno_ramp" mechanic: tracks how long we've been hitting
        // the same target. Damage scales linearly from infernoRampStartDmg
        // to infernoRampMaxDmg over infernoRampSeconds, then caps. Resets
        // when the target changes or dies.
        Damageable _infernoLockTarget;
        float _infernoLockSeconds;

        public void Init(CardData data, Team t)
        {
            card = data;
            base.Init(t, data.hp);
            isBuilding = false;
            isAir = data.isAir;
            _attackCd = 0f;
            _retargetCd = 0f;
            _deployTimer = data.deployTime;
            _deployTotal = data.deployTime;
            CombatRegistry.Register(this);
            _hpBar = HpBar.Create(this, t == Team.Player ? new Color(0.2f, 0.5f, 1f) : new Color(1f, 0.3f, 0.2f), 2.0f);
            _renderers = GetComponentsInChildren<Renderer>();
            if (_deployTimer > 0f) BuildDeployRing();
            ApplyDeployVisual(0f);
            AudioManager.PlayOneShot(card.voiceLine, transform.position, card.sfxVolume);

            // Mechanic state: copy card values into the per-instance
            // Damageable counters so Update / TakeDamage have cheap
            // local checks instead of dereferencing card every frame.
            if (card.HasMechanic("phase_immune") && card.phaseImmuneSeconds > 0f)
            {
                phaseImmuneRemaining = card.phaseImmuneSeconds;
            }
            if (card.HasMechanic("reflect") && card.reflectFraction > 0f)
            {
                reflectFraction = card.reflectFraction;
            }
        }

        void BuildDeployRing()
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "DeployRing";
            DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(transform, false);
            ring.transform.localScale = new Vector3(1.4f, 0.02f, 1.4f);
            ring.transform.localPosition = new Vector3(0, 0.05f, 0);
            var mat = SafeShader.NewOpaqueMaterial(team == Team.Player ? new Color(0.4f, 0.7f, 1f, 0.8f) : new Color(1f, 0.4f, 0.4f, 0.8f));
            ring.GetComponent<MeshRenderer>().sharedMaterial = mat;
            _deployRing = ring.transform;
        }

        void ApplyDeployVisual(float t)
        {
            // Pulse alpha + ring fill while deploying.
            if (_deployRing != null)
            {
                float s = Mathf.Lerp(1.6f, 0.4f, t);
                _deployRing.localScale = new Vector3(s, 0.02f, s);
            }
        }

        void OnDestroy() => CombatRegistry.Unregister(this);

        void Update()
        {
            if (isDead) return;
            float dt = Time.deltaTime;

            // Tick down phase / stun / rage timers regardless of deploy
            // state so spells thrown on a deploying unit still resolve.
            if (phaseImmuneRemaining > 0f) phaseImmuneRemaining -= dt;
            if (stunRemaining > 0f) stunRemaining -= dt;
            if (rageRemaining > 0f) rageRemaining -= dt;

            // Per-frame visual overlay (freeze / lightning / rage tint).
            UpdateStatusFx();

            if (_deployTimer > 0f)
            {
                _deployTimer -= dt;
                float t = _deployTotal > 0f ? 1f - (_deployTimer / _deployTotal) : 1f;
                ApplyDeployVisual(t);
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

            // Stun = full action lock. We still tick attack/retarget
            // cooldowns so the unit doesn't get a free attack the
            // moment the stun ends.
            if (stunRemaining > 0f)
            {
                if (_attackCd > 0f) _attackCd -= dt;
                if (_retargetCd > 0f) _retargetCd -= dt;
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
                AccumulateCharge(dt);
                return;
            }

            Vector3 toTarget = _target.transform.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            float effectiveRange = card.range + (_target.isBuilding ? 0.5f : 0.3f);
            if (dist <= effectiveRange)
            {
                FaceTarget(toTarget);
                // Inferno-ramp keeps charging only while we keep
                // hitting the same target; switching targets resets.
                if (card.HasMechanic("inferno_ramp"))
                {
                    if (_infernoLockTarget != _target)
                    {
                        _infernoLockTarget = _target;
                        _infernoLockSeconds = 0f;
                    }
                    else
                    {
                        _infernoLockSeconds += dt;
                    }
                }
                if (_attackCd <= 0f)
                {
                    DoAttack();
                    // Rage cuts the cooldown so allied units swing
                    // faster while inside an active rage zone.
                    float interval = card.attackInterval;
                    if (rageRemaining > 0f) interval /= RageMultiplier;
                    _attackCd = interval;
                    // Charge consumed on hit; rebuild from scratch.
                    _chargeBuiltSeconds = 0f;
                    _chargeReady = false;
                }
            }
            else
            {
                Move(toTarget.normalized, dt);
                AccumulateCharge(dt);
                // Out of range = inferno target effectively reset on next hit.
                _infernoLockTarget = null;
                _infernoLockSeconds = 0f;
            }
        }

        /// <summary>
        /// Builds up the charge timer for cards with the "charge"
        /// mechanic. Once enough seconds of uninterrupted travel pass,
        /// the next attack will use <c>card.chargeMultiplier</c>. Cards
        /// without the tag short-circuit immediately so this is free for
        /// the rest of the roster.
        /// </summary>
        void AccumulateCharge(float dt)
        {
            if (!card.HasMechanic("charge")) return;
            if (card.chargeBuildSeconds <= 0f || card.chargeMultiplier <= 1f) return;
            _chargeBuiltSeconds += dt;
            if (_chargeBuiltSeconds >= card.chargeBuildSeconds) _chargeReady = true;
        }

        void AcquireTarget()
        {
            // Vision/aggro radius. Buildings-only chargers (pig, hog) see
            // forever so they stay laser-focused on towers. Everyone else
            // gets a tight bubble derived from their own attack range —
            // ranged units see a smidge beyond their reach, melee gets a
            // floor of 3.5u so they don't stand idle next to enemies they
            // technically can't yet attack. The previous fixed 5.5u meant
            // a melee knight at midfield could chase a ranged Pocoyo on
            // the OTHER lane; the new derived radius keeps lanes cleaner.
            bool buildingsOnly = card.Targets == TargetMode.BuildingsOnly;
            float searchRange;
            if (buildingsOnly)
            {
                searchRange = 9999f;
            }
            else
            {
                searchRange = Mathf.Max(card.range + 0.5f, 3.5f);
            }
            _target = CombatRegistry.FindClosestEnemy(transform.position, team, searchRange, buildingsOnly, card.targetsAir);
            if (_target == null && !buildingsOnly)
            {
                // Fallback: nothing in sight -> walk forward until something
                // shows up. We still call FindClosestEnemy with a huge
                // radius so movement code knows where the river is, but
                // attack code won't engage until the real range check
                // passes.
                _target = CombatRegistry.FindClosestEnemy(transform.position, team, 9999f, true, card.targetsAir);
            }
        }

        void MoveTowardEnemySide(float dt)
        {
            float dir = team == Team.Player ? 1f : -1f;
            Move(new Vector3(0f, 0f, dir), dt);
        }

        // ---- Water-jump state ---------------------------------------
        // While jumping the unit follows a parabolic arc across the
        // river. We freeze normal movement during the arc and resume the
        // moment the unit lands on the far bank.
        bool _jumping;
        Vector3 _jumpFrom;
        Vector3 _jumpTo;
        float _jumpProgress;
        const float JumpDuration = 0.55f;
        const float JumpHeight = 1.4f;

        void Move(Vector3 dir, float dt)
        {
            // Active jump arc takes priority over everything: we already
            // committed to crossing the river and shouldn't re-evaluate
            // pathing until we land.
            if (_jumping)
            {
                _jumpProgress += dt / JumpDuration;
                float t = Mathf.Clamp01(_jumpProgress);
                Vector3 flat = Vector3.Lerp(_jumpFrom, _jumpTo, t);
                // y = 4*h*t*(1-t) → smooth parabola peaking at JumpHeight.
                flat.y = 4f * JumpHeight * t * (1f - t);
                transform.position = flat;
                if (_jumpProgress >= 1f)
                {
                    _jumping = false;
                    var p = _jumpTo; p.y = 0f;
                    transform.position = p;
                }
                return;
            }

            float speed = card.moveSpeed;
            if (rageRemaining > 0f) speed *= RageMultiplier;

            // Water blocking: ground units that can't jump must walk to
            // the nearest bridge before crossing the river. Air units
            // (isAir) and "canJumpWater" units bypass this entirely.
            // Buildings never call Move so we don't worry about them.
            Vector3 desired = dir;
            if (!card.isAir && !card.canJumpWater)
            {
                desired = SteerAroundWater(desired);
            }
            else if (card.canJumpWater && !card.isAir)
            {
                if (TryStartWaterJump(desired)) return;
            }

            transform.position += desired * speed * dt;
            if (desired.sqrMagnitude > 0.001f)
            {
                var look = Quaternion.LookRotation(new Vector3(desired.x, 0f, desired.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 720f * dt);
            }
            ApplyFunnyWalk(dt, speed);
        }

        // Visual-only "funny walk" applied to IShowSpeed: bobs the model
        // up + a sideways wobble while moving so the static gltf at least
        // looks animated. Other units fall through harmlessly. Skips the
        // jump arc since y is driven by the parabola during a jump.
        float _walkPhase;
        Transform _modelChild;
        void ApplyFunnyWalk(float dt, float speed)
        {
            if (card == null) return;
            bool isFunny = card.id == "ishowspeed";
            if (!isFunny) return;
            if (_modelChild == null && transform.childCount > 0) _modelChild = transform.GetChild(0);
            if (_modelChild == null) return;
            _walkPhase += dt * (4f + speed * 2f);
            float bob = Mathf.Sin(_walkPhase) * 0.15f;
            float roll = Mathf.Cos(_walkPhase) * 8f;
            var lp = _modelChild.localPosition;
            lp.y = bob;
            _modelChild.localPosition = lp;
            _modelChild.localRotation = Quaternion.Euler(0f, 0f, roll);
        }

        /// <summary>
        /// True for the strip <c>|z| &lt; RiverHalfThickness</c> EXCLUDING
        /// the two bridges (centered at x = ±2.8, half-width 0.8). Used to
        /// decide whether a ground unit's next step would land in water.
        /// </summary>
        static bool IsWaterAt(float x, float z)
        {
            if (Mathf.Abs(z) >= ArenaController.RiverHalfThickness + 0.05f) return false;
            // Bridges are 1.6u wide centered at ±2.8 so passable strip is
            // ±2.0..±3.6 on each side.
            float ax = Mathf.Abs(x);
            bool onBridge = ax >= 2.0f && ax <= 3.6f;
            return !onBridge;
        }

        /// <summary>
        /// Reroutes a movement direction so a non-jumper ground unit walks
        /// to the nearest bridge instead of straight into water. We only
        /// override the direction when the next step would actually land
        /// in the river — units already past the river continue normally.
        /// </summary>
        Vector3 SteerAroundWater(Vector3 dir)
        {
            float lookahead = 0.6f;
            Vector3 next = transform.position + dir.normalized * lookahead;
            if (!IsWaterAt(next.x, next.z)) return dir;

            // Aim at the bridge whose x is closest to ours.
            float myX = transform.position.x;
            float bridgeX = myX >= 0f ? 2.8f : -2.8f;
            float dx = bridgeX - myX;
            float dz = transform.position.z >= 0f ? -1f : 1f; // toward the river first
            // If we're already at the river line, push across.
            if (Mathf.Abs(transform.position.z) < ArenaController.RiverHalfThickness * 0.6f)
            {
                dz = team == Team.Player ? 1f : -1f;
            }
            var v = new Vector3(dx, 0f, dz);
            if (v.sqrMagnitude < 0.001f) return dir;
            return v.normalized;
        }

        /// <summary>
        /// Initiates the parabolic water-jump arc when a jumper unit is
        /// about to step into water. Picks a landing point one tile
        /// past the river on the far bank, locked to the unit's current
        /// x so the jump looks deliberate. Returns true when a jump was
        /// started (caller must early-out so we don't double-move this
        /// frame).
        /// </summary>
        bool TryStartWaterJump(Vector3 dir)
        {
            float lookahead = 0.6f;
            Vector3 next = transform.position + dir.normalized * lookahead;
            if (!IsWaterAt(next.x, next.z)) return false;
            float farZ = team == Team.Player
                ? ArenaController.RiverHalfThickness + 0.6f
                : -(ArenaController.RiverHalfThickness + 0.6f);
            _jumpFrom = transform.position; _jumpFrom.y = 0f;
            _jumpTo = new Vector3(transform.position.x, 0f, farZ);
            _jumpProgress = 0f;
            _jumping = true;
            // Trigger one-shot voice flair if the card has a per-jump
            // line; otherwise stay silent.
            return true;
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
            float dmg = ResolveOutgoingDamage();
            if (card.range > 1.6f)
            {
                if (card.id == "nyancat")
                {
                    // Nyan cat shoots an instant rainbow beam at the target.
                    FxFactory.SpawnRainbowBeam(transform.position + Vector3.up * 0.6f, _target.AimPos);
                    _target.TakeDamage(dmg, this);
                    OnDamageDealt(dmg);
                }
                else if (card.splashRadius > 0f)
                {
                    // Ranged splash unit (Bomber, Doge Mage): lob a fireball-style
                    // arc that detonates at the target's aim point and damages
                    // every enemy in <c>splashRadius</c>.
                    var origin = transform.position + Vector3.up * 0.6f;
                    var impact = _target.AimPos;
                    float capturedDmg = dmg;
                    FxFactory.LaunchFireballMissile(origin, impact, card.splashRadius, () =>
                    {
                        ApplySplashWithDamage(impact, capturedDmg);
                        OnDamageDealt(capturedDmg);
                    });
                }
                else if (card.HasMechanic("multishot_3"))
                {
                    // Three projectiles in a horizontal spread. Center
                    // shot still locks onto the target (it's a normal
                    // homing Projectile.Spawn); the wing shots fly to
                    // virtual aim points offset by the configured
                    // spread degrees so they can clip extra units.
                    float spread = card.multishotSpreadDeg > 0f ? card.multishotSpreadDeg : 12f;
                    Projectile.Spawn(transform.position + Vector3.up * 0.6f, _target, dmg, team);
                    SpawnSpreadProjectile(dmg, +spread);
                    SpawnSpreadProjectile(dmg, -spread);
                    OnDamageDealt(dmg);
                }
                else
                {
                    Projectile.Spawn(transform.position + Vector3.up * 0.6f, _target, dmg, team);
                    OnDamageDealt(dmg);
                }
            }
            else
            {
                if (card.splashRadius > 0f)
                {
                    // Melee splash (e.g. shrek-mode swing): hit everyone in range.
                    var impact = _target.AimPos;
                    ApplySplashWithDamage(impact, dmg);
                }
                else
                {
                    _target.TakeDamage(dmg, this);
                }
                OnDamageDealt(dmg);
                PlayThemedMelee(_target.AimPos);
            }
        }

        /// <summary>
        /// Computes outgoing damage for the next swing including all
        /// mechanic modifiers: charge bonus, berserker scaling, and
        /// inferno ramp. Multiplicative so they stack predictably.
        /// </summary>
        float ResolveOutgoingDamage()
        {
            float dmg = card.damage;
            // Inferno ramp overrides base damage entirely — it has its
            // own start/max scale read from the card.
            if (card.HasMechanic("inferno_ramp") && card.infernoRampMaxDmg > card.infernoRampStartDmg)
            {
                float dur = Mathf.Max(0.01f, card.infernoRampSeconds);
                float t = Mathf.Clamp01(_infernoLockSeconds / dur);
                dmg = Mathf.Lerp(card.infernoRampStartDmg, card.infernoRampMaxDmg, t);
            }
            if (_chargeReady && card.chargeMultiplier > 1f)
            {
                dmg *= card.chargeMultiplier;
            }
            if (card.HasMechanic("berserker") && maxHp > 0f)
            {
                // Linear scale from 1.0 at full HP to 1.6 at 0 HP.
                float ratio = 1f - Mathf.Clamp01(hp / maxHp);
                dmg *= 1f + 0.6f * ratio;
            }
            return dmg;
        }

        /// <summary>
        /// Post-damage hook for mechanics that fire after the hit
        /// resolves: lifesteal heal, charge consume (already done),
        /// future on-hit buffs, etc.
        /// </summary>
        void OnDamageDealt(float dmg)
        {
            if (card.HasMechanic("lifesteal") && card.lifestealFraction > 0f)
            {
                Heal(dmg * card.lifestealFraction);
            }
        }

        /// <summary>
        /// Fires a projectile at a virtual aim point rotated
        /// <paramref name="degrees"/> around the up-axis from the line
        /// to the current target. Uses a temporary aim transform so
        /// the standard <see cref="Projectile.Spawn"/> homing path
        /// works without modification.
        /// </summary>
        void SpawnSpreadProjectile(float dmg, float degrees)
        {
            if (_target == null) return;
            Vector3 origin = transform.position + Vector3.up * 0.6f;
            Vector3 forward = (_target.AimPos - origin);
            forward.y = 0f;
            forward = Quaternion.Euler(0f, degrees, 0f) * forward.normalized;
            Vector3 aim = origin + forward * Mathf.Max(2f, (_target.transform.position - transform.position).magnitude);
            // Synthetic "phantom" target so Projectile.Spawn has
            // something to home onto. We grab the closest enemy near
            // the spread aim instead of inventing a Damageable.
            var phantom = CombatRegistry.FindClosestEnemy(aim, team, 4f, false, card.targetsAir) ?? _target;
            Projectile.Spawn(origin, phantom, dmg, team);
        }

        /// <summary>
        /// Applies <c>card.damage</c> to every enemy <see cref="Damageable"/>
        /// inside <c>card.splashRadius</c> of <paramref name="center"/>. Only
        /// damages units (not the casting team), respects air-target rules,
        /// and uses 2D distance so vertical air offsets don't dodge the AoE.
        /// </summary>
        void ApplySplash(Vector3 center) => ApplySplashWithDamage(center, card.damage);

        /// <summary>
        /// Same as <see cref="ApplySplash"/> but takes the resolved
        /// damage value so charge / berserker / inferno bonuses propagate
        /// to every enemy caught in the AoE.
        /// </summary>
        void ApplySplashWithDamage(Vector3 center, float dmg)
        {
            float r2 = card.splashRadius * card.splashRadius;
            var all = CombatRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d == null || d.isDead) continue;
                if (d.team == team) continue;
                if (d.isAir && !card.targetsAir) continue;
                var dx = d.transform.position - center;
                dx.y = 0;
                if (dx.sqrMagnitude > r2) continue;
                d.TakeDamage(dmg, this);
            }
            FxFactory.SpawnExplosion(center, card.splashRadius);
        }

        void PlayThemedMelee(Vector3 hitPos)
        {
            switch (card.id)
            {
                case "knight":
                case "gigachad":
                    FxFactory.SpawnSwordSlash(transform.position + Vector3.up * 0.6f, hitPos);
                    if (card.id == "gigachad") FxFactory.SpawnShockwave(hitPos, new Color(1f, 0.95f, 0.4f), 1.0f);
                    break;
                case "shrek":
                    FxFactory.SpawnEarthquake(hitPos);
                    break;
                case "pig":
                    FxFactory.SpawnStarPow(hitPos, new Color(1f, 0.55f, 0.7f));
                    break;
                case "cheems":
                    FxFactory.SpawnStarPow(hitPos, new Color(1f, 0.85f, 0.4f));
                    break;
                case "amongus":
                    FxFactory.SpawnStarPow(hitPos, new Color(0.95f, 0.3f, 0.3f));
                    break;
                case "skibidi":
                case "pocoyo":
                    FxFactory.SpawnMagicRing(hitPos, card.id == "skibidi"
                        ? new Color(0.6f, 0.85f, 1f) : new Color(0.4f, 0.7f, 1f));
                    break;
                default:
                    FxFactory.SpawnHit(hitPos);
                    break;
            }
        }

        protected override void OnDeath()
        {
            AudioManager.PlayOneShot("unit_death", transform.position);
            FxFactory.SpawnPoof(transform.position + Vector3.up * 0.5f);
            // "spawn_on_death" mechanic: emit N copies of another card
            // around our corpse. Used by e.g. Goblin-Barrel-style cards
            // and exploding totems. We resolve the spawn through the
            // CardDatabase so designers can chain cards by id without
            // touching code.
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
            Destroy(gameObject);
        }
    }
}
