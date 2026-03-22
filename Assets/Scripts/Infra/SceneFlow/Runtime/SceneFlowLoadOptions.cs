namespace Madbox.SceneFlow
{
    /// <summary>
    /// Options for loading an additive Addressable scene.
    /// </summary>
    public readonly struct SceneFlowLoadOptions
    {
        public SceneFlowLoadOptions(bool manageBootstrapShell)
        {
            ManageBootstrapShell = manageBootstrapShell;
        }

        /// <summary>
        /// When true, participates in Bootstrap shell suppression (camera/listener) while this load remains active.
        /// </summary>
        public bool ManageBootstrapShell { get; }

        public static SceneFlowLoadOptions Default => new SceneFlowLoadOptions(true);
    }
}
