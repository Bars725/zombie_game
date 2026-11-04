using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Weapons
{
    public enum FireMode
    {
        SemiAuto = 0,
        FullAuto = 1,
    }

    /// <summary>
    /// Design data for one weapon (hitscan). Read-only at runtime: per-weapon state lives in WeaponInstance.
    /// Upgrade tiers (Forge) will be added here as data later.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Weapons/Weapon Definition", fileName = "WD_New")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "weapon";
        [SerializeField] private string _displayName = "Weapon";

        [Header("Damage")]
        [SerializeField, Min(0f)] private float _damage = 20f;
        [SerializeField] private DamageType _damageType = DamageType.Bullet;
        [SerializeField, Min(1)] private int _pelletsPerShot = 1;
        [SerializeField, Min(1f)] private float _range = 100f;
        [Tooltip("Full damage up to this distance")]
        [SerializeField, Min(0f)] private float _falloffStart = 20f;
        [Tooltip("Minimum damage from this distance on")]
        [SerializeField, Min(0f)] private float _falloffEnd = 50f;
        [SerializeField, Range(0f, 1f)] private float _minDamageMultiplier = 0.5f;

        [Header("Handling")]
        [SerializeField] private FireMode _fireMode = FireMode.SemiAuto;
        [SerializeField, Min(1f)] private float _roundsPerMinute = 450f;
        [Tooltip("Cone half-angle in degrees")]
        [SerializeField, Min(0f)] private float _spreadDegrees = 1f;
        [SerializeField, Min(0f)] private float _reloadTime = 1.5f;

        [Header("Ammo")]
        [SerializeField, Min(1)] private int _magazineSize = 12;
        [SerializeField, Min(0)] private int _maxReserve = 96;
        [SerializeField, Min(0)] private int _startingReserve = 48;

        [Header("Economy")]
        [SerializeField, Min(0)] private int _price = 500;

        [Header("Presentation")]
        [Tooltip("First-person model, placed at the camera's viewmodel socket. Empty = placeholder box.")]
        [SerializeField] private GameObject _viewModel;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float Damage => _damage;
        public DamageType DamageType => _damageType;
        public int PelletsPerShot => _pelletsPerShot;
        public float Range => _range;
        public float FalloffStart => _falloffStart;
        public float FalloffEnd => _falloffEnd;
        public float MinDamageMultiplier => _minDamageMultiplier;
        public FireMode FireMode => _fireMode;
        public float RoundsPerMinute => _roundsPerMinute;
        public float SecondsPerShot => 60f / _roundsPerMinute;
        public float SpreadDegrees => _spreadDegrees;
        public float ReloadTime => _reloadTime;
        public int MagazineSize => _magazineSize;
        public int MaxReserve => _maxReserve;
        public int StartingReserve => Mathf.Min(_startingReserve, _maxReserve);
        public int Price => _price;
        public GameObject ViewModel => _viewModel;

        /// <summary>Damage per pellet at a distance, with linear falloff between FalloffStart and FalloffEnd.</summary>
        public float DamageAtDistance(float distance)
        {
            if (distance <= _falloffStart) return _damage;
            if (distance >= _falloffEnd || _falloffEnd <= _falloffStart) return _damage * _minDamageMultiplier;
            float t = (distance - _falloffStart) / (_falloffEnd - _falloffStart);
            return _damage * Mathf.Lerp(1f, _minDamageMultiplier, t);
        }

        /// <summary>Code setup for tests and editor tools (assets are normally edited in the Inspector).</summary>
        internal void Initialize(string id, string displayName, float damage, FireMode fireMode, float roundsPerMinute,
            int magazineSize, int maxReserve, int startingReserve, float reloadTime,
            int pelletsPerShot = 1, float spreadDegrees = 1f, float range = 100f,
            float falloffStart = 20f, float falloffEnd = 50f, float minDamageMultiplier = 0.5f,
            DamageType damageType = DamageType.Bullet, int price = 500)
        {
            _id = id;
            _displayName = displayName;
            _damage = damage;
            _fireMode = fireMode;
            _roundsPerMinute = roundsPerMinute;
            _magazineSize = magazineSize;
            _maxReserve = maxReserve;
            _startingReserve = startingReserve;
            _reloadTime = reloadTime;
            _pelletsPerShot = pelletsPerShot;
            _spreadDegrees = spreadDegrees;
            _range = range;
            _falloffStart = falloffStart;
            _falloffEnd = falloffEnd;
            _minDamageMultiplier = minDamageMultiplier;
            _damageType = damageType;
            _price = price;
            OnValidate();
        }

        private void OnValidate()
        {
            if (_falloffEnd < _falloffStart) _falloffEnd = _falloffStart;
            if (_startingReserve > _maxReserve) _startingReserve = _maxReserve;
            if (string.IsNullOrWhiteSpace(_id)) _id = name;
        }
    }
}
