using System;
using Madbox.App.GameView.Container;
using Madbox.Gold.Container;
using Madbox.LiveOps.Container;
using Madbox.Scope;
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

            LiveOpsInstaller liveOpsInstaller = new LiveOpsInstaller();
            liveOpsInstaller.Install(builder);

            GameViewInstaller gameViewInstaller = new GameViewInstaller();
            gameViewInstaller.Install(builder);
        }
    }
}
