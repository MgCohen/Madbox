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
            if (client == null) { throw new ArgumentNullException(nameof(client)); }
            this.client = client;
        }

        private readonly IAddressablesAssetClient client;

        public async Task<IReadOnlyList<AddressablesPreloadRegistration>> BuildAsync(AddressablesPreloadConfig config, CancellationToken cancellationToken)
        {
            if (config == null) { return Array.Empty<AddressablesPreloadRegistration>(); }
            cancellationToken.ThrowIfCancellationRequested();
            return await BuildRegistrationsAsync(config.Entries, cancellationToken);
        }

        private async Task<IReadOnlyList<AddressablesPreloadRegistration>> BuildRegistrationsAsync(IReadOnlyList<AddressablesPreloadConfigEntry> entries, CancellationToken cancellationToken)
        {
            List<AddressablesPreloadRegistration> registrations = new List<AddressablesPreloadRegistration>();
            await AddEntriesAsync(entries, registrations, cancellationToken);
            return registrations;
        }

        private async Task AddEntriesAsync(IReadOnlyList<AddressablesPreloadConfigEntry> entries, ICollection<AddressablesPreloadRegistration> registrations, CancellationToken cancellationToken)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<AddressablesPreloadRegistration> entryRegistrations = await BuildRegistrationsForEntryAsync(entries[i], i, cancellationToken);
                AddRange(registrations, entryRegistrations);
            }
        }

        private async Task<IReadOnlyList<AddressablesPreloadRegistration>> BuildRegistrationsForEntryAsync(AddressablesPreloadConfigEntry entry, int index, CancellationToken cancellationToken)
        {
            GuardPreloadEntry(entry, index);
            return entry.ReferenceType switch
            {
                PreloadReferenceType.AssetReference => BuildAssetReferenceRegistration(entry),
                PreloadReferenceType.LabelReference => await BuildLabelRegistrationsAsync(entry, cancellationToken),
                _ => throw CreateUnsupportedReferenceTypeException(entry, index),
            };
        }

        private IReadOnlyList<AddressablesPreloadRegistration> BuildAssetReferenceRegistration(AddressablesPreloadConfigEntry entry)
        {
            string key = ResolveReferenceKey(entry.AssetReference);
            Type assetType = entry.AssetType.Type;
            return new[] { new AddressablesPreloadRegistration(assetType, key, entry.Mode) };
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

        private void AddRange(ICollection<AddressablesPreloadRegistration> registrations, IReadOnlyList<AddressablesPreloadRegistration> additions)
        {
            for (int i = 0; i < additions.Count; i++) { registrations.Add(additions[i]); }
        }

        private Exception CreateUnsupportedReferenceTypeException(AddressablesPreloadConfigEntry entry, int index)
        {
            return new InvalidOperationException($"Invalid preload config entry at index {index}. Unsupported reference type '{entry.ReferenceType}'.");
        }

        private string ResolveReferenceKey(AssetReference reference)
        {
            GuardReference(reference);
            string key = reference.RuntimeKey?.ToString();
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
            return key;
        }

        private void GuardPreloadEntry(AddressablesPreloadConfigEntry entry, int index)
        {
            if (entry == null) { throw new InvalidOperationException($"Invalid preload config entry at index {index}. Entry is null."); }
            Type assetType = entry.AssetType?.Type;
            if (assetType == null) { throw new InvalidOperationException($"Invalid preload config entry at index {index}. AssetType is missing or unresolved."); }
            if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType)) { throw new InvalidOperationException($"Invalid preload config entry at index {index}. AssetType '{assetType.FullName}' must inherit UnityEngine.Object."); }
            if (entry.ReferenceType == PreloadReferenceType.AssetReference && entry.AssetReference == null) { throw new InvalidOperationException($"Invalid preload config entry at index {index}. AssetReference is missing."); }
            if (entry.ReferenceType == PreloadReferenceType.LabelReference && (entry.LabelReference == null || string.IsNullOrWhiteSpace(entry.LabelReference.labelString))) { throw new InvalidOperationException($"Invalid preload config entry at index {index}. LabelReference is missing labelString."); }
        }

        private void GuardReference(AssetReference reference)
        {
            if (reference == null) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
            if (reference.RuntimeKey == null) { throw new ArgumentException("Asset reference is not valid.", nameof(reference)); }
        }
    }
}
