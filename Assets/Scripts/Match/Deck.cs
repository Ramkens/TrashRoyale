using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Core;

namespace TrashRoyale.Match
{
    public class Deck
    {
        readonly List<CardData> _cards;
        readonly Queue<CardData> _draw;
        public CardData[] Hand { get; } = new CardData[4];
        public CardData NextCard { get; private set; }

        public Deck(List<string> cardIds)
        {
            CardDatabase.EnsureLoaded();
            _cards = new List<CardData>(cardIds.Count);
            foreach (var id in cardIds)
            {
                var c = CardDatabase.Get(id);
                if (c != null) _cards.Add(c);
            }
            Shuffle();
            _draw = new Queue<CardData>(_cards);
            for (int i = 0; i < 4 && _draw.Count > 0; i++) Hand[i] = _draw.Dequeue();
            NextCard = _draw.Count > 0 ? _draw.Dequeue() : null;
        }

        void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public CardData PlayHandSlot(int slot)
        {
            if (slot < 0 || slot >= 4) return null;
            var played = Hand[slot];
            if (played == null) return null;
            Hand[slot] = NextCard;
            if (_draw.Count == 0)
            {
                var refill = new List<CardData>();
                foreach (var c in _cards) if (System.Array.IndexOf(Hand, c) < 0) refill.Add(c);
                if (played != null) refill.Add(played);
                for (int i = refill.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (refill[i], refill[j]) = (refill[j], refill[i]);
                }
                foreach (var c in refill) _draw.Enqueue(c);
            }
            else
            {
                _draw.Enqueue(played);
            }
            NextCard = _draw.Count > 0 ? _draw.Dequeue() : null;
            return played;
        }
    }
}
