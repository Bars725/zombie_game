using Game.Gameplay.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class WeaponTests
    {
        private WeaponDefinition _def;

        [SetUp]
        public void SetUp()
        {
            // 600 rpm = 0.1 s per shot, mag 10, reserve 15 of max 30, reload 1 s
            _def = ScriptableObject.CreateInstance<WeaponDefinition>();
            _def.Initialize("test", "Test Gun", damage: 20f, FireMode.FullAuto, roundsPerMinute: 600f,
                magazineSize: 10, maxReserve: 30, startingReserve: 15, reloadTime: 1f,
                falloffStart: 10f, falloffEnd: 30f, minDamageMultiplier: 0.5f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_def);

        [Test]
        public void NewInstance_HasFullMagazine_AndStartingReserve()
        {
            var w = new WeaponInstance(_def);
            Assert.AreEqual(10, w.Magazine);
            Assert.AreEqual(15, w.Reserve);
        }

        [Test]
        public void TryFire_ConsumesRound_AndRespectsCooldown()
        {
            var w = new WeaponInstance(_def);
            Assert.IsTrue(w.TryFire());
            Assert.IsFalse(w.TryFire(), "cooldown should block a second shot");

            w.Tick(0.1f);
            Assert.IsTrue(w.TryFire());
            Assert.AreEqual(8, w.Magazine);
        }

        [Test]
        public void EmptyMagazine_CannotFire()
        {
            var w = new WeaponInstance(_def);
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(w.TryFire());
                w.Tick(0.1f);
            }
            Assert.IsTrue(w.IsMagazineEmpty);
            Assert.IsFalse(w.TryFire());
        }

        [Test]
        public void Reload_MovesAmmoFromReserve_AfterReloadTime()
        {
            var w = new WeaponInstance(_def);
            for (int i = 0; i < 4; i++) { w.TryFire(); w.Tick(0.1f); } // mag 6

            Assert.IsTrue(w.TryStartReload());
            w.Tick(0.5f);
            Assert.AreEqual(6, w.Magazine, "not finished yet");
            Assert.IsFalse(w.CanFire);

            w.Tick(0.6f);
            Assert.IsFalse(w.IsReloading);
            Assert.AreEqual(10, w.Magazine);
            Assert.AreEqual(11, w.Reserve);
        }

        [Test]
        public void Reload_WithSmallReserve_TakesWhatIsLeft()
        {
            var w = new WeaponInstance(_def);
            w.AddReserve(-100); // ignored
            for (int i = 0; i < 10; i++) { w.TryFire(); w.Tick(0.1f); } // mag 0, reserve 15
            w.TryStartReload(); w.Tick(1f);                             // mag 10, reserve 5
            for (int i = 0; i < 10; i++) { w.TryFire(); w.Tick(0.1f); } // mag 0
            w.TryStartReload(); w.Tick(1f);

            Assert.AreEqual(5, w.Magazine);
            Assert.AreEqual(0, w.Reserve);
            Assert.IsFalse(w.CanReload);
        }

        [Test]
        public void Reload_NotStarted_WhenMagazineFull()
        {
            var w = new WeaponInstance(_def);
            Assert.IsFalse(w.TryStartReload());
        }

        [Test]
        public void CancelReload_KeepsAmmo()
        {
            var w = new WeaponInstance(_def);
            w.TryFire();
            w.TryStartReload();
            w.CancelReload();
            w.Tick(2f);
            Assert.AreEqual(9, w.Magazine);
            Assert.AreEqual(15, w.Reserve);
        }

        [Test]
        public void AddReserve_IsCappedByMax()
        {
            var w = new WeaponInstance(_def);
            Assert.AreEqual(15, w.AddReserve(100));
            Assert.AreEqual(30, w.Reserve);
        }

        [Test]
        public void DamageAtDistance_FallsOffLinearly()
        {
            Assert.AreEqual(20f, _def.DamageAtDistance(5f), 0.001f);
            Assert.AreEqual(15f, _def.DamageAtDistance(20f), 0.001f);
            Assert.AreEqual(10f, _def.DamageAtDistance(30f), 0.001f);
            Assert.AreEqual(10f, _def.DamageAtDistance(100f), 0.001f);
        }

        [Test]
        public void SecondsPerShot_FromRpm()
        {
            Assert.AreEqual(0.1f, _def.SecondsPerShot, 0.0001f);
        }
    }
}
