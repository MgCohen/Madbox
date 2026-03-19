using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Levels.Behaviors;
using Madbox.Levels.Contracts;
using Madbox.Levels.Rules;
#pragma warning disable SCA0006
#pragma warning disable SCA0005
#pragma warning disable SCA0003
#pragma warning disable SCA0017

namespace Madbox.Levels
{
    public sealed class LevelService : ILevelService
    {
        public LevelService(IReadOnlyList<LevelDefinition> levels = null)
        {
            IReadOnlyList<LevelDefinition> source = levels;
            if (source == null || source.Count == 0) { source = BuildFallbackLevels(); }
            this.levels = source;
            indexById = BuildIndex(source);
        }

        public LevelId DefaultLevelId => levels[0].LevelId;

        private readonly IReadOnlyList<LevelDefinition> levels;
        private readonly Dictionary<string, LevelDefinition> indexById;

        public Task<LevelDefinition> LoadAsync(LevelId levelId, CancellationToken cancellationToken = default)
        {
            if (levelId == null) { throw new ArgumentNullException(nameof(levelId)); }
            if (indexById.TryGetValue(levelId.Value, out LevelDefinition level))
            {
                return Task.FromResult(level);
            }

            throw new KeyNotFoundException($"No preloaded level found for id '{levelId.Value}'.");
        }

        private static Dictionary<string, LevelDefinition> BuildIndex(IReadOnlyList<LevelDefinition> levels)
        {
            Dictionary<string, LevelDefinition> map = new Dictionary<string, LevelDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null || level.LevelId == null) { continue; }
                map[level.LevelId.Value] = level;
            }

            return map;
        }

        private static IReadOnlyList<LevelDefinition> BuildFallbackLevels()
        {
            EnemyBehaviorDefinition[] behaviors = { new MovementBehaviorDefinition(0.1f, 1f) };
            EnemyDefinition enemy = new EnemyDefinition(new EntityId("whitebox-enemy"), 10, behaviors);
            LevelEnemyDefinition[] enemies = { new LevelEnemyDefinition(enemy, 1) };
            LevelGameRuleDefinition[] rules = { new TimeLimitLoseRuleDefinition(3f) };
            LevelDefinition level = new LevelDefinition(new LevelId("whitebox-level-1"), 1, enemies, rules);
            return new List<LevelDefinition> { level };
        }
    }
}
