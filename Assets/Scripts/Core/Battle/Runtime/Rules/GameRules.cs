using System;
using Madbox.Enemies.Services;
using Madbox.Levels;
using Madbox.Levels.Rules;

namespace Madbox.Battle.Rules
{
    internal sealed class GameRuleEvaluator
    {
        public GameRuleEvaluator(LevelDefinition levelDefinition)
        {
            if (levelDefinition == null)
            {
                throw new ArgumentNullException(nameof(levelDefinition));
            }

            this.levelDefinition = levelDefinition;
        }

        private readonly LevelDefinition levelDefinition;

        public bool TryEvaluate(GameState currentState, float elapsedTimeSeconds, Player player, EnemyService enemyService, out GameEndReason reason)
        {
            if (GuardEvaluationInputs(player, enemyService, out reason) == false) return false;
            return TryEvaluateRules(currentState, elapsedTimeSeconds, player, enemyService, out reason);
        }

        private bool GuardEvaluationInputs(Player player, EnemyService enemyService, out GameEndReason reason)
        {
            if (player != null && enemyService != null)
            {
                reason = GameEndReason.None; return true;
            }
            reason = GameEndReason.None;
            return false;
        }

        private bool TryEvaluateRules(GameState currentState, float elapsedTimeSeconds, Player player, EnemyService enemyService, out GameEndReason reason)
        {
            if (CanEvaluateRules(currentState, out reason) == false) return false;
            BattleContext context = BuildBattleContext(elapsedTimeSeconds, player, enemyService);
            return TryEvaluateDefinitions(context, out reason);
        }

        private bool CanEvaluateRules(GameState currentState, out GameEndReason reason)
        {
            if (currentState == GameState.Running)
            {
                reason = GameEndReason.None; return true;
            }
            reason = GameEndReason.None;
            return false;
        }

        private BattleContext BuildBattleContext(float elapsedTimeSeconds, Player player, EnemyService enemyService)
        {
            return new BattleContext(elapsedTimeSeconds, player.CurrentHealth, enemyService.AliveEnemies);
        }

        private bool TryEvaluateDefinitions(BattleContext context, out GameEndReason reason)
        {
            foreach (LevelGameRuleDefinition ruleDefinition in levelDefinition.GameRules)
            {
                if (ruleDefinition.CheckRule(context, out reason))
                {
                    return true;
                }
            }
            reason = GameEndReason.None;
            return false;
        }
    }
}

