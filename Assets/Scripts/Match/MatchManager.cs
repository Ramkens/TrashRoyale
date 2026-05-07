using System;
using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Combat;
using TrashRoyale.Audio;

namespace TrashRoyale.Match
{
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager I { get; private set; }

        public ElixirManager PlayerElixir { get; private set; }
        public ElixirManager EnemyElixir { get; private set; }
        public Deck PlayerDeck { get; private set; }
        public Deck EnemyDeck { get; private set; }
        public MatchPhase Phase { get; private set; } = MatchPhase.Countdown;

        public float MatchDurationSeconds = 180f;
        public float DoubleElixirAt = 60f;
        public float TripleElixirAt = 30f;
        public float TimeRemaining { get; private set; }

        // ---- Overtime ----
        // When the regulation 3-minute clock expires with crowns still
        // tied (and no king has fallen), the match drops into Overtime
        // instead of the old "lowest king HP wins" tiebreak. During
        // Overtime the elixir regen multiplier ramps from TripleElixir
        // (3×) up to a cap of 7× — every OvertimeRampStepSeconds (30s)
        // in OT, multiplier += 1. After OvertimeMaxSeconds (120s) the
        // match is force-ended as a draw if nobody scored a tower.
        public const float OvertimeMaxSeconds = 120f;
        public const float OvertimeRampStepSeconds = 30f;
        public const float OvertimeStartMultiplier = 3f;
        public const float OvertimeMaxMultiplier = 7f;
        public float OvertimeElapsed { get; private set; }
        public float OvertimeElixirMultiplier { get; private set; } = OvertimeStartMultiplier;
        // Extended from 3s to 5s in PR3 to fit the CR-style banner-reveal
        // intro animation. Last 3 seconds are still the big "3..2..1.. GO!"
        // countdown — the first 2 seconds slide in player + opponent banners.
        public const float CountdownTotal = 5f;
        public float CountdownRemaining { get; private set; } = CountdownTotal;

        public Tower PlayerKing, EnemyKing;
        public List<Tower> PlayerSideTowers { get; } = new List<Tower>();
        public List<Tower> EnemySideTowers { get; } = new List<Tower>();

        public int PlayerCrowns { get; private set; }
        public int EnemyCrowns { get; private set; }

        // True when the match ended without a winner (overtime ran the
        // full clock without either side breaking the tie). BattleBootstrap
        // uses this to credit/dock zero trophies and show "НИЧЬЯ".
        public bool IsDraw { get; private set; }

