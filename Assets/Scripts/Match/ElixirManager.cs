using UnityEngine;

namespace TrashRoyale.Match
{
    public class ElixirManager
    {
        public const float Max = 10f;
        public const float SingleRegenTime = 2.8f;
        public const float DoubleRegenTime = 1.4f;
        public const float TripleRegenTime = 0.93f;

        public float Current { get; private set; } = 5f;
        public Team Owner { get; }

        public ElixirManager(Team t, float startElixir)
        {
            Owner = t;
            Current = Mathf.Clamp(startElixir, 0f, Max);
        }

        public void Tick(float dt, MatchPhase phase)
        {
            float perSec = 1f / SingleRegenTime;
            if (phase == MatchPhase.DoubleElixir) perSec = 1f / DoubleRegenTime;
            else if (phase == MatchPhase.TripleElixir) perSec = 1f / TripleRegenTime;
            Current = Mathf.Clamp(Current + perSec * dt, 0f, Max);
        }

        public bool TrySpend(int amount)
        {
            if (Current >= amount - 0.001f)
            {
                Current = Mathf.Max(0f, Current - amount);
                return true;
            }
            return false;
        }

        // Authoritative network sync — guest overrides its locally
        // simulated elixir value with the host's truth.
        public void OverrideFromSnapshot(float value)
        {
            Current = Mathf.Clamp(value, 0f, Max);
        }
    }
}
