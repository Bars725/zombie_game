using System.Collections.Generic;
using System.Linq;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class GameRandomTests
    {
        [Test]
        public void KnownSeed_ProducesGoldenSequence()
        {
            // Golden values guard against accidental algorithm changes, which would
            // silently change every seeded run. Update only on a deliberate change.
            var rng = new GameRandom(12345);

            Assert.AreEqual(1206779164u, rng.NextUInt());
            Assert.AreEqual(886047776u, rng.NextUInt());
            Assert.AreEqual(951604652u, rng.NextUInt());
        }

        [Test]
        public void Fork_KnownSeedAndName_ProducesGoldenSequence()
        {
            var waves = new GameRandom(12345).Fork("waves");

            Assert.AreEqual(250177301u, waves.NextUInt());
            Assert.AreEqual(1486704392u, waves.NextUInt());
        }

        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new GameRandom(777);
            var b = new GameRandom(777);
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.NextULong(), b.NextULong());
        }

        [Test]
        public void DifferentSeeds_DifferentSequences()
        {
            var a = new GameRandom(1);
            var b = new GameRandom(2);
            Assert.AreNotEqual(a.NextULong(), b.NextULong());
        }

        [Test]
        public void ZeroSeed_Works()
        {
            var rng = new GameRandom(0);
            var values = new HashSet<ulong>();
            for (int i = 0; i < 10; i++) values.Add(rng.NextULong());
            Assert.AreEqual(10, values.Count);
        }

        [Test]
        public void Fork_DependsOnSeedNotOnParentState()
        {
            var parentA = new GameRandom(99);
            var parentB = new GameRandom(99);
            for (int i = 0; i < 50; i++) parentB.NextULong(); // consume B

            Assert.AreEqual(parentA.Fork("loot").NextULong(), parentB.Fork("loot").NextULong());
            Assert.AreNotEqual(parentA.Fork("loot").NextULong(), parentA.Fork("waves").NextULong());
        }

        [Test]
        public void NextInt_StaysInRange_AndHitsBothEnds()
        {
            var rng = new GameRandom(5);
            bool sawMin = false, sawMax = false;
            for (int i = 0; i < 10000; i++)
            {
                int v = rng.NextInt(-3, 4);
                Assert.That(v, Is.InRange(-3, 3));
                sawMin |= v == -3;
                sawMax |= v == 3;
            }
            Assert.IsTrue(sawMin && sawMax);
        }

        [Test]
        public void NextInt_InvalidRange_Throws()
        {
            var rng = new GameRandom(5);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => rng.NextInt(3, 3));
        }

        [Test]
        public void NextFloat_IsInZeroToOne()
        {
            var rng = new GameRandom(11);
            for (int i = 0; i < 10000; i++)
            {
                float f = rng.NextFloat();
                Assert.That(f, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void Shuffle_IsPermutation_AndDeterministic()
        {
            var a = Enumerable.Range(0, 20).ToList();
            var b = Enumerable.Range(0, 20).ToList();
            new GameRandom(3).Shuffle(a);
            new GameRandom(3).Shuffle(b);

            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 20), a);
            CollectionAssert.AreNotEqual(Enumerable.Range(0, 20), a);
        }
    }
}
