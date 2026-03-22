using System.Threading;
using System.Threading.Tasks;
using Madbox.LiveOps.DTO;

namespace Madbox.LiveOps
{
    public interface ILiveOpsService
    {
        Task<PongResponse> PingAsync(PingRequest request, CancellationToken cancellationToken = default);
    }
}
