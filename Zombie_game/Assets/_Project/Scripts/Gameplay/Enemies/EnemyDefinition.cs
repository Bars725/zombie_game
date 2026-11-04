using Game.Gameplay.Audio;
using UnityEngine;

namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Design data for one enemy type (Husk, Runner...). Read-only at runtime.
    /// The prefab holds the visuals, colliders, NavMeshAgent and EnemyController;
    /// the definition supplies the numbers on every spawn, so one prefab can serve several variants.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Enemies/Enemy Definition", fileName = "ED_New")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "enemy";
        [SerializeField] private string _displayName = "Enemy";
        [SerializeField] private EnemyController _prefab;

        [Header("Stats")]
        [SerializeField, Min(1f)] private float _maxHealth = 100f;
        [SerializeField, Min(0.1f)] private float _moveSpeed = 2.5f;
        [SerializeField, Min(0.1f)] private float _acceleration = 12f;
        [SerializeField, Min(10f)] private float _angularSpeed = 540f;

        [Header("Melee attack")]
        [SerializeField, Min(0f)] private float _attackDamage = 20f;
        [Tooltip("Start an attack when the target is this close (horizontal metres)")]
        [SerializeField, Min(0.1f)] private float _attackRange = 1.6f;
        [Tooltip("Extra distance the target may move away during the windup and still be hit")]
        [SerializeField, Min(0f)] private float _attackReach = 0.5f;
        [Tooltip("Telegraph time before the hit lands (s)")]
        [SerializeField, Min(0f)] private float _attackWindup = 0.45f;
        [Tooltip("Time after the hit before the next action (s)")]
        [SerializeField, Min(0f)] private float _attackRecovery = 0.8f;

        [Header("Spawning")]
        [Tooltip("Time spent emerging before it starts chasing (s)")]
        [SerializeField, Min(0f)] private float _spawnDuration = 1f;
        [Tooltip("Corpse stays this long before returning to the pool (s)")]
        [SerializeField, Min(0f)] private float _corpseDuration = 1.5f;

        [Header("Audio")]
        [Tooltip("Played when it finishes emerging and starts the chase")]
        [SerializeField] private SoundCue _alertSound;
        [SerializeField] private SoundCue _attackSound;
        [SerializeField] private SoundCue _deathSound;

        [Header("Waves / economy")]
        [Tooltip("Wave budget points this enemy costs")]
        [SerializeField, Min(1)] private int _spawnCost = 1;
        [SerializeField, Min(0)] private int _scrapOnKill = 60;

        public string Id => _id;
        public string DisplayName => _displayName;
        public EnemyController Prefab => _prefab;
        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public float AngularSpeed => _angularSpeed;
        public float AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;
        public float AttackReach => _attackReach;
        public float AttackWindup => _attackWindup;
        public float AttackRecovery => _attackRecovery;
        public float SpawnDuration => _spawnDuration;
        public float CorpseDuration => _corpseDuration;
        public int SpawnCost => _spawnCost;
        public int ScrapOnKill => _scrapOnKill;
        public SoundCue AlertSound => _alertSound;
        public SoundCue AttackSound => _attackSound;
        public SoundCue DeathSound => _deathSound;

        internal void Initialize(string id, string displayName, EnemyController prefab, float maxHealth, float moveSpeed,
            float attackDamage, float attackRange = 1.6f, float attackWindup = 0.45f, float attackRecovery = 0.8f,
            float acceleration = 12f, int spawnCost = 1, int scrapOnKill = 60, float spawnDuration = 1f)
        {
            _id = id;
            _displayName = displayName;
            _prefab = prefab;
            _maxHealth = maxHealth;
            _moveSpeed = moveSpeed;
            _attackDamage = attackDamage;
            _attackRange = attackRange;
            _attackWindup = attackWindup;
            _attackRecovery = attackRecovery;
            _acceleration = acceleration;
            _spawnCost = spawnCost;
            _scrapOnKill = scrapOnKill;
            _spawnDuration = spawnDuration;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id)) _id = name;
        }
    }
}
