using System;
using Madbox.Battle;
using Madbox.Enemies;
using Madbox.Levels;

namespace Madbox.Battle.Events
{
    internal sealed class ResolvePlayerProjectileHitCommand : IBattleCommand
    {
        public ResolvePlayerProjectileHitCommand(EntityId projectileId, EntityId sourceId, EntityId targetId)
        {
            if (projectileId == null)
            {
                throw new ArgumentNullException(nameof(projectileId));
            }

            if (sourceId == null)
            {
                throw new ArgumentNullException(nameof(sourceId));
            }

            if (targetId == null)
            {
                throw new ArgumentNullException(nameof(targetId));
            }

            this.projectileId = projectileId;
            this.sourceId = sourceId;
            this.targetId = targetId;
        }

        private readonly EntityId projectileId;
        private readonly EntityId sourceId;
        private readonly EntityId targetId;

        public void Execute(BattleExecutionContext context)
        {
            if (CanExecute(context) == false) return;
            if (TryConsumeProjectile(context, out PendingProjectile projectile) == false) return;
            if (TryApplyDamage(context, projectile, out bool enemyKilled) == false) return;
            EmitAttack(context, projectile.Damage);
            TryEmitEnemyKilled(context, enemyKilled);
        }

        private bool CanExecute(BattleExecutionContext context)
        {
            if (context == null) return false;
            return Equals(sourceId, context.Player.EntityId);
        }

        private bool TryConsumeProjectile(BattleExecutionContext context, out PendingProjectile projectile)
        {
            return context.ProjectileRegistry.TryConsume(projectileId, sourceId, targetId, out projectile);
        }

        private bool TryApplyDamage(BattleExecutionContext context, PendingProjectile projectile, out bool enemyKilled)
        {
            enemyKilled = false;
            if (context.EnemyService.TryGetEnemy(targetId, out EnemyRuntimeState enemy) == false) return false;
            enemy.ApplyDamage(projectile.Damage);
            if (enemy.IsAlive) return true;
            context.EnemyService.TryDisposeEnemy(enemy);
            enemyKilled = true;
            return true;
        }

        private void EmitAttack(BattleExecutionContext context, int damage)
        {
            PlayerAttack attackEvent = new PlayerAttack(sourceId, targetId, damage);
            context.EmitEvent(attackEvent);
        }

        private void TryEmitEnemyKilled(BattleExecutionContext context, bool enemyKilled)
        {
            if (enemyKilled == false) return;
            EnemyKilled killedEvent = new EnemyKilled(targetId, sourceId);
            context.EmitEvent(killedEvent);
        }
    }
}

