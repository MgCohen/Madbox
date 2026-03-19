using System;
using Madbox.Battle;
using Madbox.Levels;

namespace Madbox.Battle.Events
{
    internal sealed class ResolveEquipPlayerWeaponCommand : IBattleCommand
    {
        public ResolveEquipPlayerWeaponCommand(EntityId actorId, WeaponProfile weapon)
        {
            if (actorId == null) throw new ArgumentNullException(nameof(actorId));
            this.actorId = actorId;
            this.weapon = weapon;
        }

        private readonly EntityId actorId;
        private readonly WeaponProfile weapon;

        public void Execute(BattleExecutionContext context)
        {
            if (CanExecute(context) == false) return;
            context.Player.EquipWeapon(weapon);
            PlayerWeaponEquipped equippedEvent = new PlayerWeaponEquipped(actorId, context.Player.EquippedWeapon.Id);
            context.EmitEvent(equippedEvent);
            EmitAttackData(context);
        }

        private bool CanExecute(BattleExecutionContext context)
        {
            if (context == null) return false;
            if (weapon == null) return false;
            return Equals(actorId, context.Player.EntityId);
        }

        private void EmitAttackData(BattleExecutionContext context)
        {
            PlayerAutoAttackDataUpdated attackData = new PlayerAutoAttackDataUpdated(actorId, context.Player.AttackCooldownSeconds, context.Player.AttackRange, context.Player.AttackTimingNormalized);
            context.EmitEvent(attackData);
        }
    }
}
