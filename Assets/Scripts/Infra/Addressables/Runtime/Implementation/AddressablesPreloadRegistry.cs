using System;
using System.Collections.Generic;
using Madbox.Addressables.Contracts;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Addressables
{
    public sealed class AddressablesPreloadRegistry : IAddressablesPreloadRegistry, IAddressablesPreloadSource
    {
        private readonly List<AddressablesPreloadRequest> requests = new List<AddressablesPreloadRequest>();
        private readonly object sync = new object();

        public void Register(AssetKey key, PreloadMode mode)
        {
            GuardAssetKey(key);
            Register<UnityEngine.Object>(key, mode);
        }

        public void Register(AssetReference reference, PreloadMode mode)
        {
            GuardReference(reference);
            Register<UnityEngine.Object>(reference, mode);
        }

        public void Register(AssetLabelReference label, PreloadMode mode)
        {
            GuardLabel(label);
            Register<UnityEngine.Object>(label, mode);
        }

        public void Register<T>(AssetKey key, PreloadMode mode) where T : UnityEngine.Object
        {
            GuardAssetKey(key);
            AddressablesPreloadRequest request = new AddressablesPreloadRequest(typeof(T), key, mode);
            lock (sync)
            {
                requests.Add(request);
            }
        }

        public void Register<T>(AssetReference reference, PreloadMode mode) where T : UnityEngine.Object
        {
            GuardReference(reference);
            AssetKey key = CreateAssetKey(reference);
            AddressablesPreloadRequest request = new AddressablesPreloadRequest(typeof(T), key, mode);
            lock (sync)
            {
                requests.Add(request);
            }
        }

        public void Register<T>(AssetLabelReference label, PreloadMode mode) where T : UnityEngine.Object
        {
            GuardLabel(label);
            AddressablesPreloadRequest request = new AddressablesPreloadRequest(typeof(T), label, mode);
            lock (sync)
            {
                requests.Add(request);
            }
        }

        IReadOnlyList<AddressablesPreloadRequest> IAddressablesPreloadSource.Snapshot()
        {
            lock (sync)
            {
                return requests.ToArray();
            }
        }

        private void GuardAssetKey(AssetKey key)
        {
            if (string.IsNullOrWhiteSpace(key.Value)) { throw new ArgumentException("Asset key cannot be empty.", nameof(key)); }
        }

        private void GuardReference(AssetReference reference)
        {
            if (reference == null) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
            object runtimeKey = reference.RuntimeKey;
            if (runtimeKey == null) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
            string keyValue = runtimeKey.ToString();
            if (string.IsNullOrWhiteSpace(keyValue)) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
        }

        private void GuardLabel(AssetLabelReference label)
        {
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) { throw new ArgumentException("Label reference cannot be empty.", nameof(label)); }
        }

        private AssetKey CreateAssetKey(AssetReference reference)
        {
            object runtimeKey = reference.RuntimeKey;
            string keyValue = runtimeKey?.ToString();
            return new AssetKey(keyValue);
        }
    }
}
