using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Madbox.V2.Enemies;
using Madbox.V2.Levels;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Madbox.V2.Battle
{
    public sealed class GameFactoryV2
    {
        public GameV2 CreateGame(LevelDefinitionV2 level, EnemyServiceV2 enemyService, RuleHandlerRegistryV2 ruleHandlers)
        {
            return new GameV2(level, enemyService, ruleHandlers);
        }

        /// <summary>
        /// Loads each enemy entry from Addressables (per row on <see cref="LevelDefinitionV2.EnemyEntries"/>),
        /// then asks <see cref="GameV2"/> to instantiate that many copies. Releases each load handle after spawning.
        /// </summary>
        public async Task PrepareAndSpawnEnemiesFromLevelAsync(GameV2 game, Vector3 origin, float spacingPerIndex)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            LevelDefinitionV2 level = game.Level;
            IReadOnlyList<LevelEnemySpawnEntryV2> entries = level.EnemyEntries;
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                LevelEnemySpawnEntryV2 entry = entries[i];
                if (entry == null || entry.Count <= 0)
                {
                    continue;
                }

                AsyncOperationHandle<EnemyActor> handle = entry.EnemyAssetReference.LoadAssetAsync<EnemyActor>();
                try
                {
                    await handle.Task;
                    if (handle.Status != AsyncOperationStatus.Succeeded)
                    {
                        throw new InvalidOperationException($"Failed to load enemy prefab for level entry {i}.");
                    }

                    EnemyActor prefab = handle.Result;
                    game.SpawnEnemyCopies(prefab, entry.Count, origin, spacingPerIndex);
                }
                finally
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
            }
        }
    }
}
