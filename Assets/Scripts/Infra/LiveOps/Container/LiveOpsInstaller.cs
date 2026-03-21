using global::Madbox.LiveOps.CloudCode;
using global::Madbox.LiveOps.Ugs;
using Madbox.LiveOps;
using MadboxLiveOpsContracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.LiveOps.Container
{
    public sealed class LiveOpsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IUgsInitializationService, UgsInitializationService>(Lifetime.Scoped);
            builder.Register<ICloudCodeModuleService, CloudCodeModuleService>(Lifetime.Scoped);
            builder.Register<ILiveOpsService, LiveOpsService>(Lifetime.Scoped);
        }
    }
}
