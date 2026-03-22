using System.Threading;
using System.Threading.Tasks;
using Madbox.LiveOps.DTO;
using UnityEngine;
using VContainer;

namespace Madbox.LiveOps
{
    public sealed class LiveOpsBootstrapProbe : MonoBehaviour
    {
        [SerializeField] private int pingValue = 1;

        [Inject] private ILiveOpsService liveOpsService;

        public async Task<PongResponse> PingAsync(CancellationToken cancellationToken = default)
        {
            return await RunPingAsync(cancellationToken);
        }

        private async Task<PongResponse> RunPingAsync(CancellationToken cancellationToken)
        {
            if (liveOpsService == null)
            {
                Debug.LogError("LiveOps service is not injected.");
                return new PongResponse { Value = -1 };
            }

            PingRequest pingRequest = new PingRequest { Value = pingValue };
            PongResponse response = await liveOpsService.PingAsync(pingRequest, cancellationToken);
            Debug.Log($"LiveOps ping => ping:{pingRequest.Value} pong:{response.Value}");
            return response;
        }
    }
}
