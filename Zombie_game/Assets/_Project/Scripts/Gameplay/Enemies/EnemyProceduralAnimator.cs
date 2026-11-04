using UnityEngine;

namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Stand-in animation until real (Mixamo) clips exist: rotates the enemy's Visual transform.
    ///   - walking: side-to-side sway + forward lean scaled by speed (shamble)
    ///   - attack windup: leans back (readable telegraph), then lunges forward on the hit
    ///   - dead: tips over
    /// Only rotation is touched; EnemyController owns Visual's position (emerge / sink).
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyProceduralAnimator : MonoBehaviour
    {
        [SerializeField] private float _swayDegrees = 6f;
        [SerializeField] private float _stepsPerMetre = 0.9f;
        [SerializeField] private float _moveLeanDegrees = 8f;
        [SerializeField] private float _windupLeanBack = 14f;
        [SerializeField] private float _lungeDegrees = 28f;
        [SerializeField] private float _deathTipDegrees = 80f;
        [SerializeField] private float _blendSpeed = 12f;

        private EnemyController _enemy;
        private float _phase;
        private Vector3 _current; // pitch, yaw, roll

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
        }

        private void LateUpdate()
        {
            Transform visual = _enemy.Visual;
            if (visual == null) return;

            float dt = Time.deltaTime;
            float speed = _enemy.Agent != null && _enemy.Agent.enabled ? _enemy.Agent.velocity.magnitude : 0f;
            float maxSpeed = _enemy.Definition != null ? Mathf.Max(0.1f, _enemy.Definition.MoveSpeed) : 1f;
            float moveFactor = Mathf.Clamp01(speed / maxSpeed);

            _phase += speed * _stepsPerMetre * Mathf.PI * 2f * dt;

            Vector3 target = Vector3.zero;
            switch (_enemy.CurrentState)
            {
                case EnemyController.State.Chasing:
                    target.x = _moveLeanDegrees * moveFactor;
                    target.z = Mathf.Sin(_phase) * _swayDegrees * moveFactor;
                    break;

                case EnemyController.State.Attacking:
                    float p = _enemy.AttackPhaseProgress;
                    if (_enemy.AttackPhase == MeleeAttackCycle.Phase.Windup)
                    {
                        target.x = -_windupLeanBack * Mathf.SmoothStep(0f, 1f, p);
                    }
                    else if (_enemy.AttackPhase == MeleeAttackCycle.Phase.Recovery)
                    {
                        // Snap forward right after the hit, then settle back.
                        target.x = _lungeDegrees * (1f - Mathf.SmoothStep(0f, 1f, p));
                    }
                    break;

                case EnemyController.State.Dead:
                    target.x = _deathTipDegrees;
                    break;
            }

            float blend = _enemy.CurrentState == EnemyController.State.Attacking ? _blendSpeed * 2f : _blendSpeed;
            _current = Vector3.Lerp(_current, target, 1f - Mathf.Exp(-blend * dt));
            if (_enemy.CurrentState == EnemyController.State.Spawning || _enemy.CurrentState == EnemyController.State.Inactive)
            {
                _current = Vector3.zero;
            }
            visual.localRotation = Quaternion.Euler(_current.x, 0f, _current.z);
        }
    }
}
