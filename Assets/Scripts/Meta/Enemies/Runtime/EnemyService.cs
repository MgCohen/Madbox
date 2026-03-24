using System;
using System.Collections.Generic;
using Madbox.Entities;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Orchestrates enemy lifecycle: spawns via <see cref="EnemyFactory"/> and tracks alive instances.
    /// Dead enemies are removed from the alive set immediately so win rules see the correct count,
    /// while <see cref="Damageable.DestroyDelayAfterDeathSeconds"/> on that enemy <see cref="Damageable"/> can delay <see cref="Object.Destroy"/> after death.
    /// </summary>
    public sealed class EnemyService
    {
        public EnemyService(EnemyFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            this.factory = factory;
        }

        public int AliveEnemies
        {
            get
            {
                PruneDestroyedFromTracking();
                return alive.Count;
            }
        }

        private readonly EnemyFactory factory;

        private readonly HashSet<Enemy> alive = new HashSet<Enemy>();

        private readonly List<PendingEnemyDestroy> pendingDestroys = new List<PendingEnemyDestroy>(8);

        private readonly Dictionary<Enemy, (Damageable damageable, EventHandler handler)> deathSubscriptions =
            new Dictionary<Enemy, (Damageable damageable, EventHandler handler)>(32);

        public Enemy Spawn(Enemy prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            Enemy enemy = factory.Create(prefab, position, rotation, parent);
            Register(enemy);
            return enemy;
        }

        /// <summary>
        /// Advances delayed destroys for death animations. Call from the battle tick while running.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || pendingDestroys.Count == 0)
            {
                return;
            }

            for (int i = pendingDestroys.Count - 1; i >= 0; i--)
            {
                PendingEnemyDestroy pending = pendingDestroys[i];
                if (pending.Enemy == null)
                {
                    pendingDestroys.RemoveAt(i);
                    continue;
                }

                pending.SecondsLeft -= deltaTime;
                if (pending.SecondsLeft <= 0f)
                {
                    UnityEngine.Object.Destroy(pending.Enemy.gameObject);
                    pendingDestroys.RemoveAt(i);
                }
                else
                {
                    pendingDestroys[i] = pending;
                }
            }
        }

        public bool Register(Enemy enemy)
        {
            if (enemy == null || enemy.IsInitialized == false)
            {
                return false;
            }

            if (!alive.Add(enemy))
            {
                return false;
            }

            TrySubscribeToDeath(enemy);
            return true;
        }

        public bool Unregister(Enemy enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            UnsubscribeDeath(enemy);
            return alive.Remove(enemy);
        }

        public IReadOnlyCollection<Enemy> GetAllAlive()
        {
            PruneDestroyedFromTracking();
            return alive;
        }

        private void PruneDestroyedFromTracking()
        {
            if (alive.Count == 0)
            {
                return;
            }

            List<Enemy> toRemove = null;
            foreach (Enemy enemy in alive)
            {
                if (enemy == null)
                {
                    toRemove ??= new List<Enemy>();
                    toRemove.Add(enemy);
                }
            }

            if (toRemove == null)
            {
                return;
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                Enemy gone = toRemove[i];
                alive.Remove(gone);
                UnsubscribeDeath(gone);
            }
        }

        private void TrySubscribeToDeath(Enemy enemy)
        {
            Damageable damageable = enemy.GetComponentInChildren<Damageable>(true);
            if (damageable == null)
            {
                return;
            }

            if (deathSubscriptions.ContainsKey(enemy))
            {
                return;
            }

            EventHandler handler = (_, __) => HandleEnemyDied(enemy);
            damageable.Died += handler;
            deathSubscriptions[enemy] = (damageable, handler);
        }

        private void UnsubscribeDeath(Enemy enemy)
        {
            if (!deathSubscriptions.TryGetValue(enemy, out (Damageable damageable, EventHandler handler) sub))
            {
                return;
            }

            if (sub.damageable != null)
            {
                sub.damageable.Died -= sub.handler;
            }

            deathSubscriptions.Remove(enemy);
        }

        private void HandleEnemyDied(Enemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            alive.Remove(enemy);
            UnsubscribeDeath(enemy);

            float delaySeconds = 0f;
            Damageable damageable = enemy.GetComponentInChildren<Damageable>(true);
            if (damageable != null)
            {
                delaySeconds = Mathf.Max(0f, damageable.DestroyDelayAfterDeathSeconds);
            }

            if (delaySeconds <= 0f)
            {
                UnityEngine.Object.Destroy(enemy.gameObject);
                return;
            }

            pendingDestroys.Add(new PendingEnemyDestroy { Enemy = enemy, SecondsLeft = delaySeconds });
        }

        private struct PendingEnemyDestroy
        {
            public Enemy Enemy;

            public float SecondsLeft;
        }
    }
}
