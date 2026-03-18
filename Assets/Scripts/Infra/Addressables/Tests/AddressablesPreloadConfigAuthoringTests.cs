using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Madbox.Addressables.Tests
{
    public sealed class AddressablesPreloadConfigAuthoringTests
    {
        [Test]
        public void AssetGroups_PreloadConfigEntry_UsesBootstrapConfigAssetKey()
        {
            string groupsDirectory = Path.Combine("Assets", "AddressableAssetsData", "AssetGroups");
            string[] groupFiles = Directory.GetFiles(groupsDirectory, "*.asset", SearchOption.TopDirectoryOnly);
            bool containsKey = groupFiles.Select(File.ReadAllText).Any(content => content.Contains($"m_Address: {AddressablesPreloadConstants.BootstrapConfigAssetKey}"));
            Assert.IsTrue(containsKey, $"Expected at least one Addressables group entry to use '{AddressablesPreloadConstants.BootstrapConfigAssetKey}'.");
        }
    }
}
