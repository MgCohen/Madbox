using Madbox.App.GameView.Arenas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Madbox.App.GameView.Tests
{
    public sealed class ArenaTests
    {
        [Test]
        public void TryGetWorldBounds_WithoutCollider_ReturnsFalse()
        {
            GameObject go = new GameObject("arena");
            var arena = go.AddComponent<Arena>();

            bool ok = arena.TryGetWorldBounds(out Bounds bounds);

            Assert.IsFalse(ok);
            Assert.AreEqual(default(Bounds), bounds);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryGetWorldBounds_WithBoxCollider_ReturnsColliderBounds()
        {
            GameObject go = new GameObject("arena");
            var collider = go.AddComponent<BoxCollider>();
            collider.center = new Vector3(1f, 2f, 3f);
            collider.size = new Vector3(4f, 6f, 8f);
            var arena = go.AddComponent<Arena>();

            bool ok = arena.TryGetWorldBounds(out Bounds bounds);

            Assert.IsTrue(ok);
            Assert.That(bounds.center.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(bounds.size.x, Is.EqualTo(4f).Within(0.001f));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void EnemySpawnWorldPosition_UsesSpawnTransformWhenAssigned()
        {
            GameObject arenaGo = new GameObject("arena");
            var arena = arenaGo.AddComponent<Arena>();
            GameObject spawn = new GameObject("enemySpawn");
            spawn.transform.position = new Vector3(10f, 0f, -5f);
            SerializedObject arenaSo = new SerializedObject(arena);
            arenaSo.FindProperty("enemySpawnPoint").objectReferenceValue = spawn.transform;
            arenaSo.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(arena.EnemySpawnWorldPosition, Is.EqualTo(spawn.transform.position));

            Object.DestroyImmediate(spawn);
            Object.DestroyImmediate(arenaGo);
        }

        [Test]
        public void TryFindInScene_FindsArenaUnderRoot()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                GameObject root = new GameObject("Root");
                GameObject child = new GameObject("Nested");
                child.transform.SetParent(root.transform);
                Arena arena = child.AddComponent<Arena>();

                Assert.AreEqual(scene, root.scene);
                bool found = Arena.TryFindInScene(scene, out Arena resolved);
                Assert.IsTrue(found);
                Assert.AreSame(arena, resolved);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }
        }

        [Test]
        public void TryFindInScene_InvalidScene_ReturnsFalse()
        {
            Scene invalid = default;
            bool found = Arena.TryFindInScene(invalid, out Arena arena);
            Assert.IsFalse(found);
            Assert.IsNull(arena);
        }
    }
}
