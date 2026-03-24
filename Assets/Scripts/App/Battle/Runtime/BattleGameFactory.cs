using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.Entities;
using Madbox.App.GameView.Arenas;
using Madbox.App.GameView.Input;
using Madbox.Enemies;
using Madbox.Levels;
using Madbox.Players;
using Madbox.SceneFlow;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Madbox.Battle
{
    public sealed class BattleGameFactory
    {
        private const float DefaultEnemySpacingPerIndex = 4f;

        /// <summary>Minimum horizontal distance between enemy spawn positions and the player spawn point (doubled from 4f).</summary>
        private const float MinEnemySpawnDistanceFromPlayer = 8f;

        public BattleGameFactory(
            IAddressablesGateway addressablesGateway,
            PlayerFactory playerFactory = null,
            EnemyService enemyService = null,
            RuleHandlerRegistry ruleRegistry = null)
        {
            this.addressablesGateway = addressablesGateway ?? throw new ArgumentNullException(nameof(addressablesGateway));
            this.playerFactory = playerFactory;
            this.enemyService = enemyService;
            this.ruleRegistry = ruleRegistry;
        }

        private readonly IAddressablesGateway addressablesGateway;

        private readonly PlayerFactory playerFactory;

        private readonly EnemyService enemyService;

        private readonly RuleHandlerRegistry ruleRegistry;

        public BattleGame CreateGame(LevelDefinition level, EnemyService enemyService, RuleHandlerRegistry ruleHandlers) =>
            new BattleGame(level ?? throw new ArgumentNullException(nameof(level)), enemyService, ruleHandlers);

        /// <summary>
        /// After an additive level scene load, creates session world root, resolves arena spawn data, then runs
        /// <see cref="CreatePrepareStartAsync"/>.
        /// </summary>
        public async Task<BattleGame> CreatePrepareStartAfterAdditiveSceneLoadAsync(
            SceneFlowLoadResult loadResult,
            LevelDefinition level,
            IList<IAssetHandle> sessionAddressableHandles,
            Transform gameViewRoot = null,
            CancellationToken cancellationToken = default)
        {
            if (enemyService == null || ruleRegistry == null)
            {
                throw new InvalidOperationException($"{nameof(BattleGameFactory)} requires {nameof(enemyService)} and {nameof(ruleRegistry)} for additive session setup.");
            }

            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (sessionAddressableHandles == null)
            {
                throw new ArgumentNullException(nameof(sessionAddressableHandles));
            }

            Scene levelScene = SceneManager.GetSceneByName(loadResult.SceneName);
            if (!levelScene.IsValid() || !levelScene.isLoaded)
            {
                throw new InvalidOperationException($"Loaded scene '{loadResult.SceneName}' is not available on SceneManager.");
            }

            Arena.TryFindInScene(levelScene, out Arena arena);
            GameObject worldRoot = new GameObject("SessionWorld");
            SceneManager.MoveGameObjectToScene(worldRoot, levelScene);
            return await CreatePrepareStartAsync(
                level,
                enemyService,
                ruleRegistry,
                arena,
                worldRoot.transform,
                gameViewRoot,
                DefaultEnemySpacingPerIndex,
                sessionAddressableHandles,
                cancellationToken);
        }

        /// <summary>
        /// Spawns enemies and the player from the level, wires input/cameras, then starts the battle.
        /// </summary>
        public async Task<BattleGame> CreatePrepareStartAsync(
            LevelDefinition level,
            EnemyService enemyService,
            RuleHandlerRegistry ruleRegistry,
            Arena arena,
            Transform sessionWorldRoot,
            Transform gameViewRoot,
            float enemySpacingPerIndex,
            IList<IAssetHandle> sessionAddressableHandles,
            CancellationToken cancellationToken = default)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (playerFactory == null)
            {
                throw new InvalidOperationException($"{nameof(BattleGameFactory)} requires {nameof(PlayerFactory)} for session orchestration.");
            }

            if (sessionWorldRoot == null)
            {
                throw new ArgumentNullException(nameof(sessionWorldRoot));
            }

            if (sessionAddressableHandles == null)
            {
                throw new ArgumentNullException(nameof(sessionAddressableHandles));
            }

            Vector3 enemySpawnOrigin = Vector3.zero;
            Vector3 playerPos = Vector3.zero;
            Quaternion playerRot = Quaternion.identity;
            Bounds? enemySpawnBounds = null;
            if (arena != null)
            {
                enemySpawnOrigin = arena.EnemySpawnWorldPosition;
                playerPos = arena.PlayerSpawnWorldPosition;
                playerRot = arena.transform.rotation;
                if (arena.TryGetWorldBounds(out Bounds arenaBounds))
                {
                    enemySpawnBounds = arenaBounds;
                }
            }

            BattleGame game = new BattleGame(
                level,
                enemyService,
                ruleRegistry,
                sessionWorldRoot,
                enemySpawnBounds,
                playerPos,
                MinEnemySpawnDistanceFromPlayer);
            EnemyFrameContextProvider sharedEnemyFrameContext = new EnemyFrameContextProvider();
            await PrepareAndSpawnEnemiesFromLevelAsync(game, enemySpawnOrigin, enemySpacingPerIndex, sessionAddressableHandles, cancellationToken);

            Player player = await playerFactory.CreateReadyPlayerAsync(sessionWorldRoot, playerPos, playerRot, sessionAddressableHandles, cancellationToken);
            game.SetSessionPlayer(player);
            IEntityFrameInputProvider<PlayerInputContext> inputProvider = ResolvePlayerInputProvider(arena, gameViewRoot, sessionWorldRoot);
            PlayerBehaviorRunner runner = player.GetComponentInChildren<PlayerBehaviorRunner>(true);
            if (runner != null && inputProvider != null)
            {
                runner.AssignInputProvider(inputProvider);
            }

            WireEnemyInputProviders(enemyService, player, sharedEnemyFrameContext);

            IReadOnlyList<CinemachineVirtualCamera> battleCameras = ResolveBattleCameras(arena, gameViewRoot, sessionWorldRoot);
            for (int i = 0; i < battleCameras.Count; i++)
            {
                CinemachineVirtualCamera vcam = battleCameras[i];
                if (vcam == null)
                {
                    continue;
                }

                    vcam.Follow = player.transform;
                    vcam.LookAt = player.transform;
                vcam.PreviousStateIsValid = false;
            }

            game.Start();
            return game;
        }

        public async Task PrepareAndSpawnEnemiesFromLevelAsync(
            BattleGame game,
            Vector3 origin,
            float spacingPerIndex,
            IList<IAssetHandle> sessionAddressableHandles,
            CancellationToken cancellationToken = default)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            if (sessionAddressableHandles == null)
            {
                throw new ArgumentNullException(nameof(sessionAddressableHandles));
            }

            IReadOnlyList<LevelEnemySpawnEntry> entries = game.Level.EnemyEntries;
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            await RunAllEnemyEntriesAsync(game, entries, origin, spacingPerIndex, sessionAddressableHandles, cancellationToken);
        }

        private async Task RunAllEnemyEntriesAsync(
            BattleGame game,
            IReadOnlyList<LevelEnemySpawnEntry> entries,
            Vector3 origin,
            float spacingPerIndex,
            IList<IAssetHandle> sessionAddressableHandles,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                await TrySpawnFromEntryAsync(game, entries[i], i, origin, spacingPerIndex, sessionAddressableHandles, cancellationToken);
            }
        }

        private async Task TrySpawnFromEntryAsync(
            BattleGame game,
            LevelEnemySpawnEntry entry,
            int entryIndex,
            Vector3 origin,
            float spacingPerIndex,
            IList<IAssetHandle> sessionAddressableHandles,
            CancellationToken cancellationToken)
        {
            if (entry == null || entry.Count <= 0)
            {
                return;
            }

            await SpawnFromLoadedEntryAsync(game, entry, entryIndex, origin, spacingPerIndex, sessionAddressableHandles, cancellationToken);
        }

        private async Task SpawnFromLoadedEntryAsync(
            BattleGame game,
            LevelEnemySpawnEntry entry,
            int entryIndex,
            Vector3 origin,
            float spacingPerIndex,
            IList<IAssetHandle> sessionAddressableHandles,
            CancellationToken cancellationToken)
        {
            IAssetHandle<GameObject> handle = await addressablesGateway.LoadAsync<GameObject>(entry.EnemyAssetReference, cancellationToken);
            sessionAddressableHandles.Add(handle);
            GameObject prefabRoot = handle.Asset;
            Enemy enemyPrefab = FromEnemyPrefab(prefabRoot, entryIndex);
            game.SpawnEnemyCopies(enemyPrefab, entry.Count, origin, spacingPerIndex);
        }

        private Enemy FromEnemyPrefab(GameObject prefabRoot, int entryIndex)
        {
            if (prefabRoot == null)
            {
                throw new InvalidOperationException($"Enemy load failed for entry {entryIndex} (null asset).");
            }

            Enemy enemyPrefab = prefabRoot.GetComponentInChildren<Enemy>(true);
            if (enemyPrefab == null)
            {
                throw new InvalidOperationException($"Prefab for entry {entryIndex} has no {nameof(Enemy)}.");
            }

            return enemyPrefab;
        }

        private static IReadOnlyList<CinemachineVirtualCamera> ResolveBattleCameras(Arena arena, Transform gameViewRoot, Transform sessionWorldRoot)
        {
            var result = new List<CinemachineVirtualCamera>(4);
            var seen = new HashSet<int>();

            static void AddIfUnique(List<CinemachineVirtualCamera> list, HashSet<int> ids, CinemachineVirtualCamera camera)
            {
                if (camera == null)
                {
                    return;
                }

                int id = camera.GetInstanceID();
                if (ids.Add(id))
                {
                    list.Add(camera);
                }
            }

            if (arena?.BattleVirtualCamera != null)
            {
                AddIfUnique(result, seen, arena.BattleVirtualCamera);
            }

            CinemachineVirtualCamera[] arenaCameras = arena != null ? arena.GetComponentsInChildren<CinemachineVirtualCamera>(true) : null;
            if (arenaCameras != null)
            {
                for (int i = 0; i < arenaCameras.Length; i++)
                {
                    AddIfUnique(result, seen, arenaCameras[i]);
                }
            }

            CinemachineVirtualCamera[] gameViewCameras = gameViewRoot != null ? gameViewRoot.GetComponentsInChildren<CinemachineVirtualCamera>(true) : null;
            if (gameViewCameras != null)
            {
                for (int i = 0; i < gameViewCameras.Length; i++)
                {
                    AddIfUnique(result, seen, gameViewCameras[i]);
                }
            }

            CinemachineVirtualCamera[] sessionCameras = sessionWorldRoot != null ? sessionWorldRoot.GetComponentsInChildren<CinemachineVirtualCamera>(true) : null;
            if (sessionCameras != null)
            {
                for (int i = 0; i < sessionCameras.Length; i++)
                {
                    AddIfUnique(result, seen, sessionCameras[i]);
                }
            }

            return result;
        }

        private static IEntityFrameInputProvider<PlayerInputContext> ResolvePlayerInputProvider(Arena arena, Transform gameViewRoot, Transform sessionWorldRoot)
        {
            PlayerInputProvider inputProvider =
                gameViewRoot?.GetComponentInChildren<PlayerInputProvider>(true)
                ?? arena?.GetComponentInChildren<PlayerInputProvider>(true)
                ?? sessionWorldRoot?.GetComponentInChildren<PlayerInputProvider>(true)
                ?? UnityEngine.Object.FindAnyObjectByType<PlayerInputProvider>(FindObjectsInactive.Include);
            return inputProvider;
        }

        private static void WireEnemyInputProviders(EnemyService enemyService, Player player, EnemyFrameContextProvider sharedContext)
        {
            if (enemyService == null || player == null || sharedContext == null)
            {
                return;
            }

            sharedContext.SetPlayerRoot(player.gameObject);

            IReadOnlyCollection<Enemy> aliveEnemies = enemyService.GetAllAlive();
            foreach (Enemy enemy in aliveEnemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                EnemyBehaviorRunner behaviorRunner = enemy.GetComponentInChildren<EnemyBehaviorRunner>(true);
                if (behaviorRunner != null)
                {
                    behaviorRunner.AssignInputProvider(sharedContext);
                }
            }
        }
    }
}
