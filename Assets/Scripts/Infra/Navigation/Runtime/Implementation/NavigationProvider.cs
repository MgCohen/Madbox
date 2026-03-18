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
            if (TryCreateBufferedPoint(config, controller, options, out NavigationPoint bufferedPoint)) { return bufferedPoint; }
            IAssetHandle<GameObject> handle = addressables.Load<GameObject>(config.Asset);
            Action<NavigationPoint> onDispose = CreateAddressableDisposeAction(() => handle, () => handle = null);
            NavigationPoint point = new NavigationPoint(controller, config, false, options, onDispose);
            _ = MaterializePointAsync(point, () => handle, () => handle = null);
            return point;
        }

        private Task MaterializePointAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle, Action clearHandle)
        {
            return MaterializePointSafelyAsync(point, getHandle, clearHandle);
        }

        private async Task MaterializePointSafelyAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle, Action clearHandle)
        {
            try { await MaterializePointCoreAsync(point, getHandle); }
            catch (Exception exception) { point?.FailReady(exception); }
            finally { ReleaseHandle(getHandle, clearHandle); }
        }

        private Action<NavigationPoint> CreateAddressableDisposeAction(Func<IAssetHandle<GameObject>> getHandle, Action clearHandle)
        {
            return (disposedPoint) =>
            {
                ReleaseHandle(getHandle, clearHandle);
                ReturnToBuffer(disposedPoint);
            };
        }

        private bool TryCreateBufferedPoint(ViewConfig config, IViewController controller, NavigationOptions options, out NavigationPoint point)
        {
            if (instanceBuffer.TryTake(config, out IView cachedView))
            {
                point = new NavigationPoint(cachedView, controller, config, false, options, ReturnToBuffer);
                return true;
            }

            point = null;
            return false;
        }

        private void ReturnToBuffer(NavigationPoint point)
        {
            if (point == null || point.IsSceneView || point.View == null) { return; }
            instanceBuffer.Return(point.Config, point.View);
        }

        private async Task MaterializePointCoreAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle)
        {
            IAssetHandle<GameObject> handle = getHandle();
            if (!CanMaterialize(point, handle)) { return; }
            await WaitForReadyAsync(handle);
            CompletePointMaterialization(point, getHandle);
        }

        private bool CanMaterialize(NavigationPoint point, IAssetHandle<GameObject> handle)
        {
            return point != null && !point.Disposed && handle != null;
        }

        private async Task WaitForReadyAsync(IAssetHandle<GameObject> handle)
        {
            await handle.WhenReady;
        }

        private void CompletePointMaterialization(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle)
        {
            IAssetHandle<GameObject> readyHandle = getHandle();
            if (!CanMaterialize(point, readyHandle)) { return; }
            GameObject instance = CreateInstance(readyHandle);
            IView view = ResolveView(instance);
            point.CompleteReady(view);
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

        private void ReleaseHandle(Func<IAssetHandle<GameObject>> getHandle, Action clearHandle)
        {
            IAssetHandle<GameObject> handle = getHandle();
            if (handle == null) { return; }
            handle.Release();
            clearHandle();
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
