using System;
using Madbox.Battle;
using Madbox.Battle.Services;
using Madbox.Enemies.Services;

namespace Madbox.Battle.Events
{
    internal sealed class BattleExecutionContext
    {
        public BattleExecutionContext(EnemyService enemyService, Player player, ProjectileRegistry projectileRegistry, Action<BattleEvent> emitEvent)
        {
            if (enemyService == null)
            {
                throw new ArgumentNullException(nameof(enemyService));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (projectileRegistry == null)
            {
                throw new ArgumentNullException(nameof(projectileRegistry));
            }

            if (emitEvent == null)
            {
                throw new ArgumentNullException(nameof(emitEvent));
            }

            EnemyService = enemyService;
            Player = player;
            ProjectileRegistry = projectileRegistry;
            EmitEvent = emitEvent;
        }

        public EnemyService EnemyService { get; }

        public Player Player { get; }

        public ProjectileRegistry ProjectileRegistry { get; }

        public Action<BattleEvent> EmitEvent { get; }
    }
}

