using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Game.Core.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    public class EventBusTests
    {
        private struct TestEvent : IGameEvent
        {
            public int Value;
            public TestEvent(int value) { Value = value; }
        }

        [SetUp]
        public void SetUp() => EventBus<TestEvent>.Clear();

        [TearDown]
        public void TearDown() => EventBus<TestEvent>.Clear();

        [Test]
        public void Raise_DeliversEventToSubscriber()
        {
            int received = 0;
            EventBus<TestEvent>.Subscribe(e => received = e.Value);

            EventBus<TestEvent>.Raise(new TestEvent(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int calls = 0;
            Action<TestEvent> handler = _ => calls++;
            EventBus<TestEvent>.Subscribe(handler);
            EventBus<TestEvent>.Unsubscribe(handler);

            EventBus<TestEvent>.Raise(new TestEvent(1));

            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, EventBus<TestEvent>.HandlerCount);
        }

        [Test]
        public void Subscribe_SameHandlerTwice_DeliversOnce()
        {
            int calls = 0;
            Action<TestEvent> handler = _ => calls++;
            EventBus<TestEvent>.Subscribe(handler);
            EventBus<TestEvent>.Subscribe(handler);

            EventBus<TestEvent>.Raise(new TestEvent(1));

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void UnsubscribeDuringRaise_SkipsRemovedHandler_AndKeepsOthers()
        {
            var log = new List<string>();
            Action<TestEvent> second = _ => log.Add("second");
            Action<TestEvent> first = _ =>
            {
                log.Add("first");
                EventBus<TestEvent>.Unsubscribe(second);
            };
            Action<TestEvent> third = _ => log.Add("third");
            EventBus<TestEvent>.Subscribe(first);
            EventBus<TestEvent>.Subscribe(second);
            EventBus<TestEvent>.Subscribe(third);

            EventBus<TestEvent>.Raise(new TestEvent(1));

            CollectionAssert.AreEqual(new[] { "first", "third" }, log);
            Assert.AreEqual(2, EventBus<TestEvent>.HandlerCount);
        }

        [Test]
        public void SubscribeDuringRaise_IsCalledOnNextRaiseOnly()
        {
            int lateCalls = 0;
            Action<TestEvent> late = _ => lateCalls++;
            bool added = false;
            EventBus<TestEvent>.Subscribe(_ =>
            {
                if (added) return;
                added = true;
                EventBus<TestEvent>.Subscribe(late);
            });

            EventBus<TestEvent>.Raise(new TestEvent(1));
            Assert.AreEqual(0, lateCalls);

            EventBus<TestEvent>.Raise(new TestEvent(2));
            Assert.AreEqual(1, lateCalls);
        }

        [Test]
        public void ThrowingHandler_IsLogged_AndOthersStillRun()
        {
            bool otherCalled = false;
            EventBus<TestEvent>.Subscribe(_ => throw new InvalidOperationException("boom"));
            EventBus<TestEvent>.Subscribe(_ => otherCalled = true);

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            EventBus<TestEvent>.Raise(new TestEvent(1));

            Assert.IsTrue(otherCalled);
        }

        [Test]
        public void RegistryClearAll_RemovesHandlers()
        {
            EventBus<TestEvent>.Subscribe(_ => { });

            EventBusRegistry.ClearAll();

            Assert.AreEqual(0, EventBus<TestEvent>.HandlerCount);
        }
    }
}
