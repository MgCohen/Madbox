using Madbox.App.Entity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Madbox.App.Entity.Tests
{
    public sealed class EntityDataAttributeTests
    {
        [Test]
        public void GetFloatAttribute_ReturnsEntryValue()
        {
            EntityAttribute attr = ScriptableObject.CreateInstance<EntityAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("ed");
            var data = go.AddComponent<EntityData>();
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
            EntityAttribute attr = ScriptableObject.CreateInstance<EntityAttribute>();
            attr.name = "TestAttr";

            GameObject go = new GameObject("ed");
            var data = go.AddComponent<EntityData>();
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
            var data = go.AddComponent<EntityData>();
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
            var data = go.AddComponent<EntityData>();
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
            var data = go.AddComponent<EntityData>();
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
            var data = go.AddComponent<EntityData>();
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
    }
}
