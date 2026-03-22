using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Json;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace Madbox.LiveOps.CloudCode.FetchData.Unity
{
    public abstract class UnityDataCache : IWriteableDataCache, IReadableDataCache
    {
        protected UnityDataCache(ILogger logger, IGameApiClient gameApiClient)
        {
            _logger = logger;
            _gameApiClient = gameApiClient;
        }

        protected UnityDataCache(ILogger logger, IGameApiClient gameApiClient, string playerId) : this(logger, gameApiClient)
        {
            _playerId = playerId;
        }

        protected string _playerId;
        protected string _accessToken;

        protected ILogger _logger;
        protected IGameApiClient _gameApiClient;

        protected Dictionary<string, string> _cache = new Dictionary<string, string>();
        protected Dictionary<string, object> _objectCache = new Dictionary<string, object>();
        protected List<string> _objectsToSave = new List<string>();

        protected abstract Task<Dictionary<string, string>> FetchData(IExecutionContext context);
        protected abstract Task SaveData(IExecutionContext context, string key, object value, bool useWriteLock);
        protected abstract Task SaveBatchData(IExecutionContext context, List<SetItemBody> values, bool useWriteLock);
        protected abstract Task DeleteData(IExecutionContext context, string key);

        public string PlayerId => _playerId;

        protected void SetPlayerId(string playerId)
        {
            _playerId = playerId;
        }

        protected virtual async Task InitializeData(IExecutionContext context)
        {
            if (context.AccessToken != _accessToken)
            {
                if (string.IsNullOrEmpty(_playerId))
                {
                    SetPlayerId(context.PlayerId);
                }

                await Initialize(context);
            }
        }

        public virtual string GetDebugKey(string key)
        {
            return $"'{key}'";
        }

        protected virtual string SanitizeKey(string key)
        {
            return key;
        }

        protected async Task Initialize(IExecutionContext context)
        {
            _cache = await FetchData(context);
            _objectCache.Clear();
            _accessToken = context.AccessToken;
            _logger.LogInformation("[{Type}] Refreshed cache for player {PlayerId}", GetType().Name, _playerId);
        }

        public virtual async Task<T> Get<T>(IExecutionContext context, string key, T defaultValue)
        {
            key = SanitizeKey(key);
            await InitializeData(context);
            if (_objectCache.TryGetValue(key, out object cachedObj) && cachedObj is T cachedTyped)
            {
                return cachedTyped;
            }
            if (_cache.TryGetValue(key, out string value))
            {
                T deserialized = value.FromJson<T>();
                _objectCache[key] = deserialized;
                return deserialized;
            }
            return defaultValue;
        }

        public virtual async Task<T> Get<T>(IExecutionContext context, T defaultValue) where T : IGameModuleData
        {
            return await Get(context, GameDataExtensions.GetKey<T>(), defaultValue);
        }

        public virtual async Task<Dictionary<string, T>> GetAllValues<T>(IExecutionContext context)
        {
            await InitializeData(context);
            Dictionary<string, T> tempCache = new Dictionary<string, T>();
            foreach (KeyValuePair<string, string> pair in _cache)
            {
                T deserialized = pair.Value.FromJson<T>();
                tempCache.Add(pair.Key, deserialized);
            }
            return tempCache;
        }

        private void InternalSet(string key, object value)
        {
            key = SanitizeKey(key);
            _objectCache[key] = value;
            _cache[key] = value.ToJson();
        }

        public virtual async Task Set(IExecutionContext context, string key, object value, bool useWriteLock = false)
        {
            key = SanitizeKey(key);
            await InitializeData(context);
            InternalSet(key, value);
            await SaveData(context, key, value, useWriteLock);
        }

        public virtual async Task Set(IExecutionContext context, IGameModuleData value, bool useWriteLock = false)
        {
            await Set(context, value.Key, value, useWriteLock);
        }

        public virtual async Task SetBatch(IExecutionContext context, List<SetItemBody> values, bool useWriteLock = false)
        {
            await InitializeData(context);
            foreach (SetItemBody item in values)
            {
                item.Key = SanitizeKey(item.Key);
                InternalSet(item.Key, item.Value);
            }
            await SaveBatchData(context, values, useWriteLock);
        }

        public virtual async Task SetBatch(IExecutionContext context, IEnumerable<IGameModuleData> values, bool useWriteLock = false)
        {
            List<SetItemBody> items = values.Select(v => new SetItemBody(v.Key, v)).ToList();
            await SetBatch(context, items, useWriteLock);
        }

        public virtual async Task Delete(IExecutionContext context, string key)
        {
            key = SanitizeKey(key);
            await InitializeData(context);
            _cache.Remove(key);
            _objectCache.Remove(key);
            await DeleteData(context, key);
        }

        public virtual void AddToCache(params string[] moduleKeys)
        {
            foreach (string moduleKey in moduleKeys)
            {
                string k = SanitizeKey(moduleKey);
                if (!_objectsToSave.Contains(k))
                {
                    _objectsToSave.Add(k);
                }
            }
        }

        public virtual void AddToCache(IGameModuleData moduleData)
        {
            AddToCache(moduleData.Key);
        }

        public virtual async Task SaveCache(IExecutionContext context)
        {
            if (_objectsToSave.Any())
            {
                List<SetItemBody> items = new List<SetItemBody>();
                foreach (string moduleData in _objectsToSave)
                {
                    if (_objectCache.TryGetValue(moduleData, out object cachedObj) && cachedObj != null)
                    {
                        items.Add(new SetItemBody(moduleData, cachedObj));
                    }
                }
                await SetBatch(context, items);
                _objectsToSave.Clear();
            }
        }

        public virtual async Task<bool> Exists(IExecutionContext context, string key)
        {
            key = SanitizeKey(key);
            await InitializeData(context);
            return _cache.ContainsKey(key);
        }

        public virtual async Task<T> GetOrSet<T>(IExecutionContext context, string key, T defaultValue, bool useWriteLock = false)
        {
            key = SanitizeKey(key);
            if (await Exists(context, key))
            {
                return await Get(context, key, defaultValue);
            }
            await Set(context, key, defaultValue, useWriteLock);
            return defaultValue;
        }

        public virtual async Task<T> GetOrSet<T>(IExecutionContext context, T defaultValue, bool useWriteLock = false) where T : IGameModuleData
        {
            string key = GameDataExtensions.GetKey<T>();
            if (await Exists(context, key))
            {
                return await Get(context, key, defaultValue);
            }
            await Set(context, key, defaultValue, useWriteLock);
            return defaultValue;
        }

        public virtual async Task<string> GetRaw(IExecutionContext context, string key)
        {
            key = SanitizeKey(key);
            await InitializeData(context);
            if (_cache.TryGetValue(key, out string value))
            {
                return value;
            }
            return null;
        }
    }
}
