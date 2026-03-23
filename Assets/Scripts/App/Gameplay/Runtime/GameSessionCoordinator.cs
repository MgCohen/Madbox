using System;
using System.Threading;
using System.Threading.Tasks;
using Madbox.App.GameView.Arenas;
using Madbox.Battle;
using Madbox.Enemies;
using Madbox.SceneFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Madbox.App.Gameplay
{
    /// <summary>
    /// Loads the level scene additively, starts battle domain state, and spawns the player.
    /// </summary>
    public sealed class GameSessionCoordinator
    {
        public GameSessionCoordinator(
            ISceneFlowService sceneFlowService,
            BattleGameFactory battleGameFactory,
            Func<EnemyService> enemyServiceFactory,
            RuleHandlerRegistry ruleHandlerRegistry)
        {
            this.sceneFlowService = sceneFlowService ?? throw new ArgumentNullException(nameof(sceneFlowService));
            this.battleGameFactory = battleGameFactory ?? throw new ArgumentNullException(nameof(battleGameFactory));
            this.enemyServiceFactory = enemyServiceFactory ?? throw new ArgumentNullException(nameof(enemyServiceFactory));
            this.ruleHandlerRegistry = ruleHandlerRegistry ?? throw new ArgumentNullException(nameof(ruleHandlerRegistry));
        }

        private readonly ISceneFlowService sceneFlowService;
        private readonly BattleGameFactory battleGameFactory;
        private readonly Func<EnemyService> enemyServiceFactory;
        private readonly RuleHandlerRegistry ruleHandlerRegistry;

        private SceneFlowLoadResult activeSceneLoad;
        private BattleGame activeGame;
        private bool sceneLoadActive;

        public BattleGame ActiveGame => activeGame;

        public async Task RunSessionAsync(Madbox.Levels.LevelDefinition level, IPlayerSpawnService playerSpawn, CancellationToken cancellationToken = default)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (playerSpawn == null)
            {
                throw new ArgumentNullException(nameof(playerSpawn));
            }

            await TeardownSessionAsync(cancellationToken);

            SceneFlowLoadResult loadResult = await sceneFlowService.LoadAdditiveAsync(
                level.SceneAssetReference,
                SceneFlowLoadOptions.Default,
                cancellationToken);

            activeSceneLoad = loadResult;
            sceneLoadActive = true;

            try
            {
                Scene levelScene = SceneManager.GetSceneByName(loadResult.SceneName);
                if (!levelScene.IsValid() || !levelScene.isLoaded)
                {
                    throw new InvalidOperationException($"Loaded scene '{loadResult.SceneName}' is not available on SceneManager.");
                }

                Vector3 enemyOrigin = Vector3.zero;
                Vector3 playerPos = Vector3.zero;
                Quaternion playerRot = Quaternion.identity;
                if (Arena.TryFindInScene(levelScene, out Arena arena))
                {
                    enemyOrigin = arena.EnemySpawnWorldPosition;
                    playerPos = arena.PlayerSpawnWorldPosition;
                    Transform t = arena.transform;
                    playerRot = t.rotation;
                }

                EnemyService enemyService = enemyServiceFactory();
                const float enemySpacing = 2f;
                activeGame = await battleGameFactory.CreatePrepareStartAsync(
                    level,
                    enemyService,
                    ruleHandlerRegistry,
                    enemyOrigin,
                    enemySpacing);

                GameObject worldRoot = new GameObject("SessionWorld");
                SceneManager.MoveGameObjectToScene(worldRoot, levelScene);
                await playerSpawn.SpawnPlayerAtAsync(worldRoot.transform, playerPos, playerRot, cancellationToken);
            }
            catch
            {
                await TeardownSessionAsync(cancellationToken);
                throw;
            }
        }

        public async Task TeardownSessionAsync(CancellationToken cancellationToken = default)
        {
            activeGame = null;

            if (sceneLoadActive)
            {
                await sceneFlowService.UnloadAsync(activeSceneLoad, cancellationToken);
                sceneLoadActive = false;
            }
        }
    }
}
