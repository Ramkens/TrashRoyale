using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Combat;

namespace TrashRoyale.Match
{
    public static class UnitFactory
    {
        public static void SpawnCard(CardData card, Team team, Vector3 worldPos)
        {
            if (card == null) return;
            if (card.Kind == CardKind.Spell)
            {
                AreaSpell.Cast(card, worldPos, team);
                return;
            }
            int count = Mathf.Max(1, card.spawnCount);
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
    }
}
