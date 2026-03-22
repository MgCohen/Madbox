using Madbox.App.MainMenu;
using Madbox.LiveOps;
using Madbox.LiveOps.DTO;
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
        [SerializeField] private Transform viewHolder;

        protected override LayerInstallerBase BuildLayerTree()
        {
            BootstrapAssetInstaller asset = new BootstrapAssetInstaller();
            BootstrapInfraInstaller infra = new BootstrapInfraInstaller(viewHolder);
            BootstrapCoreInstaller core = new BootstrapCoreInstaller();
            asset.AddChild(infra);
            infra.AddChild(core);
            return asset;
        }

        protected override void OnBootstrapCompleted(LifetimeScope finalScope)
        {
            Debug.Log("Bootstrap completed");
            var service = finalScope.Container.Resolve<ILiveOpsService>();
            service.PingAsync(new PingRequest { Value = 1 });
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
