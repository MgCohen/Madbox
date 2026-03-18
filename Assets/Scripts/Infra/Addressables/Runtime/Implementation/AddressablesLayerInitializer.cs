using System.Threading;
using System.Threading.Tasks;
using Madbox.Scope.Contracts;
using Scaffold.Addressables.Contracts;

namespace Scaffold.Addressables
{
    public sealed class AddressablesLayerInitializer : IAsyncLayerInitializable
    {
        private readonly IAddressablesGateway gateway;

        public AddressablesLayerInitializer(IAddressablesGateway gateway)
        {
            this.gateway = gateway;
        }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            return gateway.InitializeAsync(cancellationToken);
        }
    }
}
