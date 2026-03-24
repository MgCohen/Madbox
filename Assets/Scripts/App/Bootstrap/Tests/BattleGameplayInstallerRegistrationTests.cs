using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.SceneFlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;

namespace Madbox.App.Bootstrap.Tests
{
    public sealed class BattleGameplayInstallerRegistrationTests
    {
        private sealed class FakeAddressablesGateway : IAddressablesGateway
        {
            public Task InitializeAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<IAssetGroupHandle<T>> LoadAsync<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public IAssetGroupHandle<T> Load<T>(AssetLabelReference label, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public IAssetHandle<T> Load<T>(AssetReference reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }

            public IAssetHandle<T> Load<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                throw new NotSupportedException();
            }
        }

        private sealed class FakeSceneFlowService : ISceneFlowService
        {
            public Task<SceneFlowLoadResult> LoadAdditiveAsync(AssetReference sceneReference, SceneFlowLoadOptions options, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task UnloadAsync(SceneFlowLoadResult result, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
