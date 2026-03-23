using System.Collections.Generic;
using Madbox.App.GameView.Player;
using Madbox.Entities;
using Madbox.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Madbox.Player.Tests
{
    public sealed class PlayerAttributeTests
    {
        [Test]
        public void GetFloatAttribute_ReturnsEntryValue()
        {
            PlayerAttribute attr = ScriptableObject.CreateInstance<PlayerAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("pd");
            var data = go.AddComponent<Player>();
            SerializedObject dataSo = new SerializedObject(data);
            SerializedProperty list = dataSo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attr;
            entry.FindPropertyRelative("baseValue").floatValue = 2.5f;
            dataSo.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(data.GetFloatAttribute(attr), Is.EqualTo(2.5f).Within(0.0001f));

            Object.DestroyImmediate(attr);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetFloatAttribute_UpdatesValueAndRaisesEvent()
        {
            PlayerAttribute attr = ScriptableObject.CreateInstance<PlayerAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("pd");
            var data = go.AddComponent<Player>();
            SerializedObject dataSo = new SerializedObject(data);
            SerializedProperty list = dataSo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attr;
            entry.FindPropertyRelative("baseValue").floatValue = 1f;
            dataSo.ApplyModifiedPropertiesWithoutUndo();

            EntityAttribute changedAttr = null;
            float changedValue = -1f;
            data.AttributeValueChanged += (a, v) =>
            {
                changedAttr = a;
                changedValue = v;
            };

            data.SetFloatAttribute(attr, 3f);

            Assert.That(data.GetFloatAttribute(attr), Is.EqualTo(3f).Within(0.0001f));
            Assert.AreSame(attr, changedAttr);
            Assert.That(changedValue, Is.EqualTo(3f).Within(0.0001f));

            Object.DestroyImmediate(attr);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GetBoolAttribute_UsesGreaterThanZero()
        {
            PlayerAttribute attr = ScriptableObject.CreateInstance<PlayerAttribute>();
            attr.name = "Flag";

            GameObject go = new GameObject("pd");
            var data = go.AddComponent<Player>();
            SerializedObject dataSo = new SerializedObject(data);
            SerializedProperty list = dataSo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attr;
            entry.FindPropertyRelative("baseValue").floatValue = 0f;
            dataSo.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsFalse(data.GetBoolAttribute(attr));
            data.SetBoolAttribute(attr, true);
            Assert.IsTrue(data.GetBoolAttribute(attr));

            Object.DestroyImmediate(attr);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void EquipAndUnequip_RegistersAndUnregistersWeaponModifiers()
        {
            EntityAttribute moveSpeed = ScriptableObject.CreateInstance<EntityAttribute>();
            moveSpeed.name = "MoveSpeed";

            GameObject playerGo = new GameObject("player");
            var data = playerGo.AddComponent<Player>();
            SerializedObject dataSo = new SerializedObject(data);
            SerializedProperty list = dataSo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = moveSpeed;
            entry.FindPropertyRelative("baseValue").floatValue = 10f;
            dataSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject weaponGo = new GameObject("weapon");
            Weapon weapon = weaponGo.AddComponent<Weapon>();
            SetWeaponModifiers(weapon, moveSpeed, 2f);

            data.Equip(weapon);
            Assert.That(data.GetFloatAttribute(moveSpeed), Is.EqualTo(12f).Within(0.0001f));

            data.Unequip(weapon);
            Assert.That(data.GetFloatAttribute(moveSpeed), Is.EqualTo(10f).Within(0.0001f));

            Object.DestroyImmediate(weaponGo);
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(moveSpeed);
        }

        private static void SetWeaponModifiers(Weapon weapon, EntityAttribute attribute, float delta)
        {
            SerializedObject weaponSo = new SerializedObject(weapon);
            SerializedProperty list = weaponSo.FindProperty("modifiers");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attribute;
            entry.FindPropertyRelative("delta").floatValue = delta;
            weaponSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
