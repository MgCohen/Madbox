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
        private readonly List<T> assets = new List<T>();
        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
        private readonly Action<UnityEngine.Object> releaseAsset;

        private bool ready;
        private int releasedFlag;

        internal AssetGroupHandle(Action<UnityEngine.Object> releaseAsset)
        {
            this.releaseAsset = releaseAsset ?? throw new ArgumentNullException(nameof(releaseAsset));
        }

        public bool IsReleased => releasedFlag != 0;
        public bool IsReady => ready;
        public Task WhenReady => completion.Task;
        public IReadOnlyList<T> Assets => assets;

        internal void CompleteFromAssets(IReadOnlyList<UnityEngine.Object> loadedAssets)
        {
            if (loadedAssets == null)
            {
                throw new ArgumentNullException(nameof(loadedAssets));
            }

            lock (sync)
            {
                if (IsReleased)
                {
                    completion.TrySetResult(true);
                    return;
                }

                assets.Clear();
                for (int i = 0; i < loadedAssets.Count; i++)
                {
                    if (loadedAssets[i] is T typedAsset)
                    {
                        assets.Add(typedAsset);
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

            ReleaseAllAssets();
            completion.TrySetResult(true);
        }

        private void ReleaseAllAssets()
        {
            List<T> snapshot = new List<T>();
            lock (sync)
            {
                for (int i = 0; i < assets.Count; i++)
                {
                    snapshot.Add(assets[i]);
                }
                assets.Clear();
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                releaseAsset(snapshot[i]);
            }
        }
    }
}
