using System;
using System.Threading.Tasks;
using GameModule.GameModule;
using GameModule.ModuleFetchData;
using GameModule.Response;
using GameModuleDTO.GameModule;
using GameModuleDTO.Modules.Gold;
using GameModuleDTO.ModuleRequests;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace GameModule.Modules.Gold
{
    /// <summary>
    /// Cloud Code gold module: persistence + remote config merged into <see cref="GoldGameData"/>.
    /// </summary>
    public class GoldModule : GameModule<GoldGameData>
    {
        private readonly ILogger<GoldModule> _logger;
        private readonly ModuleRequestHandler _handler;

        public GoldModule(ILogger<GoldModule> logger, ModuleRequestHandler handler)
        {
            _logger = logger;
            _handler = handler;
        }

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData Player, IGameState gameState, IRemoteConfig remoteConfig)
        {
            _logger.LogInformation("Initializing GoldModule");

            GoldConfig config = await remoteConfig.Get(context, new GoldConfig());
            GoldRewardModuleData rewardConfig = await remoteConfig.Get(context, new GoldRewardModuleData());
            GoldPersistence persistence = await Player.GetOrSet(context, new GoldPersistence());

            long clamped = Math.Clamp(persistence.Current, config.Min, config.Max);
            if (clamped != persistence.Current)
            {
                persistence.SetCurrent(clamped);
                await Player.Set(context, persistence);
            }

            return new GoldGameData(persistence, config, rewardConfig);
        }

        public async Task AddGoldToPlayer(IExecutionContext context, IPlayerData Player, IRemoteConfig remoteConfig, long amount = 0)
        {
            _logger.LogInformation("[GoldModule] Rewarding player {PlayerId}", context.PlayerId);

            if (amount == 0)
            {
                GoldRewardModuleData rewardDefaults = await remoteConfig.Get(context, new GoldRewardModuleData());
                amount = rewardDefaults.Reward;
            }

            GoldConfig config = await remoteConfig.Get(context, new GoldConfig());
            GoldPersistence goldPersistence = await Player.GetOrSet(context, new GoldPersistence());
            long next = goldPersistence.Current + amount;
            goldPersistence.SetCurrent(Math.Clamp(next, config.Min, config.Max));
            Player.AddToCache(goldPersistence);

            _handler.AddResponse(new GoldResponse(amount));
            _logger.LogInformation("[GoldModule] Added {Amount} gold to player {PlayerId}. New total: {Total}", amount, context.PlayerId, goldPersistence.Current);
        }
    }
}
