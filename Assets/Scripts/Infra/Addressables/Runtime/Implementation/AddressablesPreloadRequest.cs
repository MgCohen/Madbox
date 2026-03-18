using System;
using Scaffold.Addressables.Contracts;

namespace Scaffold.Addressables
{
    internal readonly struct AddressablesPreloadRequest
    {
        public AddressablesPreloadRequest(Type assetType, AssetKey key, PreloadMode mode)
        {
            AssetType = assetType ?? throw new ArgumentNullException(nameof(assetType));
            Key = key;
            Catalog = default;
            Mode = mode;
            IsCatalog = false;
        }

        public AddressablesPreloadRequest(Type assetType, CatalogKey catalog, PreloadMode mode)
        {
            AssetType = assetType ?? throw new ArgumentNullException(nameof(assetType));
            Key = default;
            Catalog = catalog;
            Mode = mode;
            IsCatalog = true;
        }

        public Type AssetType { get; }
        public AssetKey Key { get; }
        public CatalogKey Catalog { get; }
        public PreloadMode Mode { get; }
        public bool IsCatalog { get; }
    }
}
