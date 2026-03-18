using System;
using System.Threading;
using Scaffold.Addressables.Contracts;
using UnityEngine;

namespace Scaffold.Addressables
{
    internal sealed class AssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
    {
        private readonly Action onRelease;
        private int releasedFlag;

        public AssetHandle(string id, T asset, Action onRelease)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Asset = asset ? asset : throw new ArgumentNullException(nameof(asset));
            this.onRelease = onRelease ?? throw new ArgumentNullException(nameof(onRelease));
        }

        public string Id { get; }
        public Type AssetType => typeof(T);
        public UnityEngine.Object UntypedAsset => Asset;
        public T Asset { get; }
        public bool IsReleased => releasedFlag != 0;

        public void Release()
        {
            if (Interlocked.Exchange(ref releasedFlag, 1) != 0)
            {
                return;
            }

            onRelease();
        }
    }
}
