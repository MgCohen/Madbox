using Madbox.Addressables;
using Madbox.Addressables.Contracts;
using Madbox.Levels;
using UnityEngine.AddressableAssets;

namespace Madbox.Bootstrap
{
    /// <summary>
    /// Preloads all <see cref="LevelDefinition"/> assets with Addressables label <c>MadboxLevels</c> (same pattern as <see cref="NavigationAssetProvider"/> for a single settings asset).
    /// </summary>
    public sealed class LevelAssetProvider : AssetGroupProvider<LevelDefinition>
    {
        public LevelAssetProvider(IAddressablesGateway gateway)
            : base(gateway)
        {
        }

        protected override AssetLabelReference LabelKey => new AssetLabelReference { labelString = "Levels" };
    }
}
