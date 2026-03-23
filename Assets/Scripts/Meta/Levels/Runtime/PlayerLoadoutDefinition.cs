using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Levels
{
    [CreateAssetMenu(menuName = "Madbox/Levels/Player Loadout", fileName = "PlayerLoadoutDefinition")]
    public sealed class PlayerLoadoutDefinition : ScriptableObject
    {
        public AssetReference PlayerPrefab => playerPrefab;

        public IReadOnlyList<AssetReference> WeaponPrefabs => weaponPrefabs ?? (IReadOnlyList<AssetReference>)Array.Empty<AssetReference>();

        [SerializeField]
        private AssetReference playerPrefab;

        [SerializeField]
        private List<AssetReference> weaponPrefabs = new List<AssetReference>();
    }
}
