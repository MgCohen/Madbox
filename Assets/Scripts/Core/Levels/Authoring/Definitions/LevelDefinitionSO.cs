using System;
using System.Collections.Generic;
using Madbox.Levels;
using Madbox.Levels.Rules;
using UnityEngine;
#pragma warning disable SCA0003
#pragma warning disable SCA0005
#pragma warning disable SCA0006
#pragma warning disable SCA0007

namespace Madbox.Levels.Authoring.Definitions
{
    [CreateAssetMenu(menuName = "Madbox/Authoring/Level Definition")]
    public sealed class LevelDefinitionSO : ScriptableObject
    {
        [SerializeField] private string levelId = "level-1";
        [SerializeField] private int goldReward;
        [SerializeField] private List<LevelEnemyEntrySO> enemies = new List<LevelEnemyEntrySO>();
        [SerializeField] private bool useTimeLimitLoseRule;
        [SerializeField] private float loseAfterSeconds = 60f;

        public LevelDefinition ToDomain()
        {
            IReadOnlyList<LevelEnemyDefinition> levelEnemies = BuildEnemies();
            List<LevelGameRuleDefinition> gameRules = new List<LevelGameRuleDefinition>
            {
                new EnemyEliminatedWinRuleDefinition()
            };

            if (useTimeLimitLoseRule)
            {
                gameRules.Add(new TimeLimitLoseRuleDefinition(loseAfterSeconds));
            }

            LevelId id = new LevelId(levelId);
            return new LevelDefinition(id, goldReward, levelEnemies, gameRules);
        }

        private IReadOnlyList<LevelEnemyDefinition> BuildEnemies()
        {
            if (enemies == null)
            {
                return Array.Empty<LevelEnemyDefinition>();
            }

            List<LevelEnemyDefinition> mapped = new List<LevelEnemyDefinition>(enemies.Count);
            for (int i = 0; i < enemies.Count; i++)
            {
                LevelEnemyEntrySO entry = enemies[i];
                if (entry == null || entry.Enemy == null)
                {
                    throw new InvalidOperationException("Level enemy entry is missing an enemy definition reference.");
                }

                mapped.Add(new LevelEnemyDefinition(entry.Enemy.ToDomain(), entry.Count));
            }

            return mapped;
        }
    }
}

