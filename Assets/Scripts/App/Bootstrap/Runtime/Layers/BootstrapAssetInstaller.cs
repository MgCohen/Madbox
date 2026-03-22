using Madbox.Addressables.Container;
using Madbox.Addressables.Contracts;
using Madbox.Bootstrap;
using Madbox.Scope;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VContainer;

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
            RegisterProvider<LevelAssetProvider>(builder);
        }

        private void RegisterProvider<T>(IContainerBuilder builder) where T: IAssetPreloader
        {
            builder.Register<T>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        }

        protected override async Task OnCompletedAsync(IObjectResolver resolver, CancellationToken cancellationToken)
        {
            IEnumerable<IAssetPreloader> preloaders = resolver.Resolve<IEnumerable<IAssetPreloader>>();
            foreach (IAssetPreloader preloader in preloaders)
            {
                await preloader.PreloadAsync(cancellationToken);
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
