using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project2048.Flow
{
    public class SceneFlowManager : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string StorySceneName = "StoryScene";
        private const string InGameSceneName = "BattleScene";

        [SerializeField] private FlowController flowController;
        [SerializeField] private float minimumLoadingSeconds = 2f;

        public event Action<float> OnLoadProgressChanged;
        public event Action OnSceneLoadCompleted;

        private void Awake()
        {
            flowController ??= GetComponent<FlowController>();
        }

        private void OnEnable()
        {
            if (flowController == null)
            {
                return;
            }

            flowController.OnNewGameStoryStarted += LoadStory;
            flowController.OnGameStarted += LoadInGame;
        }

        private void OnDisable()
        {
            if (flowController == null)
            {
                return;
            }

            flowController.OnNewGameStoryStarted -= LoadStory;
            flowController.OnGameStarted -= LoadInGame;
        }

        public void LoadMainMenu()
        {
            LoadScene(MainMenuSceneName);
        }

        public void LoadStory()
        {
            LoadScene(StorySceneName);
        }

        public void LoadInGame()
        {
            LoadScene(InGameSceneName);
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("Scene name is empty.");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            var startTime = Time.unscaledTime;
            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"Scene load failed: {sceneName}");
                yield break;
            }

            while (!operation.isDone)
            {
                var progress = Mathf.Clamp01(operation.progress / 0.9f);
                OnLoadProgressChanged?.Invoke(progress);
                yield return null;
            }

            var elapsedTime = Time.unscaledTime - startTime;
            var remainingTime = minimumLoadingSeconds - elapsedTime;
            if (remainingTime > 0f)
            {
                yield return new WaitForSecondsRealtime(remainingTime);
            }

            OnLoadProgressChanged?.Invoke(1f);
            OnSceneLoadCompleted?.Invoke();
        }
    }
}
