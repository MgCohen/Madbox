using System;
using Madbox.CloudCode.Container;
using Madbox.Gold.Container;
using Madbox.LiveOps.Container;
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

            RunInfraInstallers(builder);
        }

        private void RunInfraInstallers(IContainerBuilder builder)
        {
            InstallSharedInfra(builder);
        }

        private void InstallSharedInfra(IContainerBuilder builder)
        {
            EventsInstaller eventsInstaller = new EventsInstaller();
            eventsInstaller.Install(builder);
            NavigationInstaller navigationInstaller = new NavigationInstaller(viewHolder);
            navigationInstaller.Install(builder);
            UgsInstaller ugsInstaller = new UgsInstaller();
            ugsInstaller.Install(builder);
            CloudCodeInstaller cloudCodeInstaller = new CloudCodeInstaller();
            cloudCodeInstaller.Install(builder);
        }
    }
}
