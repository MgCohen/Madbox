using System;
using System.Reflection;
using Madbox.Addressables.Container;
using Madbox.Addressables.Contracts;
using Madbox.Scope;
using VContainer;

namespace Madbox.App.Bootstrap
{
    internal sealed class BootstrapAssetInstaller : LayerInstallerBase
    {
        private static readonly MethodInfo registerInstanceMethod =
            typeof(BootstrapAssetInstaller).GetMethod(nameof(RegisterInstanceGeneric), BindingFlags.NonPublic | BindingFlags.Static);

        protected override void Install(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddressablesInstaller installer = new AddressablesInstaller();
            installer.Install(builder);
        }

        protected override void ConfigureChildBuilder(LayerInstallerBase child, IObjectResolver parentResolver, IContainerBuilder childBuilder)
        {
            if (parentResolver == null || childBuilder == null)
            {
                return;
            }

            IPreloadedAssetProvider preloadedAssetProvider;
            try
            {
                preloadedAssetProvider = parentResolver.Resolve<IPreloadedAssetProvider>();
            }
            catch (VContainerException)
            {
                return;
            }

            var preloadedAssets = preloadedAssetProvider.GetPreloadedAssets();
            foreach (var pair in preloadedAssets)
            {
                if (pair.Key == null || pair.Value == null)
                {
                    continue;
                }

                RegisterUntypedInstance(childBuilder, pair.Key, pair.Value);
            }
        }

        private static void RegisterUntypedInstance(IContainerBuilder builder, Type serviceType, UnityEngine.Object instance)
        {
            if (!serviceType.IsInstanceOfType(instance))
            {
                throw new InvalidOperationException($"Preloaded asset instance type '{instance.GetType().FullName}' is not assignable to '{serviceType.FullName}'.");
            }

            MethodInfo closed = registerInstanceMethod.MakeGenericMethod(serviceType);
            closed.Invoke(null, new object[] { builder, instance });
        }

        private static void RegisterInstanceGeneric<TService>(IContainerBuilder builder, UnityEngine.Object instance)
            where TService : class
        {
            builder.RegisterInstance(instance as TService);
        }
    }
}
