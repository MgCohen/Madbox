using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using UnityEngine;

namespace Madbox.Addressables
{
    internal sealed class ComponentAssetHandle<TComponent> : IAssetHandle<TComponent> where TComponent : Component
    {
        public ComponentAssetHandle(IAssetHandle<GameObject> gameObjectHandle, TComponent component)
        {
            if (gameObjectHandle == null)
            {
                throw new ArgumentNullException(nameof(gameObjectHandle));
            }

            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            this.gameObjectHandle = gameObjectHandle;
            this.component = component;
        }

        public Type AssetType => typeof(TComponent);
        public UnityEngine.Object UntypedAsset => component;
        public TComponent Asset => component;
        public bool IsReleased => gameObjectHandle.IsReleased;
        public AssetHandleState State => gameObjectHandle.State;
        public bool IsReady => gameObjectHandle.IsReady;
        public Task WhenReady => gameObjectHandle.WhenReady;

        private readonly IAssetHandle<GameObject> gameObjectHandle;
        private readonly TComponent component;

        public void Release()
        {
            gameObjectHandle.Release();
        }
    }

    internal sealed class ComponentGroupHandle<TComponent> : IAssetGroupHandle<TComponent> where TComponent : Component
    {
        public ComponentGroupHandle(IAssetGroupHandle<GameObject> gameObjectGroup, IReadOnlyList<TComponent> components)
        {
            if (gameObjectGroup == null)
            {
                throw new ArgumentNullException(nameof(gameObjectGroup));
            }

            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            this.gameObjectGroup = gameObjectGroup;
            this.components = components;
        }

        public bool IsReleased => gameObjectGroup.IsReleased;
        public bool IsReady => gameObjectGroup.IsReady;
        public Task WhenReady => gameObjectGroup.WhenReady;
        public IReadOnlyList<TComponent> Assets => components;

        private readonly IAssetGroupHandle<GameObject> gameObjectGroup;
        private readonly IReadOnlyList<TComponent> components;

        public void Release()
        {
            gameObjectGroup.Release();
        }

        public void Dispose()
        {
            gameObjectGroup.Dispose();
        }
    }
}
