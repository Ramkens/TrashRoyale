using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Combat;

namespace TrashRoyale.AI
{
    public class BotController : MonoBehaviour
    {
        public float Difficulty = 0.5f;
        float _decisionCooldown = 1.5f;

        void Update()
        {
            var match = MatchManager.I;
            if (match == null || match.Phase == MatchPhase.Ended || match.Phase == MatchPhase.Countdown) return;
            if (!match.IsLocalPvE) return;

            _decisionCooldown -= Time.deltaTime;
            if (_decisionCooldown > 0f) return;
            // In Overtime the bot's elixir regens 3-7× faster, so we
            // also tighten the decision cooldown so it actually spends
            // those resources instead of sitting on a full bar (this
            // is what made the opponent look like it "stopped playing"
            // during OT — the loop was firing at the same rate but
            // most checks ended in "nothing affordable yet" because
            // the previous play had already drained their bar).
            _decisionCooldown = match.Phase == MatchPhase.Overtime
                ? Mathf.Lerp(1.3f, 0.4f, Difficulty)
                : Mathf.Lerp(2.5f, 0.7f, Difficulty);

            var deck = match.EnemyDeck;
            if (deck == null) return;

            int bestSlot = -1;
            CardData bestCard = null;
            int bestPriority = -1;

            for (int i = 0; i < 4; i++)
            {
                var c = deck.Hand[i];
                if (c == null) continue;
                if (c.elixirCost > match.EnemyElixir.Current + 0.5f) continue;
                int priority = ScoreCard(c, match);
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestCard = c;
                    bestSlot = i;
                }
            }

            if (bestCard == null) return;

            float chance = Mathf.Lerp(0.4f, 1.0f, Difficulty);
            if (Random.value > chance) return;

            Vector3 spawn = ChooseSpawnPoint(bestCard, match);
            match.TryDeployEnemy(bestSlot, spawn);
        }

        int ScoreCard(CardData c, MatchManager match)
        {
            int score = 5;
            int playerUnits = CountTeamUnits(Team.Player);
            int enemyUnits = CountTeamUnits(Team.Enemy);

            if (c.Kind == CardKind.Spell)
            {
                if (playerUnits >= 3) score += 8;
                else score -= 4;
            }
            if (c.Kind == CardKind.Building)
            {
                // Drop a defensive building when the player is pushing a
                // BuildingsOnly attacker (pig, hog) at our base.
                if (PlayerHasBuildingsOnlyAttacker()) score += 7;
                else if (playerUnits >= 2) score += 2;
                else score -= 2;
            }
            if (c.targetMode == "BuildingsOnly" && playerUnits == 0) score += 3;
            if (c.elixirCost <= 3 && match.EnemyElixir.Current < 6) score += 2;
            if (c.elixirCost >= 6 && match.EnemyElixir.Current >= 8) score += 4;
            if (c.isAir) score += 1;
            if (playerUnits > enemyUnits + 1) score += 3;
            return score + Random.Range(0, 4);
        }

        bool PlayerHasBuildingsOnlyAttacker()
        {
            var all = CombatRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d == null || d.isDead) continue;
                if (d.team != Team.Player) continue;
                var u = d as Unit;
                if (u != null && u.card != null && u.card.Targets == TargetMode.BuildingsOnly) return true;
            }
            return false;
        }

        int CountTeamUnits(Team t)
        {
            int n = 0;
            var all = CombatRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d == null || d.isDead) continue;
                if (d.team != t) continue;
                if (d.isBuilding) continue;
                n++;
            }
            return n;
        }

        Vector3 ChooseSpawnPoint(CardData card, MatchManager match)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float z;
            if (card.Kind == CardKind.Spell)
            {
                z = Random.Range(-ArenaController.HalfLength + 1.5f, -1.5f);
            }
            else if (card.Kind == CardKind.Building)
            {
                // Drop defensive buildings between the bot's princess towers
                // and king — i.e. just behind the river on the bot side
                // (positive z half for Team.Enemy).
                z = Random.Range(1.5f, 4.0f);
                side = ChooseDefenseSide();
            }
            else
            {
                if (card.targetMode == "BuildingsOnly")
                {
                    z = Random.Range(0.8f, 4.0f);
                    side = ChooseAttackingSide();
                }
                else
                {
                    z = Random.Range(0.8f, ArenaController.HalfLength - 1.2f);
                }
            }
            float x = side * Random.Range(1.2f, 3.6f);
            return new Vector3(x, 0f, z);
        }

        // Pick the side where the player is currently pushing so the building
        // sits in the path of the threat.
        float ChooseDefenseSide()
        {
            int leftThreat = 0, rightThreat = 0;
            var all = CombatRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d == null || d.isDead) continue;
                if (d.team != Team.Player) continue;
                if (d.transform.position.x < 0f) leftThreat++; else rightThreat++;
            }
            if (leftThreat > rightThreat) return -1f;
            if (rightThreat > leftThreat) return 1f;
            return Random.value < 0.5f ? -1f : 1f;
        }

        float ChooseAttackingSide()
        {
            int leftCount = 0, rightCount = 0;
            var all = CombatRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d == null || d.isDead) continue;
                if (d.team != Team.Player) continue;
                if (d.transform.position.x < 0f) leftCount++; else rightCount++;
            }
            if (leftCount < rightCount) return -1f;
            if (rightCount < leftCount) return 1f;
            return Random.value < 0.5f ? -1f : 1f;
        }
    }
}
