using NUnit.Framework;
using UnityEngine;

namespace Madbox.V2.Enemies.Tests
{
    public class EnemyServiceV2Tests
    {
        [Test]
        public void Spawn_CreatesInitializedEnemy_AndRegistersIt()
        {
            EnemyFactoryV2 factory = new EnemyFactoryV2();
            EnemyServiceV2 service = new EnemyServiceV2(factory);
            EnemyActor prefab = CreateEnemyPrefab();
            Vector3 position = new Vector3(1f, 0f, 3f);
            Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);

            EnemyActor enemy = service.Spawn(prefab, position, rotation);

            Assert.IsNotNull(enemy);
            Assert.IsTrue(enemy.IsInitialized);
            Assert.AreEqual(1, service.AliveEnemies);
            AssertContainsEnemy(service, enemy);
            Object.DestroyImmediate(enemy.gameObject);
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test]
        public void Unregister_RemovesEnemyFromTracking()
        {
            EnemyFactoryV2 factory = new EnemyFactoryV2();
            EnemyServiceV2 service = new EnemyServiceV2(factory);
            EnemyActor prefab = CreateEnemyPrefab();
            EnemyActor enemy = service.Spawn(prefab, Vector3.zero, Quaternion.identity);

            bool removed = service.Unregister(enemy);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, service.AliveEnemies);
            AssertContainsEnemy(service, enemy, expectPresent: false);
            Object.DestroyImmediate(enemy.gameObject);
            Object.DestroyImmediate(prefab.gameObject);
        }

        private static void AssertContainsEnemy(EnemyServiceV2 service, EnemyActor enemy, bool expectPresent = true)
        {
            bool found = false;
            foreach (EnemyActor alive in service.GetAllAlive())
            {
                if (alive == enemy)
                {
                    found = true;
                    break;
                }
            }

            Assert.AreEqual(expectPresent, found);
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
