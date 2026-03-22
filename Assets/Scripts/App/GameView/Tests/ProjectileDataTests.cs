using Madbox.App.GameView.Projectile;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Madbox.App.GameView.Tests
{
    public sealed class ProjectileDataTests
    {
        [Test]
        public void DamageAndSpeed_UseAssignedAttributes()
        {
            ProjectileAttribute damageAttr = ScriptableObject.CreateInstance<ProjectileAttribute>();
            damageAttr.name = "Damage";
            ProjectileAttribute speedAttr = ScriptableObject.CreateInstance<ProjectileAttribute>();
            speedAttr.name = "Speed";

            GameObject go = new GameObject("proj");
            var data = go.AddComponent<ProjectileData>();
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("damageAttribute").objectReferenceValue = damageAttr;
            so.FindProperty("speedAttribute").objectReferenceValue = speedAttr;
            SerializedProperty list = so.FindProperty("attributeEntries");
            list.arraySize = 2;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = damageAttr;
            e0.FindPropertyRelative("baseValue").floatValue = 10f;
            SerializedProperty e1 = list.GetArrayElementAtIndex(1);
            e1.FindPropertyRelative("attribute").objectReferenceValue = speedAttr;
            e1.FindPropertyRelative("baseValue").floatValue = 7f;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(data.Damage, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(data.Speed, Is.EqualTo(7f).Within(0.0001f));

            data.Damage = 3f;
            data.Speed = 20f;
            Assert.That(data.GetFloatAttribute(damageAttr), Is.EqualTo(3f).Within(0.0001f));
            Assert.That(data.GetFloatAttribute(speedAttr), Is.EqualTo(20f).Within(0.0001f));

            Object.DestroyImmediate(damageAttr);
            Object.DestroyImmediate(speedAttr);
            Object.DestroyImmediate(go);
        }
    }
}
