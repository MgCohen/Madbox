using System.Threading;
using System.Threading.Tasks;
using Madbox.LiveOps.Contracts;
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
            if (liveOpsService == null)
            {
                Debug.LogError("LiveOps service is not injected.");
                return new PingResponse { Ok = false, Message = "service-not-injected", Source = "unity-probe" };
            }

            if (liveOpsService is LiveOpsUgsService ugs)
            {
                ugs.UseFallback = useFallback;
            }

            PingResponse response = await liveOpsService.PingAsync(new PingRequest(message), cancellationToken);
            Debug.Log($"LiveOps ping => ok:{response.Ok} source:{response.Source} message:{response.Message}");
            return response;
        }
    }
}
