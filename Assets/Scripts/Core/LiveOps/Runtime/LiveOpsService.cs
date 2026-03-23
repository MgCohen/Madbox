using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Level;
using Madbox.CloudCode;
using Madbox.Scope.Contracts;
using VContainer;

namespace Madbox.LiveOps
{
    internal sealed class LiveOpsService : ILiveOpsService, IAsyncLayerInitializable
    {
        public LiveOpsService(ICloudCodeModuleService cloudCodeModuleService)
        {
            if (cloudCodeModuleService == null)
            {
                throw new ArgumentNullException(nameof(cloudCodeModuleService));
            }

            this.cloudCodeModuleService = cloudCodeModuleService;
        }

        private readonly ICloudCodeModuleService cloudCodeModuleService;
        private GameData gameData;

        public T GetModuleData<T>() where T : class, IGameModuleData
        {
            return gameData == null ? null : gameData.GetModuleData<T>();
        }

        public Task InitializeAsync(IObjectResolver resolver, CancellationToken cancellationToken)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return LoadInitialGameDataAsync(cancellationToken);
        }
        private async Task LoadInitialGameDataAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GameDataRequest request = new GameDataRequest();
            GameDataResponse response = await CallAsync(request, cancellationToken).ConfigureAwait(false);
            gameData = response?.GameData;
        }

        public async Task<TResponse> CallAsync<TResponse>(ModuleRequest<TResponse> request, CancellationToken cancellationToken = default) where TResponse : ModuleResponse
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            cancellationToken.ThrowIfCancellationRequested();
            Dictionary<string, object> payload = new Dictionary<string, object> { { "request", request } };
            return await cloudCodeModuleService.CallEndpointAsync<TResponse>(request.ModuleName, request.FunctionName, payload: payload, cancellationToken: cancellationToken);
        }
    }
}
