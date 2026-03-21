using System;
using System.Collections.Generic;
using Madbox.Battle;
using Madbox.Battle.Services;
using Madbox.Enemies.Services;

namespace Madbox.Battle.Events
{
    internal class BattleEventRouter
    {
        public BattleEventRouter(EnemyService enemyService, Player player, Action<BattleEvent> emitEvent)
        {
            if (enemyService == null)
            {
                throw new ArgumentNullException(nameof(enemyService));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (emitEvent == null)
            {
                throw new ArgumentNullException(nameof(emitEvent));
            }

            this.enemyService = enemyService;
            this.player = player;
            this.emitEvent = emitEvent;
            RegisterHandlers();
        }

        private readonly EnemyService enemyService;
        private readonly Player player;
        private readonly Action<BattleEvent> emitEvent;
        private readonly Dictionary<Type, Func<BattleEvent, IBattleCommand>> commandFactories = new Dictionary<Type, Func<BattleEvent, IBattleCommand>>();
        private readonly ProjectileRegistry projectileRegistry = new ProjectileRegistry();

        public void Route(BattleEvent gameEvent)
        {
            if (TryResolveRoute(gameEvent, out IBattleCommand command) == false) return;
            ExecuteCommand(command);
        }

        public void Tick(float deltaTime)
        {
            if (EnsureTick(deltaTime) == false) return;
            for (int i = 0; i < player.Behaviors.Count; i++)
{
    player.Behaviors[i].Tick(deltaTime);
}
        }

        private bool TryResolveRoute(BattleEvent gameEvent, out IBattleCommand command)
        {
            if (gameEvent == null)
            {
                command = null;
                return false;
            }

            return TryResolveCommand(gameEvent, out command);
        }

        private void ExecuteCommand(IBattleCommand command)
        {
            BattleExecutionContext context = CreateContext();
            command.Execute(context);
        }

        private BattleExecutionContext CreateContext()
        {
            return new BattleExecutionContext(enemyService, player, projectileRegistry, emitEvent);
        }

        private bool TryResolveCommand(BattleEvent gameEvent, out IBattleCommand command)
        {
            Type eventType = gameEvent.GetType();
            if (commandFactories.TryGetValue(eventType, out Func<BattleEvent, IBattleCommand> factory) == false)
            {
                return ReturnNoCommand(out command);
            }
            command = factory(gameEvent);
            return command != null;
        }

        private bool ReturnNoCommand(out IBattleCommand command)
        {
            command = null;
            return false;
        }

        private void RegisterHandlers()
        {
            RegisterCoreCombatHandlers();
            RegisterPlayerContractHandlers();
            RegisterObservedCombatHandlers();
        }

        private void RegisterCoreCombatHandlers()
        {
            Register<TryPlayerAttack, ResolvePlayerAttackCommand>(intent => new ResolvePlayerAttackCommand(intent.ActorId, intent.TargetId));
            Register<EquipPlayerWeaponIntent, ResolveEquipPlayerWeaponCommand>(intent => new ResolveEquipPlayerWeaponCommand(intent.ActorId, intent.Weapon));
        }

        private void RegisterPlayerContractHandlers()
        {
            Register<PlayerMovementStarted, ResolvePlayerMovementStartedCommand>(intent => new ResolvePlayerMovementStartedCommand(intent.ActorId, intent.Speed));
            Register<PlayerMovementStopped, ResolvePlayerMovementStoppedCommand>(intent => new ResolvePlayerMovementStoppedCommand(intent.ActorId));
            Register<TargetSelected, ResolveTargetSelectedCommand>(intent => new ResolveTargetSelectedCommand(intent.ActorId, intent.TargetId));
            Register<TargetCleared, ResolveTargetClearedCommand>(intent => new ResolveTargetClearedCommand(intent.ActorId));
            Register<AutoAttackTriggered, ResolveAutoAttackTriggeredCommand>(intent => new ResolveAutoAttackTriggeredCommand(intent.ActorId));
        }

        private void RegisterObservedCombatHandlers()
        {
            Register<EnemyHitObserved, ResolveEnemyHitObservedCommand>(intent => new ResolveEnemyHitObservedCommand(intent.EnemyId, intent.PlayerId, intent.RawDamage));
            Register<PlayerAutoAttackObserved, ResolvePlayerAutoAttackCommand>(intent => new ResolvePlayerAutoAttackCommand(intent.ActorId, intent.TargetId));
            Register<PlayerProjectileHitObserved, ResolvePlayerProjectileHitCommand>(intent => new ResolvePlayerProjectileHitCommand(intent.ProjectileId, intent.ActorId, intent.TargetId));
        }

        private void Register<TIntent, TCommand>(Func<TIntent, TCommand> factory) where TIntent : BattleEvent where TCommand : IBattleCommand
        {
            commandFactories[typeof(TIntent)] = WrapFactory(factory);
        }

        private Func<BattleEvent, IBattleCommand> WrapFactory<TIntent, TCommand>(Func<TIntent, TCommand> factory) where TIntent : BattleEvent where TCommand : IBattleCommand
        {
            return battleEvent =>
            {
                TIntent intent = battleEvent as TIntent;
                if (intent == null) return null;
                return factory(intent);
            };
        }

        private bool EnsureTick(float deltaTime)
        {
            return deltaTime > 0f;
        }
    }
}

