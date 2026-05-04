using Project2048.Core;
using System;
using UnityEngine;

namespace Project2048.Flow
{
    public class FlowController : MonoBehaviour
    {
        private GameContext gameContext;

        public event Action OnLoadingStarted;
        public event Action OnNewGameStoryStarted;
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
            // gameContext.SetStageId("첫 스테이지");    첫 스테이지 정해지면 설정
            gameContext.SetScore(0);
            gameContext.SetRunActive(true);

            // 나중에 PlayerManager.InitializeDefaultPlayer();
            // 나중에 ScoreManager.ResetScore();
            // 나중에 SaveLoadManager.PrepareNewSaveSlot();

            gameContext.SetGameState(GameContext.GameState.Story);

            OnNewGameStoryStarted?.Invoke();
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

            // 나중에 SaveLoadManager.Load();
            // 로드 성공 시 GameContext에 저장 데이터 반영
            // gameContext.SetStageId(saveData.currentStageId);
            // gameContext.SetScore(saveData.currentScore);
            // gameContext.SetRunActive(true);

            gameContext.SetGameState(GameContext.GameState.Playing);

            OnGameStarted?.Invoke();
        }

        public void StartGameAfterStory()
        {
            if (gameContext == null)
            {
                Debug.LogError("GameContext is not initialized");
                return;
            }

            OnLoadingStarted?.Invoke();

            gameContext.SetGameState(GameContext.GameState.Loading);

            // 나중에 스토리 완료 저장이 필요하면 여기서 SaveLoadManager.Save();

            gameContext.SetGameState(GameContext.GameState.Playing);

            OnGameStarted?.Invoke();
        }
    }
}
