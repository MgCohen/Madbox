using Madbox.App.MainMenu;
using Madbox.Scope;
using Scaffold.Navigation;
using Scaffold.Navigation.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Madbox.App.Bootstrap
{
    public sealed class BootstrapScope : LayeredScope
    {
        [SerializeField] private NavigationSettings navigationSettings;
        [SerializeField] private Transform viewHolder;

        protected override LayerInstallerBase BuildLayerTree()
        {
            BootstrapAssetInstaller asset = new BootstrapAssetInstaller();
            BootstrapInfraInstaller infra = new BootstrapInfraInstaller(navigationSettings, viewHolder);
            asset.AddChild(infra);
            return asset;
        }

        protected override void OnBootstrapCompleted(LifetimeScope finalScope)
        {
            Debug.Log("Bootstrap completed");
            OpenMainMenu(finalScope);
        }

        private void OpenMainMenu(LifetimeScope finalScope)
        {
            if (finalScope == null || finalScope.Container == null)
            {
                return;
            }

            INavigation navigation = finalScope.Container.Resolve<INavigation>();
            MainMenuViewModel mainMenu = new MainMenuViewModel();
            navigation.Open(mainMenu);
        }
    }
}
