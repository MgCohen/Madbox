using Madbox.Addressables;
using Madbox.Addressables.Contracts;
using Madbox.Levels;
using UnityEngine.AddressableAssets;

namespace Madbox.Bootstrap
{
    public sealed class PlayerLoadoutAssetProvider : AssetProvider<PlayerLoadoutDefinition>
    {
        public PlayerLoadoutAssetProvider(IAddressablesGateway gateway)
            : base(gateway)
        {
        }

        protected override AssetReference AssetKey => new AssetReference("Player Loadout");
    }
}
