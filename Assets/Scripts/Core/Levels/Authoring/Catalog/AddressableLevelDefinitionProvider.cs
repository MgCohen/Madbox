using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.Levels;
using Madbox.Levels.Authoring.Definitions;
#pragma warning disable SCA0006
#pragma warning disable SCA0007
#pragma warning disable SCA0017

namespace Madbox.Levels.Authoring.Catalog
{
    public sealed class AddressableLevelDefinitionProvider
    {
        public AddressableLevelDefinitionProvider(LevelCatalogSO catalog, IAddressablesGateway gateway)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        private readonly LevelCatalogSO catalog;
        private readonly IAddressablesGateway gateway;

        public async Task<IAssetHandle<LevelDefinitionSO>> LoadAsync(LevelId levelId, CancellationToken cancellationToken = default)
        {
            if (levelId == null)
            {
                throw new ArgumentNullException(nameof(levelId));
            }

            if (catalog.TryGetLevelReference(levelId.Value, out UnityEngine.AddressableAssets.AssetReferenceT<LevelDefinitionSO> levelReference))
            {
                return await gateway.LoadAsync(levelReference, cancellationToken);
            }

            throw new KeyNotFoundException($"No addressable level reference found for level id '{levelId.Value}'.");
        }
    }
}

