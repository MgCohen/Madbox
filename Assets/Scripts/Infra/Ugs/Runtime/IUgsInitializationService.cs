using System.Threading;
using System.Threading.Tasks;

namespace Madbox.Ugs
{
    public interface IUgsInitializationService
    {
        Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
    }
}
