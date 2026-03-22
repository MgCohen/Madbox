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
            if (bootstrapShell != null)
            {
                builder.RegisterInstance(bootstrapShell).As<ISceneFlowBootstrapShell>();
            }

            builder.Register<IAddressablesSceneOperations, AddressablesSceneOperations>(Lifetime.Scoped);
            builder.Register<ISceneFlowService>(resolver =>
            {
                IAddressablesSceneOperations operations = resolver.Resolve<IAddressablesSceneOperations>();
                return new SceneFlowService(operations, bootstrapShell);
            }, Lifetime.Scoped);
        }
    }
}
