using System;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Scaffold.Maps;
using UnityEngine;

namespace Madbox.Addressables
{
    public sealed class AddressablesAssetReferenceHandler : IAssetReferenceHandler
    {
        public AddressablesAssetReferenceHandler(IAddressablesAssetClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            this.client = client;
        }

        private readonly IAddressablesAssetClient client;
        private readonly Map<Type, string, AddressablesLoadedEntry> loaded = new Map<Type, string, AddressablesLoadedEntry>();
        private readonly object sync = new object();

        public Task<IAssetHandle<T>> AcquireAsync<T>(string key, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            return AcquireAsync<T>(key, PreloadMode.Normal, false, cancellationToken);
        }

        public async Task<IAssetHandle<T>> AcquireAsync<T>(string key, PreloadMode preloadMode, bool isPreload, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Asset key cannot be empty.", nameof(key));
            AddressablesLoadedEntry entry = await AcquireEntryAsync<T>(key, preloadMode, isPreload, cancellationToken);
            if (entry.Asset is not T typedAsset)
            {
                throw new InvalidOperationException($"Loaded asset type mismatch. Requested '{typeof(T).FullName}', actual '{entry.Asset?.GetType().FullName ?? "null"}'.");
            }

            return new AssetHandle<T>(typedAsset, () => ReleaseEntry<T>(key));
        }

        private void ReleaseEntry<T>(string key) where T : UnityEngine.Object
        {
            Type typeKey = typeof(T);
            AddressablesLoadedEntry loadedEntry;
            lock (sync)
            {
                if (!loaded.TryGetValue(typeKey, key, out loadedEntry))
                {
                    return;
                }

                if (loadedEntry.RefCount > 0)
                {
                    loadedEntry.RefCount--;
                }

                if (loadedEntry.RefCount > 0 || loadedEntry.Policy == PreloadMode.NeverDie)
                {
                    return;
                }

                loaded.Remove(typeKey, key);
            }

            client.Release(loadedEntry.Asset);
        }

        private async Task<AddressablesLoadedEntry> AcquireEntryAsync<T>(string key, PreloadMode preloadMode, bool isPreload, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            Type typeKey = typeof(T);
            lock (sync)
            {
                if (loaded.TryGetValue(typeKey, key, out AddressablesLoadedEntry existing))
                {
                    if (preloadMode == PreloadMode.NeverDie)
                    {
                        existing.Policy = PreloadMode.NeverDie;
                    }

                    if (!isPreload)
                    {
                        existing.RefCount++;
                    }

                    return existing;
                }
            }

            T asset = await client.LoadAssetAsync<T>(key, cancellationToken);
            return AddOrReuseEntry(typeKey, key, preloadMode, isPreload, asset);
        }

        private AddressablesLoadedEntry AddOrReuseEntry(Type typeKey, string key, PreloadMode preloadMode, bool isPreload, UnityEngine.Object asset)
        {
            lock (sync)
            {
                if (loaded.TryGetValue(typeKey, key, out AddressablesLoadedEntry existing))
                {
                    if (preloadMode == PreloadMode.NeverDie)
                    {
                        existing.Policy = PreloadMode.NeverDie;
                    }

                    if (!isPreload)
                    {
                        existing.RefCount++;
                    }

                    return existing;
                }

                AddressablesLoadedEntry created = new AddressablesLoadedEntry(asset, preloadMode);
                if (!isPreload)
                {
                    created.RefCount = 1;
                }

                loaded.Add(typeKey, key, created);
                return created;
            }
        }
    }
}
