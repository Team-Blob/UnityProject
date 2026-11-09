using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BlobGame.UI
{
    /// <summary>Owns scene-local pause state and scene navigation.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class UIController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;

        private const string MainMenuScene = "MainMenu";
        private const string FirstLevelScene = "First_Level";
        private float timeScaleBeforePause;
        private bool sceneChangePending;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        private void Update()
        {
            if (pausePanel != null && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                TogglePause();
        }

        private void OnDisable()
        {
            // Disabling or unloading this UI must not leave the next scene frozen.
            SetPaused(false);
        }

        public void TogglePause()
        {
            if (pausePanel != null && !sceneChangePending)
                SetPaused(!IsPaused);
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

        public void PlayFirstLevel() => LoadScene(FirstLevelScene);

        public void ExitGame()
        {
            if (!sceneChangePending)
                Application.Quit();
        }

        private void SetPaused(bool paused)
        {
            if (IsPaused == paused)
                return;

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

            if (pausePanel != null)
                pausePanel.SetActive(paused);
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
            SetPaused(false);
            return true;
        }
    }
}
