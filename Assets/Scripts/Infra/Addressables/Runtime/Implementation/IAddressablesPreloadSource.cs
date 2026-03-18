using System.Collections.Generic;

namespace Madbox.Addressables
{
    internal interface IAddressablesPreloadSource
    {
        IReadOnlyList<AddressablesPreloadRequest> Snapshot();
    }
}
