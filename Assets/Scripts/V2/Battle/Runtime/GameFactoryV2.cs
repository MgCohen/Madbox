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

        public async Task PrepareAndSpawnEnemiesFromLevelAsync(GameV2 game, Vector3 origin, float spacingPerIndex)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            IReadOnlyList<LevelEnemySpawnEntryV2> entries = game.Level.EnemyEntries;
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            await RunAllEnemyEntriesAsync(game, entries, origin, spacingPerIndex);
        }

        private async Task RunAllEnemyEntriesAsync(GameV2 game, IReadOnlyList<LevelEnemySpawnEntryV2> entries, Vector3 origin, float spacingPerIndex)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                await TrySpawnFromEntryAsync(game, entries[i], i, origin, spacingPerIndex);
            }
        }

        private async Task TrySpawnFromEntryAsync(GameV2 game, LevelEnemySpawnEntryV2 entry, int entryIndex, Vector3 origin, float spacingPerIndex)
        {
            if (entry == null || entry.Count <= 0)
            {
                return;
            }

            await SpawnFromLoadedEntryAsync(game, entry, entryIndex, origin, spacingPerIndex);
        }

        private async Task SpawnFromLoadedEntryAsync(GameV2 game, LevelEnemySpawnEntryV2 entry, int entryIndex, Vector3 origin, float spacingPerIndex)
        {
            AsyncOperationHandle<EnemyActor> handle = entry.EnemyAssetReference.LoadAssetAsync<EnemyActor>();
            try
            {
                await CompleteLoadAndSpawnForEntryAsync(game, entry, entryIndex, origin, spacingPerIndex, handle);
            }
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }

        private async Task CompleteLoadAndSpawnForEntryAsync(GameV2 game, LevelEnemySpawnEntryV2 entry, int entryIndex, Vector3 origin, float spacingPerIndex, AsyncOperationHandle<EnemyActor> handle)
        {
            await handle.Task;
            ThrowIfEnemyLoadFailed(handle, entryIndex);
            game.SpawnEnemyCopies(handle.Result, entry.Count, origin, spacingPerIndex);
        }

        private void ThrowIfEnemyLoadFailed(AsyncOperationHandle<EnemyActor> handle, int entryIndex)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException($"Failed to load enemy prefab for level entry {entryIndex}.");
            }
        }
    }
}
