using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Madbox.Addressables.Contracts
{
    public interface IAssetPreloadHandler
    {
        Task<IReadOnlyList<AddressablesPreloadRegistration>> BuildAsync(AddressablesPreloadConfig config, CancellationToken cancellationToken);
    }
}
