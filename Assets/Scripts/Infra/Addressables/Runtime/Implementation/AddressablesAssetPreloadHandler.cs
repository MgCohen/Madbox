using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine.AddressableAssets;

namespace Madbox.Addressables
{
    public sealed class AddressablesAssetPreloadHandler : IAssetPreloadHandler
    {
        public AddressablesAssetPreloadHandler(IAddressablesAssetClient client)
        {
            if (client == null)
{
    throw new ArgumentNullException(nameof(client));
}
            this.client = client;
        }

        private readonly IAddressablesAssetClient client;

        public async Task<IReadOnlyList<AddressablesPreloadRegistration>> BuildAsync(AddressablesPreloadConfig config, CancellationToken cancellationToken)
        {
            if (config == null)
{
    return Array.Empty<AddressablesPreloadRegistration>();
}
            cancellationToken.ThrowIfCancellationRequested();
            return await BuildRegistrationsAsync(config.Entries, cancellationToken);
        }

        private async Task<IReadOnlyList<AddressablesPreloadRegistration>> BuildRegistrationsAsync(IReadOnlyList<AddressablesPreloadConfigEntry> entries, CancellationToken cancellationToken)
        {
            List<AddressablesPreloadRegistration> registrations = new List<AddressablesPreloadRegistration>();
            for (int i = 0; i < entries.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<AddressablesPreloadRegistration> entryRegistrations = await BuildEntryRegistrationsAsync(entries[i], i, cancellationToken);
                for (int j = 0; j < entryRegistrations.Count; j++)
                {
                    registrations.Add(entryRegistrations[j]);
                }
            }
            return registrations;
        }

        private async Task<IReadOnlyList<AddressablesPreloadRegistration>> BuildEntryRegistrationsAsync(AddressablesPreloadConfigEntry entry, int index, CancellationToken cancellationToken)
        {
            if (entry == null || entry.AssetType?.Type == null) throw new InvalidOperationException($"Invalid preload config entry at index {index}. Entry or AssetType is missing.");
            Type assetType = entry.AssetType.Type; if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType)) throw new InvalidOperationException($"Invalid preload config entry at index {index}. AssetType '{assetType.FullName}' must inherit UnityEngine.Object.");
            if ((entry.ReferenceType == PreloadReferenceType.AssetReference && entry.AssetReference == null) || (entry.ReferenceType == PreloadReferenceType.LabelReference && (entry.LabelReference == null || string.IsNullOrWhiteSpace(entry.LabelReference.labelString)))) throw new InvalidOperationException($"Invalid preload config entry at index {index}. Reference data is missing.");
            if (entry.ReferenceType == PreloadReferenceType.AssetReference)
            {
                AssetReference reference = entry.AssetReference;
                string key = reference?.RuntimeKey?.ToString();
                if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Asset reference is not valid.", nameof(reference));
                return new[] { new AddressablesPreloadRegistration(assetType, key, entry.Mode) };
            }
            if (entry.ReferenceType == PreloadReferenceType.LabelReference) return await BuildLabelRegistrationsAsync(entry, cancellationToken);
            throw new InvalidOperationException($"Invalid preload config entry at index {index}. Unsupported reference type '{entry.ReferenceType}'.");
        }

        private async Task<IReadOnlyList<AddressablesPreloadRegistration>> BuildLabelRegistrationsAsync(AddressablesPreloadConfigEntry entry, CancellationToken cancellationToken)
        {
            Type assetType = entry.AssetType.Type;
            IReadOnlyList<string> keys = await client.ResolveLabelAsync(assetType, entry.LabelReference, cancellationToken);
            return BuildRegistrationsByKeys(assetType, keys, entry.Mode);
        }

        private IReadOnlyList<AddressablesPreloadRegistration> BuildRegistrationsByKeys(Type assetType, IReadOnlyList<string> keys, PreloadMode mode)
        {
            List<AddressablesPreloadRegistration> byLabel = new List<AddressablesPreloadRegistration>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                AddressablesPreloadRegistration registration = new AddressablesPreloadRegistration(assetType, keys[i], mode);
                byLabel.Add(registration);
            }
            return byLabel;
        }


    }
}


