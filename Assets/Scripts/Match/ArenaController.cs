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

        // Radius around the enemy king inside which the player cannot drop
        // a unit when only one princess tower has fallen. Without this you
        // can land a unit directly on the king for an instant 3-crown win.
        public const float KingExclusionRadius = 1.6f;

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

            // King exclusion is shared between the 0-alive and 1-alive
            // branches: the king should NEVER be deployable-on directly.
            // A small radius is enough — the player still has to walk a
            // unit the rest of the way.
            var enemyKing = team == Team.Player ? match.EnemyKing : match.PlayerKing;
            if (enemyKing != null && !enemyKing.isDead)
            {
                var dk = worldPos - enemyKing.transform.position;
                dk.y = 0f;
                if (dk.sqrMagnitude < KingExclusionRadius * KingExclusionRadius) return false;
            }

            // 0 alive -> full enemy half is open.
            if (aliveCount == 0) return true;

            // 1 princess tower destroyed -> open the ENTIRE LANE on the
            // side where it fell (full half-x strip from the river to the
            // back wall) — same as Clash Royale.
            //
            // Previously this used a 3.6-unit circle centered on the
            // destroyed tower; that left a hole between the river and the
            // tower (so the player couldn't push from the river up the
            // open lane) AND the circle reached the king (~2.9u away),
            // which let a single unit be dropped directly on the king for
            // an instant 3-crown sweep. Both are fixed by switching to a
            // half-strip.
            //
            // Princess towers sit at x = ±2.8; the midline x = 0
            // separates the two lanes.
            foreach (var t in enemyTowers)
            {
                if (t == null || t.isDead) continue;
                float survivingX = t.transform.position.x;
                // Mirror: the lane that fell is on -survivingX side.
                float openX = -survivingX;
                bool sameLaneAsOpenX =
                    (openX > 0f && worldPos.x > 0f) || (openX < 0f && worldPos.x < 0f);
                if (sameLaneAsOpenX) return true;
            }
            return false;
        }
    }
}
