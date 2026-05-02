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
        /// from above the target and detonate on impact; the actual damage is
        /// applied at impact time. Less rocket-y spells (poison, freeze) just
        /// detonate immediately at <paramref name="center"/>.
        /// </summary>
        public static void Cast(CardData spell, Vector3 center, Team caster)
        {
            AudioManager.PlayOneShot(spell.voiceLine, center);
            if (spell.id == "fireball")
            {
                var sky = center + new Vector3(-2f, 6f, -3f);
                FxFactory.LaunchFireballMissile(sky, center, spell.splashRadius, () =>
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
