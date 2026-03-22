using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Level;
using Madbox.Addressables.Contracts;
using Madbox.LiveOps;
using Madbox.Levels;

namespace Madbox.Level
{
    /// <summary>
    /// Client LiveOps level module: <see cref="LevelGameData"/> plus Addressables <see cref="LevelDefinition"/> assets mapped by level id.
    /// </summary>
    public class LevelService : GameClientModuleBase<LevelGameData>, ILevelService
    {
        public LevelService(ILiveOpsService liveOpsService, IAssetGroupProvider<LevelDefinition> levelAssetProvider)
        {
            if (liveOpsService == null)
            {
                throw new ArgumentNullException(nameof(liveOpsService));
            }
            if (levelAssetProvider == null)
            {
                throw new ArgumentNullException(nameof(levelAssetProvider));
            }

            this.liveOpsService = liveOpsService;
            this.levelAssetProvider = levelAssetProvider;
        }

        private readonly ILiveOpsService liveOpsService;
        private readonly IAssetGroupProvider<LevelDefinition> levelAssetProvider;
        private IReadOnlyList<AvailableLevel> availableLevels = Array.Empty<AvailableLevel>();

        public IReadOnlyList<AvailableLevel> GetAvailableLevels()
        {
            return availableLevels;
        }

        public async Task<CompleteLevelResponse> CompleteLevelAsync(int levelId, CancellationToken cancellationToken = default)
        {
            if (levelId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(levelId));
            }

            CompleteLevelRequest request = new CompleteLevelRequest(levelId);
            CompleteLevelResponse response = await liveOpsService.CallAsync(request, cancellationToken).ConfigureAwait(false);
            RefreshFromLiveOps();
            return response;
        }

        private void RefreshFromLiveOps()
        {
            LevelGameData refreshed = liveOpsService.GetModuleData<LevelGameData>();
            if (refreshed == null)
            {
                return;
            }
            data = refreshed;
            BuildAvailableLevels(refreshed);
        }

        protected override Task OnInitializedAsync(LevelGameData moduleData)
        {
            BuildAvailableLevels(moduleData);
            return Task.CompletedTask;
        }

        private void BuildAvailableLevels(LevelGameData moduleData)
        {
            if (moduleData == null)
            {
                availableLevels = Array.Empty<AvailableLevel>();
                return;
            }

            if (!levelAssetProvider.TryGet(out IReadOnlyList<LevelDefinition> definitions) || definitions == null || definitions.Count == 0)
            {
                availableLevels = Array.Empty<AvailableLevel>();
                return;
            }

            Dictionary<int, LevelDefinition> byId = new Dictionary<int, LevelDefinition>();
            for (int i = 0; i < definitions.Count; i++)
            {
                LevelDefinition def = definitions[i];
                if (def == null)
                {
                    continue;
                }
                byId[def.LevelId] = def;
            }

            List<AvailableLevel> list = new List<AvailableLevel>();
            foreach (LevelStateEntry state in moduleData.States)
            {
                if (byId.TryGetValue(state.LevelId, out LevelDefinition definition))
                {
                    list.Add(new AvailableLevel(definition, state.State));
                }
            }

            availableLevels = list;
        }
    }
}
