using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Orchestrates enemy lifecycle: spawns via <see cref="EnemyFactory"/> and tracks alive instances.
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

        public int AliveEnemies => alive.Count;

        private readonly EnemyFactory factory;

        private readonly HashSet<EnemyData> alive = new HashSet<EnemyData>();

        public EnemyData Spawn(EnemyData prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            EnemyData enemy = factory.Create(prefab, position, rotation, parent);
            Register(enemy);
            return enemy;
        }

        public bool Register(EnemyData enemy)
        {
            if (enemy == null || enemy.IsInitialized == false)
            {
                return false;
            }

            return alive.Add(enemy);
        }

        public bool Unregister(EnemyData enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            return alive.Remove(enemy);
        }

        public IReadOnlyCollection<EnemyData> GetAllAlive()
        {
            return alive;
        }
    }
}
