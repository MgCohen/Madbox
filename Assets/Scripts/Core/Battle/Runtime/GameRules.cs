using System;
using Madbox.Levels;

#pragma warning disable SCA0006
#pragma warning disable SCA0012
#pragma warning disable SCA0015
#pragma warning disable SCA0017
#pragma warning disable SCA0020

namespace Madbox.Battle
{
    internal interface IGameRule
    {
        bool TryGetEndReason(GameState currentState, Player player, EnemyService enemyService, out GameEndReason reason);
    }

    internal sealed class PlayerDefeatedRule : IGameRule
    {
        public bool TryGetEndReason(GameState currentState, Player player, EnemyService enemyService, out GameEndReason reason)
        {
            if (currentState == GameState.Running && player.CurrentHealth <= 0)
            {
                reason = GameEndReason.Lose;
                return true;
            }

            reason = GameEndReason.None;
            return false;
        }
    }

    internal sealed class AllEnemiesDefeatedRule : IGameRule
    {
        public bool TryGetEndReason(GameState currentState, Player player, EnemyService enemyService, out GameEndReason reason)
        {
            if (currentState == GameState.Running && enemyService.AliveEnemies <= 0)
            {
                reason = GameEndReason.Win;
                return true;
            }

            reason = GameEndReason.None;
            return false;
        }
    }

    internal sealed class GameRuleEvaluator
    {
        private readonly IGameRule[] gameRules;

        public GameRuleEvaluator(LevelDefinition levelDefinition)
        {
            if (levelDefinition == null)
            {
                throw new ArgumentNullException(nameof(levelDefinition));
            }

            gameRules = BuildRules(levelDefinition);
        }

        public bool TryEvaluate(GameState currentState, Player player, EnemyService enemyService, out GameEndReason reason)
        {
            for (int i = 0; i < gameRules.Length; i++)
            {
                if (gameRules[i].TryGetEndReason(currentState, player, enemyService, out reason))
                {
                    return true;
                }
            }

            reason = GameEndReason.None;
            return false;
        }

        private static IGameRule[] BuildRules(LevelDefinition levelDefinition)
        {
            return new IGameRule[]
            {
                new PlayerDefeatedRule(),
                new AllEnemiesDefeatedRule()
            };
        }
    }
}
