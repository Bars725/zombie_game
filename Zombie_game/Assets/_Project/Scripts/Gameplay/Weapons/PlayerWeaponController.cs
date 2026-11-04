using System;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay.Combat;
using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Weapons
{
    /// <summary>Result of one pellet that hit something. Used by HUD and (later) impact FX / audio.</summary>
    public readonly struct ShotHit
    {
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly Collider Collider;
        public readonly IDamageable Target;
        public readonly float Damage;
        public readonly bool Killed;
        public readonly bool Headshot;

        public ShotHit(Vector3 point, Vector3 normal, Collider collider, IDamageable target, float damage, bool killed, bool headshot)
        {
            Point = point;
            Normal = normal;
            Collider = collider;
            Target = target;
            Damage = damage;
            Killed = killed;
            Headshot = headshot;
        }
    }

    /// <summary>
    /// Player's carried weapons: switching (1-3, mouse wheel), firing (semi/auto), reload (R, auto when empty).
    /// Hitscan from the camera centre with a random cone spread per pellet.
    /// </summary>
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private Camera _camera;
        [SerializeField] private List<WeaponDefinition> _startingLoadout = new List<WeaponDefinition>();
        [SerializeField, Min(1)] private int _maxSlots = 3;
        [SerializeField] private LayerMask _hitMask = Physics.DefaultRaycastLayers;
        [SerializeField] private bool _autoReloadWhenEmpty = true;
        [SerializeField] private bool _drawDebugRays = true;

        private readonly List<WeaponInstance> _weapons = new List<WeaponInstance>();
        private int _currentIndex = -1;
        private GameRandom _spreadRandom;

        public IReadOnlyList<WeaponInstance> Weapons => _weapons;
        public int CurrentIndex => _currentIndex;
        public WeaponInstance Current => _currentIndex >= 0 && _currentIndex < _weapons.Count ? _weapons[_currentIndex] : null;

        /// <summary>A shot was fired (after ammo was consumed).</summary>
        public event Action<WeaponInstance> Fired;

        /// <summary>One pellet hit a collider (damageable or not).</summary>
        public event Action<ShotHit> Hit;

        /// <summary>Tried to fire with an empty magazine.</summary>
        public event Action<WeaponInstance> DryFired;

        public event Action<WeaponInstance> WeaponChanged;

        private void Awake()
        {
            // Spread is cosmetic-ish and not part of run generation, so a time-based seed is fine.
            _spreadRandom = new GameRandom((ulong)Environment.TickCount);
            if (_camera == null) _camera = GetComponentInChildren<Camera>();

            foreach (WeaponDefinition def in _startingLoadout)
            {
                if (def != null) AddWeapon(def);
            }
            Equip(0); // start with the first weapon of the loadout, not the last one added
        }

        private void Update()
        {
            if (_input == null) return;

            HandleSwitching();

            WeaponInstance weapon = Current;
            if (weapon == null) return;

            weapon.Tick(Time.deltaTime);

            if (_input.ReloadPressed) weapon.TryStartReload();

            bool trigger = weapon.Definition.FireMode == FireMode.FullAuto ? _input.FireHeld : _input.FirePressed;
            if (!trigger) return;

            if (weapon.TryFire())
            {
                Shoot(weapon);
                Fired?.Invoke(weapon);
            }
            else if (weapon.IsMagazineEmpty && !weapon.IsReloading && _input.FirePressed)
            {
                DryFired?.Invoke(weapon);
                if (_autoReloadWhenEmpty) weapon.TryStartReload();
            }

            if (_autoReloadWhenEmpty && weapon.IsMagazineEmpty && !weapon.IsReloading) weapon.TryStartReload();
        }

        /// <summary>Adds a weapon (wall-buy, pickup). Replaces the current one when all slots are full.</summary>
        public WeaponInstance AddWeapon(WeaponDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i].Definition == definition)
                {
                    _weapons[i].RefillAll(); // buying a weapon you already have = ammo refill
                    Equip(i);
                    return _weapons[i];
                }
            }

            var instance = new WeaponInstance(definition);
            if (_weapons.Count < _maxSlots)
            {
                _weapons.Add(instance);
                Equip(_weapons.Count - 1);
            }
            else
            {
                int slot = Mathf.Max(0, _currentIndex);
                _weapons[slot].CancelReload();
                _weapons[slot] = instance;
                _currentIndex = -1;
                Equip(slot);
            }
            return instance;
        }

        public void Equip(int index)
        {
            if (index < 0 || index >= _weapons.Count || index == _currentIndex) return;
            Current?.CancelReload();
            _currentIndex = index;
            WeaponChanged?.Invoke(Current);
        }

        private void HandleSwitching()
        {
            if (_weapons.Count < 2) return;

            int slot = _input.WeaponSlotPressed;
            if (slot >= 0)
            {
                Equip(slot);
                return;
            }

            int scroll = _input.WeaponScroll;
            if (scroll != 0)
            {
                int next = (_currentIndex - scroll + _weapons.Count) % _weapons.Count; // wheel up = previous
                Equip(next);
            }
        }

        private void Shoot(WeaponInstance weapon)
        {
            WeaponDefinition def = weapon.Definition;
            Transform cam = _camera != null ? _camera.transform : transform;
            Vector3 origin = cam.position;

            for (int i = 0; i < def.PelletsPerShot; i++)
            {
                Vector3 direction = SpreadDirection(cam, def.SpreadDegrees);

                if (!Physics.Raycast(origin, direction, out RaycastHit hit, def.Range, _hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (_drawDebugRays) Debug.DrawRay(origin, direction * def.Range, Color.gray, 0.5f);
                    continue;
                }

                if (_drawDebugRays) Debug.DrawLine(origin, hit.point, Color.yellow, 0.5f);

                if (!hit.collider.TryGetComponent(out IDamageable target))
                {
                    target = hit.collider.GetComponentInParent<IDamageable>();
                }

                float applied = 0f;
                bool headshot = false;
                bool killed = false;
                if (target != null && target.IsAlive)
                {
                    var damage = new DamageInfo(def.DamageAtDistance(hit.distance), def.DamageType, gameObject, hit.point, direction);
                    headshot = hit.collider.TryGetComponent(out HitZone zone) && zone.Zone == HitZoneType.Head;
                    applied = target.TakeDamage(damage);
                    killed = applied > 0f && !target.IsAlive;
                }

                Hit?.Invoke(new ShotHit(hit.point, hit.normal, hit.collider, target, applied, killed, headshot));
            }
        }

        /// <summary>Uniform random direction inside a cone around the camera forward.</summary>
        private Vector3 SpreadDirection(Transform cam, float spreadDegrees)
        {
            if (spreadDegrees <= 0f) return cam.forward;

            float angle = spreadDegrees * Mathf.Sqrt(_spreadRandom.NextFloat()) * Mathf.Deg2Rad;
            float theta = _spreadRandom.NextFloat() * Mathf.PI * 2f;
            float radius = Mathf.Tan(angle);
            Vector3 offset = (cam.right * Mathf.Cos(theta) + cam.up * Mathf.Sin(theta)) * radius;
            return (cam.forward + offset).normalized;
        }
    }
}
