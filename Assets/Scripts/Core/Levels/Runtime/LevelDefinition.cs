using System;
using System.Collections.Generic;
using Madbox.Levels.Rules;

namespace Madbox.Levels
{
    public class LevelDefinition
    {
        public LevelDefinition(LevelId levelId, int goldReward, IReadOnlyList<LevelEnemyDefinition> enemies)
            : this(levelId, goldReward, enemies, CreateDefaultGameRules())
        {
        }

        public LevelDefinition(LevelId levelId, int goldReward, IReadOnlyList<LevelEnemyDefinition> enemies, IReadOnlyList<LevelGameRuleDefinition> gameRules)
        {
            if (levelId == null)
            {
                throw new ArgumentNullException(nameof(levelId));
            }

            if (string.IsNullOrWhiteSpace(levelId.Value))
            {
                throw new ArgumentException("Level id is required.", nameof(levelId));
            }

            if (goldReward < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(goldReward), "Gold reward cannot be negative.");
            }

            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (enemies.Count == 0)
            {
                throw new ArgumentException("At least one enemy entry is required.", nameof(enemies));
            }

            EnsureEnemyEntries(enemies);
            IReadOnlyList<LevelGameRuleDefinition> normalizedGameRules = NormalizeGameRules(gameRules);
            EnsureGameRules(normalizedGameRules);

            LevelId = levelId;
            GoldReward = goldReward;
            Enemies = enemies;
            GameRules = normalizedGameRules;
        }

        public LevelId LevelId { get; }

        public int GoldReward { get; }

        public IReadOnlyList<LevelEnemyDefinition> Enemies { get; }

        public IReadOnlyList<LevelGameRuleDefinition> GameRules { get; }

        private void EnsureEnemyEntries(IReadOnlyList<LevelEnemyDefinition> enemies)
        {
            HashSet<EntityId> seenEnemyTypes = new HashSet<EntityId>();
            for (int i = 0; i < enemies.Count; i++)
{
    ValidateEnemyEntry(enemies, seenEnemyTypes, enemies[i]);
}
        }

        private void ValidateEnemyEntry(IReadOnlyList<LevelEnemyDefinition> enemies, HashSet<EntityId> seenEnemyTypes, LevelEnemyDefinition entry)
        {
            if (entry == null)
            {
                throw new ArgumentException("Enemy entries cannot contain null.", nameof(enemies));
            }

            EnsureEnemyTypeIsUnique(enemies, seenEnemyTypes, entry.Enemy.EnemyTypeId);
        }

        private void EnsureEnemyTypeIsUnique(IReadOnlyList<LevelEnemyDefinition> enemies, HashSet<EntityId> seenEnemyTypes, EntityId enemyTypeId)
        {
            if (seenEnemyTypes.Add(enemyTypeId))
            {
                return;
            }

            throw new ArgumentException($"Duplicate enemy type id '{enemyTypeId.Value}'.", nameof(enemies));
        }

        private void EnsureGameRules(IReadOnlyList<LevelGameRuleDefinition> gameRules)
        {
            EnsureGameRuleCollection(gameRules);
            EnsureGameRulesContainValues(gameRules);
        }

        private void EnsureGameRuleCollection(IReadOnlyList<LevelGameRuleDefinition> gameRules)
        {
            if (gameRules == null) throw new ArgumentNullException(nameof(gameRules));
            if (gameRules.Count == 0) throw new ArgumentException("At least one game rule is required.", nameof(gameRules));
        }

        private void EnsureGameRulesContainValues(IReadOnlyList<LevelGameRuleDefinition> gameRules)
        {
            for (int i = 0; i < gameRules.Count; i++)
            {
                if (gameRules[i] == null) throw new ArgumentException("Game rules cannot contain null.", nameof(gameRules));
            }
        }

        private IReadOnlyList<LevelGameRuleDefinition> NormalizeGameRules(IReadOnlyList<LevelGameRuleDefinition> gameRules)
        {
            List<LevelGameRuleDefinition> normalizedRules = CopyGameRules(gameRules);
            EnsurePlayerDefeatRule(normalizedRules);
            return normalizedRules;
        }

        private List<LevelGameRuleDefinition> CopyGameRules(IReadOnlyList<LevelGameRuleDefinition> gameRules)
        {
            if (gameRules == null) return null;
            return new List<LevelGameRuleDefinition>(gameRules);
        }

        private void EnsurePlayerDefeatRule(ICollection<LevelGameRuleDefinition> gameRules)
        {
            if (gameRules == null) return;
            if (ContainsRule<PlayerDefeatedLoseRuleDefinition>(gameRules)) return;
            PlayerDefeatedLoseRuleDefinition playerDefeatedRule = new PlayerDefeatedLoseRuleDefinition();
            gameRules.Add(playerDefeatedRule);
        }

        private bool ContainsRule<TRule>(IEnumerable<LevelGameRuleDefinition> gameRules) where TRule : LevelGameRuleDefinition
        {
            foreach (LevelGameRuleDefinition rule in gameRules)
            {
                if (rule is TRule)
                {
                    return true;
                }
            }
            return false;
        }

        private static IReadOnlyList<LevelGameRuleDefinition> CreateDefaultGameRules()
        {
            return new LevelGameRuleDefinition[] { new EnemyEliminatedWinRuleDefinition() };
        }
    }
}

