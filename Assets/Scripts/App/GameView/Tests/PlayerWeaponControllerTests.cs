using System.Collections.Generic;
using System.Reflection;
using Madbox.App.GameView.Players;
using Madbox.Entities;
using Madbox.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Madbox.App.GameView.Tests
{
    public sealed class PlayerWeaponControllerTests
    {
        [Test]
        public void EquipWeaponAtIndex_UpdatesVisualSelection()
        {
            EntityAttribute moveSpeed = ScriptableObject.CreateInstance<EntityAttribute>();
            moveSpeed.name = "MoveSpeed";
            GameObject playerGo = new GameObject("player");
            Player playerData = playerGo.AddComponent<Player>();
            SetAttributeEntry(playerData, moveSpeed, 10f);

            GameObject root = new GameObject("root");
            WeaponVisualController visual = root.AddComponent<WeaponVisualController>();
            SetSocketsViaReflection(visual, 2);

            GameObject weapon0Go = BuildWeapon("weapon0", moveSpeed, 1f);
            GameObject weapon1Go = BuildWeapon("weapon1", moveSpeed, 3f);
            weapon0Go.transform.SetParent(visual.GetWeaponSocket(0), false);
            weapon1Go.transform.SetParent(visual.GetWeaponSocket(1), false);

            GameObject controllerGo = new GameObject("controller");
            PlayerWeaponController controller = controllerGo.AddComponent<PlayerWeaponController>();
            controller.Bind(playerData, visual);
            visual.SetWeaponInstances(new List<GameObject> { weapon0Go, weapon1Go });

            playerData.SetAvailableWeapons(new List<GameObject> { weapon0Go, weapon1Go });
            Assert.AreEqual(0, visual.SelectedWeaponIndex);
            Assert.That(playerData.GetFloatAttribute(moveSpeed), Is.EqualTo(11f).Within(0.0001f));

            playerData.EquipWeaponAtIndex(1);

            Assert.AreEqual(1, visual.SelectedWeaponIndex);
            Assert.That(playerData.GetFloatAttribute(moveSpeed), Is.EqualTo(13f).Within(0.0001f));

            Object.DestroyImmediate(controllerGo);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(weapon0Go);
            Object.DestroyImmediate(weapon1Go);
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(moveSpeed);
        }

        private static void SetAttributeEntry(Entity data, EntityAttribute attribute, float baseValue)
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
    }
}
