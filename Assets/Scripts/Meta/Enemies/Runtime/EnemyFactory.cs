using System;
using UnityEngine;

namespace Madbox.Enemies
{
    public class EnemyFactory
    {
        public EnemyActor Create(EnemyActor prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            EnemyActor instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
            instance.Initialize();
            return instance;
        }
    }
}
