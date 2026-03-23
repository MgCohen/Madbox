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
            RegisterGateway(builder, assetClient, assetReferenceHandler);
        }

        private void RegisterGateway(IContainerBuilder builder, IAddressablesAssetClient assetClient, IAssetReferenceHandler assetReferenceHandler)
        {
            // Singleton: layered bootstrap calls CreateScope per layer; Scoped would allocate one gateway per scope and run catalog sync multiple times.
            builder.Register<IAddressablesGateway, AddressablesGateway>(Lifetime.Singleton)
                .WithParameter<IAddressablesAssetClient>(assetClient)
                .WithParameter<IAssetReferenceHandler>(assetReferenceHandler)
                .AsImplementedInterfaces();
        }
    }
}
