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

        protected override async void OnBootstrapCompleted(LifetimeScope finalScope)  
        {
            bootstrapLoadingView?.Hide();
            Debug.Log("Bootstrap completed");

            try
            {
                var gateway = finalScope.Container.Resolve<Madbox.Addressables.Contracts.IAddressablesGateway>();
                
                // DUMP ALL KEYS IN ADDRESSABLES TO PROVE WHAT'S AVAILABLE
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("<color=yellow>--- LIST OF ALL LOADED ADDRESSABLE KEYS ---</color>");
                foreach (var locator in UnityEngine.AddressableAssets.Addressables.ResourceLocators)
                {
                    foreach (object key in locator.Keys)
                    {
                        if (key is string s && !s.EndsWith(".bundle") && !s.EndsWith(".hash") && !s.EndsWith(".json"))
                        {
                            sb.AppendLine(" - " + s);
                        }
                    }
                }
                sb.AppendLine("<color=yellow>-------------------------------------------</color>");
                Debug.Log(sb.ToString());

                // FIX THE KEY TO THE PROPER ADDRESSABLE NAME
                Debug.Log("[Test] Starting to load GreatSword from Addressables...");
                var handle = await gateway.LoadAsync<GameObject>(new UnityEngine.AddressableAssets.AssetReference("GreatSword"));
                Debug.Log($"<color=green>[Test] SUCCESSFULLY LOADED ADDRESSABLE: {handle.Asset?.name}</color>");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Test] FAILED to load addressable: {e.Message}");
            }

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
