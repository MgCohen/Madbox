using Madbox.Entities;
using Madbox.Players;
using UnityEditor;
using UnityEngine;

namespace Madbox.Enemies.Tests
{
    internal static class EnemyTestPlayerDamageableSetup
    {
        public static void ConfigurePlayerDamageable(GameObject playerRoot, Player player, float maxHp)
        {
            EntityAttribute maxHpAttribute = ScriptableObject.CreateInstance<EntityAttribute>();
            maxHpAttribute.name = "MaxHpTest";

            SerializedObject soPlayerEntity = new SerializedObject(player);
            SerializedProperty list = soPlayerEntity.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = maxHpAttribute;
            e0.FindPropertyRelative("baseValue").floatValue = maxHp;
            soPlayerEntity.ApplyModifiedPropertiesWithoutUndo();

            Damageable damageable = playerRoot.AddComponent<Damageable>();
            SerializedObject soD = new SerializedObject(damageable);
            soD.FindProperty("entity").objectReferenceValue = player;
            soD.FindProperty("maxHpAttribute").objectReferenceValue = maxHpAttribute;
            soD.FindProperty("resetHealthInAwake").boolValue = false;
            soD.ApplyModifiedPropertiesWithoutUndo();

            damageable.ResetToFullHealth();

            Object.DestroyImmediate(maxHpAttribute);
        }
    }
}
