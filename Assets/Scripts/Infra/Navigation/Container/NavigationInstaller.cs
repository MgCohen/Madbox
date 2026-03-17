using VContainer.Unity;
using VContainer;
using UnityEngine;
using Scaffold.Navigation.Contracts;
namespace Scaffold.Navigation.Container
{
    public class NavigationInstaller : IInstaller
    {
        public NavigationInstaller(NavigationSettings settings, Transform holder)
        {
            this.settings = settings;
            this.holder = holder;
        }
        
        private NavigationSettings settings;
        private Transform holder;

        public void Install(IContainerBuilder builder)
        {
            builder.Register<INavigation, NavigationController>(Lifetime.Scoped).WithParameter<NavigationSettings>(settings).WithParameter<Transform>(holder);
            builder.Register<NavigationInjection>(Lifetime.Scoped).AsImplementedInterfaces();
        }
    }
}



