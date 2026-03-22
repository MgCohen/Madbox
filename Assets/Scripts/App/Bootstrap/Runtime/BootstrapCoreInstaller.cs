using System;
using Madbox.LiveOps.Container;
using Madbox.Scope;
using VContainer;

namespace Madbox.App.Bootstrap
{
    internal sealed class BootstrapCoreInstaller : LayerInstallerBase
    {
        protected override void Install(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            LiveOpsInstaller liveOpsInstaller = new LiveOpsInstaller();
            liveOpsInstaller.Install(builder);
        }
    }
}
