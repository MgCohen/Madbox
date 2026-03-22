using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameModule.GameModule;
using GameModule.ModuleFetchData;
using GameModule.Modules.Gold;
using GameModule.Response;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Common;
using GameModuleDTO.Modules.Tutorial;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace GameModule.Modules.Tutorial
{
    /// <summary>
    /// Cloud Code tutorial module: persistence + remote config merged into <see cref="TutorialGameData"/>.
    /// </summary>
    public class TutorialService : GameModule<TutorialGameData>
    {
        private readonly ILogger<TutorialService> _logger;
        private readonly GoldModule _goldModule;
        private readonly ModuleRequestHandler _moduleRequestHandler;

        public TutorialService(ILogger<TutorialService> logger, GoldModule goldModule, ModuleRequestHandler moduleRequestHandler)
        {
            _logger = logger;
            _goldModule = goldModule;
            _moduleRequestHandler = moduleRequestHandler;
        }

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData playerData, IGameState gameState, IRemoteConfig remoteConfig)
        {
            TutorialPersistence persistence = await playerData.GetOrSet(context, new TutorialPersistence());
            TutorialConfig config = await remoteConfig.Get(context, new TutorialConfig());
            return TutorialGameData.From(persistence, config);
        }

        [CloudCodeFunction(nameof(CompleteTutorialRequest))]
        public async Task<CompleteTutorialResponse> CompleteTutorial(IExecutionContext context, IPlayerData playerData, IRemoteConfig remoteConfig, CompleteTutorialRequest request)
        {
            TutorialConfig config = await remoteConfig.Get(context, new TutorialConfig());
            TutorialPersistence persistence = await playerData.GetOrSet(context, new TutorialPersistence());

            if (!config.Tutorials.Contains(request.TutorialId))
            {
                _logger.LogWarning("[TutorialService] Attempted to complete tutorial step {AttemptedStep} but it is not in the valid tutorials list", request.TutorialId);
                return await _moduleRequestHandler.ResolveResponse(context, request, new CompleteTutorialResponse(TutorialGameData.From(persistence, config)));
            }

            int currentTutorialStep = 1;
            List<ModuleProgress> completedSteps = persistence.Progress.Where(p => p.Status == ModuleStatus.Completed).ToList();
            if (completedSteps.Any())
            {
                int maxCompleted = completedSteps.Select(p => int.TryParse(p.Id, out int val) ? val : 0).Max();
                currentTutorialStep = maxCompleted + 1;
            }

            if (request.TutorialId != currentTutorialStep)
            {
                _logger.LogWarning("[TutorialService] Attempted to complete tutorial step {AttemptedStep} but current step is {CurrentStep}", request.TutorialId, currentTutorialStep);
                return await _moduleRequestHandler.ResolveResponse(context, request, new CompleteTutorialResponse(TutorialGameData.From(persistence, config)));
            }

            persistence.SetProgress(request.TutorialId.ToString(), ModuleStatus.Completed);
            playerData.AddToCache(persistence);

            await _goldModule.AddGoldToPlayer(context, playerData, remoteConfig, config.Reward);
            _logger.LogInformation("[TutorialService] Tutorial step {TutorialId} completed successfully for player {PlayerId}", request.TutorialId, context.PlayerId);

            TutorialGameData gameData = TutorialGameData.From(persistence, config);
            CompleteTutorialResponse response = new CompleteTutorialResponse(gameData);
            return await _moduleRequestHandler.ResolveResponse(context, request, response);
        }
    }
}
