using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Scaffold.Navigation.Contracts;
using UnityEngine;

namespace Scaffold.Navigation
{
    internal class NavigationProvider
    {
        public NavigationProvider(NavigationSettings settings, Transform viewHolder, IAddressablesGateway addressables)
        {
            GuardConstructor(settings, viewHolder, addressables);
            this.settings = settings;
            this.viewHolder = viewHolder;
            this.addressables = addressables;
            instanceBuffer = new NavigationViewInstanceBuffer(viewHolder);
            FetchContextViews();
        }

        private readonly Transform viewHolder;
        private readonly NavigationSettings settings;
        private readonly IAddressablesGateway addressables;
        private readonly NavigationViewInstanceBuffer instanceBuffer;
        private readonly Dictionary<Type, IView> contextViews = new Dictionary<Type, IView>();

        public NavigationPoint GetNavigationPoint<TController>(TController controller, NavigationOptions options) where TController : IViewController
        {
            GuardController(controller);
            ViewConfig config = settings.GetViewConfig(typeof(TController));
            NavigationOptions resolved = ValidateNavigationOptions(options, config);
            return GetNavigationPoint(config, controller, resolved);
        }

        private NavigationPoint GetNavigationPoint(ViewConfig config, IViewController controller, NavigationOptions options)
        {
            if (TryGetContextView(config.ViewType, out IView contextView))
            {
                return new NavigationPoint(contextView, controller, config, true, options);
            }
            return CreateAssetPoint(config, controller, options);
        }

        private NavigationPoint CreateAssetPoint(ViewConfig config, IViewController controller, NavigationOptions options)
        {
            if (instanceBuffer.TryTake(config, out IView cachedView))
            {
                return new NavigationPoint(cachedView, controller, config, false, options, ReturnToBuffer);
            }
            IAssetHandle<GameObject> handle = addressables.Load<GameObject>(config.Asset);
            NavigationPoint point = new NavigationPoint(controller, config, false, options, disposedPoint =>
            {
                if (handle != null)
                {
                    handle.Release();
                    handle = null;
                }
                ReturnToBuffer(disposedPoint);
            });
            _ = MaterializePointAsync(point, () => handle, () => handle = null);
            return point;
        }

        private async Task MaterializePointAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle, Action clearHandle)
        {
            try
            {
                IAssetHandle<GameObject> handle = getHandle();
                if (point == null || point.Disposed || handle == null) { return; }
                await handle.WhenReady;
                handle = getHandle();
                if (point.Disposed || handle == null) { return; }
                GameObject instance = CreateInstance(handle);
                IView view = ResolveView(instance);
                point.CompleteReady(view);
            }
            catch (Exception exception)
            {
                point?.FailReady(exception);
            }
            finally
            {
                IAssetHandle<GameObject> handle = getHandle();
                if (handle != null)
                {
                    handle.Release();
                    clearHandle();
                }
            }
        }


        private GameObject CreateInstance(IAssetHandle<GameObject> prefabHandle)
        {
            return GameObject.Instantiate(prefabHandle.Asset, viewHolder);
        }

        private IView ResolveView(GameObject instance)
        {
            IView view = instance.GetComponent<IView>();
            if (view == null) { return ThrowMissingView(instance); }
            instance.SetActive(false);
            return view;
        }

        private IView ThrowMissingView(GameObject instance)
        {
            UnityEngine.Object.Destroy(instance);
            throw new InvalidOperationException($"Addressable view '{instance.name}' does not implement {nameof(IView)}.");
        }

        private void ReturnToBuffer(NavigationPoint point)
        {
            if (point == null || point.IsSceneView || point.View == null) { return; }
            instanceBuffer.Return(point.Config, point.View);
        }

        private void FetchContextViews()
        {
            IView[] views = viewHolder.GetComponentsInChildren<IView>(true);
            foreach (IView view in views)
            {
                contextViews[view.GetType()] = view;
                view.gameObject.SetActive(false);
            }
        }

        private NavigationOptions ValidateNavigationOptions(NavigationOptions options, ViewConfig config)
        {
            if (options != null) { return options; }
            return ResolveDefaultOptions(config);
        }

        private NavigationOptions ResolveDefaultOptions(ViewConfig config)
        {
            if (config.TryGetSchema<NavigationOptionsSchema>(out NavigationOptionsSchema schema))
            {
                return schema.Options;
            }
            return new NavigationOptions();
        }

        private bool TryGetContextView(Type screenType, out IView screen)
        {
            if (screenType != null && contextViews.TryGetValue(screenType, out IView screenInstance))
            {
                screen = screenInstance;
                return true;
            }
            screen = null;
            return false;
        }

        private void GuardController(IViewController controller)
        {
            if (controller == null) { throw new ArgumentNullException(nameof(controller)); }
        }

        private void GuardConstructor(NavigationSettings settings, Transform viewHolder, IAddressablesGateway addressables)
        {
            if (settings == null) { throw new ArgumentNullException(nameof(settings)); }
            if (viewHolder == null) { throw new ArgumentNullException(nameof(viewHolder)); }
            if (addressables == null) { throw new ArgumentNullException(nameof(addressables)); }
        }
    }
}
