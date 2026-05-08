using Project2048.Combat;
using UnityEngine;

namespace Project2048.Score
{
    [CreateAssetMenu(menuName = "Game/Score Rule")]
    public class ScoreRuleSO : ScriptableObject
    {
        public int difficultyMultiplier = 100;
        public int remainingMoveMultiplier = 20;
        public int overCostMultiplier = 5;
        public int turnPenalty = 10;
        public int minimumStageScore;

        public int CalculateStageScore(CombatResult combatResult)
        {
            if (combatResult == null)
            {
                return 0;
            }

            var score =
                combatResult.enemyDifficultyScore * difficultyMultiplier +
                combatResult.remainingMoveCount * remainingMoveMultiplier +
                combatResult.overCost * overCostMultiplier -
                combatResult.turnCount * turnPenalty;

            return Mathf.Max(minimumStageScore, score);
        }

        private void OnValidate()
        {
            difficultyMultiplier = Mathf.Max(0, difficultyMultiplier);
            remainingMoveMultiplier = Mathf.Max(0, remainingMoveMultiplier);
            overCostMultiplier = Mathf.Max(0, overCostMultiplier);
            turnPenalty = Mathf.Max(0, turnPenalty);
            minimumStageScore = Mathf.Max(0, minimumStageScore);
        }
    }
}
