using System;
using Madbox.Addressables.Container;
using Madbox.Scope.Contracts;
using VContainer;

namespace Madbox.App.Bootstrap
{
    internal sealed class BootstrapAssetInstaller : ILayerInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            AddressablesInstaller installer = new AddressablesInstaller();
            installer.Install(builder);
        }
    }
}

