using System;
using Madbox.Scope;
using VContainer;

namespace Madbox.App.Bootstrap
{
    internal sealed class BootstrapGameInstaller : LayerInstallerBase
    {
        protected override void Install(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            Install(builder, new BattleGameplayInstaller());
        }
    }
}
