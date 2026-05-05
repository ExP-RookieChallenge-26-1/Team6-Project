using Project2048.Combat;
using UnityEngine;

namespace Project2048.Score
{
    public class ScoreManager : MonoBehaviour
    {
        private const string DefaultLeaderboardKey = "Project2048.LocalLeaderboard.BestScore";

        [SerializeField] private ScoreRuleSO scoreRule;
        [SerializeField] private string localLeaderboardKey = DefaultLeaderboardKey;
        [SerializeField] private int totalScore;

        private CombatManager combatManager;

        public int TotalScore => totalScore;

        public string LocalLeaderboardKey
        {
            get => string.IsNullOrWhiteSpace(localLeaderboardKey) ? DefaultLeaderboardKey : localLeaderboardKey;
            set => localLeaderboardKey = string.IsNullOrWhiteSpace(value) ? DefaultLeaderboardKey : value;
        }

        private void OnDestroy()
        {
            UnbindCombat();
        }

        public int CalculateStageScore(CombatResult combatResult)
        {
            if (scoreRule != null)
            {
                return scoreRule.CalculateStageScore(combatResult);
            }

            if (combatResult == null)
            {
                return 0;
            }

            var score =
                combatResult.enemyDifficultyScore * 100 +
                combatResult.remainingMoveCount * 20 +
                combatResult.overCost * 5 -
                combatResult.turnCount * 10;

            return Mathf.Max(0, score);
        }

        public int AddStageScore(CombatResult combatResult)
        {
            var stageScore = CalculateStageScore(combatResult);
            totalScore = Mathf.Max(0, totalScore + stageScore);
            return stageScore;
        }

        public void ResetScore()
        {
            totalScore = 0;
        }

        public bool RecordGameOverScore()
        {
            var key = LocalLeaderboardKey;
            var bestScore = PlayerPrefs.GetInt(key, 0);
            if (totalScore <= bestScore)
            {
                return false;
            }

            PlayerPrefs.SetInt(key, totalScore);
            PlayerPrefs.Save();
            return true;
        }

        public void BindCombat(CombatManager manager)
        {
            if (combatManager == manager)
            {
                return;
            }

            UnbindCombat();
            combatManager = manager;

            if (combatManager == null)
            {
                return;
            }

            combatManager.OnCombatVictory += HandleCombatVictory;
            combatManager.OnCombatDefeat += HandleCombatDefeat;
        }

        private void UnbindCombat()
        {
            if (combatManager == null)
            {
                return;
            }

            combatManager.OnCombatVictory -= HandleCombatVictory;
            combatManager.OnCombatDefeat -= HandleCombatDefeat;
            combatManager = null;
        }

        private void HandleCombatVictory(CombatResult combatResult)
        {
            AddStageScore(combatResult);
        }

        private void HandleCombatDefeat()
        {
            RecordGameOverScore();
        }
    }
}
