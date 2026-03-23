using System;
using System.Threading.Tasks;
using Madbox.Enemies;
using Madbox.Levels;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Madbox.Battle
{
    public sealed class BattleBootstrap
    {
        private readonly BattleGameFactory gameFactory;

        public BattleBootstrap()
            : this(new BattleGameFactory())
        {
        }

        public BattleBootstrap(BattleGameFactory gameFactory)
        {
            this.gameFactory = gameFactory ?? throw new ArgumentNullException(nameof(gameFactory));
        }

        public async Task<BattleBootstrapResult> StartBattleAsync(
            LevelDefinition level,
            EnemyService enemyService,
            RuleHandlerRegistry ruleRegistry,
            Vector3 enemySpawnOrigin,
            float enemySpacingPerIndex,
            LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            GuardStartBattle(level, enemyService, ruleRegistry);

            AsyncOperationHandle<SceneInstance> sceneHandle = Addressables.LoadSceneAsync(level.SceneAssetReference, loadSceneMode);
            await sceneHandle.Task;
            ThrowIfSceneLoadFailed(sceneHandle);

            BattleGame game = gameFactory.CreateGame(level, enemyService, ruleRegistry);
            await gameFactory.PrepareAndSpawnEnemiesFromLevelAsync(game, enemySpawnOrigin, enemySpacingPerIndex);
            game.Start();
            return new BattleBootstrapResult(game, sceneHandle);
        }

        private static void GuardStartBattle(LevelDefinition level, EnemyService enemyService, RuleHandlerRegistry ruleRegistry)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (enemyService == null)
            {
                throw new ArgumentNullException(nameof(enemyService));
            }

            if (ruleRegistry == null)
            {
                throw new ArgumentNullException(nameof(ruleRegistry));
            }
        }

        private static void ThrowIfSceneLoadFailed(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            if (sceneHandle.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException("Addressables scene load failed for LevelDefinition.SceneAssetReference.");
            }
        }
    }
}
