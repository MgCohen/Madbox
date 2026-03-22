using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Madbox.LiveOps.DTO.GameModule;
using Madbox.LiveOps.DTO.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Core;

namespace Madbox.LiveOps.CloudCode.FetchData
{
    /// <summary>
    /// Loads <c>Configs/*.json</c> into an in-memory map (no HTTP).
    /// </summary>
    public sealed class LocalOnlyRemoteConfig : IRemoteConfig
    {
        private readonly ILogger<LocalOnlyRemoteConfig> _logger;
        private Dictionary<string, string> _cache = new Dictionary<string, string>();
        private bool _isFetched;

        public LocalOnlyRemoteConfig(ILogger<LocalOnlyRemoteConfig> logger)
        {
            _logger = logger;
        }

        private Task EnsureLoadedAsync(IExecutionContext context)
        {
            if (_isFetched)
            {
                return Task.CompletedTask;
            }

            _cache = ReadConfigsFromDisk();
            _logger.LogInformation("[LocalOnlyRemoteConfig] Loaded {Count} config keys.", _cache.Count);
            _isFetched = true;
            return Task.CompletedTask;
        }

        private Dictionary<string, string> ReadConfigsFromDisk()
        {
            var localConfigs = new Dictionary<string, string>();
            string[] searchPaths = { "Configs", "../Configs", "../../Configs" };

            foreach (string folder in searchPaths)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                try
                {
                    string[] files = Directory.GetFiles(folder, "*.json");
                    if (files.Length > 0)
                    {
                        _logger.LogInformation("[Configs] Found {Count} files in {Path}", files.Length, Path.GetFullPath(folder));
                    }

                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName.Contains(".deps.") ||
                            fileName.Contains(".assets.") ||
                            fileName.Contains(".packagespec.") ||
                            fileName.Contains(".runtimeconfig.") ||
                            fileName.StartsWith("project.", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        string content = File.ReadAllText(file);
                        try
                        {
                            JObject json = JObject.Parse(content);
                            foreach (JProperty property in json.Properties())
                            {
                                if (!localConfigs.ContainsKey(property.Name))
                                {
                                    localConfigs[property.Name] = property.Value.ToString(Formatting.None);
                                }
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            _logger.LogWarning(jsonEx, "[Configs] Failed to parse {File}", file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Configs] Error reading folder {Folder}", folder);
                }
            }

            return localConfigs;
        }

        public async Task<T> Get<T>(IExecutionContext context, string key, T defaultValue)
        {
            await EnsureLoadedAsync(context);
            if (_cache.TryGetValue(key, out string value))
            {
                try
                {
                    return value.FromJson<T>();
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public async Task<T> Get<T>(IExecutionContext context, T defaultValue) where T : IGameModuleData
        {
            return await Get(context, GameDataExtensions.GetKey<T>(), defaultValue);
        }

        public async Task<Dictionary<string, T>> GetAllValues<T>(IExecutionContext context)
        {
            await EnsureLoadedAsync(context);
            var results = new Dictionary<string, T>();
            foreach (KeyValuePair<string, string> kvp in _cache)
            {
                try
                {
                    results[kvp.Key] = kvp.Value.FromJson<T>();
                }
                catch
                {
                    // skip invalid entries
                }
            }
            return results;
        }

        public async Task<string> GetRaw(IExecutionContext context, string key)
        {
            await EnsureLoadedAsync(context);
            return _cache.TryGetValue(key, out string value) ? value : string.Empty;
        }

        public async Task<bool> Exists(IExecutionContext context, string key)
        {
            await EnsureLoadedAsync(context);
            return _cache.ContainsKey(key);
        }
    }
}
