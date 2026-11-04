using Game.Gameplay.Combat;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class HealthModelTests
    {
        private static HealthModel Player() =>
            new HealthModel(max: 100f, regenPerSecond: 10f, regenDelay: 2f, segmentFraction: 0.25f);

        [Test]
        public void Damage_ReducesHealth_AndReturnsApplied()
        {
            var h = new HealthModel(100f);
            Assert.AreEqual(30f, h.ApplyDamage(30f));
            Assert.AreEqual(70f, h.Current);
        }

        [Test]
        public void Overkill_ClampsToZero_AndDiesOnce()
        {
            var h = new HealthModel(50f);
            int deaths = 0;
            h.Died += () => deaths++;

            Assert.AreEqual(50f, h.ApplyDamage(80f));
            Assert.AreEqual(0f, h.ApplyDamage(10f));

            Assert.AreEqual(0f, h.Current);
            Assert.IsFalse(h.IsAlive);
            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void NonPositiveDamage_IsIgnored()
        {
            var h = new HealthModel(100f);
            Assert.AreEqual(0f, h.ApplyDamage(0f));
            Assert.AreEqual(0f, h.ApplyDamage(-5f));
            Assert.AreEqual(100f, h.Current);
        }

        [Test]
        public void Heal_ClampsToMax_AndDoesNothingWhenDead()
        {
            var h = new HealthModel(100f);
            h.ApplyDamage(30f);
            Assert.AreEqual(30f, h.Heal(50f));
            Assert.AreEqual(100f, h.Current);

            h.Kill();
            Assert.AreEqual(0f, h.Heal(50f));
            Assert.AreEqual(0f, h.Current);
        }

        [Test]
        public void Regen_WaitsForDelay()
        {
            var h = Player();
            h.ApplyDamage(40f); // 60
            h.Tick(1.9f);
            Assert.AreEqual(60f, h.Current);
        }

        [Test]
        public void Regen_StopsAtTopOfCurrentSegment()
        {
            var h = Player();
            h.ApplyDamage(40f); // 60 -> segment ceiling 75
            h.Tick(2f);         // delay passed
            for (int i = 0; i < 100; i++) h.Tick(0.1f);
            Assert.AreEqual(75f, h.Current, 0.001f);
        }

        [Test]
        public void Regen_AtSegmentBoundary_DoesNothing()
        {
            var h = Player();
            h.ApplyDamage(50f); // exactly 50
            h.Tick(2f);
            h.Tick(5f);
            Assert.AreEqual(50f, h.Current, 0.001f);
        }

        [Test]
        public void Damage_ResetsRegenDelay()
        {
            var h = Player();
            h.ApplyDamage(40f); // 60
            h.Tick(1.5f);
            h.ApplyDamage(5f);  // 55, timer reset
            h.Tick(1.5f);
            Assert.AreEqual(55f, h.Current, 0.001f);
        }

        [Test]
        public void ZeroSegment_RegensToFull()
        {
            var h = new HealthModel(100f, regenPerSecond: 50f, regenDelay: 0f, segmentFraction: 0f);
            h.ApplyDamage(90f);
            for (int i = 0; i < 10; i++) h.Tick(0.5f);
            Assert.AreEqual(100f, h.Current, 0.001f);
        }

        [Test]
        public void NoRegenRate_NeverRegens()
        {
            var h = new HealthModel(100f);
            h.ApplyDamage(10f);
            h.Tick(100f);
            Assert.AreEqual(90f, h.Current);
        }

        [Test]
        public void Revive_RestoresHealth_AndCanDieAgain()
        {
            var h = new HealthModel(100f);
            int deaths = 0;
            h.Died += () => deaths++;
            h.Kill();
            h.Revive(0.5f);

            Assert.AreEqual(50f, h.Current, 0.001f);
            h.Kill();
            Assert.AreEqual(2, deaths);
        }

        [Test]
        public void SetMax_KeepsRatio()
        {
            var h = new HealthModel(100f);
            h.ApplyDamage(50f);
            h.SetMax(200f);
            Assert.AreEqual(100f, h.Current, 0.001f);
            Assert.AreEqual(200f, h.Max);
        }

        [Test]
        public void Changed_FiresWithCurrentAndMax()
        {
            var h = new HealthModel(100f);
            float lastCurrent = -1f, lastMax = -1f;
            h.Changed += (c, m) => { lastCurrent = c; lastMax = m; };
            h.ApplyDamage(25f);
            Assert.AreEqual(75f, lastCurrent);
            Assert.AreEqual(100f, lastMax);
        }
    }
}
