using Madbox.App.Bootstrap.Player;
using Madbox.LiveOps.Container;
using Madbox.Scope;
using System;
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

            Install(builder, new PlayerInstaller());
            Install(builder, new LiveOpsInstaller());
        }
    }
}
