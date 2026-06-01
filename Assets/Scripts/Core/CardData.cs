using System;
using UnityEngine;

namespace TrashRoyale.Core
{
    public enum CardKind { Unit, Spell, Building }
    public enum TargetMode { Any, BuildingsOnly }

    [Serializable]
    public class CardData
    {
        public string id;
        public string displayName;
        public string description;
        public int elixirCost;
        public string kind;
        public float hp;
        public float damage;
        public float attackInterval;
        public float range;
        public float moveSpeed;
        public float deployTime;
        public int spawnCount;
        public float spawnRadius;
        public bool isAir;
        public string targetMode;
        public bool targetsAir;
        public float splashRadius;
        public string modelKey;
        public float modelScale;
        public string voiceLine;
        // Lifetime in seconds for Building cards (cannons / teslas decay).
        // Ignored for Unit and Spell kinds. <= 0 means "permanent" (rare).
        public float lifetime;

        public CardKind Kind
        {
            get
            {
                if (kind == "Spell") return CardKind.Spell;
                if (kind == "Building") return CardKind.Building;
                return CardKind.Unit;
            }
        }
        public TargetMode Targets => targetMode == "BuildingsOnly" ? TargetMode.BuildingsOnly : TargetMode.Any;
    }

    [Serializable]
    public class CardCollection
    {
        public CardData[] cards;
    }
}
