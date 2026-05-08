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
            Tick(dt, phase, 1f);
        }

        // overtimeMultiplier scales SingleRegenTime; 1f = no extra ramp.
        // MatchManager passes a value in [3, 7] during MatchPhase.Overtime
        // so each 30 seconds in OT bumps regen by +1× until the 7× cap.
        public void Tick(float dt, MatchPhase phase, float overtimeMultiplier)
        {
            float perSec = 1f / SingleRegenTime;
            if (phase == MatchPhase.DoubleElixir) perSec = 1f / DoubleRegenTime;
            else if (phase == MatchPhase.TripleElixir) perSec = 1f / TripleRegenTime;
            else if (phase == MatchPhase.Overtime)
            {
                // Reuse SingleRegenTime as the base "1×" so a multiplier of
                // 3 matches TripleElixir, 4 = 4× single, etc.
                perSec = overtimeMultiplier / SingleRegenTime;
            }
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
