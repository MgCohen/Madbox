using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace Madbox.Addressables.Contracts
{
    public interface IAddressablesAssetClient
    {
        Task SyncCatalogAndContentAsync(CancellationToken cancellationToken);
        Task<UnityEngine.Object> LoadAssetAsync(string key, Type assetType, CancellationToken cancellationToken);
        Task<IReadOnlyList<string>> ResolveLabelAsync(Type assetType, AssetLabelReference label, CancellationToken cancellationToken);
        void Release(UnityEngine.Object asset);
    }
}

