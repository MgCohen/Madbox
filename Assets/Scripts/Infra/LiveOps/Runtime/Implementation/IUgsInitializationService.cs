using System.Threading;
using System.Threading.Tasks;

namespace Madbox.LiveOps
{
    public interface IUgsInitializationService
    {
        Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
    }
}
