using Game.Gameplay.Combat;
using Game.Gameplay.Enemies;
using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.DevTools
{
    /// <summary>
    /// Test range only: when the player dies, block input, then respawn at the start position
    /// with full health, clear all enemies and grant a short spawn protection.
    /// In real runs death goes to the results screen instead.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class DevPlayerRespawn : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _respawnDelay = 2f;
        [SerializeField, Min(0f)] private float _spawnProtection = 2f;
        [SerializeField] private bool _clearEnemiesOnRespawn = true;

        private Health _health;
        private PlayerInputReader _input;
        private PlayerMotor _motor;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private float _timer;
        private float _protectionTimer;
        private EnemyFactory _factory;

        public bool IsWaitingToRespawn => _timer > 0f;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _input = GetComponent<PlayerInputReader>();
            _motor = GetComponent<PlayerMotor>();
        }

        private void Start()
        {
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _factory = FindFirstObjectByType<EnemyFactory>();
        }

        private void OnEnable() => _health.Died += HandleDied;
        private void OnDisable() => _health.Died -= HandleDied;

        private void HandleDied(DamageInfo killingBlow)
        {
            _timer = Mathf.Max(0.01f, _respawnDelay);
            if (_input != null) _input.Blocked = true;
            Debug.Log($"[DevPlayerRespawn] Player killed by {(killingBlow.Instigator != null ? killingBlow.Instigator.name : "?")}");
        }

        private void Update()
        {
            if (_protectionTimer > 0f)
            {
                _protectionTimer -= Time.deltaTime;
                if (_protectionTimer <= 0f) _health.Invulnerable = false;
            }

            if (_timer <= 0f) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            if (_motor != null) _motor.Teleport(_spawnPosition, _spawnRotation);
            else transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            if (_clearEnemiesOnRespawn && _factory != null) _factory.DespawnAll();
            _health.ResetHealth();
            if (_spawnProtection > 0f)
            {
                _health.Invulnerable = true;
                _protectionTimer = _spawnProtection;
            }
            if (_motor != null && _motor.Stamina != null) _motor.Stamina.Refill();
            if (_input != null) _input.Blocked = false;
        }
    }
}
