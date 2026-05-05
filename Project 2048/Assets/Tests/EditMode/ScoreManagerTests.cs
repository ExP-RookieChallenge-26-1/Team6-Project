using NUnit.Framework;
using Project2048.Combat;
using Project2048.Score;
using UnityEngine;

namespace Project2048.Tests
{
    public class ScoreManagerTests
    {
        private const string LeaderboardKey = "Project2048.Tests.ScoreManager.BestScore";

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(LeaderboardKey);
        }

        [Test]
        public void CalculateStageScore_UsesDifficultyMovesOverCostAndTurnPenalty()
        {
            var manager = new GameObject("ScoreManager").AddComponent<ScoreManager>();
            try
            {
                var result = new CombatResult
                {
                    enemyDifficultyScore = 3,
                    remainingMoveCount = 2,
                    overCost = 7,
                    turnCount = 4,
                };

                Assert.That(manager.CalculateStageScore(result), Is.EqualTo(335));
            }
            finally
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        [Test]
        public void AddStageScore_AccumulatesTotalScore()
        {
            var manager = new GameObject("ScoreManager").AddComponent<ScoreManager>();
            try
            {
                manager.AddStageScore(new CombatResult
                {
                    enemyDifficultyScore = 1,
                    remainingMoveCount = 1,
                    overCost = 0,
                    turnCount = 1,
                });
                manager.AddStageScore(new CombatResult
                {
                    enemyDifficultyScore = 2,
                    remainingMoveCount = 0,
                    overCost = 4,
                    turnCount = 2,
                });

                Assert.That(manager.TotalScore, Is.EqualTo(110 + 200));
            }
            finally
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        [Test]
        public void RecordGameOverScore_WritesBestScoreToLocalLeaderboard()
        {
            var manager = new GameObject("ScoreManager").AddComponent<ScoreManager>();
            try
            {
                manager.LocalLeaderboardKey = LeaderboardKey;
                manager.AddStageScore(new CombatResult
                {
                    enemyDifficultyScore = 2,
                    remainingMoveCount = 1,
                    overCost = 2,
                    turnCount = 1,
                });

                var recorded = manager.RecordGameOverScore();

                Assert.That(recorded, Is.True);
                Assert.That(PlayerPrefs.GetInt(LeaderboardKey), Is.EqualTo(manager.TotalScore));
            }
            finally
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }
    }
}
