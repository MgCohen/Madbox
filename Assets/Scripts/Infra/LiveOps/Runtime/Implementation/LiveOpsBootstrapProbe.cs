using System.Threading;
using System.Threading.Tasks;
using MadboxLiveOpsContracts;
using UnityEngine;
using VContainer;

namespace Madbox.LiveOps
{
    public sealed class LiveOpsBootstrapProbe : MonoBehaviour
    {
        [SerializeField] private bool useFallback = true;
        [SerializeField] private string message = "hello";

        [Inject] private ILiveOpsService liveOpsService;

        public async Task<PingResponse> PingAsync(CancellationToken cancellationToken = default)
        {
            return await RunPingAsync(cancellationToken);
        }

        private async Task<PingResponse> RunPingAsync(CancellationToken cancellationToken)
        {
            if (liveOpsService == null)
            {
                Debug.LogError("LiveOps service is not injected.");
                return new PingResponse { Ok = false, Message = "service-not-injected", Source = "unity-probe" };
            }

            if (liveOpsService is LiveOpsService liveOps) liveOps.UseFallback = useFallback;
            PingRequest pingRequest = new PingRequest(message);
            PingResponse response = await liveOpsService.PingAsync(pingRequest, cancellationToken);
            Debug.Log($"LiveOps ping => ok:{response.Ok} source:{response.Source} message:{response.Message}");
            return response;
        }
    }
}
