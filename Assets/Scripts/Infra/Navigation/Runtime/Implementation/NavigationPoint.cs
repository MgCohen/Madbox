using UnityEngine;
using Scaffold.Types;
using Scaffold.Events.Contracts;
using Scaffold.Events;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using Scaffold.Navigation.Contracts;
namespace Scaffold.Navigation
{
    public class NavigationPoint
    {
        public NavigationPoint(IView view, IViewController controller, ViewConfig config, bool isSceneView, NavigationOptions options)
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
        }

        public IView View { get; private set; }
        public IViewController ViewModel { get; private set; }
        public ViewConfig Config { get; private set; }
        public bool IsSceneView { get; private set; }
        public int Depth { get; private set; }
        public NavigationOptions Options { get; private set; }

        public bool Disposed { get; private set; }

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

        private void ApplyRenderOverride(RenderMode renderMode)
        {
            var canvas = View.gameObject.GetComponentInParent<Canvas>(true);
            if (canvas != null)
            {
                canvas.renderMode = renderMode;
            }
        }

        public void Dispose()
        {
            if (Disposed) { return; }
            View = null;
            ViewModel = null;
            Config = null;
            Disposed = true;
        }
    }
}




