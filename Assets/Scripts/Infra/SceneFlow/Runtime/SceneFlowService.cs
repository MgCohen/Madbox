using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Madbox.SceneFlow
{
    public sealed class SceneFlowService : ISceneFlowService
    {
        private readonly IAddressablesSceneOperations addressablesOperations;
        private readonly ISceneFlowBootstrapShell bootstrapShell;
        private readonly Dictionary<Guid, SceneFlowLoadRecord> activeLoads = new Dictionary<Guid, SceneFlowLoadRecord>();
        private int shellManagedLoadCount;

        public SceneFlowService(
            IAddressablesSceneOperations addressablesOperations,
            ISceneFlowBootstrapShell bootstrapShell = null)
        {
            this.addressablesOperations = addressablesOperations ?? throw new ArgumentNullException(nameof(addressablesOperations));
            this.bootstrapShell = bootstrapShell;
        }

        public async Task<SceneFlowLoadResult> LoadAdditiveAsync(
            AssetReference sceneReference,
            SceneFlowLoadOptions options,
            CancellationToken cancellationToken = default)
        {
            if (sceneReference == null)
            {
                throw new ArgumentNullException(nameof(sceneReference));
            }

            bool manageShell = options.ManageBootstrapShell;
            bool shellStateCommitted = false;
            if (manageShell)
            {
                shellManagedLoadCount++;
                if (shellManagedLoadCount == 1)
                {
                    // Disable Bootstrap camera/listener/EventSystem before the additive scene activates so only one
                    // EventSystem runs (Unity logs when multiple EventSystems are enabled).
                    bootstrapShell?.SetAdditiveContentActive(true);
                }
            }

            AsyncOperationHandle<SceneInstance> handle = default;
            try
            {
                handle = addressablesOperations.LoadSceneAsync(sceneReference, LoadSceneMode.Additive, true, 100);
                await handle.Task;
                cancellationToken.ThrowIfCancellationRequested();

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new InvalidOperationException($"Addressables scene load failed with status {handle.Status}.");
                }

                Guid loadId = Guid.NewGuid();
                string sceneName = handle.Result.Scene.name;
                activeLoads[loadId] = new SceneFlowLoadRecord(handle, manageShell);
                shellStateCommitted = true;

                return new SceneFlowLoadResult(loadId, sceneName, manageShell);
            }
            catch (OperationCanceledException)
            {
                RollbackShellReservationIfNeeded(manageShell, shellStateCommitted);
                ReleaseHandleIfNeeded(handle);
                throw;
            }
            catch
            {
                RollbackShellReservationIfNeeded(manageShell, shellStateCommitted);
                ReleaseHandleIfNeeded(handle);
                throw;
            }
        }

        private void RollbackShellReservationIfNeeded(bool manageShell, bool shellStateCommitted)
        {
            if (!manageShell || shellStateCommitted)
            {
                return;
            }

            shellManagedLoadCount--;
            if (shellManagedLoadCount == 0)
            {
                bootstrapShell?.SetAdditiveContentActive(false);
            }
        }

        public async Task UnloadAsync(SceneFlowLoadResult result, CancellationToken cancellationToken = default)
        {
            if (!activeLoads.TryGetValue(result.LoadId, out SceneFlowLoadRecord record))
            {
                throw new InvalidOperationException("Unknown scene flow load id; unload may have already completed.");
            }

            // Scene may have been released elsewhere; avoid UnloadSceneAsync with a dead handle.
            if (!record.SceneLoadHandle.IsValid())
            {
                RemoveActiveLoadRecord(result.LoadId, record);
                return;
            }

            AsyncOperationHandle<SceneInstance> unloadHandle = addressablesOperations.UnloadSceneAsync(record.SceneLoadHandle, true);
            await unloadHandle.Task;
            cancellationToken.ThrowIfCancellationRequested();

            // After a successful await, Unity may invalidate the unload handle; reading .Status then throws.
            if (unloadHandle.IsValid() && unloadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException($"Addressables scene unload failed with status {unloadHandle.Status}.");
            }

            RemoveActiveLoadRecord(result.LoadId, record);
        }

        private void RemoveActiveLoadRecord(Guid loadId, SceneFlowLoadRecord record)
        {
            activeLoads.Remove(loadId);

            if (record.ManageBootstrapShell)
            {
                shellManagedLoadCount--;
                if (shellManagedLoadCount == 0)
                {
                    bootstrapShell?.SetAdditiveContentActive(false);
                }
            }
        }

        private static void ReleaseHandleIfNeeded(AsyncOperationHandle<SceneInstance> handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
}
