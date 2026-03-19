using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.Addressables.Contracts
{
    public interface IAssetGroupHandle<out T> : IDisposable where T : UnityEngine.Object
    {
        bool IsReleased { get; }
        IReadOnlyList<IAssetHandle<T>> TypedHandles { get; }
        void Release();
    }
}
