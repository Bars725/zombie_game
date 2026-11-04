using System;
using Game.Gameplay.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Melee enemy on a NavMeshAgent with a small state machine:
    ///   Spawning (emerges from the floor) -> Chasing -> Attacking -> ... -> Dead (sinks) -> back to pool.
    /// Stats come from EnemyDefinition on every Spawn, so pooled instances are fully reset.
    ///
    /// Prefab layout: root (NavMeshAgent, Health, EnemyController, body collider)
    ///   + "Visual" child (meshes, moved for emerge/sink) + head collider with HitZone.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public sealed class EnemyController : MonoBehaviour
    {
        public enum State { Inactive, Spawning, Chasing, Attacking, Dead }

        private const float RepathInterval = 0.25f;
        private const float RepathMinTargetMove = 0.5f;
        private const float EmergeDepth = 1.8f;

        [Tooltip("Moved down/up for the emerge and sink effect. Should not contain the colliders.")]
        [SerializeField] private Transform _visual;
        [Tooltip("Colliders disabled while dead (body, head). Empty = all child colliders.")]
        [SerializeField] private Collider[] _hitColliders = Array.Empty<Collider>();

        private NavMeshAgent _agent;
        private Health _health;
        private MeleeAttackCycle _attack;
        private Action<EnemyController> _release;
        private Vector3 _visualRestPosition;
        private Vector3 _lastDestination;
        private float _stateTimer;
        private float _repathTimer;

        public EnemyDefinition Definition { get; private set; }
        public State CurrentState { get; private set; } = State.Inactive;
        public Health Health => _health;
        public bool IsAlive => CurrentState != State.Dead && CurrentState != State.Inactive && _health.IsAlive;

        /// <summary>Fired once when the enemy dies (before the corpse timer).</summary>
        public event Action<EnemyController> Killed;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _attack = new MeleeAttackCycle(0.45f, 0.8f);
            if (_visual != null) _visualRestPosition = _visual.localPosition;
            if (_hitColliders == null || _hitColliders.Length == 0) _hitColliders = GetComponentsInChildren<Collider>(true);
        }

        private void OnEnable() => _health.Died += HandleDied;
        private void OnDisable() => _health.Died -= HandleDied;

        /// <summary>Called by the pool/spawner. The object may be inactive before this call.</summary>
        public void Spawn(EnemyDefinition definition, Vector3 position, Quaternion rotation, Action<EnemyController> release)
        {
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            _release = release;

            transform.SetPositionAndRotation(position, rotation);
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            _agent.enabled = true;
            if (!_agent.Warp(position))
            {
                Debug.LogWarning($"Enemy '{name}' spawned off the NavMesh at {position}.", this);
            }
            _agent.speed = definition.MoveSpeed;
            _agent.acceleration = definition.Acceleration;
            _agent.angularSpeed = definition.AngularSpeed;
            _agent.stoppingDistance = definition.AttackRange * 0.8f;

            _health.Configure(definition.MaxHealth);
            _attack.Configure(definition.AttackWindup, definition.AttackRecovery);
            SetCollidersEnabled(true);

            // Stagger repaths so a whole wave does not path on the same frame.
            _repathTimer = UnityEngine.Random.Range(0f, RepathInterval);
            EnterState(State.Spawning);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _stateTimer += dt;

            switch (CurrentState)
            {
                case State.Spawning: TickSpawning(); break;
                case State.Chasing: TickChasing(dt); break;
                case State.Attacking: TickAttacking(dt); break;
                case State.Dead: TickDead(); break;
            }
        }

        // ------------------------------------------------------------ states

        private void EnterState(State next)
        {
            CurrentState = next;
            _stateTimer = 0f;

            switch (next)
            {
                case State.Spawning:
                    Stop();
                    _health.Invulnerable = true; // can't be killed while still under the floor
                    SetVisualDepth(EmergeDepth);
                    break;
                case State.Chasing:
                    Resume();
                    _agent.updateRotation = true;
                    _repathTimer = 0f;
                    break;
                case State.Attacking:
                    Stop();
                    _agent.updateRotation = false;
                    _attack.Begin();
                    break;
                case State.Dead:
                    _attack.Cancel();
                    Stop();
                    SetCollidersEnabled(false);
                    break;
            }
        }

        private void TickSpawning()
        {
            float duration = Definition.SpawnDuration;
            float t = duration > 0f ? Mathf.Clamp01(_stateTimer / duration) : 1f;
            SetVisualDepth(EmergeDepth * (1f - t));
            if (t >= 1f)
            {
                _health.Invulnerable = false;
                EnterState(State.Chasing);
            }
        }

        private void TickChasing(float dt)
        {
            PlayerAnchor target = PlayerAnchor.Current;
            if (target == null || !target.IsAlive || !_agent.isOnNavMesh)
            {
                Stop();
                return;
            }
            Resume();

            if (HorizontalDistanceTo(target.Position) <= Definition.AttackRange)
            {
                EnterState(State.Attacking);
                return;
            }

            _repathTimer -= dt;
            if (_repathTimer <= 0f)
            {
                _repathTimer = RepathInterval;
                Vector3 destination = target.Position;
                if (!_agent.hasPath || (destination - _lastDestination).sqrMagnitude > RepathMinTargetMove * RepathMinTargetMove)
                {
                    _agent.SetDestination(destination);
                    _lastDestination = destination;
                }
            }
        }

        private void TickAttacking(float dt)
        {
            PlayerAnchor target = PlayerAnchor.Current;
            if (target == null || !target.IsAlive)
            {
                EnterState(State.Chasing);
                return;
            }

            FaceTowards(target.Position, dt);

            switch (_attack.Tick(dt))
            {
                case MeleeAttackCycle.Result.HitMoment:
                    if (HorizontalDistanceTo(target.Position) <= Definition.AttackRange + Definition.AttackReach)
                    {
                        Vector3 dir = (target.Position - transform.position).normalized;
                        target.Health.TakeDamage(new DamageInfo(Definition.AttackDamage, DamageType.Melee, gameObject,
                            target.Position + Vector3.up, dir));
                    }
                    break;

                case MeleeAttackCycle.Result.Finished:
                    if (HorizontalDistanceTo(target.Position) <= Definition.AttackRange) _attack.Begin();
                    else EnterState(State.Chasing);
                    break;
            }
        }

        private void TickDead()
        {
            float duration = Definition.CorpseDuration;
            float t = duration > 0f ? Mathf.Clamp01(_stateTimer / duration) : 1f;
            // Lie still for the first half, then sink.
            SetVisualDepth(EmergeDepth * Mathf.Clamp01((t - 0.5f) * 2f));
            if (t >= 1f) Despawn();
        }

        private void HandleDied(DamageInfo killingBlow)
        {
            if (CurrentState == State.Dead || CurrentState == State.Inactive) return;
            EnterState(State.Dead);
            Killed?.Invoke(this);
        }

        // ------------------------------------------------------------ helpers

        /// <summary>Returns the enemy to its pool (or destroys it if spawned without one).</summary>
        public void Despawn()
        {
            CurrentState = State.Inactive;
            Stop();
            SetVisualDepth(0f);
            Action<EnemyController> release = _release;
            _release = null;
            if (release != null) release(this);
            else Destroy(gameObject);
        }

        private void Stop()
        {
            if (!_agent.enabled || !_agent.isOnNavMesh) return;
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        private void Resume()
        {
            if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        }

        private float HorizontalDistanceTo(Vector3 point)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        private void FaceTowards(Vector3 point, float dt)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            Quaternion look = Quaternion.LookRotation(d);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, Definition.AngularSpeed * dt);
        }

        private void SetVisualDepth(float depth)
        {
            if (_visual == null) return;
            _visual.localPosition = _visualRestPosition + Vector3.down * depth;
        }

        private void SetCollidersEnabled(bool value)
        {
            foreach (Collider c in _hitColliders)
            {
                if (c != null) c.enabled = value;
            }
        }
    }
}
