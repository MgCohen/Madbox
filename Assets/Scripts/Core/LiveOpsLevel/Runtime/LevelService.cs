using System;
using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Level;
using Madbox.LiveOps;

namespace Madbox.Level
{
    /// <summary>
    /// Client LiveOps level progression module: <see cref="LevelGameData"/> from aggregated <see cref="GameModuleDTO.ModuleRequests.GameDataRequest"/> results.
    /// </summary>
    public class LevelService : GameClientModuleBase<LevelGameData>
    {
        public LevelService(ILiveOpsService liveOpsService)
        {
            if (liveOpsService == null)
            {
                throw new ArgumentNullException(nameof(liveOpsService));
            }

            this.liveOpsService = liveOpsService;
        }

        private readonly ILiveOpsService liveOpsService;

        public Task<CompleteLevelResponse> CompleteLevelAsync(int levelId, CancellationToken cancellationToken = default)
        {
            if (levelId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(levelId));
            }

            CompleteLevelRequest request = new CompleteLevelRequest(levelId);
            return liveOpsService.CallAsync(request, cancellationToken);
        }
    }
}
