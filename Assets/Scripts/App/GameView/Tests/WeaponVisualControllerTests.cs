using System.Collections.Generic;
using System.Reflection;
using Madbox.Entity;
using Madbox.App.GameView.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Madbox.App.GameView.Tests
{
    public sealed class WeaponVisualControllerTests
    {
        [Test]
        public void SetSelectedWeaponIndex_OnlyOneWeaponActive()
        {
            GameObject root = new GameObject("root");
            WeaponVisualController visual = root.AddComponent<WeaponVisualController>();
            SetSocketsViaReflection(visual, 3);
            GameObject w0 = new GameObject("w0");
            GameObject w1 = new GameObject("w1");
            GameObject w2 = new GameObject("w2");
            visual.SetWeaponInstances(new List<GameObject> { w0, w1, w2 });

            visual.SetSelectedWeaponIndex(1);

            Assert.IsFalse(w0.activeSelf);
            Assert.IsTrue(w1.activeSelf);
            Assert.IsFalse(w2.activeSelf);
            Assert.AreEqual(1, visual.SelectedWeaponIndex);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(w0);
            Object.DestroyImmediate(w1);
            Object.DestroyImmediate(w2);
        }

        [Test]
        public void SetSelectedWeaponIndex_BeforeSetWeaponInstances_Throws()
        {
            GameObject root = new GameObject("root");
            WeaponVisualController visual = root.AddComponent<WeaponVisualController>();
            SetSocketsViaReflection(visual, 3);
            Assert.Throws<System.InvalidOperationException>(() => visual.SetSelectedWeaponIndex(0));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void SetSelectedWeaponIndex_RemovesOldAndAppliesNewWeaponModifiers()
        {
            EntityAttribute moveSpeed = ScriptableObject.CreateInstance<EntityAttribute>();
            moveSpeed.name = "MoveSpeed";
            GameObject dataGo = new GameObject("entityData");
            EntityData data = dataGo.AddComponent<EntityData>();
            SetAttributeEntry(data, moveSpeed, 10f);

            GameObject root = new GameObject("root");
            WeaponVisualController visual = root.AddComponent<WeaponVisualController>();
            SetSocketsViaReflection(visual, 2);
            visual.SetModifierTarget(data);
            visual.SetWeaponModifiers(new List<IReadOnlyList<EntityAttributeModifierEntry>>
            {
                new List<EntityAttributeModifierEntry> { new EntityAttributeModifierEntry(moveSpeed, 1f) },
                new List<EntityAttributeModifierEntry> { new EntityAttributeModifierEntry(moveSpeed, 3f) }
            });

            GameObject w0 = new GameObject("w0");
            GameObject w1 = new GameObject("w1");
            visual.SetWeaponInstances(new List<GameObject> { w0, w1 });
            Assert.That(data.GetFloatAttribute(moveSpeed), Is.EqualTo(11f).Within(0.0001f));

            visual.SetSelectedWeaponIndex(1);

            Assert.That(data.GetFloatAttribute(moveSpeed), Is.EqualTo(13f).Within(0.0001f));
            Assert.AreEqual(1, visual.SelectedWeaponIndex);
            Assert.IsFalse(w0.activeSelf);
            Assert.IsTrue(w1.activeSelf);
            Assert.AreEqual(1, data.AttributeModifiers.Count);
            Assert.AreSame(moveSpeed, data.AttributeModifiers[0].Attribute);
            Assert.That(data.AttributeModifiers[0].Delta, Is.EqualTo(3f).Within(0.0001f));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(w0);
            Object.DestroyImmediate(w1);
            Object.DestroyImmediate(dataGo);
            Object.DestroyImmediate(moveSpeed);
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
    }
}
