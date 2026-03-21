using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Levels.Behaviors;
using Madbox.Levels.Contracts;
using Madbox.Levels.Rules;

namespace Madbox.Levels
{
    public sealed class LevelService : ILevelService
    {
        public LevelService(IReadOnlyList<LevelDefinition> levels = null)
        {
            ValidateLevelsArgument(levels);
            IReadOnlyList<LevelDefinition> source = BuildSourceLevels(levels);
            this.levels = source;
            indexById = BuildIndex(source);
        }

        public LevelId DefaultLevelId => levels[0].LevelId;

        private readonly IReadOnlyList<LevelDefinition> levels;
        private readonly Dictionary<string, LevelDefinition> indexById;

        public Task<LevelDefinition> LoadAsync(LevelId levelId, CancellationToken cancellationToken = default)
        {
            if (levelId == null)
            {
                throw new ArgumentNullException(nameof(levelId));
            }
            if (indexById.TryGetValue(levelId.Value, out LevelDefinition level))
            {
                return Task.FromResult(level);
            }

            throw new KeyNotFoundException($"No preloaded level found for id '{levelId.Value}'.");
        }

        private IReadOnlyList<LevelDefinition> BuildSourceLevels(IReadOnlyList<LevelDefinition> levels)
        {
            if (levels == null || levels.Count == 0)
            {
                return BuildFallbackLevels();
            }

            return levels;
        }

        private void ValidateLevelsArgument(IReadOnlyList<LevelDefinition> levels)
        {
            if (levels == null) return;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] == null) throw new ArgumentException("Levels cannot contain null entries.", nameof(levels));
            }
        }

        private static Dictionary<string, LevelDefinition> BuildIndex(IReadOnlyList<LevelDefinition> levels)
        {
            Dictionary<string, LevelDefinition> map = new Dictionary<string, LevelDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null || level.LevelId == null)
                {
                    continue;
                }
                map[level.LevelId.Value] = level;
            }

            return map;
        }

        private static IReadOnlyList<LevelDefinition> BuildFallbackLevels()
        {
            MovementBehaviorDefinition movementBehavior = new MovementBehaviorDefinition(0.1f, 1f);
            EnemyBehaviorDefinition[] behaviors = { movementBehavior };
            EntityId enemyId = new EntityId("whitebox-enemy");
            EnemyDefinition enemy = new EnemyDefinition(enemyId, 10, behaviors);
            LevelEnemyDefinition levelEnemy = new LevelEnemyDefinition(enemy, 1);
            LevelEnemyDefinition[] enemies = { levelEnemy };
            TimeLimitLoseRuleDefinition timeLimitLoseRule = new TimeLimitLoseRuleDefinition(3f);
            LevelGameRuleDefinition[] rules = { timeLimitLoseRule };
            LevelId levelId = new LevelId("whitebox-level-1");
            LevelDefinition level = new LevelDefinition(levelId, 1, enemies, rules);
            return new List<LevelDefinition> { level };
        }

    }
}

