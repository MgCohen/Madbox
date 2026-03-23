using VContainer;
using VContainer.Unity;

namespace Madbox.App.Bootstrap.Player
{
    public sealed class PlayerInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<PlayerService>(Lifetime.Scoped).AsSelf();
            builder.Register<PlayerFactory>(Lifetime.Scoped).AsSelf();
        }
    }
}
