using System.Threading;
using System.Threading.Tasks;

namespace Madbox.Scope.Contracts
{
    public interface IAsyncLayerInitializable
    {
        Task InitializeAsync(CancellationToken cancellationToken);
    }
}
