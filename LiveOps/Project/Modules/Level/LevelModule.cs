using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Madbox.LiveOps.CloudCode.FetchData;
using Madbox.LiveOps.CloudCode.GameModules;
using Madbox.LiveOps.CloudCode.Modules.Gold;
using Madbox.LiveOps.CloudCode.Response;
using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Modules.Common;
using Madbox.LiveOps.DTO.Modules.Level;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.Modules.Level
{
    public sealed class LevelModule : GameModule<LevelModuleData>
    {
        private readonly ILogger<LevelModule> _logger;
        private readonly GoldModule _goldModule;
        private readonly ModuleRequestHandler _moduleRequestHandler;

        public LevelModule(ILogger<LevelModule> logger, GoldModule goldModule, ModuleRequestHandler moduleRequestHandler)
        {
            _logger = logger;
            _goldModule = goldModule;
            _moduleRequestHandler = moduleRequestHandler;
        }

        public override bool Client => true;
        public override bool Server => false;

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData playerData, IGameState gameState, IRemoteConfig remoteConfig)
        {
            return await playerData.GetOrSet(context, new LevelModuleData());
        }

        [CloudCodeFunction(nameof(CompleteLevelRequest))]
        public async Task<CompleteLevelResponse> CompleteLevel(
            IExecutionContext context,
            IPlayerData playerData,
            IRemoteConfig remoteConfig,
            CompleteLevelRequest request)
        {
            LevelConfigData config = await remoteConfig.Get(context, new LevelConfigData());
            LevelModuleData data = await playerData.GetOrSet(context, new LevelModuleData());

            if (!config.Levels.Contains(request.LevelId))
            {
                _logger.LogWarning("[LevelModule] Level {LevelId} not in config list", request.LevelId);
                return await _moduleRequestHandler.ResolveResponse(context, request, new CompleteLevelResponse(data));
            }

            int currentLevel = 1;
            List<ModuleProgress> completedLevels = data.Progress.Where(p => p.Status == ModuleStatus.Completed).ToList();
            if (completedLevels.Any())
            {
                int maxCompleted = completedLevels.Select(p => int.TryParse(p.Id, out int val) ? val : 0).Max();
                currentLevel = maxCompleted + 1;
            }

            if (request.LevelId != currentLevel)
            {
                _logger.LogWarning("[LevelModule] Attempted level {Attempted} but current is {Current}", request.LevelId, currentLevel);
                return await _moduleRequestHandler.ResolveResponse(context, request, new CompleteLevelResponse(data));
            }

            data.SetProgress(request.LevelId.ToString(), ModuleStatus.Completed);
            playerData.AddToCache(data);

            await _goldModule.AddGoldToPlayer(context, playerData, remoteConfig, config.Reward);
            _logger.LogInformation("[LevelModule] Level {LevelId} completed for player {PlayerId}", request.LevelId, context.PlayerId);

            var response = new CompleteLevelResponse(data);
            return await _moduleRequestHandler.ResolveResponse(context, request, response);
        }
    }
}
