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
        }
    }
}

