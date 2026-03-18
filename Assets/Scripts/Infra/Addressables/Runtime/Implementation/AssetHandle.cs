using System;
using System.Threading;
using Madbox.Addressables.Contracts;
using UnityEngine;

namespace Madbox.Addressables
{
    internal sealed class AssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
    {
        public AssetHandle(string id, T asset, Action onRelease)
        {
            GuardConstructor(id, asset, onRelease);
            Id = id;
            Asset = asset;
            this.onRelease = onRelease;
        }

        public string Id { get; }
        public Type AssetType => typeof(T);
        public UnityEngine.Object UntypedAsset => Asset;
        public T Asset { get; }
        public bool IsReleased => releasedFlag != 0;

        private readonly Action onRelease;
        private int releasedFlag;

        public void Release()
        {
            if (Interlocked.Exchange(ref releasedFlag, 1) != 0)
            {
                return;
            }

            onRelease();
        }

        private void GuardConstructor(string id, T asset, Action onRelease)
        {
            if (string.IsNullOrWhiteSpace(id)) { throw new ArgumentException("Handle id cannot be empty.", nameof(id)); }
            if (asset == null) { throw new ArgumentNullException(nameof(asset)); }
            if (onRelease == null) { throw new ArgumentNullException(nameof(onRelease)); }
        }
    }
}
