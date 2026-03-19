using Madbox.Addressables.Contracts;
using VContainer;
using VContainer.Unity;

namespace Madbox.Addressables.Container
{
    public sealed class AddressablesInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<AddressablesGateway>(Lifetime.Scoped).AsImplementedInterfaces();
        }
    }
}
