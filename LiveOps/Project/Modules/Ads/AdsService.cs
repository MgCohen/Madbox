using System.Threading.Tasks;
using GameModule.GameModule;
using GameModule.ModuleFetchData;
using GameModule.Response;
using GameModuleDTO.GameModule;
using GameModuleDTO.ModuleRequests;
using GameModuleDTO.Modules.Ads;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace GameModule.Modules.Ads
{
    /// <summary>
    /// Cloud Code ads module: persistence + remote config merged into <see cref="AdsGameData"/>.
    /// </summary>
    public class AdsService : GameModule<AdsGameData>
    {
        private readonly ILogger<AdsService> _logger;
        private readonly ModuleRequestHandler _moduleRequestHandler;

        public AdsService(ILogger<AdsService> logger, ModuleRequestHandler moduleRequestHandler)
        {
            _logger = logger;
            _moduleRequestHandler = moduleRequestHandler;
        }

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData playerData, IGameState gameState, IRemoteConfig remoteConfig)
        {
            AdsPersistence persistence = await playerData.GetOrSet(context, new AdsPersistence());
            AdsConfig config = await remoteConfig.Get(context, new AdsConfig());
            return AdsGameData.From(persistence, config);
        }

        [CloudCodeFunction(nameof(WatchAdRequest))]
        public async Task<WatchAdResponse> WatchAd(IExecutionContext context, IPlayerData playerData, IRemoteConfig remoteConfig, WatchAdRequest request)
        {
            _logger.LogInformation("[WatchAdRequest] Starting");
            AdsConfig config = await remoteConfig.Get(context, new AdsConfig());
            AdsPersistence persistence = await playerData.GetOrSet(context, new AdsPersistence());

            if (persistence.IsAdAvailable())
            {
                persistence.SetNextAdAvailableTime(config.Cooldown);
                playerData.AddToCache(persistence);
                _logger.LogInformation("[AdsService] Ad watched successfully. Next available at: {NextAdAvailableTime}", persistence.NextAdAvailableTime);
            }
            else
            {
                _logger.LogWarning("[AdsService] Cannot watch ad yet. Remaining cooldown: {RemainingCooldown}", persistence.GetRemainingCooldown());
            }

            AdsGameData gameData = AdsGameData.From(persistence, config);
            WatchAdResponse response = new WatchAdResponse(gameData);
            return await _moduleRequestHandler.ResolveResponse(context, request, response);
        }
    }
}
