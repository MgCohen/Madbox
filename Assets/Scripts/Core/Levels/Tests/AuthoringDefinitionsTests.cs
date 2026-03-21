using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Madbox.Addressables.Contracts;
using Madbox.Levels;
using Madbox.Levels.Authoring.Catalog;
using Madbox.Levels.Authoring.Definitions;
using Madbox.Enemies.Authoring.Definitions;
using Madbox.Levels.Behaviors;
using Madbox.Levels.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
#pragma warning disable SCA0003
#pragma warning disable SCA0005
#pragma warning disable SCA0006

namespace Madbox.Levels.Tests
{
    public sealed class AuthoringDefinitionsTests
    {
        [Test]
        public void EnemyDefinitionSO_ToDomain_MapsSerializedFields()
        {
            EnemyDefinitionSO enemy = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            BuildConfigureEnemy(enemy, "bee", 25);

            EnemyDefinition mapped = enemy.ToDomain();

            Assert.AreEqual("bee", mapped.EnemyTypeId.Value);
            Assert.AreEqual(25, mapped.MaxHealth);
            Assert.AreEqual(2, mapped.Behaviors.Count);
        }

        [Test]
        public void LevelDefinitionSO_ToDomain_MapsEntriesAndRules()
        {
            EnemyDefinitionSO enemy = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            BuildConfigureEnemy(enemy, "bee", 20);
            LevelEnemyEntrySO entry = CreateLevelEntry(enemy, 2);
            LevelDefinitionSO level = ScriptableObject.CreateInstance<LevelDefinitionSO>();
            BuildConfigureLevel(level, "level-1", 15, new List<LevelEnemyEntrySO> { entry }, useTimeLimit: true, loseAfterSeconds: 30f);

            LevelDefinition mapped = level.ToDomain();

            Assert.AreEqual("level-1", mapped.LevelId.Value);
            Assert.AreEqual(15, mapped.GoldReward);
            Assert.AreEqual(1, mapped.Enemies.Count);
            Assert.AreEqual(2, mapped.Enemies[0].Count);
            Assert.That(mapped.GameRules, Has.Some.InstanceOf<EnemyEliminatedWinRuleDefinition>());
            Assert.That(mapped.GameRules, Has.Some.InstanceOf<TimeLimitLoseRuleDefinition>());
            Assert.That(mapped.GameRules, Has.Some.InstanceOf<PlayerDefeatedLoseRuleDefinition>());
        }

