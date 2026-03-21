using System;
using System.Collections.Generic;
using Madbox.Battle;
using Madbox.Enemies;
using Madbox.Enemies.Behaviors;
using Madbox.Enemies.Contracts;
using Madbox.Levels;

namespace Madbox.Battle.Events
{
    internal sealed class ResolveEnemyHitObservedCommand : IBattleCommand
    {
        public ResolveEnemyHitObservedCommand(EntityId enemyId, EntityId playerId, int rawDamage)
        {
            if (enemyId == null)
            {
                throw new ArgumentNullException(nameof(enemyId));
            }

            if (playerId == null)
            {
                throw new ArgumentNullException(nameof(playerId));
            }

            this.enemyId = enemyId;
            this.playerId = playerId;
            this.rawDamage = rawDamage;
        }

        private readonly EntityId enemyId;
        private readonly EntityId playerId;
        private readonly int rawDamage;

        public void Execute(BattleExecutionContext context)
        {
            if (context == null) return;
            if (TryResolve(context, out int appliedDamage, out int remainingHp, out bool playerKilled) == false) return;
            EmitPlayerDamaged(context, appliedDamage, remainingHp);
            TryEmitPlayerKilled(context, playerKilled);
        }

        private bool TryResolve(BattleExecutionContext context, out int appliedDamage, out int remainingHp, out bool playerKilled)
        {
            if (CanResolve(context) == false) return ReturnNoResolution(out appliedDamage, out remainingHp, out playerKilled);
            if (TryResolveAppliedDamage(context, out appliedDamage) == false) return ReturnNoResolution(out appliedDamage, out remainingHp, out playerKilled);
            return ResolvePlayerDamage(context, appliedDamage, out remainingHp, out playerKilled);
        }

        private bool TryResolveAppliedDamage(BattleExecutionContext context, out int appliedDamage)
        {
            appliedDamage = default;
            if (context.EnemyService.TryGetEnemy(enemyId, out EnemyRuntimeState enemy) == false) return false;
            ContactAttackBehaviorRuntime attackBehavior = ResolveContactAttack(enemy);
            if (attackBehavior == null) return false;
            return attackBehavior.TryConsume(rawDamage, out appliedDamage);
        }

        private bool ResolvePlayerDamage(BattleExecutionContext context, int appliedDamage, out int remainingHp, out bool playerKilled)
        {
            remainingHp = context.Player.ApplyDamage(appliedDamage);
            playerKilled = remainingHp == 0;
            return true;
        }

        private ContactAttackBehaviorRuntime ResolveContactAttack(EnemyRuntimeState enemy)
        {
            if (enemy == null) return null;
            if (TryFindContactAttack(enemy.Behaviors, out ContactAttackBehaviorRuntime attackBehavior)) return attackBehavior;
            return null;
        }

        private bool TryFindContactAttack(IReadOnlyList<IEnemyBehaviorRuntime> behaviors, out ContactAttackBehaviorRuntime attackBehavior)
        {
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is ContactAttackBehaviorRuntime resolvedBehavior)
                {
                    return ReturnContactAttack(resolvedBehavior, out attackBehavior);
                }
            }
            attackBehavior = null;
            return false;
        }

        private bool ReturnContactAttack(ContactAttackBehaviorRuntime behavior, out ContactAttackBehaviorRuntime attackBehavior)
        {
            attackBehavior = behavior;
            return true;
        }

        private bool CanResolve(BattleExecutionContext context)
        {
            if (context == null) return false;
            if (rawDamage <= 0) return false;
            return Equals(playerId, context.Player.EntityId);
        }

        private bool ReturnNoResolution(out int appliedDamage, out int remainingHp, out bool playerKilled)
        {
            appliedDamage = default;
            remainingHp = default;
            playerKilled = false;
            return false;
        }

        private void EmitPlayerDamaged(BattleExecutionContext context, int appliedDamage, int remainingHp)
        {
            PlayerDamaged damagedEvent = new PlayerDamaged(context.Player.EntityId, enemyId, appliedDamage, remainingHp);
            context.EmitEvent(damagedEvent);
        }

        private void TryEmitPlayerKilled(BattleExecutionContext context, bool playerKilled)
        {
            if (playerKilled == false) return;
            PlayerKilled playerKilledEvent = new PlayerKilled(context.Player.EntityId, enemyId);
            context.EmitEvent(playerKilledEvent);
        }
    }
}

