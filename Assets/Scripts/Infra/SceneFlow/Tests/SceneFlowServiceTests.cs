using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.AddressableAssets;

namespace Madbox.SceneFlow.Tests
{
    public sealed class SceneFlowServiceTests
    {
        [Test]
        public void LoadAdditiveAsync_ThrowsArgumentNull_WhenSceneReferenceNull()
        {
            RunAsync(async () =>
            {
                FakeAddressablesSceneOperations operations = new FakeAddressablesSceneOperations();
                SceneFlowService service = new SceneFlowService(operations, new RecordingSceneFlowBootstrapShell());

                await ExpectExceptionAsync<ArgumentNullException>(async () =>
                    await service.LoadAdditiveAsync(null, SceneFlowLoadOptions.Default));
            });
        }

        [Test]
        public void LoadAdditiveAsync_WhenManageShell_FirstLoad_SetsShellActive()
        {
            RunAsync(async () =>
            {
                FakeAddressablesSceneOperations operations = new FakeAddressablesSceneOperations();
                RecordingSceneFlowBootstrapShell shell = new RecordingSceneFlowBootstrapShell();
                SceneFlowService service = new SceneFlowService(operations, shell);
                AssetReference reference = new AssetReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

                SceneFlowLoadResult result = await service.LoadAdditiveAsync(reference, new SceneFlowLoadOptions(true));

                Assert.AreEqual(1, shell.SetAdditiveContentActiveCallCount);
                Assert.IsTrue(shell.LastAdditiveContentActive);
                Assert.IsTrue(result.ManageBootstrapShell);
            });
        }

        [Test]
        public void UnloadAsync_SecondCall_ThrowsInvalidOperation()
        {
            RunAsync(async () =>
            {
                FakeAddressablesSceneOperations operations = new FakeAddressablesSceneOperations();
                SceneFlowService service = new SceneFlowService(operations, new RecordingSceneFlowBootstrapShell());
                AssetReference reference = new AssetReference("cccccccccccccccccccccccccccccccc");

                SceneFlowLoadResult loadResult = await service.LoadAdditiveAsync(reference, new SceneFlowLoadOptions(true));
                await service.UnloadAsync(loadResult);

                await ExpectExceptionAsync<InvalidOperationException>(async () =>
                    await service.UnloadAsync(loadResult));
            });
        }

        [Test]
        public void UnloadAsync_WhenLastShellManagedLoad_SetsShellInactive()
        {
            RunAsync(async () =>
            {
                FakeAddressablesSceneOperations operations = new FakeAddressablesSceneOperations();
                RecordingSceneFlowBootstrapShell shell = new RecordingSceneFlowBootstrapShell();
                SceneFlowService service = new SceneFlowService(operations, shell);
                AssetReference reference = new AssetReference("dddddddddddddddddddddddddddddddd");

                SceneFlowLoadResult loadResult = await service.LoadAdditiveAsync(reference, new SceneFlowLoadOptions(true));
                await service.UnloadAsync(loadResult);

                Assert.IsTrue(shell.SetAdditiveContentActiveCallCount >= 2);
                Assert.IsFalse(shell.LastAdditiveContentActive);
            });
        }

        [Test]
        public void LoadAdditiveAsync_WhenOperationsThrows_PropagatesException()
        {
            RunAsync(async () =>
            {
                ThrowingAddressablesSceneOperations operations = new ThrowingAddressablesSceneOperations();
                SceneFlowService service = new SceneFlowService(operations, new RecordingSceneFlowBootstrapShell());
                AssetReference reference = new AssetReference("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee");

                await ExpectExceptionAsync<InvalidOperationException>(async () =>
                    await service.LoadAdditiveAsync(reference, new SceneFlowLoadOptions(false)));
            });
        }

        private static void RunAsync(Func<Task> test)
        {
            test().GetAwaiter().GetResult();
        }

        private static async Task ExpectExceptionAsync<TException>(Func<Task> action)
            where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected {typeof(TException).Name}, but {ex.GetType().Name} was thrown: {ex.Message}");
            }

            Assert.Fail($"Expected {typeof(TException).Name}, but no exception was thrown.");
        }

        private sealed class ThrowingAddressablesSceneOperations : IAddressablesSceneOperations
        {
            public UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> LoadSceneAsync(
                AssetReference sceneReference,
                UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
                bool activateOnLoad = true,
                int priority = 100)
            {
                throw new InvalidOperationException("Simulated load failure.");
            }

            public UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> UnloadSceneAsync(
                UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> sceneLoadHandle,
                bool autoReleaseHandle = true)
            {
                throw new InvalidOperationException("Simulated unload failure.");
            }
        }
    }
}
