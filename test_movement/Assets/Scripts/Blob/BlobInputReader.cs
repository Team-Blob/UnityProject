using UnityEngine;
using UnityEngine.InputSystem;

namespace BlobGame.Player
{
    /// <summary>
    /// Converts Unity Input System actions into player movement, jump, and sprint intent.
    /// Falls back to direct keyboard input when no InputActionAsset is assigned.
    /// </summary>
    public sealed class BlobInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string sprintActionName = "Sprint";

        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction sprintAction;

        /// <summary>
        /// Horizontal movement input in the range -1 to 1.
        /// </summary>
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

        /// <summary>
        /// True only on the frame when the jump action is pressed.
        /// </summary>
        public bool JumpPressed
        {
            get
            {
                if (jumpAction != null)
                    return jumpAction.WasPressedThisFrame();

                return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            }
        }

        /// <summary>
        /// True only on the frame when the jump action is released.
        /// </summary>
        public bool JumpReleased
        {
            get
            {
                if (jumpAction != null)
                    return jumpAction.WasReleasedThisFrame();

                return Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame;
            }
        }

        /// <summary>
        /// True only on the frame when the sprint action is pressed.
        /// </summary>
        public bool SprintPressed
        {
            get
            {
                if (sprintAction != null)
                    return sprintAction.WasPressedThisFrame();

                return Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
            }
        }

        /// <summary>
        /// True while the sprint action remains held.
        /// </summary>
        public bool SprintHeld
        {
            get
            {
                if (sprintAction != null)
                    return sprintAction.IsPressed();

                return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
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

            // Resolve actions by name so this component can use a shared InputActionAsset.
            playerMap = inputActions.FindActionMap(actionMapName, false);
            moveAction = playerMap?.FindAction(moveActionName, false);
            jumpAction = playerMap?.FindAction(jumpActionName, false);
            sprintAction = playerMap?.FindAction(sprintActionName, false);
        }
    }
}
