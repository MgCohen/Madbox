using System;
using Madbox.Entities;
using UnityEngine;

namespace Madbox.Enemies
{
    public class EnemyFactory
    {
        public Enemy Create(Enemy prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            Enemy instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
            // Prefabs may serialize a non-zero default local position; force world spawn after instantiate.
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.Initialize();
            Damageable damageable = instance.GetComponentInChildren<Damageable>(true);
            if (damageable != null)
            {
                damageable.ResetToFullHealth();
            }

            return instance;
        }
    }
}
