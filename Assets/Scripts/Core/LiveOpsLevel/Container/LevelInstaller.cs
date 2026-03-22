using Madbox.Level;
using Madbox.LiveOps;
using Madbox.Scope.Contracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.Level.Container
{
    public sealed class LevelInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<LevelService>(Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
        }
    }
}
