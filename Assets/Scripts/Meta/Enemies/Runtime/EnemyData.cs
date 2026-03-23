using Madbox.Entities;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Enemy entity <see cref="EntityData"/> (stats via <see cref="EnemyAttribute"/>). Place on the enemy root for hit collider resolution, spawn/tracking, and call <see cref="Initialize"/> after instantiate or pool get.
    /// </summary>
    public sealed class EnemyData : EntityData
    {
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Call after cloning the prefab or when taking a pooled instance so spawn registration and enemy brain behaviors can run.
        /// </summary>
        public void Initialize()
        {
            IsInitialized = true;
        }
    }
}
