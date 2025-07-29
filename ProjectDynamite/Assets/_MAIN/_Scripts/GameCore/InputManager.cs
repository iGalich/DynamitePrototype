using UnityEngine;
using UnityEngine.InputSystem;

namespace Galich.GameCore
{
    public class InputManager : MonoBehaviour
    {
        private enum InputActions
        {
            Move, Jump, Run,
        }

        private static PlayerInput _playerInput;
        private static Vector2 _movement;
        private static bool _jumpWasPressed;
        private static bool _jumpIsHeld;
        private static bool _jumpWasReleased;
        private static bool _runIsHeld;

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _runAction;
        private PlayerInputActions _inputActions;

        public static PlayerInput PlayerInput => _playerInput;
        public static Vector2 Movement => _movement;
        public static bool JumpWasPressed => _jumpWasPressed;
        public static bool JumpIsHeld => _jumpIsHeld;
        public static bool JumpWasReleased => _jumpWasReleased;
        public static bool RunIsHeld => _runIsHeld;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();

            _moveAction = _playerInput.actions[$"{InputActions.Move}"];
            _jumpAction = _playerInput.actions[$"{InputActions.Jump}"];
            _runAction = _playerInput.actions[$"{InputActions.Run}"];

        }

        private void Update()
        {
            _movement = _moveAction.ReadValue<Vector2>();

            _jumpWasPressed = _jumpAction.WasPressedThisFrame();
            _jumpIsHeld = _jumpAction.IsPressed();
            _jumpWasReleased = _jumpAction.WasReleasedThisFrame();

            _runIsHeld = _runAction.IsPressed();
        }
    }
}