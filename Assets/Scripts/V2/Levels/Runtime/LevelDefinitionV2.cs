using System.Collections.Generic;
using Madbox.V2.Levels.Rules;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.V2.Levels
{
    [CreateAssetMenu(menuName = "Madbox/V2/Level Definition")]
    public sealed class LevelDefinitionV2 : ScriptableObject
    {
        public AssetReference SceneAssetReference => sceneAssetReference;

        public IReadOnlyList<LevelEnemySpawnEntryV2> EnemyEntries => enemyEntries;

        public IReadOnlyList<LevelRuleDefinitionV2> GameRules => gameRules;

        [SerializeField] private AssetReference sceneAssetReference;

        [SerializeField] private List<LevelEnemySpawnEntryV2> enemyEntries = new List<LevelEnemySpawnEntryV2>();

        [SerializeField] private List<LevelRuleDefinitionV2> gameRules = new List<LevelRuleDefinitionV2>();
    }
}
