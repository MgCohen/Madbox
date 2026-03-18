using System;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine;
#pragma warning disable SCA0006

namespace Madbox.Addressables
{
    internal sealed class AssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
    {
        public AssetHandle(string id, T asset, Action onRelease)
        {
            GuardConstructor(id, asset, onRelease);
            Id = id;
            this.asset = asset;
            this.onRelease = onRelease;
            state = AssetHandleState.Ready;
            completion.TrySetResult(true);
        }

        public AssetHandle(string id)
        {
            GuardHandleId(id);
            Id = id;
            state = AssetHandleState.Loading;
        }

        public string Id { get; }
        public Type AssetType => typeof(T);
        public UnityEngine.Object UntypedAsset => IsReady ? asset : null;
        public T Asset
        {
            get
            {
                if (!IsReady) { throw new InvalidOperationException($"Asset handle '{Id}' is not ready."); }
                return asset;
            }
        }
        public bool IsReleased => releasedFlag != 0;
        public AssetHandleState State => state;
        public bool IsReady => state == AssetHandleState.Ready;
        public Task WhenReady => completion.Task;

        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
        private Action onRelease;
        private IAssetHandle<T> inner;
        private T asset;
        private AssetHandleState state;
        private int releasedFlag;

        internal void Complete(IAssetHandle<T> loadedHandle)
        {
            if (loadedHandle == null) { throw new ArgumentNullException(nameof(loadedHandle)); }
            if (state != AssetHandleState.Loading) { return; }

            inner = loadedHandle;
            asset = loadedHandle.Asset;
            state = IsReleased ? AssetHandleState.Released : AssetHandleState.Ready;
            completion.TrySetResult(true);
            if (IsReleased) { loadedHandle.Release(); }
        }

        internal void Fail(Exception exception)
        {
            if (exception == null) { throw new ArgumentNullException(nameof(exception)); }
            if (state != AssetHandleState.Loading) { return; }
            state = IsReleased ? AssetHandleState.Released : AssetHandleState.Faulted;
            completion.TrySetException(exception);
        }

        public void Release()
        {
            if (Interlocked.Exchange(ref releasedFlag, 1) != 0) { return; }
            if (state == AssetHandleState.Loading) { return; }
            ReleaseReadyHandle();
            state = AssetHandleState.Released;
        }

        private void ReleaseReadyHandle()
        {
            if (state != AssetHandleState.Ready) { return; }
            if (inner != null) { inner.Release(); return; }
            onRelease?.Invoke();
        }

        private void GuardConstructor(string id, T asset, Action onRelease)
        {
            GuardHandleId(id);
            if (asset == null) { throw new ArgumentNullException(nameof(asset)); }
            if (onRelease == null) { throw new ArgumentNullException(nameof(onRelease)); }
        }

        private void GuardHandleId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) { throw new ArgumentException("Handle id cannot be empty.", nameof(id)); }
        }
    }
}
#pragma warning restore SCA0006
