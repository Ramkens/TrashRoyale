using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Match;

namespace TrashRoyale.Combat
{
    public static class CombatRegistry
    {
        static readonly List<Damageable> _all = new List<Damageable>(256);
        public static IReadOnlyList<Damageable> All => _all;

        public static void Register(Damageable d) { if (!_all.Contains(d)) _all.Add(d); }
        public static void Unregister(Damageable d) { _all.Remove(d); }

        public static Damageable FindClosestEnemy(Vector3 pos, Team self, float maxRange,
            bool buildingsOnly, bool canHitAir)
        {
            Damageable best = null;
            float bestDist = float.PositiveInfinity;
            for (int i = 0; i < _all.Count; i++)
            {
                var d = _all[i];
                if (d == null || d.isDead) continue;
                if (d.team == self) continue;
                if (buildingsOnly && !d.isBuilding) continue;
                if (d.isAir && !canHitAir) continue;
                var dx = d.transform.position - pos;
                dx.y = 0;
                var sq = dx.sqrMagnitude;
                if (sq < bestDist && (maxRange <= 0f || sq <= maxRange * maxRange))
                {
                    bestDist = sq;
                    best = d;
                }
            }
            return best;
        }

        public static int CountTeam(Team team, bool buildingsOnly = false)
        {
            int n = 0;
            for (int i = 0; i < _all.Count; i++)
            {
                var d = _all[i];
                if (d == null || d.isDead) continue;
                if (d.team != team) continue;
                if (buildingsOnly && !d.isBuilding) continue;
                n++;
            }
            return n;
        }

        public static void Clear() => _all.Clear();
    }
}
