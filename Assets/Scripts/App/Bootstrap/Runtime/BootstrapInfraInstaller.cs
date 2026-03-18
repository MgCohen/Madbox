using System;
using Madbox.Scope.Contracts;
using Scaffold.Events.Container;
using Scaffold.Navigation;
using Scaffold.Navigation.Container;
using UnityEngine;
using VContainer;

namespace Madbox.App.Bootstrap
{
    internal sealed class BootstrapInfraInstaller : ILayerInstaller
    {
        internal BootstrapInfraInstaller(NavigationSettings navigationSettings, Transform viewHolder)
        {
            this.navigationSettings = navigationSettings ?? throw new ArgumentNullException(nameof(navigationSettings));
            this.viewHolder = viewHolder ?? throw new ArgumentNullException(nameof(viewHolder));
        }

        private readonly NavigationSettings navigationSettings;
        private readonly Transform viewHolder;

        public void Install(IContainerBuilder builder)
        {
            ValidateBuilder(builder);
            InstallEvents(builder);
            InstallNavigation(builder);
        }

        private void InstallEvents(IContainerBuilder builder)
        {
            EventsInstaller installer = new EventsInstaller();
            installer.Install(builder);
        }

        private void InstallNavigation(IContainerBuilder builder)
        {
            NavigationInstaller installer = new NavigationInstaller(navigationSettings, viewHolder);
            installer.Install(builder);
        }

        private void ValidateBuilder(object builder)
        {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }
        }
    }
}
