using System.Threading;
using System.Threading.Tasks;

namespace MadboxLiveOpsContracts
{
    public interface ILiveOpsService
    {
        Task<PingResponse> PingAsync(PingRequest request, CancellationToken cancellationToken = default);
    }
}
