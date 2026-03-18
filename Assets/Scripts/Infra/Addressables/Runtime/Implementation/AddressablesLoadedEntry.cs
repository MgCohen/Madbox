using UnityEngine;

namespace Madbox.Addressables
{
    internal sealed class AddressablesLoadedEntry
    {
        public AddressablesLoadedEntry(Object asset)
        {
            GuardAsset(asset);
            Asset = asset;
            RefCount = 1;
        }

        public Object Asset { get; }
        public int RefCount { get; set; }

        private void GuardAsset(Object asset)
        {
            if (asset == null) { throw new System.ArgumentNullException(nameof(asset)); }
        }
    }
}
