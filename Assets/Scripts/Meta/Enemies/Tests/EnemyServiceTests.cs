using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Madbox.Entities;

namespace Madbox.Enemies.Tests
{
    public class EnemyServiceTests
    {
        private const string BeePrefabPath = "Assets/Prefabs/Enemies/BeeEnemy.prefab";

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

        [Test]
        public void AliveEnemies_WhenEnemyDiesWithDelayedDestroy_IsZeroBeforeTick()
        {
            EnemyFactory factory = new EnemyFactory();
            EnemyService service = new EnemyService(factory);
            Enemy prefab = CreateEnemyPrefabWithDamageable(maxHp: 2f);
            Enemy enemy = service.Spawn(prefab, Vector3.zero, Quaternion.identity);
            Damageable damageable = enemy.GetComponentInChildren<Damageable>(true);
            Assert.IsNotNull(damageable);
            SerializedObject damageableSo = new SerializedObject(damageable);
            damageableSo.FindProperty("destroyDelayAfterDeathSeconds").floatValue = 30f;
            damageableSo.ApplyModifiedPropertiesWithoutUndo();

            damageable.DoDamage(2f);

            Assert.AreEqual(0, service.AliveEnemies);
            Assert.IsNotNull(enemy);
            AssertContainsEnemy(service, enemy, expectPresent: false);

            Object.DestroyImmediate(enemy.gameObject);
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test]
        public void BeePrefab_HasTriggerColliderAndDamageableWiring()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeePrefabPath);
            Assert.IsNotNull(prefab, $"Missing prefab at {BeePrefabPath}");

            CapsuleCollider collider = prefab.GetComponent<CapsuleCollider>();
            Assert.IsNotNull(collider, "Bee prefab requires a capsule collider on the root.");
            Assert.IsTrue(collider.isTrigger, "Bee collider must be a trigger for overlap-based hit detection.");

            Damageable damageable = prefab.GetComponentInChildren<Damageable>(true);
            Assert.IsNotNull(damageable, "Bee prefab requires a Damageable component (root or child).");

            SerializedObject so = new SerializedObject(damageable);
            Assert.IsNotNull(so.FindProperty("entity").objectReferenceValue, "Damageable must reference the bee Enemy entity.");
            Assert.IsNotNull(so.FindProperty("maxHpAttribute").objectReferenceValue, "Damageable must reference a MaxHp attribute.");
        }

        [Test]
        public void BeePrefab_HasEnemyWorldHealthBarWiredToDamageable()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeePrefabPath);
            Assert.IsNotNull(prefab, $"Missing prefab at {BeePrefabPath}");

            EnemyWorldHealthBarView healthBar = prefab.GetComponentInChildren<EnemyWorldHealthBarView>(true);
            Assert.IsNotNull(healthBar, "Bee prefab should include EnemyWorldHealthBarView (world-space HP bar).");

            Damageable damageableOnPrefab = prefab.GetComponentInChildren<Damageable>(true);
            Assert.IsNotNull(damageableOnPrefab);

            SerializedObject healthBarSo = new SerializedObject(healthBar);
            Object wired = healthBarSo.FindProperty("damageable").objectReferenceValue;
            Assert.IsNotNull(wired, "EnemyWorldHealthBarView.damageable must reference the bee Damageable.");
            Assert.AreSame(damageableOnPrefab, wired as Damageable, "Health bar must use the hierarchy Damageable.");

            Object prefabRef = healthBarSo.FindProperty("healthBarUiPrefab").objectReferenceValue;
            Assert.IsNotNull(prefabRef, "EnemyWorldHealthBarView.healthBarUiPrefab should reference the authored EnemyWorldHealthBar prefab.");
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

        private static Enemy CreateEnemyPrefabWithDamageable(float maxHp)
        {
            GameObject root = new GameObject("EnemyPrefabWithDamageable");
            root.AddComponent<EnemyMoveForwardBehaviour>();
            Enemy enemy = root.AddComponent<Enemy>();
            Damageable damageable = root.AddComponent<Damageable>();

            EntityAttribute maxHpAttribute = ScriptableObject.CreateInstance<EntityAttribute>();
            SerializedObject enemySerialized = new SerializedObject(enemy);
            SerializedProperty entries = enemySerialized.FindProperty("attributeEntries");
            entries.arraySize = 1;
            SerializedProperty entry = entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = maxHpAttribute;
            entry.FindPropertyRelative("baseValue").floatValue = maxHp;
            enemySerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject damageableSerialized = new SerializedObject(damageable);
            damageableSerialized.FindProperty("entity").objectReferenceValue = enemy;
            damageableSerialized.FindProperty("maxHpAttribute").objectReferenceValue = maxHpAttribute;
            damageableSerialized.FindProperty("resetHealthInAwake").boolValue = false;
            damageableSerialized.ApplyModifiedPropertiesWithoutUndo();
            damageable.ResetToFullHealth();
            return enemy;
        }
    }
}
