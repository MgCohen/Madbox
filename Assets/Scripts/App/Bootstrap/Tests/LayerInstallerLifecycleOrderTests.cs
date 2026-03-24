using System.Threading;
using System.Threading.Tasks;
using Madbox.Scope;
using NUnit.Framework;
using VContainer;

namespace Madbox.App.Bootstrap.Tests
{
    public sealed class LayerInstallerLifecycleOrderTests
    {
        [Test]
        public void BuildAsync_ParentCompletedRunsBeforeBuildChildren()
        {
            PipelineProbeInstaller installer = new PipelineProbeInstaller();
            installer.Run();
            Assert.IsTrue(installer.WasCompletedWhenBuildingChildren);
        }

        private sealed class PipelineProbeInstaller : LayerInstallerBase
        {
            private bool completed;

            public bool WasCompletedWhenBuildingChildren { get; private set; }

            public void Run()
            {
                BuildAsync(CancellationToken.None).GetAwaiter().GetResult();
            }

            protected override void Install(IContainerBuilder builder)
            {
            }

            protected override Task OnCompletedAsync(IObjectResolver resolver, CancellationToken cancellationToken)
            {
                completed = true;
                return Task.CompletedTask;
            }

            protected override Task BuildChildrenAsync(CancellationToken cancellationToken)
            {
                WasCompletedWhenBuildingChildren = completed;
                return Task.CompletedTask;
            }
        }
    }
}
