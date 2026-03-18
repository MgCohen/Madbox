using System;
using System.Collections.Generic;
using System.Reflection;
using Madbox.App.MainMenu;
using Madbox.Scope;
using Madbox.Scope.Contracts;
using Scaffold.Navigation;
using Scaffold.Navigation.Contracts;
using Scaffold.Types;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Madbox.App.Bootstrap
{
    public sealed class BootstrapScope : LayeredScope
    {
        [SerializeField] private NavigationSettings navigationSettings;
        [SerializeField] private Transform viewHolder;

        protected override IReadOnlyList<ILayerInstaller> BuildLayerInstallers()
        {
            return new ILayerInstaller[]
            {
                new BootstrapAssetInstaller(),
                new BootstrapInfraInstaller(navigationSettings, viewHolder)
            };
        }

        protected override void OnBootstrapCompleted(LifetimeScope finalScope)
        {
            Debug.Log("Bootstrap completed");
            OpenMainMenu(finalScope);
        }

        private void OpenMainMenu(LifetimeScope finalScope)
        {
            if (finalScope == null || finalScope.Container == null) { return; }
            INavigation navigation = finalScope.Container.Resolve<INavigation>();
            MainMenuViewModel mainMenu = new MainMenuViewModel();
            navigation.Open(mainMenu);
        }
    }
}
