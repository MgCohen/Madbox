using Madbox.LiveOps.DTO;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.Services.PingPong
{
    public sealed class PingPongService
    {
        [CloudCodeFunction(nameof(PingRequest))]
        public PongResponse PingRequest(PingRequest request)
        {
            int incoming = request?.Value ?? 0;
            return new PongResponse { Value = incoming + 1 };
        }
    }
}
