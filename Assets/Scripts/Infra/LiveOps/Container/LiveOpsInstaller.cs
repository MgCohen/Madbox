using Madbox.LiveOps.Contracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.LiveOps.Container
{
    public sealed class LiveOpsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IUgsInitializationService, UgsInitializationService>(Lifetime.Scoped);
            builder.Register<ILiveOpsService, LiveOpsUgsService>(Lifetime.Scoped);
        }
    }
}
