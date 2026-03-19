using System;
using Madbox.Levels;

namespace Madbox.Battle.Events
{
    internal sealed class ResolvePlayerMovementStartedCommand : IBattleCommand
    {
        public ResolvePlayerMovementStartedCommand(EntityId actorId, float speed)
        {
            if (actorId == null) throw new ArgumentNullException(nameof(actorId));
            this.actorId = actorId;
            this.speed = speed;
        }

        private readonly EntityId actorId;
        private readonly float speed;

        public void Execute(BattleExecutionContext context)
        {
            if (CanExecute(context) == false) return;
            context.Player.StartMoving(speed);
            PlayerMovementChanged movementChanged = new PlayerMovementChanged(actorId, context.Player.IsMoving, context.Player.MovementSpeed);
            context.EmitEvent(movementChanged);
        }

        private bool CanExecute(BattleExecutionContext context)
        {
            if (context == null) return false;
            if (speed < 0f) return false;
            return Equals(actorId, context.Player.EntityId);
        }
    }
}
