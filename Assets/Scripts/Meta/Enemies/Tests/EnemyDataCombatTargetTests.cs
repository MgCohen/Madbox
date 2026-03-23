using Madbox.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.Enemies.Tests
{
    public sealed class EnemyDataCombatTargetTests
    {
        [Test]
        public void EnemyData_OnColliderRoot_IsResolvableForCombatQueries()
        {
            var go = new GameObject("enemy-target");
            go.AddComponent<CapsuleCollider>();
            EnemyData data = go.AddComponent<EnemyData>();

            Assert.That(go.GetComponent<EnemyData>(), Is.SameAs(data));
            Assert.That(data.IsInitialized, Is.False);
            data.Initialize();
            Assert.That(data.IsInitialized, Is.True);

            Object.DestroyImmediate(go);
        }
    }
}
