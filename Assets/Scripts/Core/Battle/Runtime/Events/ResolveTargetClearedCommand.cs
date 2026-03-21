using System;
using Madbox.Levels;

namespace Madbox.Battle.Events
{
    internal sealed class ResolveTargetClearedCommand : IBattleCommand
    {
        public ResolveTargetClearedCommand(EntityId actorId)
        {
            if (actorId == null) throw new ArgumentNullException(nameof(actorId));
            this.actorId = actorId;
        }

        private readonly EntityId actorId;

        public void Execute(BattleExecutionContext context)
        {
            if (CanExecute(context) == false) return;
            context.Player.ClearTarget();
            PlayerTargetChanged targetChanged = new PlayerTargetChanged(actorId, context.Player.SelectedTargetId);
            context.EmitEvent(targetChanged);
        }

        private bool CanExecute(BattleExecutionContext context)
        {
            if (context == null) return false;
            return Equals(actorId, context.Player.EntityId);
        }
    }
}

