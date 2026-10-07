using UnityEngine;
using UnityEngine.InputSystem;

namespace BlobGame.Player
{
    public sealed class BlobInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";

        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction jumpAction;

        public float MoveX
        {
            get
            {
                if (moveAction != null)
                    return moveAction.ReadValue<Vector2>().x;

                float keyboard = 0f;
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                        keyboard -= 1f;
                    if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                        keyboard += 1f;
                }

                return keyboard;
            }
        }

        public bool JumpPressed
        {
            get
            {
                if (jumpAction != null)
                    return jumpAction.WasPressedThisFrame();

                return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            }
        }

        public bool JumpReleased
        {
            get
            {
                if (jumpAction != null)
                    return jumpAction.WasReleasedThisFrame();

                return Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame;
            }
        }

        private void OnEnable()
        {
            ResolveActions();
            playerMap?.Enable();
        }

        private void OnDisable()
        {
            playerMap?.Disable();
        }

        private void ResolveActions()
        {
            if (inputActions == null)
                return;

            playerMap = inputActions.FindActionMap(actionMapName, false);
            moveAction = playerMap?.FindAction(moveActionName, false);
            jumpAction = playerMap?.FindAction(jumpActionName, false);
        }
    }
}
