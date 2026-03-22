using System.Collections.Generic;
using System.Threading.Tasks;
using GameModule.GameModule;
using GameModule.ModuleFetchData;
using GameModule.Response;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Level;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace GameModule.Modules.Level
{
    /// <summary>
    /// Cloud Code level module: persistence + remote config merged into <see cref="LevelGameData"/>.
    /// </summary>
    public class LevelService : GameModule<LevelGameData>
    {
        private readonly ILogger<LevelService> _logger;
        private readonly ModuleRequestHandler _moduleRequestHandler;

        public LevelService(ILogger<LevelService> logger, ModuleRequestHandler moduleRequestHandler)
        {
            _logger = logger;
            _moduleRequestHandler = moduleRequestHandler;
        }

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData playerData, IGameState gameState, IRemoteConfig remoteConfig)
        {
            LevelPersistence persistence = await playerData.GetOrSet(context, new LevelPersistence());
            LevelConfig config = await remoteConfig.Get(context, new LevelConfig());
            return new LevelGameData(persistence, config);
        }

        [CloudCodeFunction(nameof(CompleteLevelRequest))]
        public async Task<CompleteLevelResponse> CompleteLevel(IExecutionContext context, IPlayerData playerData, IRemoteConfig remoteConfig, CompleteLevelRequest request)
        {
            LevelConfig config = await remoteConfig.Get(context, new LevelConfig());
            LevelPersistence persistence = await playerData.GetOrSet(context, new LevelPersistence());

            IReadOnlyList<int> levels = config.Levels;
            int index = IndexOfLevelId(levels, request.LevelId);
            if (index < 0)
            {
                _logger.LogWarning("[LevelService] Attempted to complete level {AttemptedLevel} but it is not in the valid levels list", request.LevelId);
                return await _moduleRequestHandler.ResolveResponse(context, request, new CompleteLevelResponse(false));
            }

            HashSet<int> completed = new HashSet<int>(persistence.CompletedLevelIds);
            if (completed.Contains(request.LevelId))
            {
                _logger.LogWarning("[LevelService] Level {LevelId} is already completed", request.LevelId);
                return await _moduleRequestHandler.ResolveResponse(context, request, new CompleteLevelResponse(false));
            }

            if (index > 0)
            {
                int previousId = levels[index - 1];
                if (!completed.Contains(previousId))
                {
                    _logger.LogWarning("[LevelService] Previous level {PreviousId} is not completed for {AttemptedLevel}", previousId, request.LevelId);
                    return await _moduleRequestHandler.ResolveResponse(context, request, new CompleteLevelResponse(false));
                }
            }

            persistence.AddCompletedLevel(request.LevelId);
            playerData.AddToCache(persistence);

            _logger.LogInformation("[LevelService] Level {LevelId} completed successfully for player {PlayerId}", request.LevelId, context.PlayerId);

            CompleteLevelResponse response = new CompleteLevelResponse(true, request.LevelId);
            return await _moduleRequestHandler.ResolveResponse(context, request, response);
        }

        private static int IndexOfLevelId(IReadOnlyList<int> levels, int levelId)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] == levelId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
