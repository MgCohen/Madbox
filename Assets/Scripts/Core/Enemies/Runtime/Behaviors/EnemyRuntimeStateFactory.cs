using System;
using System.Collections.Generic;
using Madbox.Enemies;
using Madbox.Enemies.Contracts;
using Madbox.Levels;
using Madbox.Levels.Behaviors;

namespace Madbox.Enemies.Behaviors
{
    internal class EnemyRuntimeStateFactory
    {
        public EnemyRuntimeStateFactory()
        {
            RegisterRuntimeBehaviors();
        }

        private readonly Dictionary<Type, Func<EnemyBehaviorDefinition, IEnemyBehaviorRuntime>> runtimeBehaviorFactories = new Dictionary<Type, Func<EnemyBehaviorDefinition, IEnemyBehaviorRuntime>>();

        public Dictionary<EntityId, EnemyRuntimeState> CreateFromLevel(LevelDefinition definition)
        {
            if (EnsureDefinition(definition) == false) return new Dictionary<EntityId, EnemyRuntimeState>();
            Dictionary<EntityId, EnemyRuntimeState> enemies = new Dictionary<EntityId, EnemyRuntimeState>();
            PopulateEnemies(definition, enemies);
            return enemies;
        }

        private bool EnsureDefinition(LevelDefinition definition)
        {
            return definition != null;
        }

        private void PopulateEnemies(LevelDefinition definition, Dictionary<EntityId, EnemyRuntimeState> enemies)
        {
            int enemyIndex = 1;
            foreach (LevelEnemyDefinition levelEnemy in definition.Enemies)
{
    enemyIndex = AddEnemyBatch(enemies, levelEnemy, enemyIndex);
}
        }

        private int AddEnemyBatch(Dictionary<EntityId, EnemyRuntimeState> enemies, LevelEnemyDefinition levelEnemy, int enemyIndex)
        {
            for (int i = 0; i < levelEnemy.Count; i++)
            {
                CreateEnemy(enemies, levelEnemy, enemyIndex);
                enemyIndex++;
            }

            return enemyIndex;
        }

        private void CreateEnemy(Dictionary<EntityId, EnemyRuntimeState> enemies, LevelEnemyDefinition levelEnemy, int enemyIndex)
        {
            EntityId runtimeId = new EntityId($"enemy-{enemyIndex}");
            IEnemyBehaviorRuntime[] behaviors = CreateBehaviors(levelEnemy.Enemy.Behaviors);
            EnemyRuntimeState state = new EnemyRuntimeState(runtimeId, levelEnemy.Enemy, levelEnemy.Enemy.MaxHealth, behaviors);
            enemies.Add(runtimeId, state);
        }

        private IEnemyBehaviorRuntime[] CreateBehaviors(IReadOnlyList<EnemyBehaviorDefinition> behaviorDefinitions)
        {
            List<IEnemyBehaviorRuntime> behaviors = InitializeBehaviorList(behaviorDefinitions);
            if (behaviorDefinitions == null) return behaviors.ToArray();
            AddRuntimeBehaviors(behaviorDefinitions, behaviors);
            return behaviors.ToArray();
        }

        private List<IEnemyBehaviorRuntime> InitializeBehaviorList(IReadOnlyList<EnemyBehaviorDefinition> behaviorDefinitions)
        {
            int capacity = behaviorDefinitions?.Count ?? 0;
            return new List<IEnemyBehaviorRuntime>(capacity);
        }

        private void AddRuntimeBehaviors(IReadOnlyList<EnemyBehaviorDefinition> behaviorDefinitions, List<IEnemyBehaviorRuntime> behaviors)
        {
            for (int i = 0; i < behaviorDefinitions.Count; i++)
            {
                if (TryCreateRuntimeBehavior(behaviorDefinitions[i], out IEnemyBehaviorRuntime runtimeBehavior))
                {
                    behaviors.Add(runtimeBehavior);
                }
            }
        }

        private bool TryCreateRuntimeBehavior(EnemyBehaviorDefinition definition, out IEnemyBehaviorRuntime behavior)
        {
            if (definition == null) return ReturnNoBehavior(out behavior);
            Type behaviorType = definition.GetType();
            if (runtimeBehaviorFactories.TryGetValue(behaviorType, out Func<EnemyBehaviorDefinition, IEnemyBehaviorRuntime> factory) == false)
            {
                return ReturnNoBehavior(out behavior);
            }
            behavior = factory(definition);
            return behavior != null;
        }

        private bool ReturnNoBehavior(out IEnemyBehaviorRuntime behavior)
        {
            behavior = null;
            return false;
        }

        private void RegisterRuntimeBehaviors()
        {
            Register<ContactAttackBehaviorDefinition>(definition => new ContactAttackBehaviorRuntime(definition));
        }

        private void Register<TBehavior>(Func<TBehavior, IEnemyBehaviorRuntime> factory) where TBehavior : EnemyBehaviorDefinition
        {
            runtimeBehaviorFactories[typeof(TBehavior)] = definition => factory((TBehavior)definition);
        }
    }
}

