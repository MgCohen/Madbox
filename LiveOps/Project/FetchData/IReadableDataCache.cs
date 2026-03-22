using System.Collections.Generic;
using System.Threading.Tasks;
using Madbox.LiveOps.DTO.GameModule;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.FetchData
{
    public interface IReadableDataCache
    {
        Task<T> Get<T>(IExecutionContext context, string key, T defaultValue);
        Task<T> Get<T>(IExecutionContext context, T defaultValue) where T : IGameModuleData;
        Task<Dictionary<string, T>> GetAllValues<T>(IExecutionContext context);
        Task<bool> Exists(IExecutionContext context, string key);
        Task<string> GetRaw(IExecutionContext context, string key);
    }
}
