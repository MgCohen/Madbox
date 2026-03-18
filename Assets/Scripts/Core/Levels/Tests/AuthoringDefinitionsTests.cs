using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Madbox.Addressables.Contracts;
using Madbox.Levels;
using Madbox.Levels.Authoring.Catalog;
using Madbox.Levels.Authoring.Definitions;
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
            ConfigureEnemy(enemy, "bee", 25);

            EnemyDefinition mapped = enemy.ToDomain();

            Assert.AreEqual("bee", mapped.EnemyTypeId.Value);
            Assert.AreEqual(25, mapped.MaxHealth);
            Assert.AreEqual(2, mapped.Behaviors.Count);
        }

        [Test]
        public void LevelDefinitionSO_ToDomain_MapsEntriesAndRules()
        {
            EnemyDefinitionSO enemy = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            ConfigureEnemy(enemy, "bee", 20);
            LevelEnemyEntrySO entry = CreateLevelEntry(enemy, 2);
            LevelDefinitionSO level = ScriptableObject.CreateInstance<LevelDefinitionSO>();
            ConfigureLevel(level, "level-1", 15, new List<LevelEnemyEntrySO> { entry }, useTimeLimit: true, loseAfterSeconds: 30f);

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
            SetPrivateField(catalog, "levels", new List<LevelCatalogEntry>
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

        private void ConfigureEnemy(EnemyDefinitionSO enemy, string enemyId, int maxHealth)
        {
            SetPrivateField(enemy, "enemyTypeId", enemyId);
            SetPrivateField(enemy, "maxHealth", maxHealth);
            SetPrivateField(enemy, "behaviorRules", new List<EnemyBehaviorDefinition>
            {
                new MovementBehaviorDefinition(1f, 2f),
                new ContactAttackBehaviorDefinition(3, 1f, 0.7f)
            });
        }

        private LevelEnemyEntrySO CreateLevelEntry(EnemyDefinitionSO enemy, int count)
        {
            LevelEnemyEntrySO entry = new LevelEnemyEntrySO();
            SetPrivateField(entry, "enemy", enemy);
            SetPrivateField(entry, "count", count);
            return entry;
        }

        private void ConfigureLevel(
            LevelDefinitionSO level,
            string levelId,
            int goldReward,
            List<LevelEnemyEntrySO> enemies,
            bool useTimeLimit,
            float loseAfterSeconds)
        {
            SetPrivateField(level, "levelId", levelId);
            SetPrivateField(level, "goldReward", goldReward);
            SetPrivateField(level, "enemies", enemies);
            SetPrivateField(level, "useTimeLimitLoseRule", useTimeLimit);
            SetPrivateField(level, "loseAfterSeconds", loseAfterSeconds);
        }

        private LevelCatalogEntry CreateCatalogEntry(string levelId, AssetReferenceT<LevelDefinitionSO> reference)
        {
            LevelCatalogEntry entry = new LevelCatalogEntry();
            SetPrivateField(entry, "levelId", levelId);
            SetPrivateField(entry, "levelReference", reference);
            return entry;
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().Name, fieldName);
            }

            field.SetValue(target, value);
        }

        private sealed class RecordingGateway : IAddressablesGateway
        {
            public int LoadCount { get; private set; }

            public AssetReferenceT<LevelDefinitionSO> LastReference { get; private set; }

            public System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken = default)
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }

            public System.Threading.Tasks.Task<IAssetHandle<T>> LoadAsync<T>(AssetKey key, CancellationToken cancellationToken = default)
                where T : UnityEngine.Object
            {
                throw new NotSupportedException();
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
        }

        private sealed class TestHandle<T> : IAssetHandle<T>
            where T : UnityEngine.Object
        {
            public TestHandle(T asset)
            {
                Asset = asset;
            }

            public string Id => "test";

            public Type AssetType => typeof(T);

            public UnityEngine.Object UntypedAsset => Asset;

            public T Asset { get; }

            public bool IsReleased { get; private set; }

            public void Release()
            {
                IsReleased = true;
            }
        }
    }
}
