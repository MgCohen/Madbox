using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine;

namespace Scaffold.Navigation
{
    internal sealed class NavigationPrefabStore
    {
        public NavigationPrefabStore(IAddressablesGateway gateway, IReadOnlyList<ViewConfig> configs)
        {
            if (gateway == null) { throw new ArgumentNullException(nameof(gateway)); }
            this.gateway = gateway;
            this.configs = configs ?? Array.Empty<ViewConfig>();
        }

        private readonly IAddressablesGateway gateway;
        private readonly IReadOnlyList<ViewConfig> configs;
        private readonly Dictionary<string, Task<IAssetHandle<GameObject>>> handlesByKey = new Dictionary<string, Task<IAssetHandle<GameObject>>>();
        private readonly object sync = new object();

        public void Warmup()
        {
            foreach (ViewConfig config in configs)
            {
                WarmupConfig(config);
            }
        }

        public Task<IAssetHandle<GameObject>> GetHandleAsync(ViewConfig config)
        {
            GuardConfig(config);
            string key = GetAssetKey(config);
            return GetOrCreateHandleTask(config, key);
        }

        private void WarmupConfig(ViewConfig config)
        {
            if (!TryGetAssetKey(config, out string key)) { return; }
            _ = GetOrCreateHandleTask(config, key);
        }

        private Task<IAssetHandle<GameObject>> GetOrCreateHandleTask(ViewConfig config, string key)
        {
            lock (sync)
            {
                return ResolveOrCreateHandleTask(config, key);
            }
        }

        private Task<IAssetHandle<GameObject>> ResolveOrCreateHandleTask(ViewConfig config, string key)
        {
            if (handlesByKey.TryGetValue(key, out Task<IAssetHandle<GameObject>> existing)) { return existing; }
            Task<IAssetHandle<GameObject>> created = LoadHandleAsync(config, key);
            handlesByKey[key] = created;
            return created;
        }

        private async Task<IAssetHandle<GameObject>> LoadHandleAsync(ViewConfig config, string key)
        {
            try { return await gateway.LoadAsync<GameObject>(config.Asset); }
            catch { return FailLoad(key); }
        }

        private IAssetHandle<GameObject> FailLoad(string key)
        {
            RemoveFailedKey(key);
            throw new InvalidOperationException("Failed to load navigation prefab handle.");
        }

        private void RemoveFailedKey(string key)
        {
            lock (sync)
            {
                handlesByKey.Remove(key);
            }
        }

        private void GuardConfig(ViewConfig config)
        {
            if (!TryGetAssetKey(config, out _))
            {
                throw new InvalidOperationException("Navigation view config is missing a valid addressable reference.");
            }
        }

        private string GetAssetKey(ViewConfig config)
        {
            TryGetAssetKey(config, out string key);
            return key;
        }

        private bool TryGetAssetKey(ViewConfig config, out string key)
        {
            key = null;
            if (config == null || config.Asset == null) { return false; }
            object runtimeKey = config.Asset.RuntimeKey;
            if (runtimeKey == null) { return false; }
            string value = runtimeKey.ToString();
            if (string.IsNullOrWhiteSpace(value)) { return false; }
            key = value;
            return true;
        }
    }
}
