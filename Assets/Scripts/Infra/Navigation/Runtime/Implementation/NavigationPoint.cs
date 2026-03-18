using Madbox.Addressables.Contracts;
using Scaffold.Navigation.Contracts;
using UnityEngine;

namespace Scaffold.Navigation
{
    public class NavigationPoint
    {
        public NavigationPoint(IView view, IViewController controller, ViewConfig config, bool isSceneView, NavigationOptions options, IAssetHandle assetHandle = null)
        {
            if (view is null) { throw new System.ArgumentNullException(nameof(view)); }
            if (controller is null) { throw new System.ArgumentNullException(nameof(controller)); }
            if (config is null) { throw new System.ArgumentNullException(nameof(config)); }
            if (options is null) { throw new System.ArgumentNullException(nameof(options)); }
            View = view;
            ViewModel = controller;
            Config = config;
            IsSceneView = isSceneView;
            Options = options;
            this.assetHandle = assetHandle;
        }

        public IView View { get; private set; }
        public IViewController ViewModel { get; private set; }
        public ViewConfig Config { get; private set; }
        public bool IsSceneView { get; private set; }
        public int Depth { get; private set; }
        public NavigationOptions Options { get; private set; }
        public bool Disposed { get; private set; }

        private IAssetHandle assetHandle;

        public void SetDepth(int depth, NavigationOptions options)
        {
            if (options is null) { throw new System.ArgumentNullException(nameof(options)); }
            Depth = depth;
            View.Order(depth);
            if (options.RenderOverride.HasValue)
            {
                ApplyRenderOverride(options.RenderOverride.Value);
            }
        }

        public void Dispose()
        {
            if (Disposed) { return; }
            ReleaseAssetHandle();
            View = null;
            ViewModel = null;
            Config = null;
            Disposed = true;
        }

        private void ReleaseAssetHandle()
        {
            if (assetHandle == null) { return; }
            assetHandle.Release();
            assetHandle = null;
        }

        private void ApplyRenderOverride(RenderMode renderMode)
        {
            Canvas canvas = View.gameObject.GetComponentInParent<Canvas>(true);
            if (canvas == null) { return; }
            canvas.renderMode = renderMode;
        }
    }
}
