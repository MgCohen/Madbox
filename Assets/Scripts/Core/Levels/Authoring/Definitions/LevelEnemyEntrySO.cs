using System;
using Madbox.Enemies.Authoring.Definitions;
using UnityEngine;
#pragma warning disable SCA0007
#pragma warning disable SCA0020

namespace Madbox.Levels.Authoring.Definitions
{
    [Serializable]
    public sealed class LevelEnemyEntrySO
    {
        [SerializeField] private EnemyDefinitionSO enemy;
        [SerializeField] private int count = 1;

        public EnemyDefinitionSO Enemy => enemy;

        public int Count => count;
    }
}
