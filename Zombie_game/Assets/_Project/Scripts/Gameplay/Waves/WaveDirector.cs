using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Gameplay.Audio;
using Game.Gameplay.Enemies;
using UnityEngine;

namespace Game.Gameplay.Waves
{
    /// <summary>Runs budget-based waves, lulls, spawn visibility checks, and wave lifecycle events.</summary>
    public sealed class WaveDirector : MonoBehaviour
    {
        private enum DirectorState { Idle, Starting, Spawning, WaitingForClear, Lull, Completed }

        [Header("Scene references")]
        [SerializeField] private EnemyFactory _factory;
        [SerializeField] private List<EnemyDefinition> _definitions = new List<EnemyDefinition>();
        [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private SoundCue _waveStartSound;

        [Header("Run")]
        [SerializeField] private bool _startAutomatically = true;
        [SerializeField, Min(1)] private int _maxWaves = 10;
        [SerializeField] private long _runSeed = 1;
        [SerializeField, Min(0f)] private float _initialDelay = 3f;

        [Header("Wave budget and pacing")]
        [SerializeField, Min(1)] private int _baseBudget = 4;
        [SerializeField, Min(0)] private int _budgetGrowthPerWave = 4;
        [SerializeField, Min(1)] private int _maxAlive = 30;
        [SerializeField, Min(0.1f)] private float _spawnInterval = 1f;
        [SerializeField, Min(0f)] private float _minLull = 20f;
        [SerializeField, Min(0f)] private float _maxLull = 40f;

        [Header("Spawn safety")]
        [SerializeField, Min(0f)] private float _minimumPlayerDistance = 12f;
        [SerializeField] private LayerMask _visibilityMask = ~0;

        private readonly Queue<EnemyDefinition> _pendingEnemies = new Queue<EnemyDefinition>();
        private GameRandom _waveRandom;
        private DirectorState _state;
        private float _timer;
        private int _waveNumber;

        public int CurrentWave => _waveNumber;
        public int AliveCount => _factory != null ? _factory.AliveCount : 0;
        public int PendingCount => _pendingEnemies.Count;
        public bool IsRunning => _state != DirectorState.Idle && _state != DirectorState.Completed;
        public bool IsCompleted => _state == DirectorState.Completed;

        private void Start()
        {
            if (_factory == null) _factory = GetComponent<EnemyFactory>();
            if (_playerCamera == null && Camera.main != null) _playerCamera = Camera.main;

            if (_startAutomatically) BeginRun(unchecked((ulong)_runSeed));

            if (_factory != null && _definitions != null)
            {
                foreach (EnemyDefinition definition in _definitions)
                {
                    if (definition != null) _factory.Prewarm(definition);
                }
            }
        }

        private void Update()
        {
            switch (_state)
            {
                case DirectorState.Starting:
                case DirectorState.Lull:
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f) StartNextWave();
                    break;

                case DirectorState.Spawning:
                    TickSpawning();
                    break;

                case DirectorState.WaitingForClear:
                    if (AliveCount == 0) ClearCurrentWave();
                    break;
            }
        }

        /// <summary>Starts a fresh run and derives the wave stream from the run seed.</summary>
        public void BeginRun(ulong runSeed)
        {
            if (_factory == null) _factory = GetComponent<EnemyFactory>();
            if (_factory == null)
            {
                Debug.LogError("WaveDirector requires an EnemyFactory on this GameObject or an assigned reference.", this);
                _state = DirectorState.Idle;
                return;
            }

            _factory.DespawnAll();
            _pendingEnemies.Clear();
            _waveNumber = 0;
            _waveRandom = new GameRandom(runSeed).Fork("waves");
            _timer = _initialDelay;
            _state = DirectorState.Starting;
        }

        public void StopRun()
        {
            _state = DirectorState.Idle;
            _timer = 0f;
            _pendingEnemies.Clear();
            if (_factory != null) _factory.DespawnAll();
        }

