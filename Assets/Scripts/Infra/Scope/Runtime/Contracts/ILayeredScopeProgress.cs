namespace Madbox.Scope.Contracts
{
    /// <summary>
    /// Optional listener for layered scope build progress. Invoked once per <see cref="LayerInstallerBase"/>
    /// node in depth-first pre-order, after that node's <c>OnCompletedAsync</c> and before child installers run.
    /// </summary>
    public interface ILayeredScopeProgress
    {
        /// <param name="completedLayerIndex">1-based index through <paramref name="totalLayers"/>.</param>
        /// <param name="totalLayers">Total <see cref="LayerInstallerBase"/> nodes in the built tree.</param>
        void OnLayerPipelineStep(int completedLayerIndex, int totalLayers);
    }
}
