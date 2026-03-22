using System.Threading.Tasks;
using Madbox.LiveOps.CloudCode.FetchData;
using Madbox.LiveOps.CloudCode.GameModules;
using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Modules.Level;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.Modules.Level
{
    public sealed class LevelConfigModule : GameModule<LevelConfigData>
    {
        private readonly ILogger<LevelConfigModule> _logger;

        public LevelConfigModule(ILogger<LevelConfigModule> logger)
        {
            _logger = logger;
        }

        public override bool Client => true;
        public override bool Server => true;

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData playerData, IGameState gameState, IRemoteConfig remoteConfig)
        {
            _logger.LogInformation("Initializing LevelConfigModule");
            return await remoteConfig.Get(context, new LevelConfigData());
        }
    }
}
