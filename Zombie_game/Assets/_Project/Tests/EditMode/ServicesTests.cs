using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class ServicesTests
    {
        private interface IFoo { }
        private sealed class Foo : IFoo { }

        [SetUp]
        public void SetUp() => Services.Clear();

        [TearDown]
        public void TearDown() => Services.Clear();

        [Test]
        public void Register_ThenGet_ReturnsSameInstance()
        {
            var foo = new Foo();
            Services.Register<IFoo>(foo);
            Assert.AreSame(foo, Services.Get<IFoo>());
        }

        [Test]
        public void Get_Missing_ThrowsWithHelpfulMessage()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Services.Get<IFoo>());
            StringAssert.Contains("IFoo", ex.Message);
        }

        [Test]
        public void Register_Twice_Throws()
        {
            Services.Register<IFoo>(new Foo());
            Assert.Throws<InvalidOperationException>(() => Services.Register<IFoo>(new Foo()));
        }

        [Test]
        public void Unregister_OnlyRemovesSameInstance()
        {
            var registered = new Foo();
            Services.Register<IFoo>(registered);

            Services.Unregister<IFoo>(new Foo());
            Assert.IsTrue(Services.IsRegistered<IFoo>());

            Services.Unregister<IFoo>(registered);
            Assert.IsFalse(Services.TryGet<IFoo>(out _));
        }
    }
}
