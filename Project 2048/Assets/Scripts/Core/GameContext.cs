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
        int currentSaveSlotId;
        bool isRunActive;

        public GameState CurrentGameState => currentGameState;
        public string CurrentStageId => currentStageId;
        public int CurrentScore => currentScore;
        public int CurrentSaveSlotId => currentSaveSlotId;
        public bool IsRunActive => isRunActive;

        public event Action<GameState> OnGameStateChanged;
        public event Action<string> OnStageIdChanged;
        public event Action<int> OnScoreChanged;
        public event Action<bool> OnRunActiveChanged;

        public void SetStageId(string stageId)
        {
            currentStageId = stageId;
        }

        public void SetScore(int score)
        {
            currentScore = score;

            // 변경된 점수 반영 필요한 곳에서 사용
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
