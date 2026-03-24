using Madbox.LiveOps;
using Madbox.Scope.Contracts;
using Madbox.Levels;
using VContainer;
using VContainer.Unity;

namespace Madbox.Level.Container
{
    /// <summary>
    /// Registers <see cref="LevelService"/> (LiveOps + Addressables level definitions) for meta scope.
    /// </summary>
    public sealed class LevelInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<LevelService>(Lifetime.Singleton)
                .AsSelf()
                .As<ILevelService>()
                .As<IGameClientModule>()
                .As<IAsyncLayerInitializable>();
        }
    }
}