using System.Collections.Generic;

namespace Scaffold.Addressables
{
    internal interface IAddressablesPreloadSource
    {
        IReadOnlyList<AddressablesPreloadRequest> Snapshot();
    }
}