        public Action<Team> OnMatchEnded;
        public Action<Tower> OnTowerDown;
        public bool IsLocalPvE { get; set; } = true;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            TimeRemaining = MatchDurationSeconds;
        }

        void OnDestroy() { if (I == this) I = null; }

        public void InitMatch(List<string> playerDeck, List<string> enemyDeck, bool pve)
        {
            PlayerElixir = new ElixirManager(Team.Player, 5f);
            EnemyElixir = new ElixirManager(Team.Enemy, 5f);
            PlayerDeck = new Deck(playerDeck);
            EnemyDeck = new Deck(enemyDeck);
            IsLocalPvE = pve;
            Phase = MatchPhase.Countdown;
            CountdownRemaining = CountdownTotal;
            TimeRemaining = MatchDurationSeconds;
            PlayerCrowns = 0;
            EnemyCrowns = 0;
            OvertimeElapsed = 0f;
            OvertimeElixirMultiplier = OvertimeStartMultiplier;
            IsDraw = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Phase == MatchPhase.Countdown)
            {
                CountdownRemaining -= dt;
                if (CountdownRemaining <= 0f)
                {
                    Phase = MatchPhase.SingleElixir;
                    AudioManager.PlayOneShot("match_start", Vector3.zero);
                }
                return;
            }
            if (Phase == MatchPhase.Ended) return;

            // Overtime phase doesn't decrement TimeRemaining further —
            // we drive its own elapsed timer instead so the HUD shows
            // OT countup rather than negative time.
            if (Phase == MatchPhase.Overtime)
            {
                OvertimeElapsed += dt;
                int ramps = Mathf.FloorToInt(OvertimeElapsed / OvertimeRampStepSeconds);
                OvertimeElixirMultiplier = Mathf.Min(
                    OvertimeStartMultiplier + ramps,
                    OvertimeMaxMultiplier);
                PlayerElixir.Tick(dt, Phase, OvertimeElixirMultiplier);
                EnemyElixir.Tick(dt, Phase, OvertimeElixirMultiplier);
                if (OvertimeElapsed >= OvertimeMaxSeconds)
                {
                    EndAsDraw();
                }
                return;
            }

            TimeRemaining -= dt;
            if (Phase == MatchPhase.SingleElixir && TimeRemaining <= MatchDurationSeconds - 60f) Phase = MatchPhase.DoubleElixir;
            if (Phase == MatchPhase.DoubleElixir && TimeRemaining <= 30f) Phase = MatchPhase.TripleElixir;

            PlayerElixir.Tick(dt, Phase);
            EnemyElixir.Tick(dt, Phase);

            if (TimeRemaining <= 0f)
            {
                EndOnTime();
            }
        }

        public bool TryDeployPlayer(int handSlot, Vector3 worldPos)
        {
            return TryDeploy(Team.Player, handSlot, worldPos);
        }

        public bool TryDeployEnemy(int handSlot, Vector3 worldPos)
        {
            return TryDeploy(Team.Enemy, handSlot, worldPos);
        }

        public bool TryDeploy(Team team, int handSlot, Vector3 worldPos)
        {
            if (Phase == MatchPhase.Ended || Phase == MatchPhase.Countdown) return false;
            var deck = team == Team.Player ? PlayerDeck : EnemyDeck;
            var elixir = team == Team.Player ? PlayerElixir : EnemyElixir;
            if (deck == null) return false;
            var card = deck.Hand[handSlot];
            if (card == null) return false;
            if (!ArenaController.I.IsValidPlacement(team, worldPos, card)) return false;
            if (!elixir.TrySpend(card.elixirCost)) return false;
            deck.PlayHandSlot(handSlot);
            UnitFactory.SpawnCard(card, team, worldPos);
            AudioManager.PlayOneShot("card_play", worldPos);

            if (team == Team.Player && !IsLocalPvE)
            {
                var netSync = GetComponent<TrashRoyale.Net.NetMatchSync>();
                if (netSync != null) netSync.SendCardPlay(handSlot, card.id, worldPos);
            }
            return true;
        }

        // Guest path: apply host's authoritative snapshot. The list
        // of HPs is fixed-size:
        //   [0..1] = PlayerSideTowers, [2..3] = EnemySideTowers,
        //   [4]    = PlayerKing,       [5]    = EnemyKing.
        // Mirror across teams because host and guest see swapped sides.
        public void ApplyAuthoritativeSnapshot(float[] towerHps, float playerElixir, float enemyElixir,
            int playerCrowns, int enemyCrowns, float timeRemaining)
        {
            if (Phase == MatchPhase.Ended) return;
            // Mirror: host's "player" half is the guest's "enemy" half.
            ApplyTowerHp(EnemySideTowers, 0, towerHps, 0);
            ApplyTowerHp(EnemySideTowers, 1, towerHps, 1);
            ApplyTowerHp(PlayerSideTowers, 0, towerHps, 2);
            ApplyTowerHp(PlayerSideTowers, 1, towerHps, 3);
            ApplyTowerHp(EnemyKing, towerHps, 4);
            ApplyTowerHp(PlayerKing, towerHps, 5);
            PlayerElixir?.OverrideFromSnapshot(enemyElixir);
            EnemyElixir?.OverrideFromSnapshot(playerElixir);
            PlayerCrowns = enemyCrowns;
            EnemyCrowns = playerCrowns;
            // Clamp local clock toward host so the countdown matches.
            if (timeRemaining > 0f) TimeRemaining = timeRemaining;
        }

        static void ApplyTowerHp(List<Tower> list, int idx, float[] hps, int hpIdx)
        {
            if (list == null || idx >= list.Count) return;
            ApplyTowerHp(list[idx], hps, hpIdx);
        }

        static void ApplyTowerHp(Tower t, float[] hps, int hpIdx)
        {
            if (t == null || t.isDead) return;
            if (hpIdx < 0 || hpIdx >= hps.Length) return;
            float v = hps[hpIdx];
            if (v <= 0f)
            {
                // Force destroy via the existing damage path so all
                // event handlers (king activation, crowns) still fire.
                t.TakeDamage(t.hp + 1f, null);
            }
            else if (v < t.hp)
            {
                // Snap the visible HP down — apply the missing damage
                // through TakeDamage so HpBar / FX update correctly.
                t.TakeDamage(t.hp - v, null);
            }
            // We never heal towers from a snapshot — local sim having
            // less HP than host just means host is slightly behind.
        }

        public void OnTowerDestroyed(Tower t)
        {
            OnTowerDown?.Invoke(t);
            if (t.team == Team.Player)
            {
                EnemyCrowns++;
                if (PlayerSideTowers.Remove(t)) ActivatePlayerKing();
            }
            else
            {
                PlayerCrowns++;
                if (EnemySideTowers.Remove(t)) ActivateEnemyKing();
            }
            if (t.isKing)
            {
                EndMatch(t.team == Team.Player ? Team.Enemy : Team.Player);
                return;
            }
            if (PlayerCrowns >= 3) EndMatch(Team.Player);
            else if (EnemyCrowns >= 3) EndMatch(Team.Enemy);
        }

        void ActivatePlayerKing()
        {
            if (PlayerKing != null && !PlayerKing.isActive) PlayerKing.Activate();
        }

        void ActivateEnemyKing()
        {
            if (EnemyKing != null && !EnemyKing.isActive) EnemyKing.Activate();
        }

        void EndOnTime()
        {
            // Crowns broken -> normal time-up win.
            if (PlayerCrowns > EnemyCrowns) { EndMatch(Team.Player); return; }
            if (EnemyCrowns > PlayerCrowns) { EndMatch(Team.Enemy); return; }
            // Crowns tied at the end of regulation -> CR-style overtime.
            // Whoever takes a tower (or burns the king) first wins.
            // OT also has a hard ceiling (OvertimeMaxSeconds) so the
            // match resolves as a draw if absolutely no one scores.
            Phase = MatchPhase.Overtime;
            OvertimeElapsed = 0f;
            OvertimeElixirMultiplier = OvertimeStartMultiplier;
        }

        void EndAsDraw()
        {
            if (Phase == MatchPhase.Ended) return;
            // Pick the lower-king-hp side as the "nominal" winner so the
            // UI flow still has a Team to color the result screen with;
            // BattleBootstrap reads IsDraw and overrides the title text.
            IsDraw = true;
            EndMatch(LowestKingHpTeam());
        }

        Team LowestKingHpTeam()
        {
            float p = PlayerKing != null ? PlayerKing.hp / Mathf.Max(1f, PlayerKing.maxHp) : 0f;
            float e = EnemyKing != null ? EnemyKing.hp / Mathf.Max(1f, EnemyKing.maxHp) : 0f;
            if (p > e) return Team.Player;
            if (e > p) return Team.Enemy;
            return Team.Player;
        }

        void EndMatch(Team winner)
        {
            if (Phase == MatchPhase.Ended) return;
            Phase = MatchPhase.Ended;
            OnMatchEnded?.Invoke(winner);
        }

        public void PlayerSurrender()
        {
            if (Phase == MatchPhase.Ended) return;
            EnemyCrowns = 3;
            EndMatch(Team.Enemy);
        }
    }
}
