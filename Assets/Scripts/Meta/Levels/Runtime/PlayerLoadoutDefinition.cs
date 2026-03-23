using System;
using System.Collections.Generic;
using Madbox.Entity;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Levels
{
    [CreateAssetMenu(menuName = "Madbox/Levels/Player Loadout", fileName = "PlayerLoadoutDefinition")]
    public sealed class PlayerLoadoutDefinition : ScriptableObject
    {
        public AssetReference PlayerPrefab => playerPrefab;

        public IReadOnlyList<AssetReference> WeaponPrefabs => weaponPrefabs ?? (IReadOnlyList<AssetReference>)Array.Empty<AssetReference>();

        public IReadOnlyList<WeaponModifierSet> WeaponModifiers => weaponModifiers ?? (IReadOnlyList<WeaponModifierSet>)Array.Empty<WeaponModifierSet>();

        [SerializeField]
        private AssetReference playerPrefab;

        [SerializeField]
        private List<AssetReference> weaponPrefabs = new List<AssetReference>();

        [SerializeField]
        private List<WeaponModifierSet> weaponModifiers = new List<WeaponModifierSet>();
    }

    [Serializable]
    public sealed class WeaponModifierSet
    {
        public IReadOnlyList<EntityAttributeModifierEntry> Modifiers =>
            modifiers ?? (IReadOnlyList<EntityAttributeModifierEntry>)Array.Empty<EntityAttributeModifierEntry>();

        [SerializeField]
        private List<EntityAttributeModifierEntry> modifiers = new List<EntityAttributeModifierEntry>();
    }
}
