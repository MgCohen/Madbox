using System.Collections.Generic;
using System.Reflection;
using Madbox.Entities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Madbox.Entities.Tests
{
    public sealed class EntityAttributeTests
    {
        [Test]
        public void GetFloatAttribute_ReturnsEntryValue()
        {
            EntityAttribute attr = ScriptableObject.CreateInstance<EntityAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("ed");
            var data = go.AddComponent<Entity>();
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
        public void EntityAttributeEntry_OnValueChanged_RaisesWhenEffectiveValueChanges()
        {
            EntityAttribute attr = ScriptableObject.CreateInstance<EntityAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("ed");
            var data = go.AddComponent<Entity>();
            SerializedObject dataSo = new SerializedObject(data);
            SerializedProperty listProp = dataSo.FindProperty("attributeEntries");
            listProp.arraySize = 1;
            SerializedProperty entryProp = listProp.GetArrayElementAtIndex(0);
            entryProp.FindPropertyRelative("attribute").objectReferenceValue = attr;
            entryProp.FindPropertyRelative("baseValue").floatValue = 1f;
            dataSo.ApplyModifiedPropertiesWithoutUndo();

            FieldInfo entriesField = typeof(Entity).GetField(
                "attributeEntries",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var entries = (List<EntityAttributeEntry>)entriesField.GetValue(data);
            EntityAttributeEntry entry = entries[0];

            float received = -1f;
            entry.OnValueChanged += v => received = v;

            data.SetFloatAttribute(attr, 3f);

            Assert.That(received, Is.EqualTo(3f).Within(0.0001f));

            Object.DestroyImmediate(attr);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetFloatAttribute_UpdatesValueAndRaisesEvent()
        {
            EntityAttribute attr = ScriptableObject.CreateInstance<EntityAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("ed");
            var data = go.AddComponent<Entity>();
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
        public void AddAttributeModifier_IncreasesEffectiveValue()
        {
            EntityAttribute attr = ScriptableObject.CreateInstance<EntityAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("ed");
            var data = go.AddComponent<Entity>();
            SerializedObject dataSo = new SerializedObject(data);
            SerializedProperty list = dataSo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attr;
            entry.FindPropertyRelative("baseValue").floatValue = 10f;
            dataSo.ApplyModifiedPropertiesWithoutUndo();

            data.AddAttributeModifier(attr, 2.5f);

            Assert.That(data.GetFloatAttribute(attr), Is.EqualTo(12.5f).Within(0.0001f));

            Object.DestroyImmediate(attr);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void RemoveAttributeModifier_RestoresEffectiveValue()
        {
            EntityAttribute attr = ScriptableObject.CreateInstance<EntityAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("ed");
            var data = go.AddComponent<Entity>();
            SerializedObject dataSo = new SerializedObject(data);
            SerializedProperty list = dataSo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attr;
            entry.FindPropertyRelative("baseValue").floatValue = 5f;
            dataSo.ApplyModifiedPropertiesWithoutUndo();

            data.AddAttributeModifier(attr, 3f);
            Assert.That(data.GetFloatAttribute(attr), Is.EqualTo(8f).Within(0.0001f));

            Assert.IsTrue(data.RemoveAttributeModifier(attr, 3f));
            Assert.That(data.GetFloatAttribute(attr), Is.EqualTo(5f).Within(0.0001f));

            Object.DestroyImmediate(attr);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetFloatAttribute_UpdatesBaseWhileKeepingModifiers()
        {
            EntityAttribute attr = ScriptableObject.CreateInstance<EntityAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("ed");
            var data = go.AddComponent<Entity>();
            SerializedObject dataSo = new SerializedObject(data);
            SerializedProperty list = dataSo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attr;
            entry.FindPropertyRelative("baseValue").floatValue = 1f;
            dataSo.ApplyModifiedPropertiesWithoutUndo();

            data.AddAttributeModifier(attr, 4f);
            data.SetFloatAttribute(attr, 2f);

            Assert.That(data.GetFloatAttribute(attr), Is.EqualTo(6f).Within(0.0001f));

            Object.DestroyImmediate(attr);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ModifierChange_RaisesAttributeValueChangedWithEffectiveValue()
        {
            EntityAttribute attr = ScriptableObject.CreateInstance<EntityAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("ed");
            var data = go.AddComponent<Entity>();
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

            data.AddAttributeModifier(attr, 2f);

            Assert.AreSame(attr, changedAttr);
            Assert.That(changedValue, Is.EqualTo(3f).Within(0.0001f));

            Object.DestroyImmediate(attr);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void RemoveAttributeModifiersAt_RemovesSliceAndRecalculates()
        {
            EntityAttribute attr = ScriptableObject.CreateInstance<EntityAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("ed");
            var data = go.AddComponent<Entity>();
            SerializedObject dataSo = new SerializedObject(data);
            SerializedProperty list = dataSo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attr;
            entry.FindPropertyRelative("baseValue").floatValue = 10f;
            dataSo.ApplyModifiedPropertiesWithoutUndo();

            data.AddAttributeModifier(attr, 1f);
            data.AddAttributeModifier(attr, 2f);
            data.AddAttributeModifier(attr, 4f);
            Assert.That(data.GetFloatAttribute(attr), Is.EqualTo(17f).Within(0.0001f));

            data.RemoveAttributeModifiersAt(1, 2);
            Assert.That(data.GetFloatAttribute(attr), Is.EqualTo(11f).Within(0.0001f));

            Object.DestroyImmediate(attr);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void RemoveAttributeModifiersAt_InvalidRange_Throws()
        {
            EntityAttribute attr = ScriptableObject.CreateInstance<EntityAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("ed");
            var data = go.AddComponent<Entity>();
            SerializedObject dataSo = new SerializedObject(data);
            SerializedProperty list = dataSo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attr;
            entry.FindPropertyRelative("baseValue").floatValue = 10f;
            dataSo.ApplyModifiedPropertiesWithoutUndo();

            data.AddAttributeModifier(attr, 1f);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => data.RemoveAttributeModifiersAt(0, 2));

            Object.DestroyImmediate(attr);
            Object.DestroyImmediate(go);
        }
    }
}
