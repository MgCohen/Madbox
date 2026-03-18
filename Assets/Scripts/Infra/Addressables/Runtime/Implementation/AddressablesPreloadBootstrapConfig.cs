using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Addressables
{
    [CreateAssetMenu(menuName = "Madbox/Addressables/Preload Bootstrap Config", fileName = "AddressablesPreloadBootstrapConfig")]
    public class AddressablesPreloadBootstrapConfig : ScriptableObject
    {
        public IReadOnlyList<AssetReferenceT<AddressablesPreloadConfigWrapper>> Wrappers => wrappers;
        [SerializeField] private List<AssetReferenceT<AddressablesPreloadConfigWrapper>> wrappers = new List<AssetReferenceT<AddressablesPreloadConfigWrapper>>();
    }
}
