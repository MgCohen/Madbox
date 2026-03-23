using System;
using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Ads;
using Madbox.LiveOps;

namespace Madbox.Ads
{
    /// <summary>
    /// Client module for ads runtime data from LiveOps <see cref="GameDataRequest"/> and <see cref="WatchAdRequest"/>.
    /// </summary>
    public sealed class AdsClientModule : GameClientModuleBase<AdData>
    {
        public async Task WatchAdAsync(ILiveOpsService liveOps, CancellationToken cancellationToken = default)
        {
            if (liveOps == null)
            {
                throw new ArgumentNullException(nameof(liveOps));
            }

            WatchAdRequest request = new WatchAdRequest();
            WatchAdResponse response = await liveOps.CallAsync(request, cancellationToken).ConfigureAwait(false);
            if (response?.Data != null)
            {
                data = response.Data;
            }
        }

        public bool IsAdAvailable()
        {
            return data != null && data.IsAdAvailable();
        }
    }
}
