using NUnit.Framework;
using UnityEngine;

namespace Madbox.Enemies.Tests
{
    public sealed class PrefabPoolTests
    {
        [Test]
        public void WarmUp_CreatesInactiveInstances()
        {
            TestPoolActor prefab = CreatePrefab();
            PrefabPool<TestPoolActor> pool = new PrefabPool<TestPoolActor>(prefab);

            pool.WarmUp(3);

            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(3, pool.InactiveCount);
            Assert.AreEqual(3, pool.TotalCount);

            pool.Unload();
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test]
        public void GetThenRelease_ReusesSameInstance()
        {
            TestPoolActor prefab = CreatePrefab();
            PrefabPool<TestPoolActor> pool = new PrefabPool<TestPoolActor>(prefab);
            pool.WarmUp(1);

            TestPoolActor first = pool.Get();
            pool.Release(first);
            TestPoolActor second = pool.Get();

            Assert.AreSame(first, second);
            Assert.AreEqual(1, pool.ActiveCount);
            Assert.AreEqual(0, pool.InactiveCount);
            Assert.AreEqual(1, pool.TotalCount);

            pool.Unload();
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test]
        public void Release_WhenInstanceCannotBeReused_DestroysIt()
        {
            TestPoolActor prefab = CreatePrefab();
            PrefabPool<TestPoolActor> pool = new PrefabPool<TestPoolActor>(
                prefab,
                canReuse: actor => actor.CanReuse);

            TestPoolActor instance = pool.Get();
            instance.CanReuse = false;

            bool released = pool.Release(instance);

            Assert.IsTrue(released);
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(0, pool.InactiveCount);
            Assert.AreEqual(0, pool.TotalCount);

            pool.Unload();
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test]
        public void Unload_DestroysAllTrackedInstances()
        {
            TestPoolActor prefab = CreatePrefab();
            PrefabPool<TestPoolActor> pool = new PrefabPool<TestPoolActor>(prefab);
            pool.WarmUp(2);
            TestPoolActor active = pool.Get();

            pool.Unload();

            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(0, pool.InactiveCount);
            Assert.AreEqual(0, pool.TotalCount);
            Assert.IsTrue(active == null);

            Object.DestroyImmediate(prefab.gameObject);
        }

        private static TestPoolActor CreatePrefab()
        {
            GameObject go = new GameObject("PoolPrefab");
            return go.AddComponent<TestPoolActor>();
        }

        private sealed class TestPoolActor : MonoBehaviour
        {
            public bool CanReuse { get; set; } = true;
        }
    }
}
