using Madbox.Addressables.Contracts;
using UnityEngine;

namespace Madbox.Players
{
    /// <summary>
    /// An instantiated GameObject plus the Addressables handle that must stay alive for the instance.
    /// </summary>
    public readonly struct AddressablePrefabSpawn
    {
        public AddressablePrefabSpawn(GameObject instance, IAssetHandle<GameObject> handle)
        {
            Instance = instance;
            Handle = handle;
        }

        public GameObject Instance { get; }

        public IAssetHandle<GameObject> Handle { get; }
    }
}
