using NUnit.Framework;
using UnityEngine;

namespace Madbox.Enemies.Tests
{
    public class EnemyServiceTests
    {
        [Test]
        public void Spawn_CreatesInitializedEnemy_AndRegistersIt()
        {
            EnemyFactory factory = new EnemyFactory();
            EnemyService service = new EnemyService(factory);
            Enemy prefab = CreateEnemyPrefab();
            Vector3 position = new Vector3(1f, 0f, 3f);
            Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);

            Enemy enemy = service.Spawn(prefab, position, rotation);

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
            EnemyFactory factory = new EnemyFactory();
            EnemyService service = new EnemyService(factory);
            Enemy prefab = CreateEnemyPrefab();
            Enemy enemy = service.Spawn(prefab, Vector3.zero, Quaternion.identity);

            bool removed = service.Unregister(enemy);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, service.AliveEnemies);
            AssertContainsEnemy(service, enemy, expectPresent: false);
            Object.DestroyImmediate(enemy.gameObject);
            Object.DestroyImmediate(prefab.gameObject);
        }

        private static void AssertContainsEnemy(EnemyService service, Enemy enemy, bool expectPresent = true)
        {
            bool found = false;
            foreach (Enemy alive in service.GetAllAlive())
            {
                if (alive == enemy)
                {
                    found = true;
                    break;
                }
            }

            Assert.AreEqual(expectPresent, found);
        }

        private static Enemy CreateEnemyPrefab()
        {
            GameObject go = new GameObject("EnemyPrefab");
            go.AddComponent<EnemyMoveForwardBehaviour>();
            Enemy data = go.AddComponent<Enemy>();
            return data;
        }
    }
}
