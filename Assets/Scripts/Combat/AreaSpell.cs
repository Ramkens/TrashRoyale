using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Audio;

namespace TrashRoyale.Combat
{
    public static class AreaSpell
    {
        /// <summary>
        /// Casts a splash spell. Each spell id has its own cinematic
        /// (fireball arc, poison cloud, lightning arcs, ...). Damage and
        /// status effects are applied at impact time so visuals and
        /// gameplay stay in sync.
        /// </summary>
        public static void Cast(CardData spell, Vector3 center, Team caster)
        {
            AudioManager.PlayOneShot(spell.voiceLine, center, spell.sfxVolume);
            switch (spell.id)
            {
                case "fireball":
                {
                    var origin = ResolveSpellOrigin(caster, center);
                    FxFactory.LaunchFireballMissile(origin, center, spell.splashRadius, () =>
                    {
                        ApplyDamage(spell, center, caster, spell.damage);
                    });
                    break;
                }
                case "log_spell":
                {
                    // Rolling log: spawns a moving log object that travels
                    // forward from caster's side, knocks back grounded
                    // units it touches and applies a brief stun. The log
                    // mesh is a simple capsule with a wood tint; we skip a
                    // heavy gltf so the spell stays cheap on mobile.
                    var dir = caster == Team.Player ? Vector3.forward : Vector3.back;
                    var go = new GameObject("LogProjectile");
                    go.transform.position = center - dir * 1.0f;
                    var lp = go.AddComponent<LogProjectile>();
                    lp.Init(spell, caster, center, dir);
                    break;
                }
                case "freeze_spell":
                {
                    // Slight damage burst + stun for spell.freezeStunSeconds.
                    FxFactory.SpawnShockwave(center, new Color(0.6f, 0.85f, 1f), spell.splashRadius);
                    ApplyDamageAndStun(spell, center, caster, spell.damage, spell.freezeStunSeconds);
                    break;
                }
                case "lightning_spell":
                {
                    // Picks the 3 highest-HP enemies inside splashRadius
                    // and zaps them for spell.damage each + a short stun.
                    var picks = PickHighestHpEnemies(center, spell.splashRadius, caster, 3, spell.targetsAir);
                    foreach (var d in picks)
                    {
                        FxFactory.SpawnRainbowBeam(center + Vector3.up * 4f, d.AimPos);
                        d.TakeDamage(spell.damage);
                        if (spell.freezeStunSeconds > 0f && d.stunRemaining < spell.freezeStunSeconds)
                            d.stunRemaining = spell.freezeStunSeconds;
                    }
                    break;
                }
                case "rage_spell":
                {
                    // Buff zone for ALLIES: faster atk/move while inside.
                    // Implemented as a TickZone in "rage" mode: it doesn't
                    // damage, it tags allies' rageRemaining timer. Unit
                    // code consumes that tag in its movement/attack logic.
                    var go = new GameObject("RageZone");
                    go.transform.position = center;
                    var z = go.AddComponent<TickZone>();
                    z.Init(spell, caster, center, spell.splashRadius, 6f, 0.25f, 0f, 0.25f, isRage: true);
                    FxFactory.SpawnShockwave(center, new Color(1f, 0.4f, 0.4f), spell.splashRadius);
                    break;
                }
                default:
                {
                    // Generic AOE damage (covers spell-shaped cards we
                    // haven't given a unique cinematic to yet).
                    FxFactory.SpawnExplosion(center, spell.splashRadius);
                    ApplyDamage(spell, center, caster, spell.damage);
                    break;
                }
            }
        }

        /// <summary>
        /// Returns the spawn point for a projected spell missile: top of the
        /// caster's king tower if available, otherwise high above the impact
        /// point so the missile still has a visible arc.
        /// </summary>
        static Vector3 ResolveSpellOrigin(Team caster, Vector3 impact)
        {
            var match = MatchManager.I;
            Tower king = null;
            if (match != null)
            {
                king = caster == Team.Player ? match.PlayerKing : match.EnemyKing;
            }
            if (king != null && !king.isDead)
            {
                return king.transform.position + new Vector3(0f, 3.0f, 0f);
            }
            return impact + new Vector3(0f, 6f, 0f);
        }

        static void ApplyDamage(CardData spell, Vector3 center, Team caster, float dmg)
        {
            var all = CombatRegistry.All;
            float r2 = spell.splashRadius * spell.splashRadius;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                var d = all[i];
                if (d == null || d.isDead) continue;
                if (d.team == caster) continue;
                if (d.isAir && !spell.targetsAir) continue;
                var dx = d.transform.position - center;
                dx.y = 0;
                if (dx.sqrMagnitude <= r2)
                {
                    d.TakeDamage(dmg);
                }
            }
        }

