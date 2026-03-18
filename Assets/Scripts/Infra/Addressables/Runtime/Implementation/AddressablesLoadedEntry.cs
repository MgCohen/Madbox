using UnityEngine;

namespace Scaffold.Addressables
{
    internal sealed class AddressablesLoadedEntry
    {
        public AddressablesLoadedEntry(Object asset)
        {
            Asset = asset;
            RefCount = 1;
        }

        public Object Asset { get; }
        public int RefCount { get; set; }
    }
}
