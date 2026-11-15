using UnityEngine;

namespace BlobGame.UI
{
    /// <summary>Forwards the goal popup buttons without owning gameplay state.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelCompleteUI : MonoBehaviour
    {
        [SerializeField] private UIController uiController;

        public void Continue()
        {
            if (uiController != null)
                uiController.ContinueLevel();
        }

        public void BackToMenu()
        {
            if (uiController != null)
                uiController.BackToMenu();
        }
    }
}
