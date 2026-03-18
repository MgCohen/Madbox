using System;
using UnityEngine;

namespace Scaffold.Addressables.Contracts
{
    public interface IAssetHandle
    {
        string Id { get; }
        Type AssetType { get; }
        UnityEngine.Object UntypedAsset { get; }
        bool IsReleased { get; }
        void Release();
    }

    public interface IAssetHandle<out T> : IAssetHandle where T : UnityEngine.Object
    {
        T Asset { get; }
    }
}
