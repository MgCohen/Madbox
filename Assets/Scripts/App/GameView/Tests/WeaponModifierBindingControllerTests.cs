using System.Collections.Generic;
using System.Reflection;
using Madbox.App.GameView.Player;
using Madbox.Entity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Madbox.App.GameView.Tests
{
    public sealed class WeaponModifierBindingControllerTests
    {
        [Test]
        public void SelectedWeaponChanged_SwapRemovesOldAndAppliesNew()
        {
            EntityAttribute moveSpeed = ScriptableObject.CreateInstance<EntityAttribute>();
            moveSpeed.name = "MoveSpeed";
            GameObject playerGo = new GameObject("player");
            PlayerData playerData = playerGo.AddComponent<PlayerData>();
            SetAttributeEntry(playerData, moveSpeed, 10f);

            GameObject root = new GameObject("root");
            WeaponVisualController visual = root.AddComponent<WeaponVisualController>();
            SetSocketsViaReflection(visual, 2);

            GameObject weapon0Go = BuildWeapon("weapon0", moveSpeed, 1f);
            GameObject weapon1Go = BuildWeapon("weapon1", moveSpeed, 3f);
            weapon0Go.transform.SetParent(visual.GetWeaponSocket(0), false);
            weapon1Go.transform.SetParent(visual.GetWeaponSocket(1), false);

            GameObject binderGo = new GameObject("binder");
            WeaponModifierBindingController binding = binderGo.AddComponent<WeaponModifierBindingController>();
            SetPrivateField(binding, "playerData", playerData);
            SetPrivateField(binding, "weaponVisualController", visual);
            binding.enabled = true;

            visual.SetWeaponInstances(new List<GameObject> { weapon0Go, weapon1Go });
            Assert.That(playerData.GetFloatAttribute(moveSpeed), Is.EqualTo(11f).Within(0.0001f));

            visual.SetSelectedWeaponIndex(1);

            Assert.That(playerData.GetFloatAttribute(moveSpeed), Is.EqualTo(13f).Within(0.0001f));
            Assert.AreEqual(1, playerData.AttributeModifiers.Count);
            Assert.AreSame(moveSpeed, playerData.AttributeModifiers[0].Attribute);
            Assert.That(playerData.AttributeModifiers[0].Delta, Is.EqualTo(3f).Within(0.0001f));

            Object.DestroyImmediate(binderGo);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(weapon0Go);
            Object.DestroyImmediate(weapon1Go);
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(moveSpeed);
        }

        [Test]
        public void Disable_RemovesActiveModifiers()
        {
            EntityAttribute moveSpeed = ScriptableObject.CreateInstance<EntityAttribute>();
            moveSpeed.name = "MoveSpeed";
            GameObject playerGo = new GameObject("player");
            PlayerData playerData = playerGo.AddComponent<PlayerData>();
            SetAttributeEntry(playerData, moveSpeed, 10f);

            GameObject root = new GameObject("root");
            WeaponVisualController visual = root.AddComponent<WeaponVisualController>();
            SetSocketsViaReflection(visual, 1);

            GameObject weaponGo = BuildWeapon("weapon", moveSpeed, 2f);
            weaponGo.transform.SetParent(visual.GetWeaponSocket(0), false);

            GameObject binderGo = new GameObject("binder");
            WeaponModifierBindingController binding = binderGo.AddComponent<WeaponModifierBindingController>();
            SetPrivateField(binding, "playerData", playerData);
            SetPrivateField(binding, "weaponVisualController", visual);
            binding.enabled = true;

            visual.SetWeaponInstances(new List<GameObject> { weaponGo });
            Assert.That(playerData.GetFloatAttribute(moveSpeed), Is.EqualTo(12f).Within(0.0001f));

            binding.enabled = false;
            Assert.That(playerData.GetFloatAttribute(moveSpeed), Is.EqualTo(10f).Within(0.0001f));
            Assert.AreEqual(0, playerData.AttributeModifiers.Count);

            Object.DestroyImmediate(binderGo);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(weaponGo);
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(moveSpeed);
        }

        private static void SetAttributeEntry(EntityData data, EntityAttribute attribute, float baseValue)
        {
            SerializedObject dataSo = new SerializedObject(data);
            SerializedProperty list = dataSo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attribute;
            entry.FindPropertyRelative("baseValue").floatValue = baseValue;
            dataSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildWeapon(string name, EntityAttribute attribute, float delta)
        {
            GameObject go = new GameObject(name);
            Weapon weapon = go.AddComponent<Weapon>();
            SerializedObject weaponSo = new SerializedObject(weapon);
            SerializedProperty list = weaponSo.FindProperty("modifiers");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attribute;
            entry.FindPropertyRelative("delta").floatValue = delta;
            weaponSo.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        private static void SetSocketsViaReflection(WeaponVisualController visual, int count)
        {
            var sockets = new List<Transform>();
            for (int i = 0; i < count; i++)
            {
                Transform s = new GameObject("s" + i).transform;
                s.SetParent(visual.transform, false);
                sockets.Add(s);
            }

            typeof(WeaponVisualController).GetField("weaponSockets", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(visual, sockets);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(target, value);
        }
    }
}
