using Madbox.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.Enemies.Tests
{
    public sealed class EnemyCombatTargetTests
    {
        [Test]
        public void Enemy_OnColliderRoot_IsResolvableForCombatQueries()
        {
            var go = new GameObject("enemy-target");
            go.AddComponent<CapsuleCollider>();
            Enemy data = go.AddComponent<Enemy>();

            Assert.That(go.GetComponent<Enemy>(), Is.SameAs(data));
            Assert.That(data.IsInitialized, Is.False);
            data.Initialize();
            Assert.That(data.IsInitialized, Is.True);

            Object.DestroyImmediate(go);
        }
    }
}
