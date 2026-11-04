using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Mouse look: yaw rotates the player body, pitch rotates the camera pivot.
    /// Mouse delta is already "per frame", so it is NOT multiplied by deltaTime.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private Transform _cameraPivot;

        [Tooltip("Degrees per mouse count")]
        [SerializeField, Range(0.01f, 1f)] private float _sensitivity = 0.1f;
        [SerializeField] private bool _invertY;
        [SerializeField, Range(-89f, 0f)] private float _minPitch = -85f;
        [SerializeField, Range(0f, 89f)] private float _maxPitch = 85f;

        private float _pitch;

        public float Sensitivity
        {
            get => _sensitivity;
            set => _sensitivity = Mathf.Clamp(value, 0.01f, 1f);
        }

        public float Pitch => _pitch;

        private void Update()
        {
            if (_input == null || _cameraPivot == null) return;

            Vector2 delta = _input.LookDelta * _sensitivity;
            if (delta.sqrMagnitude <= 0f) return;

            transform.Rotate(0f, delta.x, 0f, Space.Self);

            _pitch += _invertY ? delta.y : -delta.y;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
            _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
