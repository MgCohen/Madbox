using System;
using Madbox.Ads.Container;
using Madbox.Gold.Container;
using Madbox.Level.Container;
using Madbox.Tutorial.Container;
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

            GoldInstaller goldInstaller = new GoldInstaller();
            goldInstaller.Install(builder);

            LevelInstaller levelInstaller = new LevelInstaller();
            levelInstaller.Install(builder);

            AdsInstaller adsInstaller = new AdsInstaller();
            adsInstaller.Install(builder);

            TutorialInstaller tutorialInstaller = new TutorialInstaller();
            tutorialInstaller.Install(builder);
        }
    }
}
