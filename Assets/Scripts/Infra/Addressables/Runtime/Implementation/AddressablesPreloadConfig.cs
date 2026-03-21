using System.Collections.Generic;
using UnityEngine;

namespace Madbox.Addressables
{
    [CreateAssetMenu(menuName = "Madbox/Addressables/Preload Config", fileName = "AddressablesPreloadConfig")]
    public class AddressablesPreloadConfig : ScriptableObject
    {
        public IReadOnlyList<AddressablesPreloadConfigEntry> Entries => entries;
        [SerializeField] private List<AddressablesPreloadConfigEntry> entries = new List<AddressablesPreloadConfigEntry>();
    }
}

