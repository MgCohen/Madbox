using System;
using NUnit.Framework;
using Scaffold.Events.Contracts;

namespace Scaffold.Events.Tests
{
    public class EventsTests
    {
        [Test]
        public void AddListener_WithSubscriber_CallsSubscriberOnRaise()
        {
            EventController bus = new EventController();
            bool called = false;
            bus.AddListener<TestEvent>(_ => called = true);
            BuildRaiseTestEvent(bus);
            Assert.IsTrue(called);
        }

        [Test]
        public void AddListener_OpenTypeWithSubscriber_CallsSubscriberOnRaise()
        {
            EventController bus = new EventController();
            bool called = false;
            Action<ContextEvent> handler = _ => called = true;
            bus.AddListener(typeof(TestEvent), handler);
            BuildRaiseTestEvent(bus);
            Assert.IsTrue(called);
        }

        [Test]
        public void RemoveListener_AfterSubscribing_StopsReceivingEvents()
        {
            EventController bus = new EventController();
            int callCount = 0;
            Action<TestEvent> handler = _ => callCount++;
            bus.AddListener(handler);
            bus.RemoveListener(handler);
            BuildRaiseTestEvent(bus);
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void RemoveListener_OpenTypeAfterSubscribing_StopsReceivingEvents()
        {
            EventController bus = new EventController();
            int callCount = 0;
            Action<ContextEvent> handler = _ => callCount++;
            bus.AddListener(typeof(TestEvent), handler);
            bus.RemoveListener(typeof(TestEvent), handler);
            BuildRaiseTestEvent(bus);
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void AddListener_GenericDuplicate_IsIdempotent()
        {
            EventController bus = new EventController();
            int callCount = 0;
            Action<TestEvent> handler = _ => callCount++;
            bus.AddListener(handler);
            bus.AddListener(handler);
            BuildRaiseTestEvent(bus);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void RemoveListener_GenericDuplicate_IsIdempotent()
        {
            EventController bus = new EventController();
            int callCount = 0;
            Action<TestEvent> handler = _ => callCount++;
            bus.AddListener(handler);
            bus.RemoveListener(handler);
            bus.RemoveListener(handler);
            BuildRaiseTestEvent(bus);
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void SingleAddRaiseRemove_GenericFlow_CallsOnceAcrossLifecycle()
        {
            EventController bus = new EventController();
            int callCount = 0;
            Action<TestEvent> handler = _ => callCount++;
            bus.AddListener(handler);
            BuildRaiseTestEvent(bus);
            bus.RemoveListener(handler);
            BuildRaiseTestEvent(bus);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void SingleAddRaiseRemove_OpenTypeFlow_CallsOnceAcrossLifecycle()
        {
            EventController bus = new EventController();
            int callCount = 0;
            Action<ContextEvent> handler = _ => callCount++;
            bus.AddListener(typeof(TestEvent), handler);
            BuildRaiseTestEvent(bus);
            bus.RemoveListener(typeof(TestEvent), handler);
            BuildRaiseTestEvent(bus);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Clear_WithListeners_AllListenersAreRemoved()
        {
            EventController bus = new EventController();
            bool called = false;
            bus.AddListener<TestEvent>(_ => called = true);
            bus.Clear();
            BuildRaiseTestEvent(bus);
            Assert.IsFalse(called);
        }

        [Test]
        public void Raise_WithNullEvent_ThrowsArgumentNullException()
        {
            EventController bus = new EventController();
            TestDelegate action = () => bus.Raise(null);
            Assert.Throws<ArgumentNullException>(action);
        }

        [Test]
        public void IEventBus_GenericAddRaiseRemove_FollowsExpectedContract()
        {
            IEventBus bus = new EventController();
            int calls = 0;
            Action<TestEvent> handler = _ => calls++;
            BuildExecuteIEventBusLifecycle(bus, handler);
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void AddListener_OpenTypeWithNullHandler_ThrowsArgumentNullException()
        {
            EventController bus = new EventController();
            TestDelegate action = () => bus.AddListener(typeof(TestEvent), null);
            Assert.Throws<ArgumentNullException>(action);
        }

        private static void BuildRaiseTestEvent(EventController bus)
        {
            TestEvent evt = new TestEvent();
            bus.Raise(evt);
        }

        private static void BuildExecuteIEventBusLifecycle(IEventBus bus, Action<TestEvent> handler)
        {
            bus.AddListener(handler);
            TestEvent first = new TestEvent();
            bus.Raise(first);
            bus.RemoveListener(handler);
            TestEvent second = new TestEvent();
            bus.Raise(second);
        }

        private record TestEvent : ContextEvent;
    }
}


