using UnityEngine;
using TrashRoyale.Combat;
using TrashRoyale.Core;

namespace TrashRoyale.Match
{
    public class ArenaController : MonoBehaviour
    {
        public static ArenaController I { get; private set; }
        public const float HalfWidth = 4.5f;
        public const float HalfLength = 8.0f;
        public const float RiverHalfThickness = 0.5f;

        // Bonus radius around a destroyed enemy princess tower the player is allowed
        // to deploy in. Tuned so it covers the broken tower and a strip of land in
        // front of / behind it (CR-style "tower zone unlock").
        public const float TowerUnlockRadius = 3.6f;

        public Transform PlayerLeftBridge, PlayerRightBridge;
        public Transform[] PlayerSpawnPoints;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        void OnDestroy() { if (I == this) I = null; }

        /// <summary>
        /// Returns true iff <paramref name="team"/> may legally drop <paramref name="card"/>
        /// at <paramref name="worldPos"/>. Unlocks more enemy territory as opponent
        /// princess towers fall. Spells are allowed anywhere on the arena.
        /// </summary>
        public bool IsValidPlacement(Team team, Vector3 worldPos, CardData card)
        {
            if (card != null && card.Kind == CardKind.Spell)
            {
                if (Mathf.Abs(worldPos.x) > HalfWidth) return false;
                if (Mathf.Abs(worldPos.z) > HalfLength) return false;
                return true;
            }
            if (Mathf.Abs(worldPos.x) > HalfWidth) return false;
            if (Mathf.Abs(worldPos.z) > HalfLength) return false;
            if (Mathf.Abs(worldPos.z) < RiverHalfThickness) return false;

            // Default: own half only.
            float zSign = team == Team.Player ? -1f : +1f;
            bool inOwnHalf = (zSign < 0 && worldPos.z < 0f) || (zSign > 0 && worldPos.z > 0f);
            if (inOwnHalf) return true;

            // Cross-river deployment: allowed only if the corresponding enemy
            // princess tower is destroyed. With both princess towers down, the
            // entire enemy half (except a small king-tower exclusion) opens up.
            return IsEnemyHalfDeployable(team, worldPos);
        }

        bool IsEnemyHalfDeployable(Team team, Vector3 worldPos)
        {
            var match = MatchManager.I;
            if (match == null) return false;
            // Towers belonging to the opponent (the side we want to invade).
            var enemyTowers = team == Team.Player ? match.EnemySideTowers : match.PlayerSideTowers;
            int aliveCount = 0;
            foreach (var t in enemyTowers) if (t != null && !t.isDead) aliveCount++;

            // Both princess towers still alive -> no enemy-half deployment.
            if (aliveCount >= 2) return false;

            // 0 alive -> full enemy half (except a small king-tower exclusion zone).
            if (aliveCount == 0)
            {
                // Don't let the player drop directly on top of the enemy king.
                var king = team == Team.Player ? match.EnemyKing : match.PlayerKing;
                if (king != null)
                {
                    var d = worldPos - king.transform.position;
                    d.y = 0f;
                    if (d.sqrMagnitude < 1.6f * 1.6f) return false;
                }
                return true;
            }

            // 1 princess tower destroyed -> deploy zone is a circle around the
            // destroyed tower's old position. We can't read a destroyed tower's
            // transform anymore (it's been removed), so derive the zone from the
            // surviving tower's mirrored x.
            //
            // Princess towers sit at x = ±2.8 on each side. If x=-2.8 survives, the
            // x=+2.8 tower fell -> zone center is (+2.8, ±(HalfLength-1.2)).
            float zCenter = team == Team.Player ? (HalfLength - 1.2f) : -(HalfLength - 1.2f);
            foreach (var t in enemyTowers)
            {
                if (t == null || t.isDead) continue;
                float survivingX = t.transform.position.x;
                // The other lane (mirror of survivor) is the destroyed one.
                float openX = -survivingX;
                var center = new Vector3(openX, 0f, zCenter);
                var d = worldPos - center;
                d.y = 0f;
                if (d.sqrMagnitude <= TowerUnlockRadius * TowerUnlockRadius) return true;
            }
            return false;
        }
    }
}
