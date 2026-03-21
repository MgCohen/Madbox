using System;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Scaffold.Navigation.Contracts;
using UnityEngine;

namespace Scaffold.Navigation
{
    internal sealed class AddressablesNavigationPointStrategy : INavigationPointStrategy
    {
        public AddressablesNavigationPointStrategy(IAddressablesGateway addressables, Transform viewHolder, NavigationViewInstanceBuffer instanceBuffer)
        {
            this.addressables = addressables;
            this.viewHolder = viewHolder;
            this.instanceBuffer = instanceBuffer;
        }

        private readonly IAddressablesGateway addressables;
        private readonly Transform viewHolder;
        private readonly NavigationViewInstanceBuffer instanceBuffer;

        public bool TryCreate(ViewConfig config, IViewController controller, NavigationOptions options, out NavigationPoint point)
        {
            IAssetHandle<GameObject>[] handleSlot = { addressables.Load<GameObject>(config.Asset) };
            point = new NavigationPoint(controller, config, false, options, disposed => { if (handleSlot[0] != null) handleSlot[0].Release(); handleSlot[0] = null; if (disposed == null || disposed.IsSceneView || disposed.View == null) return; instanceBuffer.Return(disposed.Config, disposed.View); });
            _ = MaterializePointAsync(point, () => handleSlot[0], () => handleSlot[0] = null);
            return true;
        }

        private async Task MaterializePointAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle, Action clearHandle)
        {
            try
            {
                await CompletePointAsync(point, getHandle);
            }
            catch (Exception exception)
            {
                point?.FailReady(exception);
            }

            IAssetHandle<GameObject> handle = getHandle();
            if (handle == null) return;
            handle.Release();
            clearHandle();
        }

        private async Task CompletePointAsync(NavigationPoint point, Func<IAssetHandle<GameObject>> getHandle)
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
    }
}
