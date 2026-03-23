using System;
using Madbox.App.Animation;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.App.Animation.Tests
{
    public sealed class AnimationEventRouterTests
    {
        [Test]
        public void OnCharacterAnimationEvent_WhenRegistered_InvokesHandlerOnce()
        {
            using var host = new RouterTestHost();
            int calls = 0;
            void Handler(AnimationEventContext ctx) => calls++;

            host.Router.Register(host.Definition, Handler);
            host.Fire(host.Definition);

            Assert.AreEqual(1, calls);
            host.Router.Unregister(host.Definition, Handler);
        }

        [Test]
        public void OnCharacterAnimationEvent_WhenUnregisteredDefinition_DoesNotInvoke()
        {
            using var host = new RouterTestHost();
            var other = ScriptableObject.CreateInstance<AnimationEventDefinition>();
            other.name = "other_event";
            try
            {
                int calls = 0;
                void Handler(AnimationEventContext ctx) => calls++;

                host.Router.Register(host.Definition, Handler);
                host.Fire(other);

                Assert.AreEqual(0, calls);
                host.Router.Unregister(host.Definition, Handler);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void OnCharacterAnimationEvent_WhenNullDefinition_DoesNotInvoke()
        {
            using var host = new RouterTestHost();
            int calls = 0;
            host.Router.Register(host.Definition, _ => calls++);
            host.Router.OnCharacterAnimationEvent((AnimationEventDefinition)null);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Register_WhenTwoHandlers_BothReceiveMulticast()
        {
            using var host = new RouterTestHost();
            int a = 0;
            int b = 0;
            void Ha(AnimationEventContext ctx) => a++;
            void Hb(AnimationEventContext ctx) => b++;
            host.Router.Register(host.Definition, Ha);
            host.Router.Register(host.Definition, Hb);
            host.Fire(host.Definition);
            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
            host.Router.Unregister(host.Definition, Ha);
            host.Router.Unregister(host.Definition, Hb);
        }

        [Test]
        public void OnCharacterAnimationEvent_ContextContainsAnimatorAndDefinition()
        {
            using var host = new RouterTestHost();
            Animator seenAnimator = null;
            AnimationEventDefinition seenDefinition = null;

            void Handler(AnimationEventContext ctx)
            {
                seenAnimator = ctx.Animator;
                seenDefinition = ctx.Definition;
            }

            host.Router.Register(host.Definition, Handler);
            host.Fire(host.Definition);

            Assert.AreSame(host.Animator, seenAnimator);
            Assert.AreSame(host.Definition, seenDefinition);

            host.Router.Unregister(host.Definition, Handler);
        }

        [Test]
        public void OnCharacterAnimationEvent_ObjectOverload_ForwardsToDefinitionOverload()
        {
            using var host = new RouterTestHost();
            int calls = 0;
            void Handler(AnimationEventContext ctx) => calls++;

            host.Router.Register(host.Definition, Handler);
            host.Router.OnCharacterAnimationEvent(host.Definition);

            Assert.AreEqual(1, calls);
            host.Router.Unregister(host.Definition, Handler);
        }

        private sealed class RouterTestHost : IDisposable
        {
            public RouterTestHost()
            {
                GameObject root = new GameObject("RouterTest");
                gameObject = root;
                Animator = root.AddComponent<Animator>();
                Router = root.AddComponent<AnimationEventRouter>();
                Definition = ScriptableObject.CreateInstance<AnimationEventDefinition>();
                EventId = "test_attack_release";
                Definition.name = EventId;
            }

            public GameObject gameObject { get; }

            public Animator Animator { get; }

            public AnimationEventRouter Router { get; }

            public AnimationEventDefinition Definition { get; }

            public string EventId { get; }

            public void Fire(AnimationEventDefinition definition)
            {
                Router.OnCharacterAnimationEvent(definition);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                UnityEngine.Object.DestroyImmediate(Definition);
            }
        }
    }
}
