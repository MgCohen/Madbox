using System;
using System.Collections.Generic;
using Madbox.Levels;
using Madbox.Levels.Behaviors;
using UnityEngine;
using UnityEngine.AddressableAssets;
#pragma warning disable SCA0006
#pragma warning disable SCA0007
#pragma warning disable SCA0020

namespace Madbox.Enemies.Authoring.Definitions
{
    [CreateAssetMenu(menuName = "Madbox/Authoring/Enemy Definition")]
    public sealed class EnemyDefinitionSO : ScriptableObject
    {
        [SerializeField] private string enemyTypeId = "enemy";
        [SerializeField] private int maxHealth = 10;
        [SerializeReference] private List<EnemyBehaviorDefinition> behaviorRules = new List<EnemyBehaviorDefinition>();
        [SerializeField] private AssetReferenceGameObject prefabReference;

        public AssetReferenceGameObject PrefabReference => prefabReference;

        public EnemyDefinition ToDomain()
        {
            IReadOnlyList<EnemyBehaviorDefinition> behaviorDefinitions = CopyBehaviorRules();
            EntityId entityId = new EntityId(enemyTypeId);
            return new EnemyDefinition(entityId, maxHealth, behaviorDefinitions);
        }

        private IReadOnlyList<EnemyBehaviorDefinition> CopyBehaviorRules()
        {
            if (behaviorRules == null)
            {
                return Array.Empty<EnemyBehaviorDefinition>();
            }

            List<EnemyBehaviorDefinition> copied = new List<EnemyBehaviorDefinition>(behaviorRules.Count);
            for (int i = 0; i < behaviorRules.Count; i++)
{
    copied.Add(behaviorRules[i]);
}

            return copied;
        }
    }
}

