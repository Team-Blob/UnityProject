using UnityEngine;

namespace BlobGame.UI
{
    /// <summary>Returns from the help panel to the menu that opened it.</summary>
    [DisallowMultipleComponent]
    public sealed class ControlsUI : MonoBehaviour
    {
        [SerializeField] private UIController uiController;

        public void Back()
        {
            if (uiController != null)
                uiController.HideControls();
        }
    }
}
