using VContainer.Unity;
using VContainer;
using UnityEngine;
using Scaffold.Navigation.Contracts;
namespace Scaffold.Navigation.Container
{
    internal class NavigationInjection : INavigationOpenHandler
    {
        public NavigationInjection(IObjectResolver resolver)
        {
            this.resolver = resolver;
        }

        private IObjectResolver resolver;

        public void OnOpen(IViewController viewModel)
        {
            resolver.Inject(viewModel);
        }
    }
}


