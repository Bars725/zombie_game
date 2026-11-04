using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>Movement and stamina tuning. Data only; read by PlayerMotor.</summary>
    [CreateAssetMenu(menuName = "Game/Player/Movement Config", fileName = "PMC_New")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("Speed (m/s)")]
        [Min(0f)] public float WalkSpeed = 4.5f;
        [Min(0f)] public float SprintSpeed = 7f;
        [Min(0f)] public float CrouchSpeed = 2.2f;

        [Header("Acceleration (m/s^2)")]
        [Min(0f)] public float GroundAcceleration = 50f;
        [Min(0f)] public float AirAcceleration = 10f;

        [Header("Jump / gravity")]
        [Min(0f)] public float JumpHeight = 1.1f;
        [Min(0.1f)] public float Gravity = 20f;
        [Tooltip("Jump still allowed this long after leaving the ground (s)")]
        [Min(0f)] public float CoyoteTime = 0.12f;
        [Tooltip("A jump pressed this long before landing still triggers (s)")]
        [Min(0f)] public float JumpBufferTime = 0.15f;

        [Header("Body (pivot at feet)")]
        [Min(0.5f)] public float StandHeight = 1.8f;
        [Min(0.5f)] public float CrouchHeight = 1.1f;
        [Tooltip("Height change speed, m/s")]
        [Min(0.1f)] public float CrouchTransitionSpeed = 6f;
        [Tooltip("Camera distance below the top of the capsule")]
        [Min(0f)] public float EyeOffsetFromTop = 0.15f;

        [Header("Stamina (sprint only)")]
        [Min(1f)] public float StaminaMax = 100f;
        [Min(0f)] public float StaminaDrainPerSecond = 20f;
        [Min(0f)] public float StaminaRegenPerSecond = 25f;
        [Min(0f)] public float StaminaRegenDelay = 1f;
        [Tooltip("After running dry, sprint is blocked until stamina is back to this fraction")]
        [Range(0f, 1f)] public float StaminaResumeFraction = 0.3f;

        private void OnValidate()
        {
            if (CrouchHeight > StandHeight) CrouchHeight = StandHeight;
        }
    }
}