        private void StartNextWave()
        {
            if (_waveNumber >= _maxWaves)
            {
                _state = DirectorState.Completed;
                EventBus<WavesCompletedEvent>.Raise(new WavesCompletedEvent(_waveNumber));
                return;
            }

            if (_definitions == null || _definitions.Count == 0 || _spawnPoints == null || _spawnPoints.Count == 0)
            {
                Debug.LogError("WaveDirector needs at least one enemy definition and one spawn point.", this);
                _state = DirectorState.Idle;
                return;
            }

            int nextWaveNumber = _waveNumber + 1;
            int budget = _baseBudget + (nextWaveNumber - 1) * _budgetGrowthPerWave;
            IReadOnlyList<EnemyDefinition> composition = WaveComposition.Build(_definitions, budget, _waveRandom);
            if (composition.Count == 0)
            {
                Debug.LogError($"WaveDirector could not build wave {nextWaveNumber} with budget {budget}. Check enemy SpawnCost values.", this);
                _state = DirectorState.Idle;
                return;
            }

            _waveNumber = nextWaveNumber;
            _pendingEnemies.Clear();
            foreach (EnemyDefinition definition in composition) _pendingEnemies.Enqueue(definition);
            _timer = 0f;
            _state = DirectorState.Spawning;
            SfxPlayer.Play(_waveStartSound);
            EventBus<WaveStartedEvent>.Raise(new WaveStartedEvent(_waveNumber, budget, composition.Count));
        }

        private void TickSpawning()
        {
            if (_pendingEnemies.Count == 0)
            {
                _state = DirectorState.WaitingForClear;
                if (AliveCount == 0) ClearCurrentWave();
                return;
            }

            if (AliveCount >= _maxAlive) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            if (!TryPickSpawnPoint(out Transform point))
            {
                _timer = Mathf.Max(_spawnInterval, 0.5f);
                return;
            }

            EnemyDefinition definition = _pendingEnemies.Peek();
            if (_factory.Spawn(definition, point.position, point.rotation) == null)
            {
                _timer = Mathf.Max(_spawnInterval, 0.5f);
                return;
            }

            _pendingEnemies.Dequeue();
            _timer = _spawnInterval;
        }

        private bool TryPickSpawnPoint(out Transform selected)
        {
            selected = null;
            int count = _spawnPoints.Count;
            if (count == 0) return false;

            int start = _waveRandom.NextInt(0, count);
            for (int i = 0; i < count; i++)
            {
                Transform candidate = _spawnPoints[(start + i) % count];
                if (candidate == null || !candidate.gameObject.activeInHierarchy) continue;
                if (IsTooClose(candidate.position) || IsVisibleToPlayer(candidate)) continue;
                selected = candidate;
                return true;
            }

            return false;
        }

        private bool IsTooClose(Vector3 position)
        {
            Vector3 playerPosition = _playerCamera != null
                ? _playerCamera.transform.position
                : PlayerAnchor.Current != null ? PlayerAnchor.Current.Position : transform.position;
            return (position - playerPosition).sqrMagnitude < _minimumPlayerDistance * _minimumPlayerDistance;
        }

        private bool IsVisibleToPlayer(Transform spawnPoint)
        {
            if (_playerCamera == null) return false;
            Vector3 position = spawnPoint.position;
            Vector3 viewport = _playerCamera.WorldToViewportPoint(position);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
                return false;

            Vector3 origin = _playerCamera.transform.position;
            Vector3 toPoint = position - origin;
            float distance = toPoint.magnitude;
            if (distance <= 0.01f) return true;
            if (Physics.Raycast(origin, toPoint / distance, out RaycastHit hit, distance, _visibilityMask,
                    QueryTriggerInteraction.Ignore))
                return hit.transform == spawnPoint || hit.transform.IsChildOf(spawnPoint);
            return true;
        }

        private void ClearCurrentWave()
        {
            if (_state == DirectorState.Lull || _state == DirectorState.Completed) return;
            _state = DirectorState.Lull;
            EventBus<WaveClearedEvent>.Raise(new WaveClearedEvent(_waveNumber));

            if (_waveNumber >= _maxWaves)
            {
                _state = DirectorState.Completed;
                EventBus<WavesCompletedEvent>.Raise(new WavesCompletedEvent(_waveNumber));
                return;
            }

            float min = Mathf.Min(_minLull, _maxLull);
            float max = Mathf.Max(_minLull, _maxLull);
            _timer = _waveRandom.Range(min, max);
        }
    }
}
