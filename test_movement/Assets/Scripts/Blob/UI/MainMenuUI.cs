using UnityEngine;

namespace BlobGame.UI
{
    /// <summary>Forwards the title screen buttons to the scene's UI controller.</summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private UIController uiController;

        public void Play()
        {
            if (uiController != null)
                uiController.PlayFirstLevel();
        }

        public void Exit()
        {
            if (uiController != null)
                uiController.ExitGame();
        }
    }
}
