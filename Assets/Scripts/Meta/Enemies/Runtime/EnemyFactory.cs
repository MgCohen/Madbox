using System;
using UnityEngine;

namespace Madbox.Enemies
{
    public class EnemyFactory
    {
        public EnemyData Create(EnemyData prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            EnemyData instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
            instance.Initialize();
            return instance;
        }
    }
}
