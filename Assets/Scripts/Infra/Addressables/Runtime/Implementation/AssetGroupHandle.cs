using System;
using System.Collections.Generic;
using System.Threading;
using Madbox.Addressables.Contracts;
using UnityEngine;

namespace Madbox.Addressables
{
    internal sealed class AssetGroupHandle<T> : IAssetGroupHandle<T> where T : UnityEngine.Object
    {
        public AssetGroupHandle(string id, IReadOnlyList<IAssetHandle<T>> handles)
        {
            GuardConstructor(id, handles);
            Id = id;
            TypedHandles = handles;
        }

        public string Id { get; }
        public bool IsReleased => releasedFlag != 0;
        public IReadOnlyList<IAssetHandle<T>> TypedHandles { get; }

        private int releasedFlag;

        public void Dispose()
        {
            Release();
        }

        public void Release()
        {
            if (Interlocked.Exchange(ref releasedFlag, 1) != 0) { return; }
            ReleaseAllHandles();
        }

        private void ReleaseAllHandles()
        {
            foreach (IAssetHandle<T> handle in TypedHandles) { handle.Release(); }
        }

        private void GuardConstructor(string id, IReadOnlyList<IAssetHandle<T>> handles)
        {
            if (string.IsNullOrWhiteSpace(id)) { throw new ArgumentException("Group handle id cannot be empty.", nameof(id)); }
            if (handles == null) { throw new ArgumentNullException(nameof(handles)); }
        }
    }
}
