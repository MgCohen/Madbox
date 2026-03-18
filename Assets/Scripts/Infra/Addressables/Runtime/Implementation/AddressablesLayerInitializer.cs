using System.Threading;
using System.Threading.Tasks;
using Madbox.Scope.Contracts;
using Madbox.Addressables.Contracts;

namespace Madbox.Addressables
{
    public sealed class AddressablesLayerInitializer : IAsyncLayerInitializable
    {
        public AddressablesLayerInitializer(IAddressablesGateway gateway)
        {
            GuardGateway(gateway);
            this.gateway = gateway;
        }

        private readonly IAddressablesGateway gateway;

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            GuardCancellation(cancellationToken);
            return gateway.InitializeAsync(cancellationToken);
        }

        private void GuardGateway(IAddressablesGateway gateway)
        {
            if (gateway == null) { throw new System.ArgumentNullException(nameof(gateway)); }
        }

        private void GuardCancellation(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
