using Madbox.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Madbox.Players.Tests
{
    public sealed class HeroPrefabInputWiringTests
    {
        private const string HeroPrefabPath = "Assets/Prefabs/Heroes/Hero.prefab";

        [Test]
        public void HeroPrefab_PlayerBehaviorRunner_InputProviderAssignedAtRuntimeBySession()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
            Assert.IsNotNull(prefab, $"Missing prefab at {HeroPrefabPath}");

            PlayerBehaviorRunner runner = prefab.GetComponentInChildren<PlayerBehaviorRunner>(true);
            Assert.IsNotNull(runner, "Hero prefab must include PlayerBehaviorRunner (root or child).");

            SerializedObject so = new SerializedObject(runner);
            SerializedProperty prop = so.FindProperty("inputProviderBehaviour");
            Assert.IsNotNull(prop, "Serialized field inputProviderBehaviour must exist.");
            Assert.IsNull(
                prop.objectReferenceValue,
                "Hero must not serialize a GameView PlayerInputProvider; it is wired at runtime when the session spawns the player.");
        }
    }
}
