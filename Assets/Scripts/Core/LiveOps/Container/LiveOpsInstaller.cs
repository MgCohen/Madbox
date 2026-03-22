using Madbox.LiveOps;
using VContainer;
using VContainer.Unity;

namespace Madbox.LiveOps.Container
{
    public sealed class LiveOpsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ILiveOpsService, LiveOpsService>(Lifetime.Scoped);
        }
    }
}
