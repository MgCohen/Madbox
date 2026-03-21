using System.Threading;
using System.Threading.Tasks;

namespace Madbox.LiveOps.Contracts
{
    public interface ILiveOpsService
    {
        Task<PingResponse> PingAsync(PingRequest request, CancellationToken cancellationToken = default);
    }
}
