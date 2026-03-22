using System;
using Madbox.Enemies;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Levels
{
    [Serializable]
    public sealed class LevelEnemySpawnEntry
    {
        public AssetReference EnemyAssetReference => enemyAssetReference;
        public int Count => count;

        [SerializeField] private AssetReference enemyAssetReference;
        [SerializeField, Min(1)] private int count = 1;
    }
}
