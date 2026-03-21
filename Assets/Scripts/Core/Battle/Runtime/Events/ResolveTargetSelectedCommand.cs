using System;
using Madbox.Levels;

namespace Madbox.Battle.Events
{
    internal sealed class ResolveTargetSelectedCommand : IBattleCommand
    {
        public ResolveTargetSelectedCommand(EntityId actorId, EntityId targetId)
        {
            if (actorId == null) throw new ArgumentNullException(nameof(actorId));
            this.actorId = actorId;
            this.targetId = targetId;
        }

        private readonly EntityId actorId;
        private readonly EntityId targetId;

        public void Execute(BattleExecutionContext context)
        {
            if (CanExecute(context) == false) return;
            context.Player.SelectTarget(targetId);
            PlayerTargetChanged targetChanged = new PlayerTargetChanged(actorId, context.Player.SelectedTargetId);
            context.EmitEvent(targetChanged);
        }

        private bool CanExecute(BattleExecutionContext context)
        {
            if (context == null) return false;
            if (targetId == null) return false;
            return Equals(actorId, context.Player.EntityId);
        }
    }
}

