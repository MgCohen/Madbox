using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Madbox.SceneFlow.Tests
{
    internal sealed class FakeAddressablesSceneOperations : IAddressablesSceneOperations
    {
        public int LoadCallCount { get; private set; }

        public int UnloadCallCount { get; private set; }

        /// <summary>
        /// Last handle returned from <see cref="LoadSceneAsync"/> (for tests that release it before unload).
        /// </summary>
        public AsyncOperationHandle<SceneInstance> LastLoadHandle { get; private set; }

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(
            AssetReference sceneReference,
            LoadSceneMode loadSceneMode,
            bool activateOnLoad = true,
            int priority = 100)
        {
            LoadCallCount++;
            SceneInstance sceneInstance = default;
            LastLoadHandle = Addressables.ResourceManager.CreateCompletedOperation(sceneInstance, string.Empty);
            return LastLoadHandle;
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(
            AsyncOperationHandle<SceneInstance> sceneLoadHandle,
            bool autoReleaseHandle = true)
        {
            UnloadCallCount++;
            SceneInstance sceneInstance = default;
            return Addressables.ResourceManager.CreateCompletedOperation(sceneInstance, string.Empty);
        }
    }
}
