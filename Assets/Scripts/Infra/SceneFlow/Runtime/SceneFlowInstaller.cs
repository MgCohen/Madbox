using VContainer;
using VContainer.Unity;

namespace Madbox.SceneFlow
{
    /// <summary>
    /// Registers additive scene flow services. Optional <see cref="ISceneFlowBootstrapShell"/> (e.g. <c>SceneFlowBootstrapShell</c> in the scene) suppresses Bootstrap camera/listener while additive content is active.
    /// </summary>
    public sealed class SceneFlowInstaller : IInstaller
    {
        private readonly ISceneFlowBootstrapShell bootstrapShell;

        public SceneFlowInstaller(ISceneFlowBootstrapShell bootstrapShell = null)
        {
            this.bootstrapShell = bootstrapShell;
        }

        public void Install(IContainerBuilder builder)
        {
            ISceneFlowBootstrapShell shell = bootstrapShell ?? new NullSceneFlowBootstrapShell();
            builder.RegisterInstance(shell).As<ISceneFlowBootstrapShell>();
            builder.Register<IAddressablesSceneOperations, AddressablesSceneOperations>(Lifetime.Scoped);
            builder.Register<ISceneFlowService, SceneFlowService>(Lifetime.Scoped);
        }
    }
}
