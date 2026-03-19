using System;
using Madbox.Levels;

namespace Madbox.Battle.Events
{
    internal sealed class ResolvePlayerMovementStoppedCommand : IBattleCommand
    {
        public ResolvePlayerMovementStoppedCommand(EntityId actorId)
        {
            if (actorId == null) throw new ArgumentNullException(nameof(actorId));
            this.actorId = actorId;
        }

        private readonly EntityId actorId;

        public void Execute(BattleExecutionContext context)
        {
            if (CanExecute(context) == false) return;
            context.Player.StopMoving();
            PlayerMovementChanged movementChanged = new PlayerMovementChanged(actorId, context.Player.IsMoving, context.Player.MovementSpeed);
            context.EmitEvent(movementChanged);
        }

        private bool CanExecute(BattleExecutionContext context)
        {
            if (context == null) return false;
            return Equals(actorId, context.Player.EntityId);
        }
    }
}
