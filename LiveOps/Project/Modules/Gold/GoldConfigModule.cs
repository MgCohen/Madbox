using System.Threading.Tasks;
using Madbox.LiveOps.CloudCode.FetchData;
using Madbox.LiveOps.CloudCode.GameModules;
using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Modules.Gold;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.Modules.Gold
{
    public sealed class GoldConfigModule : GameModule<GoldConfigData>
    {
        private readonly ILogger<GoldConfigModule> _logger;

        public GoldConfigModule(ILogger<GoldConfigModule> logger)
        {
            _logger = logger;
        }

        public override bool Client => true;
        public override bool Server => true;

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData playerData, IGameState gameState, IRemoteConfig remoteConfig)
        {
            _logger.LogInformation("Initializing GoldConfigModule");
            return await remoteConfig.Get(context, new GoldConfigData());
        }
    }
}
