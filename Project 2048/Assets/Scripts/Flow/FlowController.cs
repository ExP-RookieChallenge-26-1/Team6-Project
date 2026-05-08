using Project2048.Core;
using System;
using UnityEngine;

namespace Project2048.Flow
{
    public class FlowController : MonoBehaviour
    {
        private GameContext gameContext;

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
            // TODO: Set first stage ID when stage data is ready.
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
    }
}
