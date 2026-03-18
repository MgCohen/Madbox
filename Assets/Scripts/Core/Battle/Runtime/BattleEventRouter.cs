using System;
#pragma warning disable SCA0012
#pragma warning disable SCA0006
#pragma warning disable SCA0017
#pragma warning disable SCA0020

namespace Madbox.Battle
{
    internal class BattleEventRouter
    {
        private readonly EnemyService enemyService;
        private readonly Player player;
        private readonly Action<BattleEvent> emitEvent;

        public BattleEventRouter(EnemyService enemyService, Player player, Action<BattleEvent> emitEvent)
        {
            this.enemyService = enemyService ?? throw new ArgumentNullException(nameof(enemyService));
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.emitEvent = emitEvent ?? throw new ArgumentNullException(nameof(emitEvent));
        }

        public void Route(BattleEvent gameEvent)
        {
            if (gameEvent == null)
            {
                return;
            }

            switch (gameEvent)
            {
                case TryPlayerAttack attack:
                    HandleTryPlayerAttack(attack);
                    break;
                case EnemyHitObserved hit:
                    HandleEnemyHitObserved(hit);
                    break;
            }
        }

        private void HandleTryPlayerAttack(TryPlayerAttack attack)
        {
            enemyService.TryHandleTryPlayerAttack(attack, player, emitEvent);
        }

        private void HandleEnemyHitObserved(EnemyHitObserved hit)
        {
            enemyService.TryHandleEnemyHitObserved(hit, player, emitEvent);
        }
    }
}
