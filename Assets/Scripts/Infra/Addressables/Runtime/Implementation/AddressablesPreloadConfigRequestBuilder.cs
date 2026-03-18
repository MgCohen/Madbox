using System;
using System.Collections.Generic;
using Madbox.Addressables.Contracts;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.Addressables
{
    internal class AddressablesPreloadConfigRequestBuilder
    {
        public void AppendRequests(AddressablesPreloadConfigWrapper wrapper, AssetKey wrapperKey, ICollection<AddressablesPreloadRequest> target)
        {
            GuardWrapper(wrapper, wrapperKey);
            GuardTarget(target);
            IReadOnlyList<AddressablesPreloadConfigEntry> entries = wrapper.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                AddressablesPreloadRequest request = BuildRequest(entries[i], wrapperKey, i);
                target.Add(request);
            }
        }

        private AddressablesPreloadRequest BuildRequest(AddressablesPreloadConfigEntry entry, AssetKey wrapperKey, int index)
        {
            GuardEntry(entry, wrapperKey, index);
            Type assetType = ResolveAssetType(entry, wrapperKey, index);
            if (entry.ReferenceType == PreloadReferenceType.AssetReference) { return BuildAssetReferenceRequest(entry, assetType, wrapperKey, index); }
            if (entry.ReferenceType == PreloadReferenceType.LabelReference) { return BuildLabelReferenceRequest(entry, assetType, wrapperKey, index); }
            throw CreateConfigException(wrapperKey, index, $"Unsupported reference type '{entry.ReferenceType}'.");
        }

        private AddressablesPreloadRequest BuildAssetReferenceRequest(AddressablesPreloadConfigEntry entry, Type assetType, AssetKey wrapperKey, int index)
        {
            AssetReference reference = entry.AssetReference;
            string keyValue = reference?.RuntimeKey?.ToString();
            if (string.IsNullOrWhiteSpace(keyValue)) { throw CreateConfigException(wrapperKey, index, "AssetReference is missing or has no runtime key."); }
            AssetKey key = new AssetKey(keyValue);
            return new AddressablesPreloadRequest(assetType, key, entry.Mode);
        }

        private AddressablesPreloadRequest BuildLabelReferenceRequest(AddressablesPreloadConfigEntry entry, Type assetType, AssetKey wrapperKey, int index)
        {
            AssetLabelReference label = entry.LabelReference;
            if (label == null || string.IsNullOrWhiteSpace(label.labelString)) { throw CreateConfigException(wrapperKey, index, "LabelReference is missing labelString."); }
            return new AddressablesPreloadRequest(assetType, label, entry.Mode);
        }

        private Type ResolveAssetType(AddressablesPreloadConfigEntry entry, AssetKey wrapperKey, int index)
        {
            Type assetType = entry.AssetType?.Type;
            if (assetType == null) { throw CreateConfigException(wrapperKey, index, "AssetType is missing or unresolved."); }
            if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType)) { throw CreateConfigException(wrapperKey, index, $"AssetType '{assetType.FullName}' must inherit UnityEngine.Object."); }
            return assetType;
        }

        private Exception CreateConfigException(AssetKey wrapperKey, int index, string message)
        {
            return new InvalidOperationException($"Invalid preload config entry at wrapper '{wrapperKey.Value}', index {index}. {message}");
        }

        private void GuardWrapper(AddressablesPreloadConfigWrapper wrapper, AssetKey wrapperKey)
        {
            if (wrapper == null) { throw new ArgumentNullException(nameof(wrapper), $"Preload wrapper '{wrapperKey.Value}' failed to load."); }
        }

        private void GuardTarget(ICollection<AddressablesPreloadRequest> target)
        {
            if (target == null) { throw new ArgumentNullException(nameof(target)); }
        }

        private void GuardEntry(AddressablesPreloadConfigEntry entry, AssetKey wrapperKey, int index)
        {
            if (entry == null) { throw CreateConfigException(wrapperKey, index, "Entry is null."); }
        }
    }
}