        [Test]
        public void AddressableLevelDefinitionProvider_LoadAsync_UsesCatalogReference()
        {
            LevelCatalogSO catalog = ScriptableObject.CreateInstance<LevelCatalogSO>();
            BuildSetPrivateField(catalog, "levels", new List<LevelCatalogEntry>
            {
                CreateCatalogEntry("level-1", new AssetReferenceT<LevelDefinitionSO>("level-1"))
            });
            RecordingGateway gateway = new RecordingGateway();
            AddressableLevelDefinitionProvider provider = new AddressableLevelDefinitionProvider(catalog, gateway);

            IAssetHandle<LevelDefinitionSO> handle = provider.LoadAsync(new LevelId("level-1"), CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsNotNull(handle);
            Assert.AreEqual(1, gateway.LoadCount);
            Assert.AreEqual("level-1", gateway.LastReference.RuntimeKey.ToString());
        }

        [Test]
        public void LevelCatalogSO_TryGetLevelReference_WhenLevelsListIsNull_ReturnsFalse()
        {
            LevelCatalogSO catalog = ScriptableObject.CreateInstance<LevelCatalogSO>();
            BuildSetPrivateField(catalog, "levels", null);

            bool found = catalog.TryGetLevelReference("level-1", out AssetReferenceT<LevelDefinitionSO> reference);

            Assert.IsFalse(found);
            Assert.IsNull(reference);
        }

        [Test]
        public void EnemyBehaviorDefinitions_AreMarkedSerializable_ForSerializeReferenceSupport()
        {
            Assert.IsTrue(BuildIsSerializable(typeof(EnemyBehaviorDefinition)));
            Assert.IsTrue(BuildIsSerializable(typeof(MovementBehaviorDefinition)));
            Assert.IsTrue(BuildIsSerializable(typeof(ContactAttackBehaviorDefinition)));
        }

        private static void BuildConfigureEnemy(EnemyDefinitionSO enemy, string enemyId, int maxHealth)
        {
            BuildSetPrivateField(enemy, "enemyTypeId", enemyId);
            BuildSetPrivateField(enemy, "maxHealth", maxHealth);
            BuildSetPrivateField(enemy, "behaviorRules", new List<EnemyBehaviorDefinition>
            {
                new MovementBehaviorDefinition(1f, 2f),
                new ContactAttackBehaviorDefinition(3, 1f, 0.7f)
            });
        }

        private static LevelEnemyEntrySO CreateLevelEntry(EnemyDefinitionSO enemy, int count)
        {
            LevelEnemyEntrySO entry = new LevelEnemyEntrySO();
            BuildSetPrivateField(entry, "enemy", enemy);
            BuildSetPrivateField(entry, "count", count);
            return entry;
        }

        private static void BuildConfigureLevel(
            LevelDefinitionSO level,
            string levelId,
            int goldReward,
            List<LevelEnemyEntrySO> enemies,
            bool useTimeLimit,
            float loseAfterSeconds)
        {
            BuildSetPrivateField(level, "levelId", levelId);
            BuildSetPrivateField(level, "goldReward", goldReward);
            BuildSetPrivateField(level, "enemies", enemies);
            BuildSetPrivateField(level, "useTimeLimitLoseRule", useTimeLimit);
            BuildSetPrivateField(level, "loseAfterSeconds", loseAfterSeconds);
        }

        private static LevelCatalogEntry CreateCatalogEntry(string levelId, AssetReferenceT<LevelDefinitionSO> reference)
        {
            LevelCatalogEntry entry = new LevelCatalogEntry();
            BuildSetPrivateField(entry, "levelId", levelId);
            BuildSetPrivateField(entry, "levelReference", reference);
            return entry;
        }

        private static void BuildSetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().Name, fieldName);
            }

            field.SetValue(target, value);
        }

        private static bool BuildIsSerializable(Type type)
        {
            return type.IsDefined(typeof(SerializableAttribute), inherit: false);
        }

        private sealed class RecordingGateway : IAddressablesGateway
        {
            public int LoadCount { get; private set; }

            public AssetReferenceT<LevelDefinitionSO> LastReference { get; private set; }

            public System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken = default)
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }

            public System.Threading.Tasks.Task<IAssetGroupHandle<T>> LoadAsync<T>(AssetLabelReference label, CancellationToken cancellationToken = default)
                where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public System.Threading.Tasks.Task<IAssetHandle<T>> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default)
                where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public System.Threading.Tasks.Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default)
                where T : UnityEngine.Object
            {
                LoadCount++;
                if (reference is AssetReferenceT<LevelDefinitionSO> levelReference)
                {
                    LastReference = levelReference;
                }

                IAssetHandle<T> handle = new TestHandle<T>(ScriptableObject.CreateInstance<LevelDefinitionSO>() as T);
                return System.Threading.Tasks.Task.FromResult(handle);
            }

            public IAssetHandle<T> Load<T>(AssetReference reference, CancellationToken cancellationToken = default)
                where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public IAssetHandle<T> Load<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default)
                where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public IAssetGroupHandle<T> Load<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }
        }

        private sealed class TestHandle<T> : IAssetHandle<T>
            where T : UnityEngine.Object
        {
            public TestHandle(T asset)
            {
                Asset = asset;
            }

            public Type AssetType => typeof(T);

            public UnityEngine.Object UntypedAsset => Asset;

            public T Asset { get; }

            public bool IsReleased { get; private set; }

            public AssetHandleState State => IsReleased ? AssetHandleState.Released : AssetHandleState.Ready;

            public bool IsReady => !IsReleased;

            public System.Threading.Tasks.Task WhenReady => System.Threading.Tasks.Task.CompletedTask;

            public void Release()
            {
                IsReleased = true;
            }
        }
    }
}


