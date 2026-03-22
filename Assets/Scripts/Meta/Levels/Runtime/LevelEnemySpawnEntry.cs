using System;
using Madbox.Enemies;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Levels
{
    [Serializable]
    public sealed class LevelEnemySpawnEntry
    {
        public AssetReferenceT<EnemyActor> EnemyAssetReference => enemyAssetReference;
        public int Count => count;

        [SerializeField] private AssetReferenceT<EnemyActor> enemyAssetReference;
        [SerializeField, Min(1)] private int count = 1;
    }
}
