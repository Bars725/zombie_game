using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Single place where player input is read. Other player components ask this reader,
    /// never the Input System directly, so input can later be blocked (menus, death, cutscenes) in one place.
    ///
    /// Cursor: locked while playing. Esc unlocks; left click locks again (that click does not shoot).
    /// While the cursor is unlocked, gameplay input reads as zero.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private bool _lockCursorOnStart = true;

        private PlayerInputActions _actions;
        private int _lockedFrame = -1;

        /// <summary>False while the cursor is free or input is blocked by code.</summary>
        public bool GameplayActive => !Blocked && Cursor.lockState == CursorLockMode.Locked && _lockedFrame != Time.frameCount;

        /// <summary>Set by menus, death screen, etc.</summary>
        public bool Blocked { get; set; }

        public Vector2 Move => GameplayActive ? Vector2.ClampMagnitude(_actions.Move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        public Vector2 LookDelta => GameplayActive ? _actions.Look.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpPressed => GameplayActive && _actions.Jump.WasPressedThisFrame();
        public bool SprintHeld => GameplayActive && _actions.Sprint.IsPressed();
        public bool CrouchHeld => GameplayActive && _actions.Crouch.IsPressed();
        public bool FireHeld => GameplayActive && _actions.Fire.IsPressed();
        public bool FirePressed => GameplayActive && _actions.Fire.WasPressedThisFrame();
        public bool ReloadPressed => GameplayActive && _actions.Reload.WasPressedThisFrame();
        public bool InteractPressed => GameplayActive && _actions.Interact.WasPressedThisFrame();

        /// <summary>0-based slot pressed this frame (keys 1..3), or -1.</summary>
        public int WeaponSlotPressed
        {
            get
            {
                if (!GameplayActive) return -1;
                if (_actions.Weapon1.WasPressedThisFrame()) return 0;
                if (_actions.Weapon2.WasPressedThisFrame()) return 1;
                if (_actions.Weapon3.WasPressedThisFrame()) return 2;
                return -1;
            }
        }

        /// <summary>+1 / -1 when the mouse wheel moved this frame, else 0.</summary>
        public int WeaponScroll
        {
            get
            {
                if (!GameplayActive) return 0;
                float y = _actions.ScrollWeapon.ReadValue<float>();
                return y > 0.01f ? 1 : y < -0.01f ? -1 : 0;
            }
        }

        private void Awake()
        {
            _actions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            _actions.Enable();
            if (_lockCursorOnStart) LockCursor();
        }

        private void OnDisable()
        {
            _actions.Disable();
            UnlockCursor();
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
        }

        private void Update()
        {
            if (_actions.Pause.WasPressedThisFrame())
            {
                UnlockCursor();
            }
            else if (Cursor.lockState != CursorLockMode.Locked && !Blocked && _actions.Fire.WasPressedThisFrame())
            {
                LockCursor();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) UnlockCursor();
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _lockedFrame = Time.frameCount; // swallow the click that locked the cursor
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
