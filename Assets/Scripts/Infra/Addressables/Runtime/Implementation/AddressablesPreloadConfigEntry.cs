using System;
using Madbox.Addressables.Contracts;
using Scaffold.Types;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Addressables
{
    [Serializable]
    public class AddressablesPreloadConfigEntry
    {
        public TypeReference AssetType => assetType;
        [SerializeField] private TypeReference assetType;

        public PreloadReferenceType ReferenceType => referenceType;
        [SerializeField] private PreloadReferenceType referenceType;

        public AssetReference AssetReference => assetReference;
        [SerializeField] private AssetReference assetReference;

        public AssetLabelReference LabelReference => labelReference;
        [SerializeField] private AssetLabelReference labelReference;

        public PreloadMode Mode => mode;
        [SerializeField] private PreloadMode mode;
    }
}
