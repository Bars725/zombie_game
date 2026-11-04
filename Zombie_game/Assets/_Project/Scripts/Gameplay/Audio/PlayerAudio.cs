using Game.Gameplay.Combat;
using Game.Gameplay.Weapons;
using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Audio
{
    /// <summary>Player-side sounds: weapon fire / dry fire / reload (from WeaponDefinition), bullet impacts, getting hurt.</summary>
    public sealed class PlayerAudio : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponController _weapons;
        [SerializeField] private Health _health;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private SoundCue _hitFlesh;
        [SerializeField] private SoundCue _hitSurface;
        [SerializeField] private SoundCue _hurt;
        [SerializeField] private SoundCue _footstep;
        [SerializeField] private SoundCue _jumpLand;
        [SerializeField] private SoundCue _weaponSwitch;
        [SerializeField] private SoundCue _hitmarker;
        [SerializeField, Min(0f)] private float _hurtCooldown = 0.35f;

        private float _lastHurt = -10f;
        private WeaponInstance _watched;
        private float _footstepTimer;
        private bool _wasGrounded;

        private void OnEnable()
        {
            if (_weapons != null)
            {
                _weapons.Fired += HandleFired;
                _weapons.DryFired += HandleDryFired;
                _weapons.Hit += HandleHit;
                _weapons.WeaponChanged += Watch;
            }
            if (_health != null) _health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (_weapons != null)
            {
                _weapons.Fired -= HandleFired;
                _weapons.DryFired -= HandleDryFired;
                _weapons.Hit -= HandleHit;
                _weapons.WeaponChanged -= Watch;
            }
            if (_health != null) _health.Damaged -= HandleDamaged;
            Watch(null);
        }

        private void Start()
        {
            if (_weapons != null) Watch(_weapons.Current);
            if (_motor != null) _wasGrounded = _motor.IsGrounded;
        }

        private void Update()
        {
            if (_motor == null) return;
            bool grounded = _motor.IsGrounded;
            if (grounded && !_wasGrounded) SfxPlayer.Play(_jumpLand);
            _wasGrounded = grounded;

            if (!grounded || _motor.HorizontalSpeed < 0.25f)
            {
                _footstepTimer = 0f;
                return;
            }

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer > 0f) return;
            _footstepTimer = _motor.IsSprinting ? 0.34f : 0.48f;
            SfxPlayer.Play(_footstep);
        }

        private void Watch(WeaponInstance weapon)
        {
            if (_watched != null && weapon != _watched) SfxPlayer.Play(_weaponSwitch);
            if (_watched != null) _watched.ReloadStarted -= HandleReload;
            _watched = weapon;
            if (_watched != null) _watched.ReloadStarted += HandleReload;
        }

        private void HandleFired(WeaponInstance w) => SfxPlayer.Play(w.Definition.FireSound);
        private void HandleDryFired(WeaponInstance w) => SfxPlayer.Play(w.Definition.DryFireSound);
        private void HandleReload() { if (_watched != null) SfxPlayer.Play(_watched.Definition.ReloadSound); }

        private void HandleHit(ShotHit hit) =>
            HitSound(hit);

        private void HitSound(ShotHit hit)
        {
            bool damagedTarget = hit.Target != null && hit.Damage > 0f;
            SfxPlayer.Play(damagedTarget ? _hitFlesh : _hitSurface, hit.Point);
            if (damagedTarget) SfxPlayer.Play(_hitmarker);
        }

        private void HandleDamaged(DamageInfo damage, float applied)
        {
            if (Time.time - _lastHurt < _hurtCooldown) return;
            _lastHurt = Time.time;
            SfxPlayer.Play(_hurt);
        }
    }
}
