using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
#pragma warning disable SCA0006
#pragma warning disable SCA0007
#pragma warning disable SCA0012
#pragma warning disable SCA0020

using Madbox.Levels.Authoring.Definitions;

namespace Madbox.Levels.Authoring.Catalog
{
    [CreateAssetMenu(menuName = "Madbox/Authoring/Level Catalog")]
    public sealed class LevelCatalogSO : ScriptableObject
    {
        [SerializeField] private List<LevelCatalogEntry> levels = new List<LevelCatalogEntry>();

        public bool TryGetLevelReference(string levelId, out AssetReferenceT<LevelDefinitionSO> levelReference)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                levelReference = null;
                return false;
            }

            if (levels == null || levels.Count == 0)
            {
                levelReference = null;
                return false;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                LevelCatalogEntry entry = levels[i];
                if (entry != null && string.Equals(entry.LevelId, levelId, StringComparison.Ordinal))
                {
                    levelReference = entry.LevelReference;
                    return levelReference != null;
                }
            }

            levelReference = null;
            return false;
        }
    }

    [Serializable]
    public sealed class LevelCatalogEntry
    {
        [SerializeField] private string levelId;
        [SerializeField] private AssetReferenceT<LevelDefinitionSO> levelReference;

        public string LevelId => levelId;

        public AssetReferenceT<LevelDefinitionSO> LevelReference => levelReference;
    }
}
