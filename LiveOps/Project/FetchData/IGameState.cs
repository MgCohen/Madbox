using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.FetchData
{
    public interface IGameState : IWriteableDataCache, IReadableDataCache
    {
        Task Set(IExecutionContext context, string databaseKey, string key, object value, bool useWriteLock = false);
        Task Delete(IExecutionContext context, string databaseKey, string key);

        Task<Dictionary<string, T>> GetAllGameValues<T>(IExecutionContext context, string key);
        Task<T> GetAllGameValue<T>(IExecutionContext context, string databaseKey, string key);
    }
}
