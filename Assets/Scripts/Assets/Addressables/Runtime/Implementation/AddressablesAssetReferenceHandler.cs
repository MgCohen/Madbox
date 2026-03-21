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

        public async Task<IAssetHandle<T>> AcquireAsync<T>(string key, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            IAssetHandle untypedHandle = await AcquireByTypeAsync(typeof(T), key, PreloadMode.Normal, false, cancellationToken);
            if (untypedHandle.UntypedAsset is not T typed) throw new InvalidOperationException($"Loaded asset type mismatch. Requested '{typeof(T).FullName}', actual '{untypedHandle.UntypedAsset?.GetType().FullName ?? "null"}'.");
            return new AssetHandle<T>(typed, untypedHandle.Release);
        }

        public async Task<IAssetHandle> AcquireByTypeAsync(Type assetType, string key, PreloadMode preloadMode, bool isPreload, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
            if (assetType == null) throw new ArgumentNullException(nameof(assetType));
            if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType)) throw new ArgumentException($"Asset type '{assetType.FullName}' must inherit UnityEngine.Object.", nameof(assetType));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Asset key cannot be empty.", nameof(key));
            AddressablesLoadedEntry entry = await AcquireEntryAsync(assetType, key, preloadMode, isPreload, cancellationToken);
            return new AssetHandle<UnityEngine.Object>(entry.Asset, () => { AddressablesLoadedEntry loadedEntry; lock (sync) { if (!loaded.TryGetValue(assetType, key, out loadedEntry)) return; if (loadedEntry.RefCount > 0) loadedEntry.RefCount--; if (loadedEntry.RefCount > 0 || loadedEntry.Policy == PreloadMode.NeverDie) return; loaded.Remove(assetType, key); } client.Release(loadedEntry.Asset); });
        }

        private async Task<AddressablesLoadedEntry> AcquireEntryAsync(Type assetType, string key, PreloadMode preloadMode, bool isPreload, CancellationToken cancellationToken)
        {
            lock (sync)
            {
                if (loaded.TryGetValue(assetType, key, out AddressablesLoadedEntry existing))
                {
                    if (preloadMode == PreloadMode.NeverDie) existing.Policy = PreloadMode.NeverDie;
                    if (!isPreload) existing.RefCount++;
                    return existing;
                }
            }
            UnityEngine.Object asset = await client.LoadAssetAsync(key, assetType, cancellationToken);
            return AddOrReuseEntry(assetType, key, preloadMode, isPreload, asset);
        }

        private AddressablesLoadedEntry AddOrReuseEntry(Type assetType, string key, PreloadMode preloadMode, bool isPreload, UnityEngine.Object asset)
        {
            lock (sync)
            {
                if (loaded.TryGetValue(assetType, key, out AddressablesLoadedEntry existing))
                {
                    if (preloadMode == PreloadMode.NeverDie) existing.Policy = PreloadMode.NeverDie;
                    if (!isPreload) existing.RefCount++; return existing;
                }
                AddressablesLoadedEntry created = new AddressablesLoadedEntry(asset, preloadMode);
                if (!isPreload) created.RefCount = 1;
                loaded.Add(assetType, key, created);
                return created;
            }
        }
    }
}
