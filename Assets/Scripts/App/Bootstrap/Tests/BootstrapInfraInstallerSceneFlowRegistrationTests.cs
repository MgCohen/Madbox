using System.Reflection;
using Madbox.SceneFlow;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace Madbox.App.Bootstrap.Tests
{
    public sealed class BootstrapInfraInstallerSceneFlowRegistrationTests
    {
        [Test]
        public void BootstrapInfraInstaller_Install_RegistersSceneFlowService()
        {
            ContainerBuilder builder = new ContainerBuilder();
            Transform viewHolder = new GameObject("ViewHolder").transform;
            var infra = new BootstrapInfraInstaller(viewHolder, new FakeSceneFlowBootstrapShell());

            MethodInfo install = typeof(BootstrapInfraInstaller).GetMethod(
                "Install",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(IContainerBuilder) },
                null);
            Assert.IsNotNull(install);
            install.Invoke(infra, new object[] { builder });

            using (IObjectResolver container = builder.Build())
            {
                ISceneFlowService service = container.Resolve<ISceneFlowService>();
                Assert.IsNotNull(service);
            }
        }

        private sealed class FakeSceneFlowBootstrapShell : ISceneFlowBootstrapShell
        {
            public void SetAdditiveContentActive(bool active)
            {
            }
        }
    }
}
