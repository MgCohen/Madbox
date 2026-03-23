using Madbox.Addressables.Container;
using Madbox.Addressables.Contracts;
using NUnit.Framework;
using VContainer;

namespace Madbox.Addressables.Tests
{
    public sealed class AddressablesInstallerTests
    {
        [Test]
        public void Gateway_IsSingleton_ResolvesSameInstanceFromNestedScopes()
        {
            ContainerBuilder builder = new ContainerBuilder();
            new AddressablesInstaller().Install(builder);
            IObjectResolver root = builder.Build();

            IScopedObjectResolver nested = root.CreateScope();
            IScopedObjectResolver deeper = nested.CreateScope();

            IAddressablesGateway fromRoot = root.Resolve<IAddressablesGateway>();
            IAddressablesGateway fromNested = nested.Resolve<IAddressablesGateway>();
            IAddressablesGateway fromDeeper = deeper.Resolve<IAddressablesGateway>();

            Assert.AreSame(fromRoot, fromNested);
            Assert.AreSame(fromRoot, fromDeeper);
        }
    }
}
