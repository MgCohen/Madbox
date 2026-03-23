using System.Collections.Generic;
using System.Threading.Tasks;
using GameModule.GameModule;
using GameModule.ModuleFetchData;
using GameModule.Modules.Gold;
using GameModule.Response;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
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
            return new TutorialGameData(persistence, config);
        }

        [CloudCodeFunction(nameof(CompleteTutorialRequest))]
        public async Task<CompleteTutorialResponse> CompleteTutorial(IExecutionContext context, IPlayerData playerData, IRemoteConfig remoteConfig, CompleteTutorialRequest request)
        {
            TutorialConfig config = await remoteConfig.Get(context, new TutorialConfig());
            TutorialPersistence persistence = await playerData.GetOrSet(context, new TutorialPersistence());
            IReadOnlyList<int> steps = config.Tutorials;

            if (steps.Count == 0 || !ContainsStep(steps, request.TutorialId))
            {
                _logger.LogWarning("[TutorialService] Attempted to complete tutorial step {AttemptedStep} but it is not in the valid tutorials list", request.TutorialId);
                return await _moduleRequestHandler.ResolveResponse(context, request, new CompleteTutorialResponse(new TutorialGameData(persistence, config)));
            }

            int expectedNext = NextExpectedStepId(steps, persistence.CompletedTutorialIds);
            if (request.TutorialId != expectedNext)
            {
                _logger.LogWarning("[TutorialService] Attempted to complete tutorial step {AttemptedStep} but current step is {CurrentStep}", request.TutorialId, expectedNext);
                return await _moduleRequestHandler.ResolveResponse(context, request, new CompleteTutorialResponse(new TutorialGameData(persistence, config)));
            }

            persistence.AddCompletedTutorial(request.TutorialId);
            playerData.AddToCache(persistence);

            await _goldModule.AddGoldToPlayer(context, playerData, remoteConfig, config.Reward);
            _logger.LogInformation("[TutorialService] Tutorial step {TutorialId} completed successfully for player {PlayerId}", request.TutorialId, context.PlayerId);

            TutorialGameData gameData = new TutorialGameData(persistence, config);
            CompleteTutorialResponse response = new CompleteTutorialResponse(gameData);
            return await _moduleRequestHandler.ResolveResponse(context, request, response);
        }

        private static bool ContainsStep(IReadOnlyList<int> steps, int tutorialId)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] == tutorialId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int NextExpectedStepId(IReadOnlyList<int> order, IReadOnlyList<int> completedIds)
        {
            HashSet<int> completed = new HashSet<int>(completedIds);
            for (int i = 0; i < order.Count; i++)
            {
                int id = order[i];
                if (!completed.Contains(id))
                {
                    return id;
                }
            }

            return -1;
        }
    }
}
