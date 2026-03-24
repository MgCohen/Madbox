using Madbox.Entities;
using UnityEngine;

namespace Madbox.Enemies
{
    /// <summary>
    /// Enemy entity <see cref="Entity"/> (stats via <see cref="EntityAttribute"/>). Place on the enemy root for hit collider resolution, spawn/tracking, and call <see cref="Initialize"/> after instantiate or pool get.
    /// </summary>
    public sealed class Enemy : Entity
    {
        public bool IsInitialized { get; private set; }
        public void Initialize()
        {
            IsInitialized = true;
        }
    }
}
