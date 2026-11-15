using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BlobGame.UI
{
    /// <summary>Owns scene-local menus, gameplay pause, and scene navigation.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class UIController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private string playSceneName = "LevelDesignTest";

        private const string MainMenuScene = "MainMenu";
        private float timeScaleBeforePause;
        private bool sceneChangePending;
        private bool mainMenuWasVisible;
        private bool pauseMenuWasVisible;

        public bool IsPaused { get; private set; }
        public bool ControlsAreOpen { get; private set; }
        public bool IsLevelComplete { get; private set; }

        private void Awake()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);
        }

        private void Update()
        {
            if (sceneChangePending || Keyboard.current?.escapeKey.wasPressedThisFrame != true)
                return;

            // Closing help returns to its parent menu without resuming gameplay.
            if (ControlsAreOpen)
                HideControls();
            else if (IsLevelComplete)
                ContinueLevel();
            else
                TogglePause();
        }

        private void OnDisable()
        {
            // Disabling or unloading this UI must not leave the next scene frozen.
            CloseOverlays();
        }

        public void TogglePause()
        {
            if (sceneChangePending || IsLevelComplete)
                return;

            if (ControlsAreOpen)
            {
                HideControls();
                return;
            }

            if (pausePanel != null)
                SetPaused(!IsPaused);
        }

        public void ShowControls()
        {
            if (controlsPanel == null || sceneChangePending || ControlsAreOpen || IsLevelComplete)
                return;

            mainMenuWasVisible = mainMenuPanel != null && mainMenuPanel.activeSelf;
            pauseMenuWasVisible = pausePanel != null && pausePanel.activeSelf;
            ControlsAreOpen = true;

            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
            if (pausePanel != null)
                pausePanel.SetActive(false);
            controlsPanel.SetActive(true);
        }

        public void HideControls()
        {
            if (!ControlsAreOpen)
                return;

            ControlsAreOpen = false;
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(mainMenuWasVisible);
            if (pausePanel != null)
                pausePanel.SetActive(pauseMenuWasVisible && IsPaused && !IsLevelComplete);
        }

        /// <summary>Shows a goal popup once the level's goal trigger reports success.</summary>
        public void ShowLevelComplete()
        {
            if (levelCompletePanel == null || sceneChangePending || IsLevelComplete)
                return;

            HideControls();
            IsLevelComplete = true;
            SetPaused(true);
            levelCompletePanel.SetActive(true);
        }

        public void ContinueLevel()
        {
            if (!IsLevelComplete || sceneChangePending)
                return;

            IsLevelComplete = false;
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);
            SetPaused(false);
        }

        public void ReloadLevel()
        {
            Scene scene = SceneManager.GetActiveScene();
            int buildIndex = scene.buildIndex;
            bool canReloadFromBuild = buildIndex >= 0
                && SceneUtility.GetScenePathByBuildIndex(buildIndex) == scene.path
                && Application.CanStreamedLevelBeLoaded(buildIndex);

#if UNITY_EDITOR
            // The sandbox level is deliberately excluded from the build scene list.
            if (!canReloadFromBuild && scene.IsValid() && !string.IsNullOrEmpty(scene.path))
            {
                if (TryBeginSceneChange(true, scene.path))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                        scene.path, new LoadSceneParameters(LoadSceneMode.Single));
                }
                return;
            }
#endif

            if (TryBeginSceneChange(canReloadFromBuild, scene.name))
                SceneManager.LoadScene(buildIndex);
        }

        public void BackToMenu() => LoadScene(MainMenuScene);

        // Keep this method name so existing persistent button listeners remain valid.
        public void PlayFirstLevel() => LoadScene(playSceneName);

        public void ExitGame()
        {
            if (!sceneChangePending)
                Application.Quit();
        }

        private void SetPaused(bool paused)
        {
            if (IsPaused != paused)
            {
                IsPaused = paused;
                if (paused)
                {
                    timeScaleBeforePause = Time.timeScale;
                    Time.timeScale = 0f;
                }
                else
                {
                    Time.timeScale = timeScaleBeforePause;
                }
            }

            if (pausePanel != null)
                pausePanel.SetActive(paused && !ControlsAreOpen && !IsLevelComplete);
        }

        private void CloseOverlays()
        {
            HideControls();
            IsLevelComplete = false;
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);
            SetPaused(false);
        }

        private void LoadScene(string sceneName)
        {
            if (TryBeginSceneChange(Application.CanStreamedLevelBeLoaded(sceneName), sceneName))
                SceneManager.LoadScene(sceneName);
        }

        private bool TryBeginSceneChange(bool canLoad, string sceneName)
        {
            if (sceneChangePending)
                return false;

            if (!canLoad)
            {
                Debug.LogWarning($"Scene '{sceneName}' is not available to load.", this);
                return false;
            }

            sceneChangePending = true;
            CloseOverlays();
            return true;
        }
    }
}
