using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Madbox.LiveOps.DTO.Keys;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace Madbox.LiveOps.CloudCode.FetchData.Unity
{
    public sealed class UnityGameState : UnityDataCache, IGameState
    {
        private string _key = ModuleKeys.GameState;

        public UnityGameState(ILogger<UnityDataCache> logger, IGameApiClient gameApiClient) : base(logger, gameApiClient)
        {
        }

        public override string GetDebugKey(string key)
        {
            return $"'{_key}'.'{key}'";
        }

        protected override string SanitizeKey(string key)
        {
            return key;
        }

        protected override async Task InitializeData(IExecutionContext context)
        {
            SetPlayerId(context.PlayerId);
            await Initialize(context);
        }

        protected override async Task<Dictionary<string, string>> FetchData(IExecutionContext context)
        {
            try
            {
                Dictionary<string, string> allFetchedData = new Dictionary<string, string>();
                string after = null;
                int pageNumber = 0;

                do
                {
                    pageNumber++;
                    ApiResponse<GetItemsResponse> result =
                        await _gameApiClient.CloudSaveData.GetPrivateCustomItemsAsync(context, context.ServiceToken,
                            context.ProjectId, _key, keys: null, after);

                    int itemsThisPage = result.Data?.Results?.Count ?? 0;
                    if (itemsThisPage > 0)
                    {
                        foreach (Item item in result.Data.Results)
                        {
                            allFetchedData[item.Key] = item.Value?.ToString() ?? string.Empty;
                        }
                    }

                    string nextUrl = result.Data?.Links?.Next;
                    after = null;

                    if (!string.IsNullOrEmpty(nextUrl))
                    {
                        const string afterMarker = "after=";
                        int afterIndex = nextUrl.IndexOf(afterMarker, StringComparison.Ordinal);
                        if (afterIndex != -1)
                        {
                            string cursorWithPotentialParams = nextUrl.Substring(afterIndex + afterMarker.Length);
                            after = cursorWithPotentialParams.Split('&')[0];
                        }
                    }
                }
                while (!string.IsNullOrEmpty(after));

                return allFetchedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GameState] Fetch failed for key {Key}: {Message}", _key, ex.Message);
                throw;
            }
        }

        protected override async Task SaveData(IExecutionContext context, string key, object value, bool useWriteLock)
        {
            SetItemBody item = new SetItemBody(key, value);
            await _gameApiClient.CloudSaveData.SetPrivateCustomItemAsync(context, context.ServiceToken, context.ProjectId, _key, item);
        }

        protected override async Task DeleteData(IExecutionContext context, string key)
        {
            await _gameApiClient.CloudSaveData.DeletePrivateCustomItemAsync(context, context.ServiceToken, context.ProjectId, _key, key);
        }

        public async Task Delete(IExecutionContext context, string databaseKey, string key)
        {
            _key = databaseKey;
            await Delete(context, key);
        }

        protected override async Task SaveBatchData(IExecutionContext context, List<SetItemBody> values, bool useWriteLock)
        {
            SetItemBatchBody request = new SetItemBatchBody(values);
            await _gameApiClient.CloudSaveData.SetPrivateCustomItemBatchAsync(context, context.ServiceToken, context.ProjectId, _key, request);
        }

        public async Task<Dictionary<string, T>> GetAllGameValues<T>(IExecutionContext context, string key)
        {
            _key = key;
            return await GetAllValues<T>(context);
        }

        public async Task<T> GetAllGameValue<T>(IExecutionContext context, string databaseKey, string key)
        {
            Dictionary<string, T> gameValues = await GetAllGameValues<T>(context, databaseKey);
            return gameValues.TryGetValue(key, out T value) ? value : default;
        }

        public async Task Set(IExecutionContext context, string databaseKey, string key, object value, bool useWriteLock = false)
        {
            _key = databaseKey;
            await Set(context, key, value, useWriteLock);
        }
    }
}
