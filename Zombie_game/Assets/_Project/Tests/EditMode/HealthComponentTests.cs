using Game.Core.Events;
using Game.Gameplay.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Health and HitZone MonoBehaviours, exercised in EditMode (no Awake; Health inits lazily).</summary>
    public class HealthComponentTests
    {
        private GameObject _root;
        private Health _health;

        [SetUp]
        public void SetUp()
        {
            EventBus<EntityDiedEvent>.Clear();
            EventBus<DamageTakenEvent>.Clear();
            _root = new GameObject("TestEnemy");
            _health = _root.AddComponent<Health>();
            _health.Configure(maxHealth: 100f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            EventBus<EntityDiedEvent>.Clear();
            EventBus<DamageTakenEvent>.Clear();
        }

        [Test]
        public void TakeDamage_ReducesHealth_AndRaisesDamageTaken()
        {
            float raisedApplied = 0f;
            EventBus<DamageTakenEvent>.Subscribe(e => raisedApplied = e.Applied);

            float applied = _health.TakeDamage(new DamageInfo(30f, DamageType.Bullet));

            Assert.AreEqual(30f, applied);
            Assert.AreEqual(70f, _health.Current);
            Assert.AreEqual(30f, raisedApplied);
        }

        [Test]
        public void LethalDamage_RaisesEntityDied_WithInstigator()
        {
            var shooter = new GameObject("Shooter");
            try
            {
                Health died = null;
                GameObject killer = null;
                EventBus<EntityDiedEvent>.Subscribe(e => { died = e.Target; killer = e.Instigator; });

                _health.TakeDamage(new DamageInfo(500f, DamageType.Bullet, shooter));

                Assert.IsFalse(_health.IsAlive);
                Assert.AreSame(_health, died);
                Assert.AreSame(shooter, killer);
            }
            finally
            {
                Object.DestroyImmediate(shooter);
            }
        }

        [Test]
        public void Invulnerable_TakesNoDamage()
        {
            _health.Invulnerable = true;
            Assert.AreEqual(0f, _health.TakeDamage(new DamageInfo(50f)));
            Assert.AreEqual(100f, _health.Current);
        }

        [Test]
        public void HitZone_AppliesMultiplier_AndMarksZone()
        {
            var head = new GameObject("Head");
            head.transform.SetParent(_root.transform);
            head.AddComponent<SphereCollider>();
            var zone = head.AddComponent<HitZone>();
            zone.Configure(HitZoneType.Head, 2f);

            HitZoneType receivedZone = HitZoneType.Body;
            _health.Damaged += (d, _) => receivedZone = d.Zone;

            float applied = zone.TakeDamage(new DamageInfo(20f, DamageType.Bullet));

            Assert.AreEqual(40f, applied);
            Assert.AreEqual(60f, _health.Current);
            Assert.AreEqual(HitZoneType.Head, receivedZone);
            Assert.AreSame(_health, zone.Owner);
        }

        [Test]
        public void ResetHealth_RevivesToFull()
        {
            _health.Kill();
            _health.ResetHealth();
            Assert.IsTrue(_health.IsAlive);
            Assert.AreEqual(100f, _health.Current);
        }
    }
}
