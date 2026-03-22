using System;
using System.Reflection;
using Madbox.App.GameView.Animation;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.App.GameView.Tests
{
    public sealed class CharacterAnimationEventRouterTests
    {
        [Test]
        public void OnCharacterAnimationEvent_WhenRegistered_InvokesHandlerOnce()
        {
            using var host = new RouterTestHost();
            int calls = 0;
            void Handler(CharacterAnimationEventContext ctx) => calls++;

            host.Router.Register(host.Definition, Handler);
            host.Fire(1001);

            Assert.AreEqual(1, calls);
            host.Router.Unregister(host.Definition, Handler);
        }

        [Test]
        public void OnCharacterAnimationEvent_WhenUnknownId_DoesNotThrowAndDoesNotInvoke()
        {
            using var host = new RouterTestHost();
            int calls = 0;
            void Handler(CharacterAnimationEventContext ctx) => calls++;

            host.Router.Register(host.Definition, Handler);
            host.Fire(9999);

            Assert.AreEqual(0, calls);
            host.Router.Unregister(host.Definition, Handler);
        }

        [Test]
        public void OnCharacterAnimationEvent_WhenIntParameterZero_DoesNotInvoke()
        {
            using var host = new RouterTestHost();
            int calls = 0;
            host.Router.Register(host.Definition, _ => calls++);
            host.Fire(0);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Register_WhenTwoHandlers_BothReceiveMulticast()
        {
            using var host = new RouterTestHost();
            int a = 0;
            int b = 0;
            void Ha(CharacterAnimationEventContext ctx) => a++;
            void Hb(CharacterAnimationEventContext ctx) => b++;
            host.Router.Register(host.Definition, Ha);
            host.Router.Register(host.Definition, Hb);
            host.Fire(1001);
            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
            host.Router.Unregister(host.Definition, Ha);
            host.Router.Unregister(host.Definition, Hb);
        }

        private sealed class RouterTestHost : IDisposable
        {
            public RouterTestHost()
            {
                GameObject root = new GameObject("RouterTest");
                gameObject = root;
                root.AddComponent<Animator>();
                Router = root.AddComponent<CharacterAnimationEventRouter>();
                Definition = ScriptableObject.CreateInstance<AnimationEventDefinition>();
                SetStableIdViaReflection(Definition, 1001);
            }

            public GameObject gameObject { get; }

            public CharacterAnimationEventRouter Router { get; }

            public AnimationEventDefinition Definition { get; }

            public void Fire(int intParameter)
            {
                AnimationEvent evt = new AnimationEvent { intParameter = intParameter };
                Router.OnCharacterAnimationEvent(evt);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                UnityEngine.Object.DestroyImmediate(Definition);
            }

            private static void SetStableIdViaReflection(AnimationEventDefinition def, int id)
            {
                FieldInfo field = typeof(AnimationEventDefinition).GetField("stableId", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(field);
                field.SetValue(def, id);
            }
        }
    }
}
