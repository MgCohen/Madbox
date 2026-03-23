using System;
using Madbox.App.Gameplay;
using Madbox.Battle;
using Madbox.Enemies;
using Madbox.Levels.Rules;
using VContainer;
using VContainer.Unity;

namespace Madbox.App.Bootstrap
{
    internal sealed class BattleGameplayInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Register<EnemyFactory>(Lifetime.Transient);
            builder.Register<EnemyService>(Lifetime.Transient);
            builder.Register(_ =>
            {
                RuleHandlerRegistry registry = new RuleHandlerRegistry();
                registry.Register<TimeElapsedCompleteRule, TimeElapsedCompleteRuleHandler>();
                return registry;
            }, Lifetime.Singleton).AsSelf();

            builder.Register<BattleGameFactory>(Lifetime.Singleton);
            builder.Register<Func<EnemyService>>(c => () => c.Resolve<EnemyService>(), Lifetime.Singleton);
            builder.Register<GameSessionCoordinator>(Lifetime.Singleton);

            builder.Register<IGameFlowService, GameNavigationFlowService>(Lifetime.Singleton);
            builder.Register<IMainMenuLauncher, BootstrapMainMenuLauncher>(Lifetime.Singleton);
            builder.Register<IPlayerSpawnService, PlayerSpawnBridge>(Lifetime.Singleton);
        }
    }
}
