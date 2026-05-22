using System;
using System.Collections;
using Project2048.Core;
using UnityEngine;

namespace Project2048.Flow
{
    public class FlowController : MonoBehaviour
    {
        private const int FirstStageIndex = 1;

        private GameContext gameContext;
        private StageFlowController stageFlowController;
        private Coroutine restartStageCoroutine;

        public event Action OnLoadingStarted;
        public event Action OnMainMenuSceneLoadRequested;
        public event Action OnStorySceneLoadRequested;
        public event Action OnBattleSceneLoadRequested;
        public event Action OnGameStarted;

        public void Initialized(GameContext context)
        {
            gameContext = context;
        }

        public void SetNewGame()
        {
            if (gameContext == null)
            {
                Debug.LogError("GameContext is not initialized");
                return;
            }

            OnLoadingStarted?.Invoke();

            gameContext.SetGameState(GameContext.GameState.Loading);
            gameContext.SetStageIndex(FirstStageIndex);
            gameContext.SetScore(0);
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

            // TODO: SaveLoadManager.Load();
            // TODO: Restore GameContext from save data.

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
                return;
            }

            gameContext.AdvanceStage();

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
        }
    }
}
