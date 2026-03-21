using System;
using Madbox.Battle;
using Madbox.Enemies;
using Madbox.Enemies.Services;
using Madbox.Levels;

namespace Madbox.Battle.Events
{
    internal sealed class ResolvePlayerAttackCommand : IBattleCommand
    {
        public ResolvePlayerAttackCommand(EntityId sourceId, EntityId targetId)
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
            if (TryApplyDamage(context, out EnemyRuntimeState enemy, out bool enemyKilled) == false) return;
            EmitAttack(context);
            TryHandleEnemyKilled(context, enemy, enemyKilled);
        }

        private bool CanExecute(BattleExecutionContext context)
        {
            if (context == null) return false;
            return Equals(sourceId, context.Player.EntityId);
        }

        private bool TryApplyDamage(BattleExecutionContext context, out EnemyRuntimeState enemy, out bool enemyKilled)
        {
            enemyKilled = false;
            if (context.EnemyService.TryGetEnemy(targetId, out enemy) == false) return false;
            enemy.ApplyDamage(EnemyService.PlayerBaseDamage);
            enemyKilled = enemy.IsAlive == false;
            return true;
        }

        private void EmitAttack(BattleExecutionContext context)
        {
            PlayerAttack playerAttack = new PlayerAttack(sourceId, targetId, EnemyService.PlayerBaseDamage);
            context.EmitEvent(playerAttack);
        }

        private void TryHandleEnemyKilled(BattleExecutionContext context, EnemyRuntimeState enemy, bool enemyKilled)
        {
            if (enemyKilled == false) return;
            context.EnemyService.TryDisposeEnemy(enemy);
            EnemyKilled killedEvent = new EnemyKilled(targetId, sourceId);
            context.EmitEvent(killedEvent);
        }
    }
}

