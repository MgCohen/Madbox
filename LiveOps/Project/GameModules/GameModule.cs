using System.Threading.Tasks;
using Madbox.LiveOps.CloudCode.FetchData;
using Madbox.LiveOps.DTO.GameModule;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.GameModules
{
    public abstract class GameModule<T> : IGameModule where T : IGameModuleData
    {
        public abstract bool Client { get; }
        public abstract bool Server { get; }

        public string Key => GameDataExtensions.GetKey<T>();

        public abstract Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData playerData, IGameState gameState, IRemoteConfig remoteConfig);
    }
}
