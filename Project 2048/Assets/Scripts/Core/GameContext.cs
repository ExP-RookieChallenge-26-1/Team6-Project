using System;
using UnityEngine;

namespace Project2048.Core
{
    public class GameContext
    {
        public enum GameState
        {
            MainMenu,
            Loading,
            Story,
            Playing,
            Paused,
            Result
        }

        GameState currentGameState;
        int currentStageIndex = 1;
        int currentScore;
        bool isRunActive;

        public GameState CurrentGameState => currentGameState;
        public int CurrentStageIndex => currentStageIndex;
        public int CurrentScore => currentScore;
        public bool IsRunActive => isRunActive;

        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnStageIndexChanged;
        public event Action<int> OnScoreChanged;

        public void SetStageIndex(int stageIndex)
        {
            currentStageIndex = Mathf.Max(1, stageIndex);
            OnStageIndexChanged?.Invoke(currentStageIndex);
        }

        public void AdvanceStage()
        {
            SetStageIndex(currentStageIndex + 1);
        }

        public void SetScore(int score)
        {
            currentScore = score;
            OnScoreChanged?.Invoke(currentScore);
        }

        public void SetRunActive(bool active)
        {
            isRunActive = active;
        }

        public void SetGameState(GameState state)
        {
            if (currentGameState == state)
            {
                return;
            }

            currentGameState = state;
            OnGameStateChanged?.Invoke(currentGameState);
        }
    }
}
