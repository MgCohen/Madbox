using System.Threading.Tasks;
using GameModule.GameModule;
using GameModule.ModuleFetchData;
using GameModule.Response;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Sample.CounterModule;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace GameModule.Sample
{
    /// <summary>
    /// Example system demonstrating client increment features.
    /// </summary>
    public class CounterModule : GameModule<CounterModuleData>
    {
        public CounterModule(ILogger<CounterModule> logger, ModuleRequestHandler moduleRequestHandler)
        {
            _logger = logger;
            _moduleRequestHandler = moduleRequestHandler;
        }

        private readonly ILogger<CounterModule> _logger;
        private readonly ModuleRequestHandler _moduleRequestHandler;

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData Player, IGameState gameState, IRemoteConfig remoteConfig)
        {
            return await Player.GetOrSet<CounterModuleData>(context, new CounterModuleData());
        }

        [CloudCodeFunction(nameof(IncrementCounterRequest))]
        public async Task<IncrementCounterResponse> IncrementCounter(IExecutionContext context, IPlayerData Player, IncrementCounterRequest request)
        {
            _logger.LogInformation("[IncrementCounterRequest] Starting");
            CounterModuleData counterData = await Player.GetOrSet<CounterModuleData>(context, new CounterModuleData());
            Player.AddToCache(counterData);

            int valueToIncrement = 1;
            counterData.IncreaseValue(valueToIncrement);
            IncrementCounterResponse incrementCounterResponse = new IncrementCounterResponse(valueToIncrement);
            return await _moduleRequestHandler.ResolveResponse(context, request, incrementCounterResponse);
        }
    }
}