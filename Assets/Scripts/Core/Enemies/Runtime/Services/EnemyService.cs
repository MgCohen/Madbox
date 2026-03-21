using System;
using System.Collections.Generic;
using Madbox.Enemies.Behaviors;
using Madbox.Levels;

namespace Madbox.Enemies.Services
{
    public class EnemyService
    {
        public const int PlayerBaseDamage = 10;

        public EnemyService(LevelDefinition definition)
        {
            if (EnsureDefinition(definition) == false)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            enemiesByRuntimeId = enemyFactory.CreateFromLevel(definition);
            AliveEnemies = enemiesByRuntimeId.Count;
        }

        public int AliveEnemies { get; private set; }

        public int DeadEnemies { get; private set; }

        private readonly Dictionary<EntityId, EnemyRuntimeState> enemiesByRuntimeId;
        private readonly EnemyRuntimeStateFactory enemyFactory = new EnemyRuntimeStateFactory();

        public void Tick(float deltaTime)
        {
            if (EnsureTick(deltaTime) == false) return;
            TickBehaviors(deltaTime);
        }

        public bool HasLiveEnemy(EntityId enemyId)
        {
            if (enemyId == null) return false;
            return TryGetEnemy(enemyId, out _);
        }

        public bool TryGetEnemy(EntityId enemyId, out EnemyRuntimeState enemy)
        {
            if (EnsureTryGetEnemyInput(enemyId, out enemy) == false) return false;
            if (TryResolveEnemy(enemyId, out enemy) == false) return false;
            return enemy.IsAlive;
        }

        public bool TryDisposeEnemy(EnemyRuntimeState enemy)
        {
            if (EnsureDisposeEnemyInput(enemy) == false) return false;
            if (enemiesByRuntimeId.Remove(enemy.RuntimeEntityId) == false) return false;
            AliveEnemies = Math.Max(0, AliveEnemies - 1);
            DeadEnemies++;
            return true;
        }

        private bool EnsureDefinition(LevelDefinition definition)
        {
            return definition != null;
        }

        private bool EnsureTick(float deltaTime)
        {
            return deltaTime > 0f;
        }

        private bool EnsureDisposeEnemyInput(EnemyRuntimeState enemy)
        {
            if (enemy == null) return false;
            return enemy.IsAlive == false;
        }

        private bool EnsureTryGetEnemyInput(EntityId enemyId, out EnemyRuntimeState enemy)
        {
            if (enemyId != null)
            {
                enemy = null;
                return true;
            }

            enemy = null;
            return false;
        }

        private bool TryResolveEnemy(EntityId enemyId, out EnemyRuntimeState enemy)
        {
            return enemiesByRuntimeId.TryGetValue(enemyId, out enemy);
        }

        private void TickBehaviors(float deltaTime)
        {
            foreach (EnemyRuntimeState enemy in enemiesByRuntimeId.Values)
            {
                if (enemy.IsAlive == false) continue;
                for (int i = 0; i < enemy.Behaviors.Count; i++)
{
    enemy.Behaviors[i].Tick(deltaTime);
}
            }
        }
    }
}

