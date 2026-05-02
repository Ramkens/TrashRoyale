using System.Collections.Generic;
using UnityEngine;

namespace TrashRoyale.Core
{
    public static class CardDatabase
    {
        static Dictionary<string, CardData> _byId;
        public static IReadOnlyList<CardData> All { get; private set; }

        public static void EnsureLoaded()
        {
            if (_byId != null) return;
            var json = Resources.Load<TextAsset>("Cards/cards");
            if (json == null)
            {
                Debug.LogError("Cards/cards.json missing in Resources");
                _byId = new Dictionary<string, CardData>();
                All = new CardData[0];
                return;
            }
            var collection = JsonUtility.FromJson<CardCollection>(json.text);
            _byId = new Dictionary<string, CardData>(collection.cards.Length);
            foreach (var c in collection.cards) _byId[c.id] = c;
            All = collection.cards;
            Debug.Log($"[CardDB] Loaded {collection.cards.Length} cards");
        }

        public static CardData Get(string id)
        {
            EnsureLoaded();
            return _byId.TryGetValue(id, out var c) ? c : null;
        }
    }
}
