using System;
using System.Threading.Tasks;
using Scaffold.Navigation.Contracts;
using UnityEngine;

namespace Scaffold.Navigation
{
    public class NavigationPoint
    {
        public NavigationPoint(IView view, IViewController controller, ViewConfig config, bool isSceneView, NavigationOptions options, Action<NavigationPoint> disposeAction = null)
            : this(controller, config, isSceneView, options, disposeAction)
        {
            if (view is null) { throw new ArgumentNullException(nameof(view)); }
            View = view;
            readyTask = Task.CompletedTask;
        }

        internal NavigationPoint(IViewController controller, ViewConfig config, bool isSceneView, NavigationOptions options, Task<IView> viewLoadTask, Action<NavigationPoint> disposeAction = null)
            : this(controller, config, isSceneView, options, disposeAction)
        {
            readyTask = CreateReadyTask(viewLoadTask);
        }

        private NavigationPoint(IViewController controller, ViewConfig config, bool isSceneView, NavigationOptions options, Action<NavigationPoint> disposeAction)
        {
            if (controller is null) { throw new ArgumentNullException(nameof(controller)); }
            if (config is null) { throw new ArgumentNullException(nameof(config)); }
            if (options is null) { throw new ArgumentNullException(nameof(options)); }
            ViewModel = controller;
            Config = config;
            IsSceneView = isSceneView;
            Options = options;
            this.disposeAction = disposeAction;
        }

        public IView View { get; private set; }
        public IViewController ViewModel { get; private set; }
        public ViewConfig Config { get; private set; }
        public bool IsSceneView { get; private set; }
        public int Depth { get; private set; }
        public NavigationOptions Options { get; private set; }
        public bool Disposed { get; private set; }

        private readonly Action<NavigationPoint> disposeAction;
        private readonly Task readyTask;

        public void SetDepth(int depth, NavigationOptions options)
        {
            if (options is null) { throw new ArgumentNullException(nameof(options)); }
            Depth = depth;
            if (View == null) { return; }
            ApplyDepth(options);
        }

        internal Task EnsureReadyAsync()
        {
            return readyTask;
        }

        public void Dispose()
        {
            if (Disposed) { return; }
            disposeAction?.Invoke(this);
            View = null;
            ViewModel = null;
            Config = null;
            Disposed = true;
        }

        private Task CreateReadyTask(Task<IView> viewLoadTask)
        {
            if (viewLoadTask == null) { throw new ArgumentNullException(nameof(viewLoadTask)); }
            return ResolveViewAsync(viewLoadTask);
        }

        private async Task ResolveViewAsync(Task<IView> viewLoadTask)
        {
            IView loadedView = await viewLoadTask;
            if (loadedView == null) { throw new InvalidOperationException("Navigation view load returned null."); }
            View = loadedView;
            ApplyDepth(Options);
        }

        private void ApplyDepth(NavigationOptions options)
        {
            View.Order(Depth);
            if (options.RenderOverride.HasValue)
            {
                ApplyRenderOverride(options.RenderOverride.Value);
            }
        }

        private void ApplyRenderOverride(RenderMode renderMode)
        {
            Canvas canvas = View.gameObject.GetComponentInParent<Canvas>(true);
            if (canvas == null) { return; }
            canvas.renderMode = renderMode;
        }
    }
}
