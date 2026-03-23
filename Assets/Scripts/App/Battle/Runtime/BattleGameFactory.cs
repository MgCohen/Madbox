using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Madbox.Enemies;
using Madbox.Enemies;
using Madbox.Levels;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Madbox.Battle
{
    public sealed class BattleGameFactory
    {
        public BattleGame CreateGame(LevelDefinition level, EnemyService enemyService, RuleHandlerRegistry ruleHandlers)
        {
            return new BattleGame(level, enemyService, ruleHandlers);
        }

        public async Task PrepareAndSpawnEnemiesFromLevelAsync(BattleGame game, Vector3 origin, float spacingPerIndex)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            IReadOnlyList<LevelEnemySpawnEntry> entries = game.Level.EnemyEntries;
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            await RunAllEnemyEntriesAsync(game, entries, origin, spacingPerIndex);
        }

        /// <summary>
        /// Builds domain state, spawns enemies from the level definition, and starts the session. Does not load scenes;
        /// load the level additively first (for example via Addressables or the app scene-flow service).
        /// </summary>
        public async Task<BattleGame> CreatePrepareStartAsync(
            LevelDefinition level,
            EnemyService enemyService,
            RuleHandlerRegistry ruleRegistry,
            Vector3 enemySpawnOrigin,
            float enemySpacingPerIndex)
        {
            BattleGame game = CreateGame(level, enemyService, ruleRegistry);
            await PrepareAndSpawnEnemiesFromLevelAsync(game, enemySpawnOrigin, enemySpacingPerIndex);
            game.Start();
            return game;
        }

        private async Task RunAllEnemyEntriesAsync(BattleGame game, IReadOnlyList<LevelEnemySpawnEntry> entries, Vector3 origin, float spacingPerIndex)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                await TrySpawnFromEntryAsync(game, entries[i], i, origin, spacingPerIndex);
            }
        }

        private async Task TrySpawnFromEntryAsync(BattleGame game, LevelEnemySpawnEntry entry, int entryIndex, Vector3 origin, float spacingPerIndex)
        {
            if (entry == null || entry.Count <= 0)
            {
                return;
            }

            await SpawnFromLoadedEntryAsync(game, entry, entryIndex, origin, spacingPerIndex);
        }

        private async Task SpawnFromLoadedEntryAsync(BattleGame game, LevelEnemySpawnEntry entry, int entryIndex, Vector3 origin, float spacingPerIndex)
        {
            AsyncOperationHandle<EnemyData> handle = entry.EnemyAssetReference.LoadAssetAsync<EnemyData>();
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

        private async Task CompleteLoadAndSpawnForEntryAsync(BattleGame game, LevelEnemySpawnEntry entry, int entryIndex, Vector3 origin, float spacingPerIndex, AsyncOperationHandle<EnemyData> handle)
        {
            await handle.Task;
            ThrowIfEnemyLoadFailed(handle, entryIndex);
            game.SpawnEnemyCopies(handle.Result, entry.Count, origin, spacingPerIndex);
        }

        private void ThrowIfEnemyLoadFailed(AsyncOperationHandle<EnemyData> handle, int entryIndex)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException($"Failed to load enemy prefab for level entry {entryIndex}.");
            }
        }
    }
}
