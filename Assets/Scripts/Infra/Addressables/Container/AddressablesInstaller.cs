using Madbox.Scope.Contracts;
using Scaffold.Addressables.Contracts;
using VContainer;
using VContainer.Unity;

namespace Scaffold.Addressables.Container
{
    public sealed class AddressablesInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IAddressablesAssetClient, AddressablesAssetClient>(Lifetime.Scoped);
            builder.Register<IAddressablesPreloadRegistry, AddressablesPreloadRegistry>(Lifetime.Scoped);
            builder.Register<IAddressablesGateway, AddressablesGateway>(Lifetime.Scoped);
            builder.Register<IAsyncLayerInitializable, AddressablesLayerInitializer>(Lifetime.Scoped);
        }
    }
}
