using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Madbox.Addressables.Contracts
{
    public interface IAssetHandle
    {
        Type AssetType { get; }
        UnityEngine.Object UntypedAsset { get; }
        bool IsReleased { get; }
        AssetHandleState State { get; }
        bool IsReady { get; }
        Task WhenReady { get; }
        void Release();
    }

    public interface IAssetHandle<out T> : IAssetHandle where T : UnityEngine.Object
    {
        T Asset { get; }
    }
}

