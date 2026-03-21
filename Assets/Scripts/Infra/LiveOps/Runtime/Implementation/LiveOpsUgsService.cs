using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.LiveOps.Contracts;
using Unity.Services.CloudCode;
using UnityEngine;

namespace Madbox.LiveOps
{
    public sealed class LiveOpsUgsService : ILiveOpsService
    {
        private const string moduleName = "LiveOps";
        private readonly IUgsInitializationService ugsInitializationService;

        public LiveOpsUgsService(IUgsInitializationService ugsInitializationService)
        {
            if (ugsInitializationService == null)
            {
                throw new ArgumentNullException(nameof(ugsInitializationService));
            }

            this.ugsInitializationService = ugsInitializationService;
        }

        public bool UseFallback { get; set; } = true;

        public async Task<PingResponse> PingAsync(PingRequest request, CancellationToken cancellationToken = default)
        {
            PingRequest safeRequest = request ?? new PingRequest(string.Empty);
            if (UseFallback)
            {
                return BuildFallbackResponse(safeRequest.Message);
            }

            await ugsInitializationService.EnsureInitializedAsync(cancellationToken);
            Dictionary<string, object> payload = new Dictionary<string, object> { { "request", safeRequest } };
            string json = await CloudCodeService.Instance.CallModuleEndpointAsync(moduleName, nameof(PingRequest), payload);
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
