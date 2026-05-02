using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Audio;

namespace TrashRoyale.Combat
{
    public static class AreaSpell
    {
        public static void Cast(CardData spell, Vector3 center, Team caster)
        {
            FxFactory.SpawnExplosion(center, spell.splashRadius);
            AudioManager.PlayOneShot(spell.voiceLine, center);
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
