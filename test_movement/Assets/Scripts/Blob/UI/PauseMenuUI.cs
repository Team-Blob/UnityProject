using UnityEngine;

namespace BlobGame.UI
{
    /// <summary>Forwards the pause panel buttons to the scene's UI controller.</summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private UIController uiController;

        public void ResetLevel()
        {
            if (uiController != null)
                uiController.ReloadLevel();
        }

        public void BackToMenu()
        {
            if (uiController != null)
                uiController.BackToMenu();
        }
    }
}
