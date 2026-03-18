using System;
using System.Collections.Generic;
using Madbox.Addressables.Contracts;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Addressables
{
    internal class AddressablesPreloadConfigRequestBuilder
    {
        public void AppendRequests(AddressablesPreloadConfig config, AssetKey configKey, ICollection<AddressablesPreloadRequest> target)
        {
            GuardConfig(config, configKey);
            GuardTarget(target);
            IReadOnlyList<AddressablesPreloadConfigEntry> entries = config.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                AddressablesPreloadRequest request = BuildRequest(entries[i], configKey, i);
                target.Add(request);
            }
        }

        private AddressablesPreloadRequest BuildRequest(AddressablesPreloadConfigEntry entry, AssetKey configKey, int index)
        {
            GuardEntry(entry, configKey, index);
            Type assetType = ResolveAssetType(entry, configKey, index);
            if (entry.ReferenceType == PreloadReferenceType.AssetReference) { return BuildAssetReferenceRequest(entry, assetType, configKey, index); }
            if (entry.ReferenceType == PreloadReferenceType.LabelReference) { return BuildLabelReferenceRequest(entry, assetType, configKey, index); }
            throw CreateConfigException(configKey, index, $"Unsupported reference type '{entry.ReferenceType}'.");
        }

        private AddressablesPreloadRequest BuildAssetReferenceRequest(AddressablesPreloadConfigEntry entry, Type assetType, AssetKey configKey, int index)
        {
            AssetReference reference = entry.AssetReference;
            string keyValue = reference?.RuntimeKey?.ToString();
            if (string.IsNullOrWhiteSpace(keyValue)) { throw CreateConfigException(configKey, index, "AssetReference is missing or has no runtime key."); }
            AssetKey key = new AssetKey(keyValue);
            return new AddressablesPreloadRequest(assetType, key, entry.Mode);
        }

        private AddressablesPreloadRequest BuildLabelReferenceRequest(AddressablesPreloadConfigEntry entry, Type assetType, AssetKey configKey, int index)
        {
            AssetLabelReference label = entry.LabelReference;
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) { throw CreateConfigException(configKey, index, "LabelReference is missing labelString."); }
            return new AddressablesPreloadRequest(assetType, label, entry.Mode);
        }

        private Type ResolveAssetType(AddressablesPreloadConfigEntry entry, AssetKey configKey, int index)
        {
            Type assetType = entry.AssetType?.Type;
            if (assetType == null) { throw CreateConfigException(configKey, index, "AssetType is missing or unresolved."); }
            if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType)) { throw CreateConfigException(configKey, index, $"AssetType '{assetType.FullName}' must inherit UnityEngine.Object."); }
            return assetType;
        }

        private Exception CreateConfigException(AssetKey configKey, int index, string message)
        {
            return new InvalidOperationException($"Invalid preload config entry at config '{configKey.Value}', index {index}. {message}");
        }

        private void GuardConfig(AddressablesPreloadConfig config, AssetKey configKey)
        {
            if (config == null) { throw new ArgumentNullException(nameof(config), $"Preload config '{configKey.Value}' failed to load."); }
        }

        private void GuardTarget(ICollection<AddressablesPreloadRequest> target)
        {
            if (target == null) { throw new ArgumentNullException(nameof(target)); }
        }

        private void GuardEntry(AddressablesPreloadConfigEntry entry, AssetKey configKey, int index)
        {
            if (entry == null) { throw CreateConfigException(configKey, index, "Entry is null."); }
        }
    }
}
