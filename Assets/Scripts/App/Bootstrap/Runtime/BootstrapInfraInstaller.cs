using System;
using Madbox.App.GameView.Container;
using Madbox.CloudCode.Container;
using Madbox.Gold.Container;
using Madbox.Scope;
using Madbox.Ugs.Container;
using Scaffold.Events.Container;
using Scaffold.Navigation;
using Scaffold.Navigation.Container;
using UnityEngine;
using VContainer;

namespace Madbox.App.Bootstrap
{
    internal sealed class BootstrapInfraInstaller : LayerInstallerBase
    {
        internal BootstrapInfraInstaller(Transform viewHolder)
        {
            this.viewHolder = viewHolder ?? throw new ArgumentNullException(nameof(viewHolder));
        }

        private readonly Transform viewHolder;

        protected override void Install(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            EventsInstaller eventsInstaller = new EventsInstaller();
            eventsInstaller.Install(builder);
            
            NavigationInstaller navigationInstaller = new NavigationInstaller(viewHolder);
            navigationInstaller.Install(builder);

            GoldInstaller goldInstaller = new GoldInstaller();
            goldInstaller.Install(builder);

            UgsInstaller ugsInstaller = new UgsInstaller();
            ugsInstaller.Install(builder);

            CloudCodeInstaller cloudCodeInstaller = new CloudCodeInstaller();
            cloudCodeInstaller.Install(builder);

            GameViewInstaller gameViewInstaller = new GameViewInstaller();
            gameViewInstaller.Install(builder);
        }
    }
}
