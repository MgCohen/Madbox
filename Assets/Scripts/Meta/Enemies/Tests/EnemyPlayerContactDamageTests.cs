using System.Reflection;
using Madbox.Entities;
using Madbox.Enemies;
using Madbox.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Madbox.Enemies.Tests
{
    public sealed class EnemyPlayerContactDamageTests
    {
        [Test]
        public void OnTriggerStay_WhenOverlappingPlayer_ReducesPlayerHp()
        {
            EntityAttribute damageAttr = ScriptableObject.CreateInstance<EntityAttribute>();

            GameObject enemyGo = new GameObject("Enemy");
            Enemy enemy = enemyGo.AddComponent<Enemy>();
            enemy.Initialize();

            SerializedObject enemySo = new SerializedObject(enemy);
            SerializedProperty list = enemySo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = damageAttr;
            e0.FindPropertyRelative("baseValue").floatValue = 4f;
            enemySo.ApplyModifiedPropertiesWithoutUndo();

            EnemyPlayerContactDamage contactDamage = enemyGo.AddComponent<EnemyPlayerContactDamage>();
            SerializedObject contactSo = new SerializedObject(contactDamage);
            contactSo.FindProperty("enemy").objectReferenceValue = enemy;
            contactSo.FindProperty("damageAttribute").objectReferenceValue = damageAttr;
            int playerLayer = playerGoLayerForTest();
            contactSo.FindProperty("playerLayers").intValue = 1 << playerLayer;
            contactSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject playerGo = new GameObject("Player");
            playerGo.layer = playerLayer;
            Player player = playerGo.AddComponent<Player>();
            EnemyTestPlayerDamageableSetup.ConfigurePlayerDamageable(playerGo, player, maxHp: 100f);

            playerGo.AddComponent<BoxCollider>();

            MethodInfo onTriggerStay = typeof(EnemyPlayerContactDamage).GetMethod(
                "OnTriggerStay",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(onTriggerStay);
            onTriggerStay.Invoke(contactDamage, new object[] { playerGo.GetComponent<BoxCollider>() });

            Damageable damageable = playerGo.GetComponentInChildren<Damageable>();
            Assert.That(damageable.CurrentHp, Is.EqualTo(96f).Within(0.0001f));

            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(damageAttr);
        }

        private static int playerGoLayerForTest()
        {
            return 10;
        }
    }
}
