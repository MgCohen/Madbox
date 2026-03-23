using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Minimal prefab-based pool with warm-up, reuse and unload support.
    /// </summary>
    /// <typeparam name="T">Component type attached to the prefab root.</typeparam>
    public sealed class PrefabPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform defaultParent;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;
        private readonly Func<T, bool> canReuse;
        private readonly Stack<T> inactive = new Stack<T>();
        private readonly HashSet<T> active = new HashSet<T>();
        private readonly HashSet<T> knownInstances = new HashSet<T>();

        public PrefabPool(
            T prefab,
            Transform defaultParent = null,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Func<T, bool> canReuse = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            this.prefab = prefab;
            this.defaultParent = defaultParent;
            this.onGet = onGet;
            this.onRelease = onRelease;
            this.canReuse = canReuse ?? (_ => true);
        }

        public int ActiveCount => active.Count;

        public int InactiveCount => inactive.Count;

        public int TotalCount => knownInstances.Count;

        public void WarmUp(int count)
        {
            if (count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                T instance = CreateNewInstance();
                StoreInactive(instance);
            }
        }

        public T Get(Transform parentOverride = null)
        {
            T instance = TakeReusableInactiveOrCreate(parentOverride);
            if (instance == null)
            {
                instance = CreateNewInstance(parentOverride);
            }

            active.Add(instance);
            instance.gameObject.SetActive(true);
            onGet?.Invoke(instance);
            return instance;
        }

        public bool Release(T instance)
        {
            if (instance == null || active.Contains(instance) == false)
            {
                return false;
            }

            active.Remove(instance);
            if (canReuse(instance) == false)
            {
                knownInstances.Remove(instance);
                if (!Application.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(instance.gameObject);
                }
                else
                {
                    UnityEngine.Object.Destroy(instance.gameObject);
                }
                return true;
            }

            StoreInactive(instance);
            return true;
        }

        public void Unload()
        {
            foreach (T instance in active)
            {
                if (instance != null)
                {
                    if (!Application.isPlaying)
                    {
                        UnityEngine.Object.DestroyImmediate(instance.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(instance.gameObject);
                    }
                }
            }

            while (inactive.Count > 0)
            {
                T instance = inactive.Pop();
                if (instance != null)
                {
                    if (!Application.isPlaying)
                    {
                        UnityEngine.Object.DestroyImmediate(instance.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(instance.gameObject);
                    }
                }
            }

            active.Clear();
            knownInstances.Clear();
        }

        private T TakeReusableInactiveOrCreate(Transform parentOverride)
        {
            while (inactive.Count > 0)
            {
                T instance = inactive.Pop();
                if (instance == null)
                {
                    continue;
                }

                if (canReuse(instance) == false)
                {
                    knownInstances.Remove(instance);
                    if (!Application.isPlaying)
                    {
                        UnityEngine.Object.DestroyImmediate(instance.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(instance.gameObject);
                    }
                    continue;
                }

                Transform parent = parentOverride != null ? parentOverride : defaultParent;
                instance.transform.SetParent(parent, worldPositionStays: false);
                return instance;
            }

            return null;
        }

        private T CreateNewInstance(Transform parent = null)
        {
            Transform spawnParent = parent != null ? parent : defaultParent;
            T instance = UnityEngine.Object.Instantiate(prefab, spawnParent);
            knownInstances.Add(instance);
            return instance;
        }

        private void StoreInactive(T instance)
        {
            Transform parent = defaultParent;
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.gameObject.SetActive(false);
            onRelease?.Invoke(instance);
            inactive.Push(instance);
        }
    }
}
