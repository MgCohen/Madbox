using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.App.Bootstrap.Player;
using Madbox.App.GameView.Player;
using Madbox.Player;
using Madbox.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.App.Bootstrap.Tests
{
    public sealed class PlayerFactoryTests
    {
        [Test]
        public void CreateReadyPlayerAsync_ReturnsPlayerWithWeaponsWired()
        {
            GameObject playerPrefabRoot = BuildPlayerPrefabWithSockets(3);
            GameObject weaponPrefab0 = new GameObject("weaponPrefab0");
            GameObject weaponPrefab1 = new GameObject("weaponPrefab1");
            GameObject weaponPrefab2 = new GameObject("weaponPrefab2");

            PlayerLoadoutDefinition loadout = ScriptableObject.CreateInstance<PlayerLoadoutDefinition>();
            SetPrivateAssetReference(loadout, "playerPrefab", new AssetReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
            SetPrivateWeaponPrefabs(
                loadout,
                new AssetReference("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                new AssetReference("cccccccccccccccccccccccccccccccc"),
                new AssetReference("dddddddddddddddddddddddddddddddd"));

            var fake = new FakeAddressablesGateway();
            fake.Register(new AssetReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), playerPrefabRoot);
            fake.Register(new AssetReference("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"), weaponPrefab0);
            fake.Register(new AssetReference("cccccccccccccccccccccccccccccccc"), weaponPrefab1);
            fake.Register(new AssetReference("dddddddddddddddddddddddddddddddd"), weaponPrefab2);

            var playerService = new PlayerService(loadout);
            var factory = new PlayerFactory(playerService, fake);

            Task<Player> task = factory.CreateReadyPlayerAsync(null, Vector3.zero, Quaternion.identity);
            Player player = task.GetAwaiter().GetResult();

            Assert.IsNotNull(player);
            WeaponVisualController visual = player.GetComponentInChildren<WeaponVisualController>(true);
            Assert.IsNotNull(visual);
            Assert.AreEqual(0, visual.SelectedWeaponIndex);
            UnityEngine.Object.DestroyImmediate(player.gameObject);
            UnityEngine.Object.DestroyImmediate(playerPrefabRoot);
            UnityEngine.Object.DestroyImmediate(weaponPrefab0);
            UnityEngine.Object.DestroyImmediate(weaponPrefab1);
            UnityEngine.Object.DestroyImmediate(weaponPrefab2);
        }

        [Test]
        public void PlayerService_NullLoadout_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerService(null));
        }

        private static GameObject BuildPlayerPrefabWithSockets(int socketCount)
        {
            GameObject root = new GameObject("playerPrefab");
            root.AddComponent<Player>();
            WeaponVisualController visual = root.AddComponent<WeaponVisualController>();
            var sockets = new List<Transform>();
            for (int i = 0; i < socketCount; i++)
            {
                GameObject socketGo = new GameObject("socket" + i);
                socketGo.transform.SetParent(root.transform, false);
                sockets.Add(socketGo.transform);
            }

            typeof(WeaponVisualController).GetField("weaponSockets", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(visual, sockets);
            return root;
        }

        private static void SetPrivateAssetReference(object target, string fieldName, AssetReference value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(target, value);
        }

        private static void SetPrivateWeaponPrefabs(PlayerLoadoutDefinition loadout, params AssetReference[] refs)
        {
            var list = new List<AssetReference>(refs);
            typeof(PlayerLoadoutDefinition).GetField("weaponPrefabs", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(loadout, list);
        }

        private sealed class FakeAddressablesGateway : IAddressablesGateway
        {
            private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

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
                cancellationToken.ThrowIfCancellationRequested();
                if (typeof(T) != typeof(GameObject))
                {
                    throw new NotSupportedException();
                }

                string key = reference.RuntimeKey.ToString();
                if (!prefabs.TryGetValue(key, out GameObject prefab))
                {
                    throw new InvalidOperationException("Missing fake prefab for key '" + key + "'.");
                }

                IAssetHandle<T> handle = (IAssetHandle<T>)(object)new ImmediateHandle<GameObject>(prefab);
                return Task.FromResult(handle);
            }

            public Task<IAssetHandle<T>> LoadAsync<T>(AssetReferenceT<T> reference, CancellationToken cancellationToken = default) where T : UnityEngine.Object
            {
                return LoadAsync<T>((AssetReference)reference, cancellationToken);
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

            public void Register(AssetReference reference, GameObject prefab)
            {
                string key = reference.RuntimeKey.ToString();
                prefabs[key] = prefab;
            }
        }

        private sealed class ImmediateHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
        {
            public ImmediateHandle(T assetValue)
            {
                asset = assetValue;
            }

            private readonly T asset;
            private bool released;

            public Type AssetType => typeof(T);

            public UnityEngine.Object UntypedAsset => asset;

            public bool IsReleased => released;

            public AssetHandleState State => released ? AssetHandleState.Released : AssetHandleState.Ready;

            public bool IsReady => true;

            public Task WhenReady => Task.CompletedTask;

            public T Asset => asset;

            public void Release()
            {
                released = true;
            }
        }
    }
}
