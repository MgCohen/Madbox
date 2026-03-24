using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.Battle;
using Madbox.Entities;
using Madbox.Enemies;
using Madbox.Levels;
using Madbox.Levels.Rules;
using Madbox.Players;
using Madbox.SceneFlow;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Battle.Tests
{
    public class BattleGameTests
    {
        [Test]
        public void SpawnEnemyCopies_WithEnemySpawnParent_ParentsSpawnedEnemiesUnderTransform()
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            SetPrivateField(level, "sceneAssetReference", CreateSceneReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
            SetPrivateField(level, "enemyEntries", new List<LevelEnemySpawnEntry>());
            SetPrivateField(level, "gameRules", new List<LevelRuleDefinition>());

            RuleHandlerRegistry ruleRegistry = new RuleHandlerRegistry();
            EnemyService enemyService = new EnemyService(new EnemyFactory());
            GameObject parentGo = new GameObject("EnemySpawnRoot");
            BattleGame game = new BattleGame(level, enemyService, ruleRegistry, parentGo.transform);

            Enemy prefab = CreateEnemyPrefab();
            game.SpawnEnemyCopies(prefab, 2, Vector3.zero, 2f);

            Assert.AreEqual(2, enemyService.AliveEnemies);
            foreach (Enemy alive in enemyService.GetAllAlive())
            {
                Assert.AreEqual(parentGo.transform, alive.transform.parent);
            }

            foreach (Enemy alive in enemyService.GetAllAlive())
            {
                UnityEngine.Object.DestroyImmediate(alive.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(level);
            UnityEngine.Object.DestroyImmediate(prefab.gameObject);
            UnityEngine.Object.DestroyImmediate(parentGo);
        }

        [Test]
        public void SetSessionPlayer_StoresReferenceForUiRouting()
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            SetPrivateField(level, "sceneAssetReference", CreateSceneReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
            SetPrivateField(level, "enemyEntries", new List<LevelEnemySpawnEntry>());
            SetPrivateField(level, "gameRules", new List<LevelRuleDefinition>());

            RuleHandlerRegistry ruleRegistry = new RuleHandlerRegistry();
            EnemyService enemyService = new EnemyService(new EnemyFactory());
            BattleGame game = new BattleGame(level, enemyService, ruleRegistry);

            GameObject playerGo = new GameObject("SessionPlayer");
            Player player = playerGo.AddComponent<Player>();
            game.SetSessionPlayer(player);

            Assert.AreSame(player, game.SessionPlayer);

            UnityEngine.Object.DestroyImmediate(playerGo);
            UnityEngine.Object.DestroyImmediate(level);
        }

        [Test]
        public void PrepareAndSpawnEnemiesFromLevelAsync_WithMultipleEntries_SpawnsAtDistinctPositions()
        {
            AssetReference enemyRefA = new AssetReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            AssetReference enemyRefB = new AssetReference("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            LevelEnemySpawnEntry entryA = new LevelEnemySpawnEntry();
            LevelEnemySpawnEntry entryB = new LevelEnemySpawnEntry();
            SetPrivateField(entryA, "enemyAssetReference", enemyRefA);
            SetPrivateField(entryA, "count", 1);
            SetPrivateField(entryB, "enemyAssetReference", enemyRefB);
            SetPrivateField(entryB, "count", 1);

            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            SetPrivateField(level, "sceneAssetReference", CreateSceneReference("cccccccccccccccccccccccccccccccc"));
            SetPrivateField(level, "enemyEntries", new List<LevelEnemySpawnEntry> { entryA, entryB });
            SetPrivateField(level, "gameRules", new List<LevelRuleDefinition>());

            EnemyService enemyService = new EnemyService(new EnemyFactory());
            RuleHandlerRegistry ruleRegistry = new RuleHandlerRegistry();
            BattleGame game = new BattleGame(level, enemyService, ruleRegistry);

            GameObject prefabRootA = new GameObject("EnemyPrefabA");
            GameObject prefabRootB = new GameObject("EnemyPrefabB");
            Enemy prefabA = prefabRootA.AddComponent<Enemy>();
            Enemy prefabB = prefabRootB.AddComponent<Enemy>();
            prefabRootA.AddComponent<EnemyMoveForwardBehaviour>();
            prefabRootB.AddComponent<EnemyMoveForwardBehaviour>();

            FakeAddressablesGateway gateway = new FakeAddressablesGateway();
            gateway.Register(enemyRefA, prefabRootA);
            gateway.Register(enemyRefB, prefabRootB);
            BattleGameFactory factory = new BattleGameFactory(gateway);
            var sessionAddressableHandles = new List<IAssetHandle>();

            try
            {
                factory.PrepareAndSpawnEnemiesFromLevelAsync(game, Vector3.zero, 2f, sessionAddressableHandles).GetAwaiter().GetResult();

                List<Enemy> spawned = new List<Enemy>(enemyService.GetAllAlive());
                Assert.AreEqual(2, spawned.Count);
                CollectionAssert.AreEquivalent(
                    new[] { "EnemyPrefabA(Clone)", "EnemyPrefabB(Clone)" },
                    new[] { spawned[0].name, spawned[1].name });
            }
            finally
            {
                foreach (Enemy alive in enemyService.GetAllAlive())
                {
                    UnityEngine.Object.DestroyImmediate(alive.gameObject);
                }

                for (int i = 0; i < sessionAddressableHandles.Count; i++)
                {
                    IAssetHandle handle = sessionAddressableHandles[i];
                    if (handle != null && !handle.IsReleased)
                    {
                        handle.Release();
                    }
                }

                UnityEngine.Object.DestroyImmediate(prefabRootA);
                UnityEngine.Object.DestroyImmediate(prefabRootB);
                UnityEngine.Object.DestroyImmediate(level);
            }
        }

        private static Enemy CreateEnemyPrefab()
        {
            GameObject go = new GameObject("EnemyPrefab");
            go.AddComponent<EnemyMoveForwardBehaviour>();
            return go.AddComponent<Enemy>();
        }

        private static AssetReference CreateSceneReference(string guid)
        {
            return new AssetReference(guid);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        [Test]
        public void CreatePrepareStartAfterAdditiveSceneLoadAsync_WithoutSessionCollaborators_ThrowsInvalidOperationException()
        {
            BattleGameFactory factory = new BattleGameFactory(new NullAddressablesGateway());
            SceneFlowLoadResult loadResult = new SceneFlowLoadResult(Guid.NewGuid(), "NonExistentScene", false);
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                Task<BattleGame> task = factory.CreatePrepareStartAfterAdditiveSceneLoadAsync(loadResult, level, new List<IAssetHandle>());
                InvalidOperationException caught = null;
                try
                {
                    task.GetAwaiter().GetResult();
                }
                catch (InvalidOperationException ex)
                {
                    caught = ex;
                }

                Assert.IsNotNull(caught);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void CreatePrepareStartAsync_WithoutPlayerFactory_ThrowsInvalidOperationException()
        {
            BattleGameFactory factory = new BattleGameFactory(new NullAddressablesGateway());
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            SetPrivateField(level, "sceneAssetReference", CreateSceneReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
            SetPrivateField(level, "enemyEntries", new List<LevelEnemySpawnEntry>());
            SetPrivateField(level, "gameRules", new List<LevelRuleDefinition>());

            EnemyService enemyService = new EnemyService(new EnemyFactory());
            RuleHandlerRegistry ruleRegistry = new RuleHandlerRegistry();
            GameObject root = new GameObject("SessionWorld");
            try
            {
                MethodInfo createPrepareStart = GetCreatePrepareStartAsyncMethod();
                Exception thrown = null;
                try
                {
                    object taskObject = createPrepareStart.Invoke(
                        factory,
                        new object[]
                        {
                            level,
                            enemyService,
                            ruleRegistry,
                            null,
                            root.transform,
                            null,
                            2f,
                            new List<IAssetHandle>(),
                            CancellationToken.None
                        });
                    ((Task)taskObject).GetAwaiter().GetResult();
                }
                catch (TargetInvocationException ex)
                {
                    thrown = ex.InnerException;
                }
                catch (Exception ex)
                {
                    thrown = ex;
                }

                InvalidOperationException io = thrown as InvalidOperationException
                    ?? (thrown as AggregateException)?.InnerException as InvalidOperationException;
                Assert.IsNotNull(io);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(level);
            }
        }

        private static MethodInfo GetCreatePrepareStartAsyncMethod()
        {
            foreach (MethodInfo method in typeof(BattleGameFactory).GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name != nameof(BattleGameFactory.CreatePrepareStartAsync))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 9)
                {
                    return method;
                }
            }

            throw new InvalidOperationException(
                "Expected BattleGameFactory.CreatePrepareStartAsync(LevelDefinition, EnemyService, RuleHandlerRegistry, Arena, Transform, Transform, float, IList<IAssetHandle>, CancellationToken).");
        }

        [Test]
        public void WireEnemyInputProviders_WhenEnemyHasContextProvider_AssignsSpawnedPlayerRoot()
        {
            GameObject enemyGo = new GameObject("Enemy");
            GameObject playerGo = new GameObject("Player");
            try
            {
                Enemy enemy = enemyGo.AddComponent<Enemy>();
                EnemyFrameContextProvider provider = new EnemyFrameContextProvider();
                enemy.Initialize();

                EnemyService enemyService = new EnemyService(new EnemyFactory());
                bool registered = enemyService.Register(enemy);
                Assert.IsTrue(registered, "Expected initialized enemy to register.");

                Player player = playerGo.AddComponent<Player>();

                MethodInfo wire = typeof(BattleGameFactory).GetMethod("WireEnemyInputProviders", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(wire, "Expected private WireEnemyInputProviders helper.");

                wire.Invoke(null, new object[] { enemyService, player, provider });

                EnemyInputContext input = provider.GetFrameInput();
                Assert.AreSame(player, input.PlayerData);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerGo);
                UnityEngine.Object.DestroyImmediate(enemyGo);
            }
        }

        private sealed class NullAddressablesGateway : IAddressablesGateway
        {
            public Task InitializeAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<IAssetGroupHandle<T>> LoadAsync<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new System.NotSupportedException();
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new System.NotSupportedException();
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new System.NotSupportedException();
            }

            public IAssetGroupHandle<T> Load<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new System.NotSupportedException();
            }

            public IAssetHandle<T> Load<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new System.NotSupportedException();
            }

            public IAssetHandle<T> Load<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new System.NotSupportedException();
            }
        }

        private sealed class FakeAddressablesGateway : IAddressablesGateway
        {
            private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

            public Task InitializeAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<IAssetGroupHandle<T>> LoadAsync<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (typeof(T) != typeof(GameObject))
                {
                    throw new NotSupportedException();
                }

                string key = reference.RuntimeKey.ToString();
                if (!prefabs.TryGetValue(key, out GameObject prefab))
                {
                    throw new InvalidOperationException("Missing fake prefab for key '" + key + "'.");
                }

                IAssetHandle<T> handle = (IAssetHandle<T>)(object)new ImmediateHandle<GameObject>(prefab);
                return Task.FromResult(handle);
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                return LoadAsync<T>((AssetReference)reference, cancellationToken);
            }

            public IAssetGroupHandle<T> Load<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public IAssetHandle<T> Load<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public IAssetHandle<T> Load<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public void Register(AssetReference reference, GameObject prefab)
            {
                string key = reference.RuntimeKey.ToString();
                prefabs[key] = prefab;
            }
        }

        private sealed class ImmediateHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
        {
            public ImmediateHandle(T assetValue)
            {
                asset = assetValue;
            }

            private readonly T asset;
            private bool released;

            public Type AssetType => typeof(T);

            public UnityEngine.Object UntypedAsset => asset;

            public bool IsReleased => released;

            public AssetHandleState State => released ? AssetHandleState.Released : AssetHandleState.Ready;

            public bool IsReady => true;

            public Task WhenReady => Task.CompletedTask;

            public T Asset => asset;

            public void Release()
            {
                released = true;
            }
        }

    }
}
