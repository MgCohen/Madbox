using System.Threading.Tasks;
using Madbox.LiveOps.CloudCode.FetchData;
using Madbox.LiveOps.CloudCode.GameModules;
using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Modules.Tutorial;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.Modules.Tutorial
{
    public sealed class TutorialConfigModule : GameModule<TutorialConfigData>
    {
        private readonly ILogger<TutorialConfigModule> _logger;

        public TutorialConfigModule(ILogger<TutorialConfigModule> logger)
        {
            _logger = logger;
        }

        public override bool Client => true;
        public override bool Server => true;

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData playerData, IGameState gameState, IRemoteConfig remoteConfig)
        {
            _logger.LogInformation("Initializing TutorialConfigModule");
            return await remoteConfig.Get(context, new TutorialConfigData());
        }
    }
}
