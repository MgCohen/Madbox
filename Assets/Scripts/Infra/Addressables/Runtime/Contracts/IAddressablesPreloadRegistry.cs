using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Addressables.Contracts
{
    public interface IAddressablesPreloadRegistry
    {
        void Register(AssetKey key, PreloadMode mode);
        void Register(AssetReference reference, PreloadMode mode);
        void Register(AssetLabelReference label, PreloadMode mode);
        void Register<T>(AssetKey key, PreloadMode mode) where T : UnityEngine.Object;
        void Register<T>(AssetReference reference, PreloadMode mode) where T : UnityEngine.Object;
        void Register<T>(AssetLabelReference label, PreloadMode mode) where T : UnityEngine.Object;
    }
}
