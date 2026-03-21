using Madbox.Addressables.Contracts;
using Madbox.Scope.Contracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.Addressables.Container
{
    public sealed class AddressablesInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            IAddressablesAssetClient assetClient = new AddressablesAssetClient();
            IAssetReferenceHandler assetReferenceHandler = new AddressablesAssetReferenceHandler(assetClient);
            IAssetPreloadHandler assetPreloadHandler = new AddressablesAssetPreloadHandler(assetClient);
            RegisterGateway(builder, assetClient, assetReferenceHandler, assetPreloadHandler);
        }

        private void RegisterGateway(IContainerBuilder builder, IAddressablesAssetClient assetClient, IAssetReferenceHandler assetReferenceHandler, IAssetPreloadHandler assetPreloadHandler)
        {
            builder.Register<IAddressablesGateway, AddressablesGateway>(Lifetime.Scoped)
                .WithParameter<IAddressablesAssetClient>(assetClient)
                .WithParameter<IAssetReferenceHandler>(assetReferenceHandler)
                .WithParameter<IAssetPreloadHandler>(assetPreloadHandler)
                .AsImplementedInterfaces();
        }
    }
}

