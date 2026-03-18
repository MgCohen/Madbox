using System;
using Madbox.Addressables.Contracts;
using UnityEngine.AddressableAssets;

namespace Madbox.Addressables
{
    internal readonly struct AddressablesPreloadRequest
    {
        public AddressablesPreloadRequest(Type assetType, AssetKey key, PreloadMode mode)
        {
            if (assetType == null) { throw new ArgumentNullException(nameof(assetType)); }
            if (string.IsNullOrWhiteSpace(key.Value)) { throw new ArgumentException("Asset key cannot be empty.", nameof(key)); }
            AssetType = assetType;
            Key = key;
            Label = default;
            Mode = mode;
            IsCatalog = false;
        }

        public AddressablesPreloadRequest(Type assetType, AssetLabelReference label, PreloadMode mode)
        {
            if (assetType == null) { throw new ArgumentNullException(nameof(assetType)); }
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) { throw new ArgumentException("Label reference cannot be empty.", nameof(label)); }
            AssetType = assetType;
            Key = default;
            Label = CreateLabelCopy(label.labelString);
            Mode = mode;
            IsCatalog = true;
        }

        public Type AssetType { get; }
        public AssetKey Key { get; }
        public AssetLabelReference Label { get; }
        public PreloadMode Mode { get; }
        public bool IsCatalog { get; }

        private static AssetLabelReference CreateLabelCopy(string value)
        {
            AssetLabelReference label = new AssetLabelReference();
            label.labelString = value;
            return label;
        }
    }
}
