using System;
using System.Collections;
using Project2048.Core;
using Project2048.Save;
using UnityEngine;

namespace Project2048.Flow
{
    public class FlowController : MonoBehaviour
    {
        private const int FirstStageIndex = 1;

        private GameContext gameContext;
        private SaveLoadManager saveLoadManager;
        private StageFlowController stageFlowController;
        private Coroutine restartStageCoroutine;

        public event Action OnLoadingStarted;
        public event Action OnMainMenuSceneLoadRequested;
        public event Action OnStorySceneLoadRequested;
        public event Action OnBattleSceneLoadRequested;
        public event Action OnGameStarted;

        public void Initialized(GameContext context, SaveLoadManager saveManager = null)
        {
            gameContext = context;
            saveLoadManager = saveManager;
        }

        public void SetNewGame()
        {
            if (gameContext == null)
            {
                Debug.LogError("GameContext is not initialized");
                return;
            }

            OnLoadingStarted?.Invoke();

            saveLoadManager?.DeleteSave();
            gameContext.SetGameState(GameContext.GameState.Loading);
            gameContext.SetStageIndex(FirstStageIndex);
            gameContext.SetRunActive(true);

            // TODO: PlayerManager.InitializeDefaultPlayer();
            // TODO: SaveLoadManager.PrepareNewSaveSlot();

            gameContext.SetGameState(GameContext.GameState.Story);
            OnStorySceneLoadRequested?.Invoke();
        }

        public void ContinueGame()
        {
            if (gameContext == null)
            {
                Debug.LogError("GameContext is not initialized");
                return;
            }

            OnLoadingStarted?.Invoke();

            gameContext.SetGameState(GameContext.GameState.Loading);

            if (saveLoadManager == null || !saveLoadManager.TryLoadGameContext())
            {
                Debug.LogWarning("No save data found.");
                gameContext.SetGameState(GameContext.GameState.MainMenu);
                return;
            }

            if (!gameContext.IsRunActive)
            {
                Debug.LogWarning("Save data does not contain an active run.");
                gameContext.SetGameState(GameContext.GameState.MainMenu);
                return;
            }

            OnBattleSceneLoadRequested?.Invoke();
        }

        public void CompleteOpeningStory()
        {
            if (gameContext == null)
            {
                Debug.LogError("GameContext is not initialized");
                return;
            }

            OnLoadingStarted?.Invoke();

            gameContext.SetGameState(GameContext.GameState.Loading);

            // TODO: Save story checkpoint if needed.
            saveLoadManager?.SaveInitialRun();

            OnBattleSceneLoadRequested?.Invoke();
        }

        public void CompleteBattleSceneLoad()
        {
            if (gameContext == null)
            {
                Debug.LogError("GameContext is not initialized");
                return;
            }

            gameContext.SetGameState(GameContext.GameState.Playing);
            OnGameStarted?.Invoke();
            StartCurrentStageFlow();
        }

        public void RequestMainMenu()
        {
            if (gameContext == null)
            {
                Debug.LogError("GameContext is not initialized");
                return;
            }

            OnLoadingStarted?.Invoke();
            gameContext.SetGameState(GameContext.GameState.Loading);
            OnMainMenuSceneLoadRequested?.Invoke();
        }

        private void StartCurrentStageFlow()
        {
            if (!ResolveStageFlowController())
            {
                Debug.LogError("StageFlowController is not present in the battle scene.");
                return;
            }

            if (saveLoadManager != null && saveLoadManager.TryApplyLoadedRunProgress(stageFlowController.RunProgress))
            {
                stageFlowController.InitializeRunProgress();
            }
            else if (gameContext.CurrentStageIndex == FirstStageIndex)
            {
                stageFlowController.ResetRunProgress();
            }

            stageFlowController.StartStage(gameContext.CurrentStageIndex);
        }

        private bool ResolveStageFlowController()
        {
            var resolvedController = FindStageFlowController();
            if (resolvedController == null)
            {
                return false;
            }

            if (stageFlowController == resolvedController)
            {
                return true;
            }

            UnbindStageFlowController();
            stageFlowController = resolvedController;
            BindStageFlowController();
            return true;
        }

        private static StageFlowController FindStageFlowController()
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindAnyObjectByType<StageFlowController>(FindObjectsInactive.Include);
#else
            return UnityEngine.Object.FindObjectOfType<StageFlowController>(true);
#endif
        }

        private void BindStageFlowController()
        {
            if (stageFlowController == null)
            {
                return;
            }

            stageFlowController.OnStageCompleted -= HandleStageCompleted;
            stageFlowController.OnStageFailed -= HandleStageFailed;
            stageFlowController.OnStageCompleted += HandleStageCompleted;
            stageFlowController.OnStageFailed += HandleStageFailed;
        }

        private void UnbindStageFlowController()
        {
            if (stageFlowController == null)
            {
                return;
            }

            stageFlowController.OnStageCompleted -= HandleStageCompleted;
            stageFlowController.OnStageFailed -= HandleStageFailed;
        }

        private void HandleStageCompleted(StageResult result)
        {
            if (gameContext == null || !gameContext.IsRunActive)
            {
                return;
            }

            if (result.RunCompleted)
            {
                gameContext.SetRunActive(false);
                gameContext.SetGameState(GameContext.GameState.Result);
                SaveCurrentRun();
                return;
            }

            gameContext.AdvanceStage();
            SaveCurrentRun();

            if (restartStageCoroutine != null)
            {
                StopCoroutine(restartStageCoroutine);
            }

            restartStageCoroutine = StartCoroutine(RestartStageNextFrame());
        }

        private IEnumerator RestartStageNextFrame()
        {
            yield return null;
            restartStageCoroutine = null;
            StartCurrentStageFlow();
        }

        private void HandleStageFailed()
        {
            if (restartStageCoroutine != null)
            {
                StopCoroutine(restartStageCoroutine);
                restartStageCoroutine = null;
            }

            if (gameContext == null)
            {
                return;
            }

            gameContext.SetRunActive(false);
            gameContext.SetGameState(GameContext.GameState.Result);
            saveLoadManager?.DeleteSave();
        }

        private void SaveCurrentRun()
        {
            if (saveLoadManager == null || stageFlowController == null)
            {
                return;
            }

            saveLoadManager.SaveRun(stageFlowController.RunProgress);
        }
    }
}
