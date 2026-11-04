using System.Collections.Generic;
using Game.Gameplay.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.DevTools
{
    /// <summary>
    /// Test-range spawner (the Wave Director replaces it in arenas): keeps N enemies alive,
    /// cycling through the given definitions and spawn points.
    /// Keys (dev only): F1 toggle spawning, F2 despawn all.
    /// </summary>
    public sealed class DevEnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyFactory _factory;
        [SerializeField] private List<EnemyDefinition> _definitions = new List<EnemyDefinition>();
        [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
        [SerializeField, Min(0)] private int _keepAlive = 4;
        [SerializeField, Min(0.1f)] private float _spawnInterval = 1.5f;
        [SerializeField] private float _startDelay = 1f;
        [SerializeField] private bool _spawning = true;

        private float _timer;
        private int _nextDefinition;
        private int _nextPoint;

        public bool Spawning => _spawning;
        public int AliveCount => _factory != null ? _factory.AliveCount : 0;

        private void Start()
        {
            _timer = _startDelay;
            if (_factory == null) return;
            foreach (EnemyDefinition def in _definitions)
            {
                if (def != null) _factory.Prewarm(def);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f1Key.wasPressedThisFrame) _spawning = !_spawning;
                if (keyboard.f2Key.wasPressedThisFrame && _factory != null) _factory.DespawnAll();
            }

            if (!_spawning || _factory == null || _definitions.Count == 0 || _spawnPoints.Count == 0) return;
            if (_factory.AliveCount >= _keepAlive) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _spawnInterval;

            EnemyDefinition def = _definitions[_nextDefinition++ % _definitions.Count];
            Transform point = _spawnPoints[_nextPoint++ % _spawnPoints.Count];
            if (def != null && point != null) _factory.Spawn(def, point.position, point.rotation);
        }
    }
}
