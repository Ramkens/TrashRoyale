using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Combat;

namespace TrashRoyale.Match
{
    /// <summary>
    /// Card -> world-object dispatcher. Spells go through <see cref="AreaSpell"/>,
    /// stationary defensive structures go through <see cref="Building"/>, and
    /// everything else spawns one or more mobile <see cref="Unit"/>s in a
    /// circle around <paramref name="worldPos"/>.
    /// </summary>
    public static class UnitFactory
    {
        public static void SpawnCard(CardData card, Team team, Vector3 worldPos)
        {
            if (card == null) return;
            switch (card.Kind)
            {
                case CardKind.Spell:
                    AreaSpell.Cast(card, worldPos, team);
                    return;
                case CardKind.Building:
                    SpawnBuilding(card, team, worldPos);
                    return;
                default:
                    SpawnUnits(card, team, worldPos);
                    return;
            }
        }

        static void SpawnUnits(CardData card, Team team, Vector3 worldPos)
        {
            int count = Mathf.Max(1, card.spawnCount);
            SpawnUnitsExact(card, team, worldPos, count);
        }

        /// <summary>
        /// Spawns exactly <paramref name="count"/> instances of the unit
        /// in a ring around <paramref name="worldPos"/>, ignoring
        /// <c>card.spawnCount</c>. Used by spawner-on-tick buildings
        /// (e.g. Хата Sus emits 1 imposter per tick instead of the
        /// 4-pack the amongus card normally produces).
        /// </summary>
        public static void SpawnUnitsExact(CardData card, Team team, Vector3 worldPos, int count)
        {
            if (card == null) return;
            count = Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Vector3.zero;
                if (count > 1)
                {
                    float ang = (i / (float)count) * Mathf.PI * 2f;
                    offset = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * card.spawnRadius;
                }
                var go = ModelLoader.InstantiateUnit(card);
                go.transform.position = worldPos + offset;
                go.transform.rotation = Quaternion.Euler(0f, team == Team.Player ? 0f : 180f, 0f);
                if (card.isAir) go.transform.position += Vector3.up * 1.6f;
                var unit = go.GetComponent<Unit>();
                if (unit == null) unit = go.AddComponent<Unit>();
                unit.Init(card, team);
                unit.aimPoint = new GameObject("Aim").transform;
                unit.aimPoint.SetParent(go.transform, false);
                unit.aimPoint.localPosition = new Vector3(0, card.isAir ? 0.4f : 0.7f, 0);
            }
        }

        static void SpawnBuilding(CardData card, Team team, Vector3 worldPos)
        {
            var go = ModelLoader.InstantiateBuilding(card);
            go.transform.position = worldPos;
            // Buildings face the lane (toward enemy half) so cosmetic forward
            // matches their projectile direction.
            go.transform.rotation = Quaternion.Euler(0f, team == Team.Player ? 0f : 180f, 0f);
            var b = go.GetComponent<Building>();
            if (b == null) b = go.AddComponent<Building>();
            b.Init(card, team);
            b.aimPoint = new GameObject("Aim").transform;
            b.aimPoint.SetParent(go.transform, false);
            b.aimPoint.localPosition = new Vector3(0, 0.9f, 0);
        }
    }
}
