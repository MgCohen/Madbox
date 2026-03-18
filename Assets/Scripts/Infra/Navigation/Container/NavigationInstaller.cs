using System;
using Madbox.Addressables.Contracts;
using Scaffold.Navigation.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Scaffold.Navigation.Container
{
    public class NavigationInstaller : IInstaller
    {
        public NavigationInstaller(NavigationSettings settings, Transform holder)
        {
            this.settings = settings;
            this.holder = holder;
        }

        private readonly NavigationSettings settings;
        private readonly Transform holder;

        public void Install(IContainerBuilder builder)
        {
            builder.Register<INavigation, NavigationController>(Lifetime.Scoped)
                .WithParameter<NavigationSettings>(settings)
                .WithParameter<Transform>(holder);
            builder.Register<NavigationInjection>(Lifetime.Scoped).AsImplementedInterfaces();
            builder.RegisterBuildCallback(RegisterNavigationPreloads);
        }

        private void RegisterNavigationPreloads(IObjectResolver resolver)
        {
            IAddressablesPreloadRegistry preloads = resolver.Resolve<IAddressablesPreloadRegistry>();
            foreach (ViewConfig config in settings.Screens)
            {
                RegisterViewPreload(preloads, config);
            }
        }

        private void RegisterViewPreload(IAddressablesPreloadRegistry preloads, ViewConfig config)
        {
            if (config == null || config.Asset == null) { return; }
            object runtimeKey = config.Asset.RuntimeKey;
            if (runtimeKey == null) { return; }
            string keyValue = runtimeKey.ToString();
            if (string.IsNullOrWhiteSpace(keyValue)) { return; }
            preloads.Register<GameObject>(config.Asset, PreloadMode.NeverDie);
        }
    }
}
