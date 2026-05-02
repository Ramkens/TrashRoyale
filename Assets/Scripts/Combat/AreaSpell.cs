using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Audio;

namespace TrashRoyale.Combat
{
    public static class AreaSpell
    {
        /// <summary>
        /// Casts a splash spell. Fireball-style spells launch a visible rocket
        /// from the top of the caster's king tower (the way Clash Royale does
        /// it — the king is visibly throwing the spell) and detonate on
        /// impact; the actual damage is applied at impact time. Less rocket-y
        /// spells (poison, freeze) just detonate immediately at
        /// <paramref name="center"/>.
        /// </summary>
        public static void Cast(CardData spell, Vector3 center, Team caster)
        {
            AudioManager.PlayOneShot(spell.voiceLine, center);
            if (spell.id == "fireball")
            {
                var origin = ResolveSpellOrigin(caster, center);
                FxFactory.LaunchFireballMissile(origin, center, spell.splashRadius, () =>
                {
                    ApplyDamage(spell, center, caster);
                });
            }
            else
            {
                FxFactory.SpawnExplosion(center, spell.splashRadius);
                ApplyDamage(spell, center, caster);
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
                // King-tower roof sits ~2.7u above the tower transform. Add a
                // bit more so the missile clears the flagpole.
                return king.transform.position + new Vector3(0f, 3.0f, 0f);
            }
            // Fallback when the king tower is missing (shouldn't happen, but
            // keeps the spell visible if MatchManager is being torn down).
            return impact + new Vector3(0f, 6f, 0f);
        }

        static void ApplyDamage(CardData spell, Vector3 center, Team caster)
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
                    d.TakeDamage(spell.damage);
                }
            }
        }
    }
}
