using System;
using System.Collections.Generic;
using Madbox.Battle;
using Madbox.Battle.Behaviors;
using Madbox.Enemies.Services;
using Madbox.Levels;

namespace Madbox.Battle.Events
{
    internal sealed class ResolvePlayerAutoAttackCommand : IBattleCommand
    {
        public ResolvePlayerAutoAttackCommand(EntityId sourceId, EntityId targetId)
        {
            if (sourceId == null)
            {
                throw new ArgumentNullException(nameof(sourceId));
            }

            if (targetId == null)
            {
                throw new ArgumentNullException(nameof(targetId));
            }

            this.sourceId = sourceId;
            this.targetId = targetId;
        }

        private readonly EntityId sourceId;
        private readonly EntityId targetId;

        public void Execute(BattleExecutionContext context)
        {
            if (CanExecute(context) == false) return;
            PlayerAutoAttackBehaviorState autoAttackBehavior = ResolveAutoAttackBehavior(context.Player);
            if (autoAttackBehavior == null) return;
            if (TrySpawnProjectile(context, out PlayerProjectileSpawned spawnedEvent) == false) return;
            autoAttackBehavior.StartCooldown(context.Player.AttackCooldownSeconds);
            context.EmitEvent(spawnedEvent);
        }

        private PlayerAutoAttackBehaviorState ResolveAutoAttackBehavior(Player player)
        {
            if (player == null) return null;
            if (TryFindAutoAttack(player.Behaviors, out PlayerAutoAttackBehaviorState autoAttackBehavior)) return autoAttackBehavior;
            return null;
        }

        private bool TryFindAutoAttack(IReadOnlyList<IPlayerBehaviorRuntime> behaviors, out PlayerAutoAttackBehaviorState autoAttackBehavior)
        {
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is PlayerAutoAttackBehaviorState foundBehavior)
                {
                    return ReturnAutoAttack(foundBehavior, out autoAttackBehavior);
                }
            }
            autoAttackBehavior = null;
            return false;
        }

        private bool ReturnAutoAttack(PlayerAutoAttackBehaviorState behavior, out PlayerAutoAttackBehaviorState autoAttackBehavior)
        {
            autoAttackBehavior = behavior;
            return true;
        }

        private bool TrySpawnProjectile(BattleExecutionContext context, out PlayerProjectileSpawned spawnedEvent)
        {
            return context.ProjectileRegistry.TrySpawn(sourceId, targetId, EnemyService.PlayerBaseDamage, out spawnedEvent);
        }

        private bool CanExecute(BattleExecutionContext context)
        {
            if (context == null) return false;
            if (Equals(sourceId, context.Player.EntityId) == false) return false;
            PlayerAutoAttackBehaviorState autoAttackBehavior = ResolveAutoAttackBehavior(context.Player);
            if (autoAttackBehavior == null || autoAttackBehavior.CanAttack() == false) return false;
            return context.EnemyService.HasLiveEnemy(targetId);
        }
    }
}

