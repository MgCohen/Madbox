using System.Threading.Tasks;
using GameModule.GameModule;
using GameModule.ModuleFetchData;
using GameModuleDTO.GameModule;
using GameModuleDTO.Sample.SimpleModule;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;


namespace GameModule.Sample
{
    /// <summary>
    /// Example system showcasing bare minimum implementations.
    /// </summary>
    public class SimpleModule : GameModule<SimpleModuleData>
    {
        public SimpleModule(ILogger<SimpleModule> logger)
        {
            _logger = logger;
        }

        private readonly ILogger<SimpleModule> _logger;

        public override async Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData Player, IGameState gameState, IRemoteConfig remoteConfig)
        {
            return await Player.GetOrSet<SimpleModuleData>(context, new SimpleModuleData());
        }
    }
}