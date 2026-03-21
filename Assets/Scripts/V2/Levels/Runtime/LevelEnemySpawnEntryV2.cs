using System;
using Madbox.V2.Enemies;
using UnityEngine;

namespace Madbox.V2.Levels
{
    [Serializable]
    public sealed class LevelEnemySpawnEntryV2
    {
        public EnemyActor EnemyPrefab => enemyPrefab;
        public int Count => count;

        [SerializeField] private EnemyActor enemyPrefab;
        [SerializeField, Min(1)] private int count = 1;
    }
}
