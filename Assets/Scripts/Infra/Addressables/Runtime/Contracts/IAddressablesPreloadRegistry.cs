using UnityEngine;

namespace Scaffold.Addressables.Contracts
{
    public interface IAddressablesPreloadRegistry
    {
        void Register(AssetKey key, PreloadMode mode);
        void Register(CatalogKey key, PreloadMode mode);
        void Register<T>(AssetKey key, PreloadMode mode) where T : UnityEngine.Object;
        void Register<T>(CatalogKey key, PreloadMode mode) where T : UnityEngine.Object;
    }
}
