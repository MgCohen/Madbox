using Madbox.Scope.Contracts;
using Madbox.Ugs;
using VContainer;
using VContainer.Unity;

namespace Madbox.Ugs.Container
{
    public sealed class UgsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<Ugs>(Lifetime.Singleton).AsSelf().As<IAsyncLayerInitializable>();
        }
    }
}
