using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Put on a collider (head, limb) of a character. Applies a damage multiplier
    /// and forwards the hit to the owner's Health.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class HitZone : MonoBehaviour, IDamageable
    {
        [SerializeField] private HitZoneType _zone = HitZoneType.Head;
        [SerializeField, Min(0f)] private float _damageMultiplier = 2f;
        [Tooltip("Leave empty to use the Health in parents.")]
        [SerializeField] private Health _owner;

        public HitZoneType Zone => _zone;
        public float DamageMultiplier => _damageMultiplier;

        public Health Owner
        {
            get
            {
                if (_owner == null) _owner = GetComponentInParent<Health>();
                return _owner;
            }
        }

        public bool IsAlive => Owner != null && Owner.IsAlive;

        public void Configure(HitZoneType zone, float damageMultiplier, Health owner = null)
        {
            _zone = zone;
            _damageMultiplier = damageMultiplier;
            if (owner != null) _owner = owner;
        }

        public float TakeDamage(in DamageInfo damage)
        {
            Health owner = Owner;
            if (owner == null)
            {
                Debug.LogWarning($"HitZone on '{name}' has no Health in parents.", this);
                return 0f;
            }
            return owner.TakeDamage(damage.WithZone(_zone, _damageMultiplier));
        }
    }
}
