using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
            DestroyImmediateAllTestActors();
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
            DestroyImmediateAllTestActors();
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
            DestroyImmediateAllTestActors();
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

            DestroyImmediateAllTestActors();
        }

        private static TestPoolActor CreatePrefab()
        {
            GameObject go = new GameObject("PoolPrefab");
            return go.AddComponent<TestPoolActor>();
        }

        /// <summary>
        /// Non-<see cref="PrefabPool{T}.Unload"/> destroy paths still use <see cref="Object.Destroy"/> at runtime; in EditMode Unity logs an error.
        /// </summary>
        private static void ExpectPrefabPoolDestroyEditModeErrors(int count)
        {
            for (int i = 0; i < count; i++)
            {
                LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            }
        }

        private static void DestroyImmediateAllTestActors()
        {
            foreach (TestPoolActor actor in Object.FindObjectsByType<TestPoolActor>(FindObjectsSortMode.None))
            {
                if (actor != null)
                {
                    Object.DestroyImmediate(actor.gameObject);
                }
            }
        }

        private sealed class TestPoolActor : MonoBehaviour
        {
            public bool CanReuse { get; set; } = true;
        }
    }
}
