using NUnit.Framework;
using UnityEngine;

namespace Madbox.V2.Enemies.Tests
{
    public class EnemySpawnerV2Tests
    {
        [Test]
        public void Spawn_CreatesInitializedEnemy_AndRegistersIt()
        {
            EnemyFactoryV2 factory = new EnemyFactoryV2();
            EnemyRuntimeRegistryV2 registry = new EnemyRuntimeRegistryV2();
            EnemySpawnerV2 spawner = new EnemySpawnerV2(factory, registry);
            EnemyActor prefab = CreateEnemyPrefab();
            EnemySpawnRequestV2 request = new EnemySpawnRequestV2(
                runtimeId: "enemy-runtime-001",
                teamId: 2,
                position: new Vector3(1f, 0f, 3f),
                rotation: Quaternion.Euler(0f, 90f, 0f));

            EnemyActor enemy = spawner.Spawn(prefab, request);

            Assert.IsNotNull(enemy);
            Assert.IsTrue(enemy.IsInitialized);
            Assert.AreEqual("enemy-runtime-001", enemy.RuntimeId);
            Assert.AreEqual(2, enemy.TeamId);
            Assert.AreEqual(1, registry.AliveEnemies);
            Assert.IsTrue(registry.TryGet("enemy-runtime-001", out EnemyActor registeredEnemy));
            Assert.AreSame(enemy, registeredEnemy);
            Object.DestroyImmediate(enemy.gameObject);
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test]
        public void Registry_Unregister_RemovesEnemyFromTracking()
        {
            EnemyFactoryV2 factory = new EnemyFactoryV2();
            EnemyRuntimeRegistryV2 registry = new EnemyRuntimeRegistryV2();
            EnemySpawnerV2 spawner = new EnemySpawnerV2(factory, registry);
            EnemyActor prefab = CreateEnemyPrefab();
            EnemyActor enemy = spawner.Spawn(
                prefab,
                new EnemySpawnRequestV2("enemy-runtime-002", 0, Vector3.zero, Quaternion.identity));

            bool removed = registry.Unregister(enemy);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, registry.AliveEnemies);
            Assert.IsFalse(registry.TryGet("enemy-runtime-002", out _));
            Object.DestroyImmediate(enemy.gameObject);
            Object.DestroyImmediate(prefab.gameObject);
        }

        private static EnemyActor CreateEnemyPrefab()
        {
            GameObject go = new GameObject("EnemyPrefab");
            go.AddComponent<EnemyMoveForwardBehaviour>();
            EnemyActor actor = go.AddComponent<EnemyActor>();
            return actor;
        }
    }
}
