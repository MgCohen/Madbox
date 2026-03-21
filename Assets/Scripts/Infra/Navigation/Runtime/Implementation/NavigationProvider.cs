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
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            ViewConfig config = settings.GetViewConfig(typeof(TController));
            NavigationOptions resolved = options;
            if (resolved == null && config.TryGetSchema<NavigationOptionsSchema>(out NavigationOptionsSchema schema)) resolved = schema.Options;
            resolved ??= new NavigationOptions();
            if (config.ViewType != null && contextViews.TryGetValue(config.ViewType, out IView contextView)) return new NavigationPoint(contextView, controller, config, true, resolved);
            return CreateAssetPoint(config, controller, resolved);
        }

        private NavigationPoint CreateAssetPoint(ViewConfig config, IViewController controller, NavigationOptions options)
        {
            if (instanceBuffer.TryTake(config, out IView cachedView)) return new NavigationPoint(cachedView, controller, config, false, options, point => { if (point == null || point.IsSceneView || point.View == null) return; instanceBuffer.Return(point.Config, point.View); });
            IAssetHandle<GameObject>[] handleSlot = { addressables.Load<GameObject>(config.Asset) };
            NavigationPoint point = new NavigationPoint(controller, config, false, options, disposedPoint => { if (handleSlot[0] != null) handleSlot[0].Release(); handleSlot[0] = null; if (disposedPoint == null || disposedPoint.IsSceneView || disposedPoint.View == null) return; instanceBuffer.Return(disposedPoint.Config, disposedPoint.View); });
            _ = MaterializePointCoreAsync(point, () => handleSlot[0], () => handleSlot[0] = null);
            return point;
        }

        private async Task MaterializePointCoreAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle, Action clearHandle)
        {
            try { await CompletePointIfReadyAsync(point, getHandle); }
            catch (Exception exception) { point?.FailReady(exception); }
            IAssetHandle<GameObject> handle = getHandle();
            if (handle == null) return;
            handle.Release();
            clearHandle();
        }

        private async Task CompletePointIfReadyAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle)
        {
            IAssetHandle<GameObject> handle = getHandle();
            if (point == null || point.Disposed || handle == null) return;
            await handle.WhenReady;
            handle = getHandle();
            if (point == null || point.Disposed || handle == null) return;
            CompletePointWithInstance(point, handle);
        }

        private void CompletePointWithInstance(NavigationPoint point, IAssetHandle<GameObject> handle)
        {
            GameObject instance = GameObject.Instantiate(handle.Asset, viewHolder);
            IView view = instance.GetComponent<IView>();
            if (view == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException($"Addressable view '{instance.name}' does not implement {nameof(IView)}.");
            }
            instance.SetActive(false);
            point.CompleteReady(view);
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

        private void GuardConstructor(NavigationSettings settings, Transform viewHolder, IAddressablesGateway addressables)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (viewHolder == null) throw new ArgumentNullException(nameof(viewHolder));
            if (addressables == null) throw new ArgumentNullException(nameof(addressables));
        }
    }
}


