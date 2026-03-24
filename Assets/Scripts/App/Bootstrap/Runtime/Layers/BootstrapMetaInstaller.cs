using System;
using Madbox.Ads.Container;
using Madbox.Gold.Container;
using Madbox.Level.Container;
using Madbox.Scope;
using VContainer;

namespace Madbox.App.Bootstrap
{
    internal sealed class BootstrapMetaInstaller : LayerInstallerBase
    {
        protected override void Install(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            Install(builder, new GoldInstaller());
            Install(builder, new LevelInstaller());
            Install(builder, new AdsInstaller());
        }
    }
}
