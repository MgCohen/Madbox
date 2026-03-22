using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Madbox.LiveOps.CloudCode.FetchData;
using Madbox.LiveOps.CloudCode.Response;
using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Keys;
using Madbox.LiveOps.DTO.ModuleRequests;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.GameModules
{
    public sealed class GameModulesController
    {
        private readonly ILogger<GameModulesController> _logger;
        private readonly ModuleRequestHandler _moduleRequestHandler;
        private readonly IPlayerData _playerData;
        private readonly IRemoteConfig _remoteConfig;

        public GameModulesController(
            ILogger<GameModulesController> logger,
            ModuleRequestHandler moduleRequestHandler,
            IPlayerData playerData,
            IRemoteConfig remoteConfig)
        {
            _logger = logger;
            _moduleRequestHandler = moduleRequestHandler;
            _playerData = playerData;
            _remoteConfig = remoteConfig;
        }

        [CloudCodeFunction(nameof(InitializeGameModulesRequest))]
        public async Task<GameDataResponse> InitializeModules(
            IExecutionContext context,
            IGameState gameState,
            InitializeGameModulesRequest request,
            IEnumerable<IGameModule> modules)
        {
            _logger.LogInformation("[InitializeModules] Starting");
            return await ProcessModulesSequentially(context, gameState, modules, request);
        }

        [CloudCodeFunction(nameof(GameDataRequest))]
        public async Task<GameDataResponse> GetGameModulesRequest(
            IExecutionContext context,
            IGameState gameState,
            GameDataRequest request,
            IEnumerable<IGameModule> modules)
        {
            _logger.LogInformation("[GetGameModulesRequest] Starting");
            return await ProcessModulesSequentially(context, gameState, modules, request, request.ModuleKeys);
        }

        private static async Task<bool> AuthMatchesAsync(IExecutionContext context, IGameState gameState, string authKey)
        {
            if (string.IsNullOrEmpty(authKey))
            {
                return false;
            }
            string unityAuth = await gameState.GetAllGameValue<string>(context, ModuleKeys.Auth, ModuleKeys.UnityToken);
            return unityAuth == authKey;
        }

        private async Task<T> ProcessModulesSequentially<T>(
            IExecutionContext context,
            IGameState gameState,
            IEnumerable<IGameModule> modules,
            ModuleRequest<T> request,
            IReadOnlyCollection<string> filterKeys = null) where T : ModuleResponse
        {
            request.AssertModule();

            var gameData = new GameData();
            bool treatAsServer = await AuthMatchesAsync(context, gameState, request.AuthKey);

            foreach (IGameModule gameModule in modules)
            {
                if (gameModule == null)
                {
                    continue;
                }

                bool hasAccess = treatAsServer ? gameModule.Server : gameModule.Client;
                if (!hasAccess)
                {
                    continue;
                }

                if (filterKeys != null && !filterKeys.Contains(gameModule.Key))
                {
                    continue;
                }

                try
                {
                    IGameModuleData moduleData = await gameModule.Initialize(context, _playerData, gameState, _remoteConfig);
                    if (moduleData != null)
                    {
                        gameData.AddModuleData(moduleData);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error on module {ModuleKey}: {Message}", gameModule.Key, e.Message);
                }
            }

            T response = new GameDataResponse(gameData) as T;
            if (response == null)
            {
                throw new InvalidOperationException($"Could not cast GameDataResponse to '{typeof(T).Name}'.");
            }
            return await _moduleRequestHandler.ResolveResponse(context, request, response);
        }
    }
}
