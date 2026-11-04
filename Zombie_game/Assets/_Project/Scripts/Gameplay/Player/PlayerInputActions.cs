using System;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Default keyboard/mouse bindings, defined in code so the player works without wiring any asset.
    /// Later (settings menu, rebinding) this can be swapped for an .inputactions asset with the same action names.
    /// </summary>
    public sealed class PlayerInputActions : IDisposable
    {
        public InputActionMap Map { get; }

        public InputAction Move { get; }
        public InputAction Look { get; }
        public InputAction Jump { get; }
        public InputAction Sprint { get; }
        public InputAction Crouch { get; }
        public InputAction Fire { get; }
        public InputAction Reload { get; }
        public InputAction Interact { get; }
        public InputAction Weapon1 { get; }
        public InputAction Weapon2 { get; }
        public InputAction Weapon3 { get; }
        public InputAction ScrollWeapon { get; }
        public InputAction Pause { get; }

        public PlayerInputActions()
        {
            Map = new InputActionMap("Player");

            Move = Map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            Look = Map.AddAction("Look", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            Jump = Map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            Sprint = Map.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            Crouch = Map.AddAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
            Crouch.AddBinding("<Keyboard>/c");
            Fire = Map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            Reload = Map.AddAction("Reload", InputActionType.Button, "<Keyboard>/r");
            Interact = Map.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            Weapon1 = Map.AddAction("Weapon1", InputActionType.Button, "<Keyboard>/1");
            Weapon2 = Map.AddAction("Weapon2", InputActionType.Button, "<Keyboard>/2");
            Weapon3 = Map.AddAction("Weapon3", InputActionType.Button, "<Keyboard>/3");
            ScrollWeapon = Map.AddAction("ScrollWeapon", InputActionType.Value, "<Mouse>/scroll/y", expectedControlLayout: "Axis");
            Pause = Map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        }

        public void Enable() => Map.Enable();
        public void Disable() => Map.Disable();

        public void Dispose()
        {
            Map.Disable();
            Map.Dispose();
        }
    }
}
