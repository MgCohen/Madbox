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
            void Handler(AnimationEventDefinition def) => calls++;

            host.Router.Register(host.Definition, Handler);
            host.Fire(host.EventId);

            Assert.AreEqual(1, calls);
            host.Router.Unregister(host.Definition, Handler);
        }

        [Test]
        public void OnCharacterAnimationEvent_WhenUnknownId_DoesNotThrowAndDoesNotInvoke()
        {
            using var host = new RouterTestHost();
            int calls = 0;
            void Handler(AnimationEventDefinition def) => calls++;

            host.Router.Register(host.Definition, Handler);
            host.Fire("unknown_event");

            Assert.AreEqual(0, calls);
            host.Router.Unregister(host.Definition, Handler);
        }

        [Test]
        public void OnCharacterAnimationEvent_WhenEmptyString_DoesNotInvoke()
        {
            using var host = new RouterTestHost();
            int calls = 0;
            host.Router.Register(host.Definition, _ => calls++);
            host.Fire(string.Empty);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Register_WhenTwoHandlers_BothReceiveMulticast()
        {
            using var host = new RouterTestHost();
            int a = 0;
            int b = 0;
            void Ha(AnimationEventDefinition def) => a++;
            void Hb(AnimationEventDefinition def) => b++;
            host.Router.Register(host.Definition, Ha);
            host.Router.Register(host.Definition, Hb);
            host.Fire(host.EventId);
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
                EventId = "test_attack_release";
                SetEventIdViaReflection(Definition, EventId);
            }

            public GameObject gameObject { get; }

            public CharacterAnimationEventRouter Router { get; }

            public AnimationEventDefinition Definition { get; }

            public string EventId { get; }

            public void Fire(string eventId)
            {
                Router.OnCharacterAnimationEvent(eventId);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                UnityEngine.Object.DestroyImmediate(Definition);
            }

            private static void SetEventIdViaReflection(AnimationEventDefinition def, string id)
            {
                FieldInfo field = typeof(AnimationEventDefinition).GetField("eventId", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(field);
                field.SetValue(def, id);
            }
        }
    }
}
