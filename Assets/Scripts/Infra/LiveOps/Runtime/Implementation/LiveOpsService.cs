using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using global::Madbox.LiveOps.CloudCode;
using MadboxLiveOpsContracts;
using UnityEngine;

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

        public bool UseFallback { get; set; } = true;

        public async Task<PingResponse> PingAsync(PingRequest request, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            if (request != null && request.Message == null)
            {
                throw new InvalidOperationException("Ping message must not be null.");
            }

            return await PingCoreAsync(request, cancellationToken);
        }

        private async Task<PingResponse> PingCoreAsync(PingRequest request, CancellationToken cancellationToken)
        {
            PingRequest safeRequest = request ?? new PingRequest(string.Empty);
            if (UseFallback)
            {
                return BuildFallbackResponse(safeRequest.Message);
            }

            Dictionary<string, object> payload = new Dictionary<string, object> { { "request", safeRequest } };
            string json = await CloudCode.CallModuleEndpointJsonAsync(moduleName, nameof(PingRequest), payload, cancellationToken);
            return JsonUtility.FromJson<PingResponse>(json);
        }

        private static PingResponse BuildFallbackResponse(string message)
        {
            return new PingResponse
            {
                Ok = true,
                Message = string.IsNullOrEmpty(message) ? "ping" : message,
                Source = "unity-fallback",
                ServerTimeUtc = DateTime.UtcNow.ToString("O")
            };
        }
    }
}
