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
            IAssetHandle<GameObject>[] handleSlot = { addressables.Load<GameObject>(config.Asset) };
            NavigationPoint point = CreatePendingAssetPoint(controller, config, options, handleSlot);
            _ = MaterializePointAsync(point, () => handleSlot[0], () => handleSlot[0] = null);
            return point;
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

        private NavigationPoint CreatePendingAssetPoint(IViewController controller, ViewConfig config, NavigationOptions options, IAssetHandle<GameObject>[] handleSlot)
        {
            return new NavigationPoint(controller, config, false, options, disposedPoint => ReleaseAndBuffer(disposedPoint, handleSlot));
        }

        private void ReleaseAndBuffer(NavigationPoint disposedPoint, IAssetHandle<GameObject>[] handleSlot)
        {
            ReleaseHandle(handleSlot);
            ReturnToBuffer(disposedPoint);
        }

        private async Task MaterializePointAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle, Action clearHandle)
        {
            await MaterializePointCoreAsync(point, getHandle, clearHandle);
        }

        private async Task MaterializePointCoreAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle, Action clearHandle)
        {
            Exception exception = await TryCompletePointIfReadyAsync(point, getHandle);
            if (exception != null) { point?.FailReady(exception); }
            ReleaseAndClear(getHandle, clearHandle);
        }

        private async Task<Exception> TryCompletePointIfReadyAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle)
        {
            try { await CompletePointIfReadyAsync(point, getHandle); return null; }
            catch (Exception exception) { return exception; }
        }

        private async Task CompletePointIfReadyAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle)
        {
            IAssetHandle<GameObject> handle = ValidatePointAndHandle(point, getHandle);
            if (handle == null) { return; }
            await handle.WhenReady;
            handle = ValidatePointAndHandle(point, getHandle);
            if (handle == null) { return; }
            CompletePointWithInstance(point, handle);
        }

        private IAssetHandle<GameObject> ValidatePointAndHandle(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle)
        {
            IAssetHandle<GameObject> handle = getHandle();
            return IsPointAndHandleUsable(point, handle) ? handle : null;
        }

        private bool IsPointAndHandleUsable(NavigationPoint point, IAssetHandle<GameObject> handle)
        {
            return point != null && !point.Disposed && handle != null;
        }

        private void CompletePointWithInstance(NavigationPoint point, IAssetHandle<GameObject> handle)
        {
            GameObject instance = CreateInstance(handle);
            IView view = ResolveView(instance);
            point.CompleteReady(view);
        }

        private void ReleaseAndClear(Func<IAssetHandle<GameObject>> getHandle, Action clearHandle)
        {
            IAssetHandle<GameObject> handle = getHandle();
            if (handle == null) { return; }
            handle.Release();
            clearHandle();
        }

        private void ReleaseHandle(IAssetHandle<GameObject>[] handleSlot)
        {
            if (handleSlot[0] == null) { return; }
            handleSlot[0].Release();
            handleSlot[0] = null;
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
