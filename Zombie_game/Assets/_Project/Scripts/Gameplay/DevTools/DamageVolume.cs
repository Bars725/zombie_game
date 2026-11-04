using System.Collections.Generic;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.DevTools
{
    /// <summary>
    /// Trigger that damages anything with IDamageable inside it, in ticks.
    /// Used on the test range to check player damage and segmented regen; later also as an arena hazard.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class DamageVolume : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _damagePerTick = 10f;
        [SerializeField, Min(0.05f)] private float _tickInterval = 0.5f;
        [SerializeField] private DamageType _damageType = DamageType.Environment;

        private readonly HashSet<IDamageable> _inside = new HashSet<IDamageable>();
        private readonly List<IDamageable> _buffer = new List<IDamageable>();
        private float _timer;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target != null && _inside.Add(target) && _inside.Count == 1) _timer = 0f;
        }

        private void OnTriggerExit(Collider other)
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target != null) _inside.Remove(target);
        }

        private void Update()
        {
            if (_inside.Count == 0) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _tickInterval;

            _buffer.Clear();
            _buffer.AddRange(_inside);
            foreach (IDamageable target in _buffer)
            {
                // Destroyed objects compare equal to null through UnityEngine.Object.
                if (target is Object unityObject && unityObject == null)
                {
                    _inside.Remove(target);
                    continue;
                }
                target.TakeDamage(new DamageInfo(_damagePerTick, _damageType, gameObject, transform.position));
            }
        }
    }
}
