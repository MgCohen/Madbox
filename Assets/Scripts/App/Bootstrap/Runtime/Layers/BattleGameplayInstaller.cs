using System;
using Madbox.App.Gameplay;
using Madbox.Battle;
using Madbox.Enemies;
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

            builder.Register<EnemyFactory>(Lifetime.Transient).AsSelf();
            builder.Register<EnemyService>(Lifetime.Transient).AsSelf();
            builder.Register<RuleHandlerRegistry>(Lifetime.Singleton).AsSelf();

            builder.Register<BattleGameFactory>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<GameSessionCoordinator>(Lifetime.Singleton).AsSelf();

        }
    }
}
