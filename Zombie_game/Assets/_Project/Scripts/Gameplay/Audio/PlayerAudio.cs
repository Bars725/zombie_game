using Game.Gameplay.Combat;
using Game.Gameplay.Weapons;
using UnityEngine;

namespace Game.Gameplay.Audio
{
    /// <summary>Player-side sounds: weapon fire / dry fire / reload (from WeaponDefinition), bullet impacts, getting hurt.</summary>
    public sealed class PlayerAudio : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponController _weapons;
        [SerializeField] private Health _health;
        [SerializeField] private SoundCue _hitFlesh;
        [SerializeField] private SoundCue _hitSurface;
        [SerializeField] private SoundCue _hurt;
        [SerializeField, Min(0f)] private float _hurtCooldown = 0.35f;

        private float _lastHurt = -10f;
        private WeaponInstance _watched;

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
        }

        private void Watch(WeaponInstance weapon)
        {
            if (_watched != null) _watched.ReloadStarted -= HandleReload;
            _watched = weapon;
            if (_watched != null) _watched.ReloadStarted += HandleReload;
        }

        private void HandleFired(WeaponInstance w) => SfxPlayer.Play(w.Definition.FireSound);
        private void HandleDryFired(WeaponInstance w) => SfxPlayer.Play(w.Definition.DryFireSound);
        private void HandleReload() { if (_watched != null) SfxPlayer.Play(_watched.Definition.ReloadSound); }

        private void HandleHit(ShotHit hit) =>
            SfxPlayer.Play(hit.Target != null && hit.Damage > 0f ? _hitFlesh : _hitSurface, hit.Point);

        private void HandleDamaged(DamageInfo damage, float applied)
        {
            if (Time.time - _lastHurt < _hurtCooldown) return;
            _lastHurt = Time.time;
            SfxPlayer.Play(_hurt);
        }
    }
}
