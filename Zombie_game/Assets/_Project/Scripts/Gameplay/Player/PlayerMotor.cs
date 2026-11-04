using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// First-person movement on a CharacterController (kinematic, no physics forces):
    /// walk / sprint (stamina) / crouch (hold) / jump, gravity, ceiling check before standing up.
    /// The transform pivot is at the feet; the camera pivot follows the capsule height.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        private const float GroundStickVelocity = -2f;

        [SerializeField] private PlayerMovementConfig _config;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private Transform _cameraPivot;
        [Tooltip("Layers that block standing up / count as ceiling")]
        [SerializeField] private LayerMask _obstacleMask = Physics.DefaultRaycastLayers;

        private CharacterController _controller;
        private StaminaModel _stamina;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _currentHeight;

        public StaminaModel Stamina => _stamina;
        public bool IsGrounded { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public float HorizontalSpeed => _horizontalVelocity.magnitude;
        public PlayerMovementConfig Config => _config;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_config == null)
            {
                Debug.LogError($"{nameof(PlayerMotor)} on '{name}' has no {nameof(PlayerMovementConfig)}. Using defaults.", this);
                _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            }

            _stamina = new StaminaModel(_config.StaminaMax, _config.StaminaDrainPerSecond,
                _config.StaminaRegenPerSecond, _config.StaminaRegenDelay, _config.StaminaResumeFraction);

            _currentHeight = _config.StandHeight;
            ApplyHeight(_currentHeight);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector2 move = _input != null ? _input.Move : Vector2.zero;
            bool grounded = _controller.isGrounded;

            UpdateCrouch(dt);

            bool wantsSprint = _input != null && _input.SprintHeld && move.y > 0.1f && !IsCrouching;
            IsSprinting = _stamina.Tick(dt, wantsSprint);

            float targetSpeed = IsCrouching ? _config.CrouchSpeed
                : IsSprinting ? _config.SprintSpeed
                : _config.WalkSpeed;

            Vector3 wish = (transform.right * move.x + transform.forward * move.y) * targetSpeed;
            float accel = grounded ? _config.GroundAcceleration : _config.AirAcceleration;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, wish, accel * dt);

            if (grounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = GroundStickVelocity;
                if (_input != null && _input.JumpPressed && !IsCrouching)
                {
                    _verticalVelocity = Mathf.Sqrt(2f * _config.Gravity * _config.JumpHeight);
                }
            }
            _verticalVelocity -= _config.Gravity * dt;

            Vector3 motion = (_horizontalVelocity + Vector3.up * _verticalVelocity) * dt;
            CollisionFlags flags = _controller.Move(motion);

            if ((flags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f) _verticalVelocity = 0f;
            IsGrounded = _controller.isGrounded;
        }

        private void UpdateCrouch(float dt)
        {
            bool wantsCrouch = _input != null && _input.CrouchHeld;
            if (wantsCrouch) IsCrouching = true;
            else if (IsCrouching && CanStandUp()) IsCrouching = false;

            float target = IsCrouching ? _config.CrouchHeight : _config.StandHeight;
            if (!Mathf.Approximately(_currentHeight, target))
            {
                _currentHeight = Mathf.MoveTowards(_currentHeight, target, _config.CrouchTransitionSpeed * dt);
                ApplyHeight(_currentHeight);
            }
        }

        private bool CanStandUp()
        {
            float radius = _controller.radius * 0.95f;
            float castDistance = _config.StandHeight - _currentHeight + _controller.skinWidth;
            if (castDistance <= 0f) return true;

            Vector3 origin = transform.position + Vector3.up * (_currentHeight - radius);
            int mask = _obstacleMask & ~(1 << gameObject.layer);
            return !Physics.SphereCast(origin, radius, Vector3.up, out _, castDistance, mask, QueryTriggerInteraction.Ignore);
        }

        private void ApplyHeight(float height)
        {
            _controller.height = height;
            _controller.center = new Vector3(0f, height * 0.5f, 0f);
            if (_cameraPivot != null)
            {
                Vector3 p = _cameraPivot.localPosition;
                p.y = height - _config.EyeOffsetFromTop;
                _cameraPivot.localPosition = p;
            }
        }

        /// <summary>Teleport safely (CharacterController overrides transform.position while enabled).</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _controller.enabled = true;
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
        }
    }
}
