using UnityEngine;
using TrashRoyale.Match;

namespace TrashRoyale.Combat
{
    public abstract class Damageable : MonoBehaviour
    {
        public Team team;
        public float maxHp = 100f;
        public float hp = 100f;
        public bool isBuilding = false;
        public bool isAir = false;
        public bool isDead = false;
        public Transform aimPoint;
        // Mechanic state — exposed so Unit/Tower can drive it without a
        // separate component. Defaults of 0 keep existing units behaving
        // identically (no immunity, no stun, no reflect).
        // Phase / deploy-immune: counter ticks down in Update; while > 0,
        // TakeDamage is a no-op. Used by cards with the "phase_immune"
        // mechanic (e.g. teleporting wizard).
        public float phaseImmuneRemaining;
        // Stun: while > 0, Unit.Update returns early (no movement / no
        // attacks) but the unit can still receive damage. Driven by
        // freeze projectiles and the Freeze spell.
        public float stunRemaining;
        // Reflect: 0..1 fraction of incoming damage echoed back at the
        // source. Set per card during Init.
        public float reflectFraction;
        // Rage buff timer set by the rage_spell zone. While > 0 the
        // Unit / Tower scales movement & attack speed by RageMultiplier.
        // Decremented in Unit.Update; spells refresh it via TickZone.
        public float rageRemaining;
        public const float RageMultiplier = 1.35f;

        public virtual void Init(Team t, float maxHpValue)
        {
            team = t;
            maxHp = Mathf.Max(1f, maxHpValue);
            hp = maxHp;
            isDead = false;
        }

        public virtual void TakeDamage(float dmg, Damageable source = null)
        {
            if (isDead) return;
            // Phase / deploy-immune: ignore damage entirely while the
            // phase counter is positive. Counter is ticked down by Unit.
            if (phaseImmuneRemaining > 0f) return;
            hp -= dmg;
            // Reflect: bounce a fraction of the damage we just took
            // back at whoever hit us, but only if the source is alive
            // and on the OPPOSING team (avoid friendly fire feedback
            // loops). We do this BEFORE checking death so a fatal hit
            // still reflects.
            if (reflectFraction > 0f && source != null && !source.isDead && source.team != team)
            {
                source.TakeDamage(dmg * reflectFraction, this);
            }
            if (hp <= 0f)
            {
                hp = 0f;
                isDead = true;
                OnDeath();
            }
        }

        /// <summary>
        /// Heals the entity, capping at <c>maxHp</c>. Safe against
        /// negative inputs and dead targets. Used by lifesteal and
        /// future heal spells.
        /// </summary>
        public virtual void Heal(float amount)
        {
            if (isDead || amount <= 0f) return;
            hp = Mathf.Min(maxHp, hp + amount);
        }

        protected abstract void OnDeath();

        public Vector3 AimPos => aimPoint != null ? aimPoint.position : transform.position + Vector3.up * 0.5f;

        // ---- Visual status overlay ------------------------------------
        // Single auto-managed component that tints the unit/building's
        // mesh based on stun/rage timers. Each Damageable polls its own
        // state every frame and lazily attaches a StatusFx helper the
        // first time we need to display anything. Built-in updates run
        // here (instead of inside Unit.cs) so towers / buildings get the
        // same visual feedback for free.
        StatusFx _statusFx;

        protected void UpdateStatusFx()
        {
            // Lazy attach: only if there's any state we'd want to draw.
            bool needsFx = stunRemaining > 0f || rageRemaining > 0f || phaseImmuneRemaining > 0f;
            if (!needsFx && _statusFx == null) return;
            if (_statusFx == null)
            {
                _statusFx = gameObject.AddComponent<StatusFx>();
            }
            _statusFx.Refresh(this);
        }

        /// <summary>
        /// Triggers a one-shot lightning-strike flash on this entity.
        /// Public so the lightning spell can bolt enemies for visible
        /// feedback without having to know about StatusFx internals.
        /// </summary>
        public void FlashLightning()
        {
            if (_statusFx == null) _statusFx = gameObject.AddComponent<StatusFx>();
            _statusFx.TriggerLightning();
        }
    }
}
