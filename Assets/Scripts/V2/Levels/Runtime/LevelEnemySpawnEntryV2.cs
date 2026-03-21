using System;
using Madbox.V2.Enemies;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.V2.Levels
{
    [Serializable]
    public sealed class LevelEnemySpawnEntryV2
    {
        public AssetReferenceT<EnemyActor> EnemyAssetReference => enemyAssetReference;
        public int Count => count;

        [SerializeField] private AssetReferenceT<EnemyActor> enemyAssetReference;
        [SerializeField, Min(1)] private int count = 1;
    }
}
