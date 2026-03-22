using System.Threading.Tasks;
using Madbox.LiveOps.CloudCode.FetchData;
using Madbox.LiveOps.CloudCode.GameModules;
using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Modules.Global;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.Modules.Global
{
    public sealed class GlobalConfigModule : GameModule<GlobalConfigData>
    {
        private readonly ILogger<GlobalConfigModule> _logger;

        public GlobalConfigModule(ILogger<GlobalConfigModule> logger)
        {
            _logger = logger;
        }

        public override bool Client => false;
        public override bool Server => true;

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData playerData, IGameState gameState, IRemoteConfig remoteConfig)
        {
            _logger.LogInformation("Initializing GlobalConfigModule");
            return await remoteConfig.Get(context, new GlobalConfigData());
        }
    }
}
