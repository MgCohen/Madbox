using System.Threading.Tasks;
using Madbox.Ads;
using Madbox.App.MainMenu;
using Madbox.LiveOps;
using Madbox.Scope;
using Madbox.Scope.Contracts;
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
        [SerializeField] private BootstrapLoadingView bootstrapLoadingView;
        [SerializeField] private SceneFlowBootstrapShell sceneFlowBootstrapShell;

        protected override LayerInstallerBase BuildLayerTree()
        {
            Debug.Log("[BootstrapScope] Building Layer Tree");
            BootstrapAssetInstaller asset = new BootstrapAssetInstaller();
            BootstrapInfraInstaller infra = new BootstrapInfraInstaller(viewHolder, sceneFlowBootstrapShell);
            BootstrapMetaInstaller meta = new BootstrapMetaInstaller();
            BootstrapCoreInstaller core = new BootstrapCoreInstaller();
            asset.AddChild(infra);
            infra.AddChild(meta);
            meta.AddChild(core);
            return asset;
        }

        protected override void OnBootstrapCompleted(LifetimeScope finalScope)  
        {
            bootstrapLoadingView?.Hide();
            Debug.Log("Bootstrap completed");
            OpenMainMenu(finalScope);
        }

        private void OpenMainMenu(LifetimeScope finalScope)
        {
            Debug.Log("[BootstrapScope] Opening Main Menu");
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
