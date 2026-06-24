using Project2048.Enemy;
using Project2048.Stage;
using NUnit.Framework;
using UnityEditor;

namespace Project2048.Tests
{
    public class StageDatabaseSOTests
    {
        [Test]
        public void PrototypeStageDatabase_UsesTwentyStageRunWithFinalBullAtStageTwenty()
        {
            var database = AssetDatabase.LoadAssetAtPath<StageDatabaseSO>("Assets/Data/Stage/Stage Database SO.asset");

            Assert.That(database, Is.Not.Null);
            Assert.That(StageDatabaseSO.TotalStageCount, Is.EqualTo(20));
            for (var stageIndex = 1; stageIndex <= StageDatabaseSO.TotalStageCount; stageIndex++)
            {
                Assert.That(database.TryGetStage(stageIndex, out var stage), Is.True, stageIndex.ToString());
                Assert.That(stage.EnemyCandidates.Count, Is.EqualTo(1), stageIndex.ToString());
            }

            Assert.That(database.TryGetStage(20, out var finalStage), Is.True);
            Assert.That(database.IsFinalStage(20), Is.True);
            Assert.That(finalStage.Floor, Is.EqualTo(StageFloor.Lower));
            Assert.That(finalStage.StageNumberInFloor, Is.EqualTo(6));

            var finalEnemy = finalStage.EnemyCandidates[0];
            Assert.That(finalEnemy.enemyName, Is.EqualTo("\uD669\uC18C"));
            Assert.That(finalEnemy.encounterRank, Is.EqualTo(EnemyEncounterRank.FinalBoss));
            Assert.That(database.TryGetStage(21, out _), Is.False);
        }

        [Test]
        public void PrototypeStageDatabase_MapsDesignFloorsToStageRanges()
        {
            var database = AssetDatabase.LoadAssetAtPath<StageDatabaseSO>("Assets/Data/Stage/Stage Database SO.asset");

            Assert.That(database.TryGetStage(1, out var upperStage), Is.True);
            Assert.That(upperStage.Floor, Is.EqualTo(StageFloor.Upper));
            Assert.That(upperStage.StageNumberInFloor, Is.EqualTo(1));

            Assert.That(database.TryGetStage(7, out var middleStage), Is.True);
            Assert.That(middleStage.Floor, Is.EqualTo(StageFloor.Middle));
            Assert.That(middleStage.StageNumberInFloor, Is.EqualTo(1));

            Assert.That(database.TryGetStage(15, out var lowerStage), Is.True);
            Assert.That(lowerStage.Floor, Is.EqualTo(StageFloor.Lower));
            Assert.That(lowerStage.StageNumberInFloor, Is.EqualTo(1));
        }
    }
}
