using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Spawns enemies from per-definition object pools (UnityEngine.Pool.ObjectPool).
    /// Waves and dev spawners call Spawn(); enemies return themselves via Despawn().
    /// </summary>
    public sealed class EnemyFactory : MonoBehaviour
    {
        [SerializeField, Min(0)] private int _prewarmPerType = 8;
        [Tooltip("Max distance from the requested point to the NavMesh")]
        [SerializeField, Min(0.1f)] private float _navMeshSnapDistance = 2f;

        private readonly Dictionary<EnemyDefinition, ObjectPool<EnemyController>> _pools =
            new Dictionary<EnemyDefinition, ObjectPool<EnemyController>>();
        private readonly List<EnemyController> _alive = new List<EnemyController>();
        private int _createdCount;
        private Transform _poolRoot;   // inactive: pooled instances never Awake/OnEnable off the NavMesh
        private Transform _activeRoot;

        public IReadOnlyList<EnemyController> Alive => _alive;
        public int AliveCount => _alive.Count;

        private void Awake()
        {
            var poolGo = new GameObject("EnemyPool (inactive)");
            poolGo.SetActive(false);
            _poolRoot = poolGo.transform;
            _poolRoot.SetParent(transform, false);
            _activeRoot = new GameObject("Enemies").transform;
            _activeRoot.SetParent(transform, false);
        }

        public void Prewarm(EnemyDefinition definition)
        {
            ObjectPool<EnemyController> pool = GetPool(definition);
            var temp = new List<EnemyController>();
            for (int i = pool.CountInactive; i < _prewarmPerType; i++) temp.Add(pool.Get());
            foreach (EnemyController e in temp) pool.Release(e);
        }

        /// <returns>The spawned enemy, or null if the point is too far from the NavMesh.</returns>
        public EnemyController Spawn(EnemyDefinition definition, Vector3 position, Quaternion rotation)
        {
            if (definition == null || definition.Prefab == null)
            {
                Debug.LogError("EnemyFactory.Spawn: definition or its prefab is missing.", this);
                return null;
            }

            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, _navMeshSnapDistance, NavMesh.AllAreas))
            {
                Debug.LogWarning($"EnemyFactory: no NavMesh near {position}; spawn skipped.", this);
                return null;
            }

            ObjectPool<EnemyController> pool = GetPool(definition);
            EnemyController enemy = pool.Get();
            enemy.transform.SetParent(_activeRoot, true);
            _alive.Add(enemy);
            enemy.Spawn(definition, hit.position, rotation, e =>
            {
                _alive.Remove(e);
                pool.Release(e);
            });
            return enemy;
        }

        public void DespawnAll()
        {
            for (int i = _alive.Count - 1; i >= 0; i--) _alive[i].Despawn();
        }

        private ObjectPool<EnemyController> GetPool(EnemyDefinition definition)
        {
            if (_pools.TryGetValue(definition, out ObjectPool<EnemyController> pool)) return pool;

            pool = new ObjectPool<EnemyController>(
                createFunc: () =>
                {
                    EnemyController e = Instantiate(definition.Prefab, _poolRoot);
                    e.name = $"{definition.DisplayName}_{++_createdCount}";
                    e.gameObject.SetActive(false); // stays inactive until Spawn() has positioned it
                    return e;
                },
                actionOnGet: null, // Spawn() activates after positioning
                actionOnRelease: e =>
                {
                    e.gameObject.SetActive(false);
                    e.transform.SetParent(_poolRoot, false);
                },
                actionOnDestroy: e => { if (e != null) Destroy(e.gameObject); },
                collectionCheck: true,
                defaultCapacity: _prewarmPerType,
                maxSize: 200);
            _pools.Add(definition, pool);
            return pool;
        }
    }
}
