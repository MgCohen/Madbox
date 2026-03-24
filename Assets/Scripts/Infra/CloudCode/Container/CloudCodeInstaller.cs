using Madbox.CloudCode;
using VContainer;
using VContainer.Unity;

namespace Madbox.CloudCode.Container
{
    public sealed class CloudCodeInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ICloudCodeModuleService, CloudCodeModuleService>(Lifetime.Singleton);
        }
    }
}
