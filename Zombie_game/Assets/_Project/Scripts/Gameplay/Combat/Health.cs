using System;
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Health component for player, enemies and destructibles. Thin Unity wrapper around HealthModel.
    /// Raises DamageTakenEvent / EntityDiedEvent on the global EventBus and local C# events.
    ///
    /// Setup: add to the root of a character. Colliders on the same object hit Health directly;
    /// colliders on child bones use HitZone to apply head/limb multipliers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float _maxHealth = 100f;

        [Header("Regeneration (0 = off, for enemies)")]
        [SerializeField, Min(0f)] private float _regenPerSecond = 0f;
        [SerializeField, Min(0f)] private float _regenDelay = 4f;
        [Tooltip("Regen only fills the current segment (0.25 = quarters). 0 = regen to full.")]
        [SerializeField, Range(0f, 1f)] private float _segmentFraction = 0.25f;

        [Header("Debug")]
        [SerializeField] private bool _invulnerable;

        private HealthModel _model;
        private DamageInfo _lastDamage;

        /// <summary>(damage, applied) after each hit that applied damage.</summary>
        public event Action<DamageInfo, float> Damaged;

        /// <summary>(current, max) after any change.</summary>
        public event Action<float, float> Changed;

        public event Action<DamageInfo> Died;

        public HealthModel Model
        {
            get
            {
                // Lazy so it also works in EditMode tests, where Awake is not called.
                if (_model == null) CreateModel();
                return _model;
            }
        }

        public float Current => Model.Current;
        public float Max => Model.Max;
        public float Normalized => Model.Normalized;
        public bool IsAlive => Model.IsAlive;

        public bool Invulnerable
        {
            get => _invulnerable;
            set => _invulnerable = value;
        }

        /// <summary>Configure from code (enemy definitions, tests) before first use.</summary>
        public void Configure(float maxHealth, float regenPerSecond = 0f, float regenDelay = 0f, float segmentFraction = 0f)
        {
            _maxHealth = maxHealth;
            _regenPerSecond = regenPerSecond;
            _regenDelay = regenDelay;
            _segmentFraction = segmentFraction;
            CreateModel();
        }

        public float TakeDamage(in DamageInfo damage)
        {
            if (_invulnerable || !IsAlive) return 0f;

            _lastDamage = damage;
            float applied = Model.ApplyDamage(damage.Amount);
            if (applied > 0f)
            {
                Damaged?.Invoke(damage, applied);
                EventBus<DamageTakenEvent>.Raise(new DamageTakenEvent(this, damage, applied));
            }
            return applied;
        }

        public float Heal(float amount) => Model.Heal(amount);

        public void Kill(GameObject instigator = null)
        {
            if (!IsAlive) return;
            _lastDamage = new DamageInfo(Current, DamageType.Generic, instigator);
            Model.Kill();
        }

        /// <summary>For pooled enemies and player respawn.</summary>
        public void ResetHealth() => Model.Revive(1f);

        private void Awake()
        {
            if (_model == null) CreateModel();
        }

        private void Update()
        {
            _model?.Tick(Time.deltaTime);
        }

        private void CreateModel()
        {
            if (_model != null)
            {
                _model.Changed -= HandleChanged;
                _model.Died -= HandleDied;
            }

            _model = new HealthModel(Mathf.Max(1f, _maxHealth), _regenPerSecond, _regenDelay, _segmentFraction);
            _model.Changed += HandleChanged;
            _model.Died += HandleDied;
        }

        private void HandleChanged(float current, float max) => Changed?.Invoke(current, max);

        private void HandleDied()
        {
            Died?.Invoke(_lastDamage);
            EventBus<EntityDiedEvent>.Raise(new EntityDiedEvent(this, _lastDamage));
        }

        private void OnValidate()
        {
            // Re-create when values change in the Inspector outside Play mode.
            if (!Application.isPlaying && _model != null) CreateModel();
        }
    }
}
