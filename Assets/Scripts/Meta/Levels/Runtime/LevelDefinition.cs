using System.Collections.Generic;
using Madbox.Levels.Rules;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Levels
{
    [CreateAssetMenu(menuName = "Madbox/Levels/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        /// <summary>Must match remote-config ordered level IDs for the Level module.</summary>
        public int LevelId => levelId;

        public AssetReference SceneAssetReference => sceneAssetReference;

        public IReadOnlyList<LevelEnemySpawnEntry> EnemyEntries => enemyEntries;

        public IReadOnlyList<LevelRuleDefinition> GameRules => gameRules;

        [SerializeField] private int levelId;

        [SerializeField] private AssetReference sceneAssetReference;

        [SerializeField] private List<LevelEnemySpawnEntry> enemyEntries = new List<LevelEnemySpawnEntry>();

        [SerializeField] private List<LevelRuleDefinition> gameRules = new List<LevelRuleDefinition>();
    }
}
