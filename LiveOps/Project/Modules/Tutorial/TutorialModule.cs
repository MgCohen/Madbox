using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Madbox.LiveOps.CloudCode.FetchData;
using Madbox.LiveOps.CloudCode.GameModules;
using Madbox.LiveOps.CloudCode.Modules.Gold;
using Madbox.LiveOps.CloudCode.Response;
using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Modules.Common;
using Madbox.LiveOps.DTO.Modules.Tutorial;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.Modules.Tutorial
{
    public sealed class TutorialModule : GameModule<TutorialModuleData>
    {
        private readonly ILogger<TutorialModule> _logger;
        private readonly GoldModule _goldModule;
        private readonly ModuleRequestHandler _moduleRequestHandler;

        public TutorialModule(ILogger<TutorialModule> logger, GoldModule goldModule, ModuleRequestHandler moduleRequestHandler)
        {
            _logger = logger;
            _goldModule = goldModule;
            _moduleRequestHandler = moduleRequestHandler;
        }

        public override bool Client => true;
        public override bool Server => false;

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData playerData, IGameState gameState, IRemoteConfig remoteConfig)
        {
            await remoteConfig.Get(context, new TutorialConfigData());
            return await playerData.GetOrSet(context, new TutorialModuleData());
        }

        [CloudCodeFunction(nameof(CompleteTutorialRequest))]
        public async Task<CompleteTutorialResponse> CompleteTutorial(
            IExecutionContext context,
            IPlayerData playerData,
            IRemoteConfig remoteConfig,
            CompleteTutorialRequest request)
        {
            TutorialConfigData config = await remoteConfig.Get(context, new TutorialConfigData());
            TutorialModuleData data = await playerData.GetOrSet(context, new TutorialModuleData());

            if (!config.Tutorials.Contains(request.TutorialId))
            {
                _logger.LogWarning("[TutorialModule] Tutorial step {Step} not in config list", request.TutorialId);
                return await _moduleRequestHandler.ResolveResponse(context, request, new CompleteTutorialResponse(data));
            }

            int currentTutorialStep = 1;
            List<ModuleProgress> completedSteps = data.Progress.Where(p => p.Status == ModuleStatus.Completed).ToList();
            if (completedSteps.Any())
            {
                int maxCompleted = completedSteps.Select(p => int.TryParse(p.Id, out int val) ? val : 0).Max();
                currentTutorialStep = maxCompleted + 1;
            }

            if (request.TutorialId != currentTutorialStep)
            {
                _logger.LogWarning("[TutorialModule] Attempted step {Attempted} but current is {Current}", request.TutorialId, currentTutorialStep);
                return await _moduleRequestHandler.ResolveResponse(context, request, new CompleteTutorialResponse(data));
            }

            data.SetProgress(request.TutorialId.ToString(), ModuleStatus.Completed);
            playerData.AddToCache(data);

            await _goldModule.AddGoldToPlayer(context, playerData, remoteConfig, config.Reward);
            _logger.LogInformation("[TutorialModule] Tutorial step {TutorialId} completed for player {PlayerId}", request.TutorialId, context.PlayerId);

            var response = new CompleteTutorialResponse(data);
            return await _moduleRequestHandler.ResolveResponse(context, request, response);
        }
    }
}
