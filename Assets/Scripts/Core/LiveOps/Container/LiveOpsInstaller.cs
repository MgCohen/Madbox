using Madbox.LiveOps;
using Madbox.Scope.Contracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.LiveOps.Container
{
    public sealed class LiveOpsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<LiveOpsService>(Lifetime.Scoped)
                .As<ILiveOpsService>()
                .As<IAsyncLayerInitializable>();
        }
    }
}
