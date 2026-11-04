using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.DevTools
{
    /// <summary>Shooting-range target: flashes when hit, turns dark when killed, respawns after a delay.</summary>
    [RequireComponent(typeof(Health))]
    public sealed class TargetDummy : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit
        private static readonly int ColorId = Shader.PropertyToID("_Color");         // fallback

        [SerializeField, Min(0f)] private float _respawnDelay = 2f;
        [SerializeField] private Color _aliveColor = new Color(0.85f, 0.55f, 0.2f);
        [SerializeField] private Color _hitColor = Color.white;
        [SerializeField] private Color _deadColor = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField, Min(0f)] private float _flashDuration = 0.08f;

        private Health _health;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private float _flashTimer;
        private float _respawnTimer;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _renderers = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
            SetColor(_aliveColor);
        }

        private void OnEnable()
        {
            _health.Damaged += HandleDamaged;
            _health.Died += HandleDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= HandleDamaged;
            _health.Died -= HandleDied;
        }

        private void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && _health.IsAlive) SetColor(_aliveColor);
            }

            if (_respawnTimer > 0f)
            {
                _respawnTimer -= Time.deltaTime;
                if (_respawnTimer <= 0f)
                {
                    _health.ResetHealth();
                    SetColor(_aliveColor);
                }
            }
        }

        private void HandleDamaged(DamageInfo damage, float applied)
        {
            if (!_health.IsAlive) return;
            _flashTimer = _flashDuration;
            SetColor(_hitColor);
        }

        private void HandleDied(DamageInfo killingBlow)
        {
            _flashTimer = 0f;
            _respawnTimer = Mathf.Max(0.01f, _respawnDelay);
            SetColor(_deadColor);
        }

        private void SetColor(Color color)
        {
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            foreach (Renderer r in _renderers) r.SetPropertyBlock(_block);
        }
    }
}
