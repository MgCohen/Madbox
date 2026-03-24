using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

namespace Madbox.Entities.Tests
{
    public sealed class DamageableTests
    {
        [Test]
        public void ResetToFullHealth_SetsCurrentHpFromEntityMaxAttribute()
        {
            EntityAttribute maxHp = ScriptableObject.CreateInstance<EntityAttribute>();
            maxHp.name = "MaxHp";

            GameObject root = new GameObject("target");
            Entity entity = root.AddComponent<Entity>();
            SerializedObject soEntity = new SerializedObject(entity);
            SerializedProperty list = soEntity.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = maxHp;
            e0.FindPropertyRelative("baseValue").floatValue = 50f;
            soEntity.ApplyModifiedPropertiesWithoutUndo();

            Damageable damageable = root.AddComponent<Damageable>();
            SerializedObject soD = new SerializedObject(damageable);
            soD.FindProperty("entity").objectReferenceValue = entity;
            soD.FindProperty("maxHpAttribute").objectReferenceValue = maxHp;
            soD.FindProperty("resetHealthInAwake").boolValue = false;
            soD.ApplyModifiedPropertiesWithoutUndo();

            damageable.ResetToFullHealth();

            Assert.That(damageable.CurrentHp, Is.EqualTo(50f).Within(0.0001f));
            Assert.That(damageable.MaxHp, Is.EqualTo(50f).Within(0.0001f));
            Assert.That(damageable.Entity, Is.SameAs(entity));

            UnityEngine.Object.DestroyImmediate(maxHp);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void DoDamage_WhenAlive_ReducesCurrentHpAndReturnsTrue()
        {
            EntityAttribute maxHp = ScriptableObject.CreateInstance<EntityAttribute>();
            maxHp.name = "MaxHp";

            GameObject root = new GameObject("target");
            Entity entity = root.AddComponent<Entity>();
            SerializedObject soEntity = new SerializedObject(entity);
            SerializedProperty list = soEntity.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = maxHp;
            e0.FindPropertyRelative("baseValue").floatValue = 10f;
            soEntity.ApplyModifiedPropertiesWithoutUndo();

            Damageable damageable = root.AddComponent<Damageable>();
            SerializedObject soD = new SerializedObject(damageable);
            soD.FindProperty("entity").objectReferenceValue = entity;
            soD.FindProperty("maxHpAttribute").objectReferenceValue = maxHp;
            soD.FindProperty("resetHealthInAwake").boolValue = false;
            soD.ApplyModifiedPropertiesWithoutUndo();

            damageable.ResetToFullHealth();
            bool applied = damageable.DoDamage(3f);

            Assert.IsTrue(applied);
            Assert.That(damageable.CurrentHp, Is.EqualTo(7f).Within(0.0001f));
            Assert.IsTrue(damageable.IsAlive);

            UnityEngine.Object.DestroyImmediate(maxHp);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void DoDamage_WhenHpWouldGoNegative_ClampsToZero()
        {
            EntityAttribute maxHp = ScriptableObject.CreateInstance<EntityAttribute>();
            maxHp.name = "MaxHp";

            GameObject root = new GameObject("target");
            Entity entity = root.AddComponent<Entity>();
            SerializedObject soEntity = new SerializedObject(entity);
            SerializedProperty list = soEntity.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = maxHp;
            e0.FindPropertyRelative("baseValue").floatValue = 5f;
            soEntity.ApplyModifiedPropertiesWithoutUndo();

            Damageable damageable = root.AddComponent<Damageable>();
            SerializedObject soD = new SerializedObject(damageable);
            soD.FindProperty("entity").objectReferenceValue = entity;
            soD.FindProperty("maxHpAttribute").objectReferenceValue = maxHp;
            soD.FindProperty("resetHealthInAwake").boolValue = false;
            soD.ApplyModifiedPropertiesWithoutUndo();

            damageable.ResetToFullHealth();
            damageable.DoDamage(100f);

            Assert.That(damageable.CurrentHp, Is.EqualTo(0f).Within(0.0001f));
            Assert.IsFalse(damageable.IsAlive);

            UnityEngine.Object.DestroyImmediate(maxHp);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void DoDamage_WhenAmountNotPositive_ReturnsFalse()
        {
            EntityAttribute maxHp = ScriptableObject.CreateInstance<EntityAttribute>();
            maxHp.name = "MaxHp";

            GameObject root = new GameObject("target");
            Entity entity = root.AddComponent<Entity>();
            SerializedObject soEntity = new SerializedObject(entity);
            SerializedProperty list = soEntity.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = maxHp;
            e0.FindPropertyRelative("baseValue").floatValue = 10f;
            soEntity.ApplyModifiedPropertiesWithoutUndo();

            Damageable damageable = root.AddComponent<Damageable>();
            SerializedObject soD = new SerializedObject(damageable);
            soD.FindProperty("entity").objectReferenceValue = entity;
            soD.FindProperty("maxHpAttribute").objectReferenceValue = maxHp;
            soD.FindProperty("resetHealthInAwake").boolValue = false;
            soD.ApplyModifiedPropertiesWithoutUndo();

            damageable.ResetToFullHealth();

            Assert.IsFalse(damageable.DoDamage(0f));
            Assert.IsFalse(damageable.DoDamage(-1f));
            Assert.That(damageable.CurrentHp, Is.EqualTo(10f).Within(0.0001f));

            UnityEngine.Object.DestroyImmediate(maxHp);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void DoDamage_WhenAlreadyDead_ReturnsFalse()
        {
            EntityAttribute maxHp = ScriptableObject.CreateInstance<EntityAttribute>();
            maxHp.name = "MaxHp";

            GameObject root = new GameObject("target");
            Entity entity = root.AddComponent<Entity>();
            SerializedObject soEntity = new SerializedObject(entity);
            SerializedProperty list = soEntity.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = maxHp;
            e0.FindPropertyRelative("baseValue").floatValue = 5f;
            soEntity.ApplyModifiedPropertiesWithoutUndo();

            Damageable damageable = root.AddComponent<Damageable>();
            SerializedObject soD = new SerializedObject(damageable);
            soD.FindProperty("entity").objectReferenceValue = entity;
            soD.FindProperty("maxHpAttribute").objectReferenceValue = maxHp;
            soD.FindProperty("resetHealthInAwake").boolValue = false;
            soD.ApplyModifiedPropertiesWithoutUndo();

            damageable.ResetToFullHealth();
            damageable.DoDamage(5f);

            Assert.IsFalse(damageable.DoDamage(1f));
            Assert.That(damageable.CurrentHp, Is.EqualTo(0f).Within(0.0001f));

            UnityEngine.Object.DestroyImmediate(maxHp);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void TryApplyDamage_WhenBeforeDamageCancels_ReturnsFalseWithoutChangingHp()
        {
            EntityAttribute maxHp = ScriptableObject.CreateInstance<EntityAttribute>();
            maxHp.name = "MaxHp";

            GameObject root = new GameObject("target");
            Entity entity = root.AddComponent<Entity>();
            SerializedObject soEntity = new SerializedObject(entity);
            SerializedProperty list = soEntity.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = maxHp;
            e0.FindPropertyRelative("baseValue").floatValue = 10f;
            soEntity.ApplyModifiedPropertiesWithoutUndo();

            Damageable damageable = root.AddComponent<Damageable>();
            SerializedObject soD = new SerializedObject(damageable);
            soD.FindProperty("entity").objectReferenceValue = entity;
            soD.FindProperty("maxHpAttribute").objectReferenceValue = maxHp;
            soD.FindProperty("resetHealthInAwake").boolValue = false;
            soD.ApplyModifiedPropertiesWithoutUndo();

            damageable.ResetToFullHealth();
            damageable.BeforeDamageApplied += (_, e) => e.Cancel = true;

            bool applied = damageable.TryApplyDamage(3f, Vector3.zero);

            Assert.IsFalse(applied);
            Assert.That(damageable.CurrentHp, Is.EqualTo(10f).Within(0.0001f));

            UnityEngine.Object.DestroyImmediate(maxHp);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void TryApplyDamage_WhenKillingTarget_RaisesDiedOnce()
        {
            EntityAttribute maxHp = ScriptableObject.CreateInstance<EntityAttribute>();
            maxHp.name = "MaxHp";

            GameObject root = new GameObject("target");
            Entity entity = root.AddComponent<Entity>();
            SerializedObject soEntity = new SerializedObject(entity);
            SerializedProperty list = soEntity.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = maxHp;
            e0.FindPropertyRelative("baseValue").floatValue = 4f;
            soEntity.ApplyModifiedPropertiesWithoutUndo();

            Damageable damageable = root.AddComponent<Damageable>();
            SerializedObject soD = new SerializedObject(damageable);
            soD.FindProperty("entity").objectReferenceValue = entity;
            soD.FindProperty("maxHpAttribute").objectReferenceValue = maxHp;
            soD.FindProperty("resetHealthInAwake").boolValue = false;
            soD.ApplyModifiedPropertiesWithoutUndo();

            damageable.ResetToFullHealth();
            int diedCount = 0;
            damageable.Died += (_, __) => diedCount++;

            Assert.IsTrue(damageable.TryApplyDamage(4f, Vector3.zero));
            Assert.AreEqual(1, diedCount);
            Assert.IsFalse(damageable.IsAlive);

            UnityEngine.Object.DestroyImmediate(maxHp);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void TryApplyDamage_WithDelay_BlocksRepeatedDamageUntilDelayExpires()
        {
            EntityAttribute maxHp = ScriptableObject.CreateInstance<EntityAttribute>();
            maxHp.name = "MaxHp";

            GameObject root = new GameObject("target");
            Entity entity = root.AddComponent<Entity>();
            SerializedObject soEntity = new SerializedObject(entity);
            SerializedProperty list = soEntity.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = maxHp;
            e0.FindPropertyRelative("baseValue").floatValue = 20f;
            soEntity.ApplyModifiedPropertiesWithoutUndo();

            Damageable damageable = root.AddComponent<Damageable>();
            SerializedObject soD = new SerializedObject(damageable);
            soD.FindProperty("entity").objectReferenceValue = entity;
            soD.FindProperty("maxHpAttribute").objectReferenceValue = maxHp;
            soD.FindProperty("resetHealthInAwake").boolValue = false;
            soD.FindProperty("damageDelaySeconds").floatValue = 0.5f;
            soD.ApplyModifiedPropertiesWithoutUndo();
            damageable.ResetToFullHealth();

            float now = 10f;
            MethodInfo setNowProvider = typeof(Damageable).GetMethod("SetNowProviderForTests", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(setNowProvider);
            setNowProvider.Invoke(damageable, new object[] { (Func<float>)(() => now) });

            Assert.IsTrue(damageable.TryApplyDamage(5f, Vector3.zero));
            float hpAfterFirst = damageable.CurrentHp;

            now = 10.1f;
            Assert.IsFalse(damageable.TryApplyDamage(5f, Vector3.zero));
            Assert.That(damageable.CurrentHp, Is.EqualTo(hpAfterFirst).Within(0.0001f));

            now = 10.6f;
            Assert.IsTrue(damageable.TryApplyDamage(5f, Vector3.zero));
            Assert.That(damageable.CurrentHp, Is.EqualTo(10f).Within(0.0001f));

            UnityEngine.Object.DestroyImmediate(maxHp);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void TryApplyDamage_WithZeroDelay_AllowsRepeatedDamage()
        {
            EntityAttribute maxHp = ScriptableObject.CreateInstance<EntityAttribute>();
            maxHp.name = "MaxHp";

            GameObject root = new GameObject("target");
            Entity entity = root.AddComponent<Entity>();
            SerializedObject soEntity = new SerializedObject(entity);
            SerializedProperty list = soEntity.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty e0 = list.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("attribute").objectReferenceValue = maxHp;
            e0.FindPropertyRelative("baseValue").floatValue = 10f;
            soEntity.ApplyModifiedPropertiesWithoutUndo();

            Damageable damageable = root.AddComponent<Damageable>();
            SerializedObject soD = new SerializedObject(damageable);
            soD.FindProperty("entity").objectReferenceValue = entity;
            soD.FindProperty("maxHpAttribute").objectReferenceValue = maxHp;
            soD.FindProperty("resetHealthInAwake").boolValue = false;
            soD.FindProperty("damageDelaySeconds").floatValue = 0f;
            soD.ApplyModifiedPropertiesWithoutUndo();
            damageable.ResetToFullHealth();

            Assert.IsTrue(damageable.TryApplyDamage(2f, Vector3.zero));
            Assert.IsTrue(damageable.TryApplyDamage(2f, Vector3.zero));
            Assert.That(damageable.CurrentHp, Is.EqualTo(6f).Within(0.0001f));

            UnityEngine.Object.DestroyImmediate(maxHp);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
