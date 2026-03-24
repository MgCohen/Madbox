using System.Reflection;
using Madbox.App.Animation;
using Madbox.Enemies;
using Madbox.Entities;
using Madbox.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Madbox.Players.Tests
{
    public sealed class PlayerAttackViewBehaviorAnimationHitTests
    {
        [Test]
        public void TryAcceptControl_WhenEnemyHasOnlyTriggerCollider_FindsEnemyTarget()
        {
            GameObject playerRoot = new GameObject("PlayerRoot");
            Player player = playerRoot.AddComponent<Player>();
            PlayerAttackViewBehavior behavior = playerRoot.AddComponent<PlayerAttackViewBehavior>();

            EntityAttribute attackRangeAttribute = ScriptableObject.CreateInstance<EntityAttribute>();
            SetEntityAttributeBase(player, attackRangeAttribute, 3f);

            SerializedObject behaviorSo = new SerializedObject(behavior);
            behaviorSo.FindProperty("attackRangeAttribute").objectReferenceValue = attackRangeAttribute;
            behaviorSo.FindProperty("isAliveAttribute").objectReferenceValue = null;
            behaviorSo.FindProperty("rayOriginHeight").floatValue = 0.5f;
            behaviorSo.FindProperty("enemyLayers").intValue = ~0;
            behaviorSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject enemyRoot = new GameObject("EnemyRoot");
            enemyRoot.transform.position = new Vector3(1f, 0f, 0f);
            enemyRoot.AddComponent<Enemy>();
            GameObject triggerChild = new GameObject("EnemyTrigger");
            triggerChild.transform.SetParent(enemyRoot.transform, false);
            SphereCollider trigger = triggerChild.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;

            bool accepted = behavior.TryAcceptControl(player, default);

            Assert.IsTrue(accepted, "Enemy trigger colliders should be detected as valid attack targets.");

            Object.DestroyImmediate(playerRoot);
            Object.DestroyImmediate(enemyRoot);
        }

        private static void AssignPlayerAttackBehaviorFields(
            PlayerAttackViewBehavior behavior,
            AnimationEventRouter router,
            AnimationEventDefinition hitEvent,
            EntityAttribute attackDamageAttribute)
        {
            SerializedObject behaviorSo = new SerializedObject(behavior);
            behaviorSo.FindProperty("animationEventRouter").objectReferenceValue = router;
            behaviorSo.FindProperty("attackHitEvent").objectReferenceValue = hitEvent;
            behaviorSo.FindProperty("attackDamageAttribute").objectReferenceValue = attackDamageAttribute;
            behaviorSo.FindProperty("attackDamageWhenAttributeMissing").floatValue = 1f;
            behaviorSo.FindProperty("attackRangeAttribute").objectReferenceValue = null;
            behaviorSo.FindProperty("isAliveAttribute").objectReferenceValue = null;
            behaviorSo.FindProperty("rayOriginHeight").floatValue = 0.5f;
            behaviorSo.FindProperty("enemyLayers").intValue = ~0;
            behaviorSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDamageable(Damageable damageable, Entity entity, EntityAttribute maxHpAttribute)
        {
            SerializedObject so = new SerializedObject(damageable);
            so.FindProperty("entity").objectReferenceValue = entity;
            so.FindProperty("maxHpAttribute").objectReferenceValue = maxHpAttribute;
            so.FindProperty("resetHealthInAwake").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEntityAttributeBase(Entity entity, EntityAttribute attribute, float baseValue)
        {
            SerializedObject so = new SerializedObject(entity);
            SerializedProperty list = so.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attribute;
            entry.FindPropertyRelative("baseValue").floatValue = baseValue;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
