using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine;

namespace Madbox.Addressables
{
    internal sealed class AssetGroupHandle<T> : IAssetGroupHandle<T> where T : UnityEngine.Object
    {
        private readonly object sync = new object();
        private readonly List<IAssetHandle<T>> typedHandles = new List<IAssetHandle<T>>();
        private readonly List<T> assets = new List<T>();
        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

        private bool ready;
        private int releasedFlag;

        public bool IsReleased => releasedFlag != 0;
        public bool IsReady => ready;
        public Task WhenReady => completion.Task;
        public IReadOnlyList<T> Assets => assets;

        internal void AddHandle(IAssetHandle<T> handle)
        {
            if (handle == null)
            {
                return;
            }

            lock (sync)
            {
                if (IsReleased)
                {
                    handle.Release();
                    return;
                }

                typedHandles.Add(handle);
            }
        }

        internal async Task CompleteAsync(CancellationToken cancellationToken)
        {
            IAssetHandle<T>[] snapshot = GetHandleSnapshot();
            for (int i = 0; i < snapshot.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await snapshot[i].WhenReady;
            }

            lock (sync)
            {
                if (IsReleased)
                {
                    completion.TrySetResult(true);
                    return;
                }

                assets.Clear();
                for (int i = 0; i < typedHandles.Count; i++)
                {
                    IAssetHandle<T> handle = typedHandles[i];
                    if (handle != null && handle.IsReady)
                    {
                        assets.Add(handle.Asset);
                    }
                }

                ready = true;
            }

            completion.TrySetResult(true);
        }

        internal void Fail(Exception exception)
        {
            if (exception == null)
            {
                completion.TrySetException(new InvalidOperationException("Group load failed."));
                return;
            }

            completion.TrySetException(exception);
        }

        public void Dispose()
        {
            Release();
        }

        public void Release()
        {
            if (Interlocked.Exchange(ref releasedFlag, 1) != 0)
            {
                return;
            }

            ReleaseAllHandles();
            completion.TrySetResult(true);
        }

        private IAssetHandle<T>[] GetHandleSnapshot()
        {
            lock (sync)
            {
                return typedHandles.ToArray();
            }
        }

        private void ReleaseAllHandles()
        {
            IAssetHandle<T>[] snapshot = GetHandleSnapshot();
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].Release();
            }
        }
    }
}
