using Madbox.CloudCode;
using Madbox.LiveOps.DTO;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Madbox.LiveOps
{
    public sealed class LiveOpsService : ILiveOpsService
    {
        private const string moduleName = "LiveOps";

        public LiveOpsService(ICloudCodeModuleService cloudCodeModuleService)
        {
            if (cloudCodeModuleService == null)
            {
                throw new ArgumentNullException(nameof(cloudCodeModuleService));
            }

            CloudCode = cloudCodeModuleService;
        }

        private ICloudCodeModuleService CloudCode { get; }

        public async Task<PongResponse> PingAsync(PingRequest request, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            return await PingCoreAsync(request, cancellationToken);
        }

        private async Task<PongResponse> PingCoreAsync(PingRequest request, CancellationToken cancellationToken)
        {
            PingRequest safeRequest = request ?? new PingRequest();
            Dictionary<string, object> payload = new Dictionary<string, object> { { "request", safeRequest } };
            PongResponse response = await CloudCode.CallEndpointAsync<PongResponse>(moduleName, nameof(PingRequest), 2, 2, payload);
            return response;
        }
    }
}
