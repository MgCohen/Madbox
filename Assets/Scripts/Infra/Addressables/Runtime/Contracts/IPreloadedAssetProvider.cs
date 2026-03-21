using System;
using System.Collections.Generic;

namespace Madbox.Addressables.Contracts
{
    public interface IPreloadedAssetProvider
    {
        IReadOnlyDictionary<Type, UnityEngine.Object> GetPreloadedAssets();
    }
}
