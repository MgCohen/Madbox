using System;
using System.Collections.Generic;
using Madbox.Levels;
#pragma warning disable SCA0003
#pragma warning disable SCA0005
#pragma warning disable SCA0006
#pragma warning disable SCA0009
#pragma warning disable SCA0012
#pragma warning disable SCA0014
#pragma warning disable SCA0017
#pragma warning disable SCA0020

namespace Madbox.Battle
{
    internal class EnemyService
    {
        private const int PlayerBaseDamage = 10;
        private const float SpawnDistanceToPlayer = 3f;

        private readonly Dictionary<EntityId, EnemyRuntimeState> enemiesByRuntimeId;

        public EnemyService(LevelDefinition definition)
        {
            enemiesByRuntimeId = CreateEnemies(definition);
            AliveEnemies = enemiesByRuntimeId.Count;
        }

        public int AliveEnemies { get; private set; }

        public int DeadEnemies { get; private set; }

        public void Tick(float deltaTime)
        {
            foreach (EnemyRuntimeState enemy in enemiesByRuntimeId.Values)
            {
                if (enemy.IsAlive == false)
                {
                    continue;
                }

                if (enemy.AttackCooldownRemaining > 0f)
                {
                    enemy.AttackCooldownRemaining = Math.Max(0f, enemy.AttackCooldownRemaining - deltaTime);
                }

                MovementBehaviorDefinition movement = FindBehavior<MovementBehaviorDefinition>(enemy.Definition.Behaviors);
                if (movement == null)
                {
                    continue;
                }

                float movedDistance = movement.MoveSpeed * deltaTime;
                enemy.DistanceToPlayer = Math.Max(0f, enemy.DistanceToPlayer - movedDistance);
            }
        }

        public bool TryGetEnemyDistance(EntityId enemyId, out float distance)
        {
            if (enemyId == null)
            {
                distance = default;
                return false;
            }

            if (enemiesByRuntimeId.TryGetValue(enemyId, out EnemyRuntimeState enemy) == false)
            {
                distance = default;
                return false;
            }

            distance = enemy.DistanceToPlayer;
            return true;
        }

        public bool TryHandleTryPlayerAttack(TryPlayerAttack attack, Player player, Action<BattleEvent> emitEvent)
        {
            if (attack == null || player == null || emitEvent == null)
            {
                return false;
            }

            if (Equals(attack.ActorId, player.EntityId) == false)
            {
                return false;
            }

            if (attack.TargetId == null)
            {
                return false;
            }

            if (enemiesByRuntimeId.TryGetValue(attack.TargetId, out EnemyRuntimeState enemy) == false)
            {
                return false;
            }

            if (enemy.IsAlive == false)
            {
                return false;
            }

            enemy.CurrentHealth -= PlayerBaseDamage;
            emitEvent(new PlayerAttack(attack.ActorId, attack.TargetId, PlayerBaseDamage));

            if (enemy.IsAlive == false)
            {
                AliveEnemies = Math.Max(0, AliveEnemies - 1);
                DeadEnemies++;
                emitEvent(new EnemyKilled(enemy.RuntimeEntityId, attack.ActorId));
            }

            return true;
        }

        public bool TryHandleEnemyHitObserved(EnemyHitObserved hit, Player player, Action<BattleEvent> emitEvent)
        {
            if (hit == null || player == null || emitEvent == null)
            {
                return false;
            }

            if (Equals(hit.PlayerId, player.EntityId) == false)
            {
                return false;
            }

            if (hit.RawDamage <= 0)
            {
                return false;
            }

            if (enemiesByRuntimeId.TryGetValue(hit.EnemyId, out EnemyRuntimeState enemy) == false)
            {
                return false;
            }

            if (enemy.IsAlive == false)
            {
                return false;
            }

            ContactAttackBehaviorDefinition attackBehavior = FindBehavior<ContactAttackBehaviorDefinition>(enemy.Definition.Behaviors);
            if (attackBehavior == null)
            {
                return false;
            }

            if (enemy.DistanceToPlayer > attackBehavior.AttackRange)
            {
                return false;
            }

            if (enemy.AttackCooldownRemaining > 0f)
            {
                return false;
            }

            int appliedDamage = Math.Min(hit.RawDamage, attackBehavior.Damage);
            if (appliedDamage <= 0)
            {
                return false;
            }

            int nextHealth = player.ApplyDamage(appliedDamage);
            enemy.AttackCooldownRemaining = Math.Max(0f, attackBehavior.CooldownSeconds);
            emitEvent(new PlayerDamaged(player.EntityId, hit.EnemyId, appliedDamage, nextHealth));

            if (nextHealth == 0)
            {
                emitEvent(new PlayerKilled(player.EntityId, hit.EnemyId));
            }

            return true;
        }

        private static Dictionary<EntityId, EnemyRuntimeState> CreateEnemies(LevelDefinition definition)
        {
            Dictionary<EntityId, EnemyRuntimeState> enemies = new Dictionary<EntityId, EnemyRuntimeState>();
            int enemyIndex = 1;

            foreach (LevelEnemyDefinition levelEnemy in definition.Enemies)
            {
                for (int i = 0; i < levelEnemy.Count; i++)
                {
                    EntityId runtimeId = new EntityId($"enemy-{enemyIndex}");
                    enemyIndex++;
                    enemies.Add(
                        runtimeId,
                        new EnemyRuntimeState(runtimeId, levelEnemy.Enemy, levelEnemy.Enemy.MaxHealth, SpawnDistanceToPlayer));
                }
            }

            return enemies;
        }

        private static TBehavior FindBehavior<TBehavior>(IReadOnlyList<EnemyBehaviorDefinition> behaviors)
            where TBehavior : EnemyBehaviorDefinition
        {
            if (behaviors == null)
            {
                return null;
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is TBehavior behavior)
                {
                    return behavior;
                }
            }

            return null;
        }
    }
}
