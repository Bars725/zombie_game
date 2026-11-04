using Game.Gameplay.Player;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class StaminaModelTests
    {
        // 100 max, 20/s drain (5 s sprint), 25/s regen after 1 s, resume at 30%
        private static StaminaModel Create() => new StaminaModel(100f, 20f, 25f, 1f, 0.3f);

        [Test]
        public void Sprinting_Drains()
        {
            var s = Create();
            Assert.IsTrue(s.Tick(1f, sprinting: true));
            Assert.AreEqual(80f, s.Current, 0.001f);
        }

        [Test]
        public void RunningDry_Exhausts_AndBlocksSprint()
        {
            var s = Create();
            for (int i = 0; i < 5; i++) s.Tick(1f, sprinting: true); // 100 -> 0

            Assert.AreEqual(0f, s.Current, 0.001f);
            Assert.IsTrue(s.Exhausted);
            Assert.IsFalse(s.CanSprint);
            Assert.IsFalse(s.Tick(0.1f, sprinting: true));
        }

        [Test]
        public void Exhausted_ResumesOnlyAfterThreshold()
        {
            var s = Create();
            for (int i = 0; i < 5; i++) s.Tick(1f, sprinting: true); // empty
            s.Tick(0.5f, sprinting: false); // still in regen delay -> 0
            Assert.AreEqual(0f, s.Current, 0.001f);
            s.Tick(0.5f, sprinting: false); // delay reached: +12.5
            s.Tick(0.6f, sprinting: false); // +15 -> 27.5 (< 30)
            Assert.IsTrue(s.Exhausted);

            s.Tick(0.2f, sprinting: false); // +5 -> 32.5 (past the 30% threshold; exact 30 is float-fragile)
            Assert.IsFalse(s.Exhausted);
            Assert.IsTrue(s.CanSprint);
        }

        [Test]
        public void Regen_WaitsForDelay_AndCapsAtMax()
        {
            var s = Create();
            s.Tick(1f, sprinting: true);  // 80
            s.Tick(0.5f, sprinting: false);
            Assert.AreEqual(80f, s.Current, 0.001f);

            for (int i = 0; i < 20; i++) s.Tick(1f, sprinting: false);
            Assert.AreEqual(100f, s.Current, 0.001f);
        }
    }
}
