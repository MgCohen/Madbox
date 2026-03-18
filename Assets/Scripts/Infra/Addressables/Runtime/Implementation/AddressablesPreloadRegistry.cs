using System;
using System.Collections.Generic;
using Scaffold.Addressables.Contracts;
using UnityEngine;

namespace Scaffold.Addressables
{
    public sealed class AddressablesPreloadRegistry : IAddressablesPreloadRegistry, IAddressablesPreloadSource
    {
        private readonly List<AddressablesPreloadRequest> requests = new List<AddressablesPreloadRequest>();
        private readonly object sync = new object();

        public void Register(AssetKey key, PreloadMode mode)
        {
            Register<UnityEngine.Object>(key, mode);
        }

        public void Register(CatalogKey key, PreloadMode mode)
        {
            Register<UnityEngine.Object>(key, mode);
        }

        public void Register<T>(AssetKey key, PreloadMode mode) where T : UnityEngine.Object
        {
            lock (sync)
            {
                requests.Add(new AddressablesPreloadRequest(typeof(T), key, mode));
            }
        }

        public void Register<T>(CatalogKey key, PreloadMode mode) where T : UnityEngine.Object
        {
            lock (sync)
            {
                requests.Add(new AddressablesPreloadRequest(typeof(T), key, mode));
            }
        }

        IReadOnlyList<AddressablesPreloadRequest> IAddressablesPreloadSource.Snapshot()
        {
            lock (sync)
            {
                return requests.ToArray();
            }
        }
    }
}
