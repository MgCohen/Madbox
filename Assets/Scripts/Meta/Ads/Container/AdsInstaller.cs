using Madbox.Ads;
using Madbox.LiveOps;
using Madbox.Scope.Contracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.Ads.Container
{
    public sealed class AdsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<AdsClientModule>(Lifetime.Scoped).AsSelf().As<IGameClientModule>().As<IAsyncLayerInitializable>();
        }
    }
}
