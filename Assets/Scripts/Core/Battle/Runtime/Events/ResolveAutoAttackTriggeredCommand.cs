using System;
using Madbox.Levels;

namespace Madbox.Battle.Events
{
    internal sealed class ResolveAutoAttackTriggeredCommand : IBattleCommand
    {
        public ResolveAutoAttackTriggeredCommand(EntityId actorId)
        {
            if (actorId == null) throw new ArgumentNullException(nameof(actorId));
            this.actorId = actorId;
        }

        private readonly EntityId actorId;

        public void Execute(BattleExecutionContext context)
        {
            if (CanExecute(context) == false) return;
            PlayerAutoAttackDataUpdated attackData = new PlayerAutoAttackDataUpdated(actorId, context.Player.AttackCooldownSeconds, context.Player.AttackRange, context.Player.AttackTimingNormalized);
            context.EmitEvent(attackData);
        }

        private bool CanExecute(BattleExecutionContext context)
        {
            if (context == null) return false;
            return Equals(actorId, context.Player.EntityId);
        }
    }
}

