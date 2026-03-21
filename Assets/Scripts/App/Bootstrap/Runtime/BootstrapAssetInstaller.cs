using Madbox.Addressables.Container;
using Madbox.Addressables.Contracts;
using Madbox.Bootstrap;
using Madbox.Scope;
using Scaffold.Navigation;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VContainer;
using static UnityEditor.ObjectChangeEventStream;

namespace Madbox.App.Bootstrap
{
    internal sealed class BootstrapAssetInstaller : LayerInstallerBase
    {
        protected override void Install(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddressablesInstaller installer = new AddressablesInstaller();
            installer.Install(builder);

            RegisterProvider<NavigationAssetProvider>(builder);
        }

        private void RegisterProvider<T>(IContainerBuilder builder) where T: IAssetProvider
        {
            builder.Register<T>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        }

        protected override async Task OnCompletedAsync(IObjectResolver resolver, CancellationToken cancellationToken)
        {
            IEnumerable<IAssetProvider> resolvedProviders = resolver.Resolve<IEnumerable<IAssetProvider>>();
            foreach (IAssetProvider provider in resolvedProviders)
            {
                await provider.PreloadAsync(cancellationToken);
            }
        }

        protected override void ConfigureChildBuilder(LayerInstallerBase child, IObjectResolver resolver, IContainerBuilder childBuilder)
        {
            if (childBuilder == null)
            {
                return;
            }
            var registrars = resolver.Resolve<IEnumerable<IAssetRegistrar>>();
            foreach (var registrar in registrars)
            {
                registrar.Register(childBuilder);
            }
        }
    }
}
