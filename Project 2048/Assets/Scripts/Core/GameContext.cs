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
        string currentStageId;
        int currentScore;
        bool isRunActive;

        public GameState CurrentGameState => currentGameState;
        public string CurrentStageId => currentStageId;
        public int CurrentScore => currentScore;
        public bool IsRunActive => isRunActive;
        public event Action<int> OnScoreChanged;

        public void SetStageId(string stageId)
        {
            currentStageId = stageId;
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
            currentGameState = state;
        }
    }
}
