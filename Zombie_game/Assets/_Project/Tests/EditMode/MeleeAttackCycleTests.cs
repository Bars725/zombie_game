using Game.Gameplay.Enemies;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class MeleeAttackCycleTests
    {
        [Test]
        public void Idle_TickReturnsNone()
        {
            var a = new MeleeAttackCycle(0.5f, 1f);
            Assert.AreEqual(MeleeAttackCycle.Result.None, a.Tick(1f));
            Assert.IsFalse(a.IsBusy);
        }

        [Test]
        public void FullCycle_Windup_Hit_Recovery_Finished()
        {
            var a = new MeleeAttackCycle(0.5f, 1f);
            Assert.IsTrue(a.Begin());

            Assert.AreEqual(MeleeAttackCycle.Result.None, a.Tick(0.25f));
            Assert.AreEqual(MeleeAttackCycle.Result.HitMoment, a.Tick(0.25f));
            Assert.AreEqual(MeleeAttackCycle.Phase.Recovery, a.Current);

            Assert.AreEqual(MeleeAttackCycle.Result.None, a.Tick(0.5f));
            Assert.AreEqual(MeleeAttackCycle.Result.Finished, a.Tick(0.5f));
            Assert.IsFalse(a.IsBusy);
        }

        [Test]
        public void HitMoment_IsReportedOncePerAttack()
        {
            var a = new MeleeAttackCycle(0.1f, 1f);
            a.Begin();
            int hits = 0;
            for (int i = 0; i < 20; i++)
            {
                if (a.Tick(0.05f) == MeleeAttackCycle.Result.HitMoment) hits++;
            }
            Assert.AreEqual(1, hits);
        }

        [Test]
        public void Begin_WhileBusy_IsIgnored()
        {
            var a = new MeleeAttackCycle(0.5f, 1f);
            Assert.IsTrue(a.Begin());
            Assert.IsFalse(a.Begin());
        }

        [Test]
        public void Cancel_ReturnsToIdle_WithoutHit()
        {
            var a = new MeleeAttackCycle(0.5f, 1f);
            a.Begin();
            a.Cancel();
            Assert.AreEqual(MeleeAttackCycle.Result.None, a.Tick(1f));
            Assert.IsFalse(a.IsBusy);
        }

        [Test]
        public void LongFrame_CarriesOverIntoRecovery()
        {
            var a = new MeleeAttackCycle(0.5f, 1f);
            a.Begin();
            Assert.AreEqual(MeleeAttackCycle.Result.HitMoment, a.Tick(1.2f)); // 0.7 s into recovery
            Assert.AreEqual(MeleeAttackCycle.Result.Finished, a.Tick(0.31f)); // 0.31 not 0.30: float-safe
        }
    }
}
