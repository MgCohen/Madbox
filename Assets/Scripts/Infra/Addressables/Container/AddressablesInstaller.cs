using Madbox.Scope.Contracts;
using Madbox.Addressables.Contracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.Addressables.Container
{
    public sealed class AddressablesInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IAddressablesAssetClient, AddressablesAssetClient>(Lifetime.Scoped);
            builder.Register<IAddressablesGateway, AddressablesGateway>(Lifetime.Scoped);
            builder.Register<IAsyncLayerInitializable, AddressablesLayerInitializer>(Lifetime.Scoped);
        }
    }
}
