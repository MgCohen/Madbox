using System.Threading.Tasks;
using Madbox.LiveOps.CloudCode.FetchData;
using Madbox.LiveOps.DTO.GameModule;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.GameModules
{
    public interface IGameModule
    {
        bool Client { get; }
        bool Server { get; }
        string Key { get; }
        Task<IGameModuleData> Initialize(IExecutionContext context, IPlayerData playerData, IGameState gameState, IRemoteConfig remoteConfig);
    }
}
