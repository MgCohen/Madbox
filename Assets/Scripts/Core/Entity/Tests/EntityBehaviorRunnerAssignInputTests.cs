using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.Entities.Tests
{
    public sealed class EntityBehaviorRunnerAssignInputTests
    {
        private sealed class TestRunner : EntityBehaviorRunner<Entity, int>
        {
        }

        private sealed class MockIntProvider : MonoBehaviour, IEntityFrameInputProvider<int>
        {
            public int Next;

            public int GetFrameInput()
            {
                return Next;
            }
        }

        [Test]
        public void AssignInputProvider_WiresFrameInput()
        {
            GameObject go = new GameObject("runner");
            go.AddComponent<Entity>();
            TestRunner runner = go.AddComponent<TestRunner>();
            GameObject mockGo = new GameObject("mock");
            MockIntProvider mock = mockGo.AddComponent<MockIntProvider>();
            mock.Next = 77;

            runner.AssignInputProvider(mock);

            FieldInfo field = typeof(EntityBehaviorRunner<Entity, int>).GetField(
                "inputProvider",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            var resolved = field.GetValue(runner) as IEntityFrameInputProvider<int>;
            Assert.IsNotNull(resolved);
            Assert.That(resolved.GetFrameInput(), Is.EqualTo(77));

            UnityEngine.Object.DestroyImmediate(mockGo);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void AssignInputProvider_Null_Throws()
        {
            GameObject go = new GameObject("runner");
            go.AddComponent<Entity>();
            TestRunner runner = go.AddComponent<TestRunner>();

            Assert.Throws<System.ArgumentNullException>(() => runner.AssignInputProvider(null));

            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
