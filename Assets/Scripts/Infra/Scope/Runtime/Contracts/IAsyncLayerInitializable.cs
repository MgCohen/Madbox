using System.Threading;
using System.Threading.Tasks;
using VContainer;

namespace Madbox.Scope.Contracts
{
    public interface IAsyncLayerInitializable
    {
        Task InitializeAsync(IObjectResolver resolver, CancellationToken cancellationToken);
    }
}