        static void ApplyDamageAndStun(CardData spell, Vector3 center, Team caster, float dmg, float stun)
        {
            var all = CombatRegistry.All;
            float r2 = spell.splashRadius * spell.splashRadius;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                var d = all[i];
                if (d == null || d.isDead) continue;
                if (d.team == caster) continue;
                if (d.isAir && !spell.targetsAir) continue;
                var dx = d.transform.position - center;
                dx.y = 0;
                if (dx.sqrMagnitude <= r2)
                {
                    d.TakeDamage(dmg);
                    if (stun > 0f && d.stunRemaining < stun) d.stunRemaining = stun;
                }
            }
        }

        static List<Damageable> PickHighestHpEnemies(Vector3 center, float radius, Team caster, int top, bool includeAir)
        {
            var all = CombatRegistry.All;
            float r2 = radius * radius;
            var result = new List<Damageable>(top);
            // Cheap: we walk the registry once, keep a simple top-N
            // by linear insertion. N is 3 in practice so this is fine.
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d == null || d.isDead) continue;
                if (d.team == caster) continue;
                if (d.isAir && !includeAir) continue;
                var dx = d.transform.position - center;
                dx.y = 0;
                if (dx.sqrMagnitude > r2) continue;
                int inserted = 0;
                for (int k = 0; k < result.Count; k++)
                {
                    if (d.hp > result[k].hp)
                    {
                        result.Insert(k, d);
                        inserted = 1;
                        break;
                    }
                }
                if (inserted == 0) result.Add(d);
                if (result.Count > top) result.RemoveAt(result.Count - 1);
            }
            return result;
        }
    }

    /// <summary>
    /// Lightweight ground zone that ticks every <c>tickPeriod</c> seconds
    /// for <c>lifetime</c> seconds. In damage mode it deals
    /// <c>perTickDamage</c> to enemies; in rage mode it tags allies'
    /// <c>rageRemaining</c> timer (consumed by Unit). One component fits
    /// both because the geometry / lifetime / cleanup are identical.
    /// </summary>
    public class TickZone : MonoBehaviour
    {
        Team _caster;
        Vector3 _center;
        float _radius2;
        float _life;
        float _tickPeriod;
        float _perTickDmg;
        float _rageDuration;
        bool _isRage;
        float _tickAcc;
        bool _air;

        public void Init(CardData spell, Team caster, Vector3 center, float radius, float lifetime, float tickPeriod, float perTickDmg, float rageDuration, bool isRage)
        {
            _caster = caster;
            _center = center;
            _radius2 = radius * radius;
            _life = lifetime;
            _tickPeriod = Mathf.Max(0.05f, tickPeriod);
            _perTickDmg = perTickDmg;
            _rageDuration = rageDuration;
            _isRage = isRage;
            _air = spell != null && spell.targetsAir;
            _tickAcc = _tickPeriod;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _life -= dt;
            _tickAcc -= dt;
            if (_tickAcc <= 0f)
            {
                _tickAcc += _tickPeriod;
                Tick();
            }
            if (_life <= 0f) Destroy(gameObject);
        }

        void Tick()
        {
            var all = CombatRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d == null || d.isDead) continue;
                bool friend = d.team == _caster;
                if (_isRage && !friend) continue;
                if (!_isRage && friend) continue;
                if (d.isAir && !_air) continue;
                var dx = d.transform.position - _center;
                dx.y = 0;
                if (dx.sqrMagnitude > _radius2) continue;
                if (_isRage)
                {
                    if (d.rageRemaining < _rageDuration) d.rageRemaining = _rageDuration;
                }
                else
                {
                    d.TakeDamage(_perTickDmg);
                }
            }
        }
    }

    /// <summary>
    /// Travelling log used by the Log spell. Lives ~1.6s, slides forward
    /// at constant speed, applies splash damage + brief stun + small
    /// knockback impulse to any grounded enemy it overlaps. Air units
    /// are ignored. Self-destructs at end of lifetime.
    /// </summary>
    public class LogProjectile : MonoBehaviour
    {
        Team _caster;
        Vector3 _dir;
        float _life = 1.6f;
        float _speed = 6f;
        float _radius;
        float _damage;
        float _stun = 0.4f;
        readonly HashSet<Damageable> _hit = new();

        public void Init(CardData spell, Team caster, Vector3 center, Vector3 dir)
        {
            _caster = caster;
            _dir = dir.normalized;
            _radius = Mathf.Max(0.6f, spell.splashRadius * 0.5f);
            _damage = spell.damage;
            transform.position = center - _dir * 1.5f;
            transform.rotation = Quaternion.LookRotation(_dir);
            // Visual: thick wooden capsule
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(transform, false);
            visual.transform.localRotation = Quaternion.Euler(0, 0, 90);
            visual.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);
            var col = visual.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var mr = visual.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = new Material(Shader.Find("Standard"))
                {
                    color = new Color(0.45f, 0.28f, 0.12f),
                };
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _life -= dt;
            transform.position += _dir * (_speed * dt);
            transform.Rotate(_dir, 720f * dt, Space.World);

            var all = CombatRegistry.All;
            float r2 = _radius * _radius;
            var here = transform.position; here.y = 0;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d == null || d.isDead) continue;
                if (d.team == _caster) continue;
                if (d.isAir) continue;
                if (_hit.Contains(d)) continue;
                var p = d.transform.position; p.y = 0;
                if ((p - here).sqrMagnitude > r2) continue;
                _hit.Add(d);
                d.TakeDamage(_damage);
                if (d.stunRemaining < _stun) d.stunRemaining = _stun;
                // Tiny knockback by nudging position; full impulse path
                // would require physics, but the visual feel of being
                // shoved is enough for a CR-style log.
                var u = d.GetComponent<Match.Unit>();
                if (u != null)
                {
                    u.transform.position += _dir * 0.5f;
                }
            }

            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
