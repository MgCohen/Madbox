using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Addressables.Contracts;
using Madbox.App.Bootstrap.Player;
using Madbox.App.GameView.Player;
using Madbox.Entity;
using Madbox.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Madbox.App.Bootstrap.Tests
{
    public sealed class PlayerFactoryTests
    {
        [Test]
        public void CreateReadyPlayerAsync_ReturnsPlayerWithWeaponsWired()
        {
            EntityAttribute moveSpeed = ScriptableObject.CreateInstance<EntityAttribute>();
            moveSpeed.name = "MoveSpeed";
            GameObject playerPrefabRoot = BuildPlayerPrefabWithSockets(3, moveSpeed, 10f);
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
            SetPrivateWeaponModifiers(
                loadout,
                new WeaponModifierSetBuilder().Add(moveSpeed, 1.5f).Build(),
                new WeaponModifierSetBuilder().Build(),
                new WeaponModifierSetBuilder().Build());

            var fake = new FakeAddressablesGateway();
            fake.Register(new AssetReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), playerPrefabRoot);
            fake.Register(new AssetReference("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"), weaponPrefab0);
            fake.Register(new AssetReference("cccccccccccccccccccccccccccccccc"), weaponPrefab1);
            fake.Register(new AssetReference("dddddddddddddddddddddddddddddddd"), weaponPrefab2);

            var playerService = new PlayerService(loadout);
            var factory = new PlayerFactory(playerService, fake);

            Task<PlayerData> task = factory.CreateReadyPlayerAsync(null, Vector3.zero, Quaternion.identity);
            PlayerData player = task.GetAwaiter().GetResult();

            Assert.IsNotNull(player);
            WeaponVisualController visual = player.GetComponentInChildren<WeaponVisualController>(true);
            Assert.IsNotNull(visual);
            Assert.AreEqual(0, visual.SelectedWeaponIndex);
            Assert.That(player.GetFloatAttribute(moveSpeed), Is.EqualTo(11.5f).Within(0.0001f));
            UnityEngine.Object.DestroyImmediate(player.gameObject);
            UnityEngine.Object.DestroyImmediate(playerPrefabRoot);
            UnityEngine.Object.DestroyImmediate(weaponPrefab0);
            UnityEngine.Object.DestroyImmediate(weaponPrefab1);
            UnityEngine.Object.DestroyImmediate(weaponPrefab2);
            UnityEngine.Object.DestroyImmediate(moveSpeed);
        }

        [Test]
        public void PlayerService_NullLoadout_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerService(null));
        }

        private static GameObject BuildPlayerPrefabWithSockets(int socketCount, EntityAttribute baseAttribute, float baseValue)
        {
            GameObject root = new GameObject("playerPrefab");
            PlayerData data = root.AddComponent<PlayerData>();
            SetBaseAttributeEntry(data, baseAttribute, baseValue);
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

        private static void SetPrivateWeaponModifiers(PlayerLoadoutDefinition loadout, params WeaponModifierSet[] modifiers)
        {
            var list = new List<WeaponModifierSet>(modifiers);
            typeof(PlayerLoadoutDefinition).GetField("weaponModifiers", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(loadout, list);
        }

        private static void SetBaseAttributeEntry(PlayerData playerData, EntityAttribute attribute, float baseValue)
        {
            SerializedObject dataSo = new SerializedObject(playerData);
            SerializedProperty list = dataSo.FindProperty("attributeEntries");
            list.arraySize = 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attribute;
            entry.FindPropertyRelative("baseValue").floatValue = baseValue;
            dataSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class WeaponModifierSetBuilder
        {
            private readonly List<EntityAttributeModifierEntry> entries = new List<EntityAttributeModifierEntry>();

            public WeaponModifierSetBuilder Add(EntityAttribute attribute, float delta)
            {
                entries.Add(new EntityAttributeModifierEntry(attribute, delta));
                return this;
            }

            public WeaponModifierSet Build()
            {
                WeaponModifierSet set = new WeaponModifierSet();
                typeof(WeaponModifierSet).GetField("modifiers", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.SetValue(set, new List<EntityAttributeModifierEntry>(entries));
                return set;
            }
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
