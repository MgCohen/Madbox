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

            EventsInstaller eventsInstaller = new EventsInstaller();
            eventsInstaller.Install(builder);

            NavigationInstaller navigationInstaller = new NavigationInstaller(navigationSettings, viewHolder);
            navigationInstaller.Install(builder);
        }

        private void ValidateBuilder(object builder)
        {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }
        }
    }
}
