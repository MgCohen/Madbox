using System;

namespace Madbox.Addressables.Contracts
{
    public readonly struct AddressablesPreloadRegistration
    {
        public AddressablesPreloadRegistration(Type assetType, string key, PreloadMode mode)
        {
            if (assetType == null) { throw new ArgumentNullException(nameof(assetType)); }
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("Preload key cannot be empty.", nameof(key)); }
            AssetType = assetType;
            Key = key;
            Mode = mode;
        }

        public Type AssetType { get; }
        public string Key { get; }
        public PreloadMode Mode { get; }
    }
}
