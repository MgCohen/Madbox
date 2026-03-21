using Madbox.LiveOps.Contracts;
using Unity.Services.CloudCode.Core;

namespace LiveOps.CloudCode.Services
{
    public sealed class LiveOpsModule
    {
        [CloudCodeFunction(nameof(PingRequest))]
        public PingResponse Ping(PingRequest request)
        {
            string message = request?.Message ?? string.Empty;
            return new PingResponse
            {
                Ok = true,
                Message = string.IsNullOrEmpty(message) ? "ping" : message,
                Source = "cloud-code",
                ServerTimeUtc = DateTime.UtcNow.ToString("O")
            };
        }
    }
}
