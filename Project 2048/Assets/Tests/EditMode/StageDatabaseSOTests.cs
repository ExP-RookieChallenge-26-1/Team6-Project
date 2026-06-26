using Project2048.Enemy;
using Project2048.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Project2048.Tests
{
    public class StageDatabaseSOTests
    {
        [Test]
        public void PrototypeStageDatabase_UsesTwentyStageRunWithFinalBossAtStageTwenty()
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
            Assert.That(finalStage.StageNumberInFloor, Is.EqualTo(7));

            var finalEnemy = finalStage.EnemyCandidates[0];
            Assert.That(finalEnemy.enemyName, Is.EqualTo("\uCD5C\uC885 \uBCF4\uC2A4"));
            Assert.That(finalEnemy.encounterRank, Is.EqualTo(EnemyEncounterRank.FinalBoss));
            Assert.That(database.TryGetStage(21, out _), Is.False);
        }

        [Test]
        public void PrototypeStageDatabase_UsesStageSheetEnemyLineup()
        {
            var database = AssetDatabase.LoadAssetAtPath<StageDatabaseSO>("Assets/Data/Stage/Stage Database SO.asset");
            var expectedNames = new[]
            {
                "\uACE0\uC2B4\uB3C4\uCE58",
                "\uD1A0\uB07C",
                "\uACE0\uC2B4\uB3C4\uCE58",
                "\uD1A0\uB07C",
                "\uAC15\uD654 \uACE0\uC2B4\uB3C4\uCE58",
                "\uBCF4\uC2A4 \uC5EC\uC6B0",
                "\uACE0\uC591\uC774",
                "\uBD80\uC5C9\uC774",
                "\uACE0\uC591\uC774",
                "\uBD80\uC5C9\uC774",
                "\uAC15\uD654 \uACE0\uC591\uC774",
                "\uAC15\uD654 \uBD80\uC5C9\uC774",
                "\uBCF4\uC2A4 \uC5EC\uC6B0",
                "\uC5FC\uC18C",
                "\uD669\uC18C",
                "\uC5FC\uC18C",
                "\uAC15\uD654 \uC5FC\uC18C",
                "\uAC15\uD654 \uD669\uC18C",
                "\uD669\uC18C",
                "\uCD5C\uC885 \uBCF4\uC2A4",
            };

            Assert.That(database, Is.Not.Null);
            Assert.That(expectedNames.Length, Is.EqualTo(StageDatabaseSO.TotalStageCount));
            for (var stageIndex = 1; stageIndex <= expectedNames.Length; stageIndex++)
            {
                Assert.That(database.TryGetStage(stageIndex, out var stage), Is.True, stageIndex.ToString());
                Assert.That(stage.EnemyCandidates.Count, Is.EqualTo(1), stageIndex.ToString());
                Assert.That(stage.EnemyCandidates[0].enemyName, Is.EqualTo(expectedNames[stageIndex - 1]), stageIndex.ToString());
            }
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

            Assert.That(database.TryGetStage(14, out var lowerStage), Is.True);
            Assert.That(lowerStage.Floor, Is.EqualTo(StageFloor.Lower));
            Assert.That(lowerStage.StageNumberInFloor, Is.EqualTo(1));
        }

        [Test]
        public void PrototypeStageBackgroundSprites_KeepConsistentWorldSizeAcrossFloors()
        {
            var database = AssetDatabase.LoadAssetAtPath<StageDatabaseSO>("Assets/Data/Stage/Stage Database SO.asset");

            Assert.That(database.TryGetStage(1, out var upperStage), Is.True);
            Assert.That(database.TryGetStage(7, out var middleStage), Is.True);
            Assert.That(database.TryGetStage(14, out var lowerStage), Is.True);

            var upperSize = ResolveBackgroundWorldSize(upperStage);
            var middleSize = ResolveBackgroundWorldSize(middleStage);
            var lowerSize = ResolveBackgroundWorldSize(lowerStage);

            Assert.That(middleSize.x, Is.EqualTo(upperSize.x).Within(0.001f));
            Assert.That(middleSize.y, Is.EqualTo(upperSize.y).Within(0.001f));
            Assert.That(lowerSize.x, Is.EqualTo(upperSize.x).Within(0.001f));
            Assert.That(lowerSize.y, Is.EqualTo(upperSize.y).Within(0.001f));
        }

        [TestCase(1, StageFloor.Upper, 1)]
        [TestCase(6, StageFloor.Upper, 6)]
        [TestCase(7, StageFloor.Middle, 1)]
        [TestCase(13, StageFloor.Middle, 7)]
        [TestCase(14, StageFloor.Lower, 1)]
        [TestCase(20, StageFloor.Lower, 7)]
        public void TryResolveStagePosition_MapsGlobalIndexToFloorPosition(
            int stageIndex,
            StageFloor expectedFloor,
            int expectedStageNumber)
        {
            var result = StageDatabaseSO.TryResolveStagePosition(
                stageIndex,
                out var floor,
                out var stageNumber);

            Assert.That(result, Is.True);
            Assert.That(floor, Is.EqualTo(expectedFloor));
            Assert.That(stageNumber, Is.EqualTo(expectedStageNumber));
        }

        [TestCase(0)]
        [TestCase(21)]
        public void TryResolveStagePosition_RejectsOutOfRangeIndex(int stageIndex)
        {
            Assert.That(
                StageDatabaseSO.TryResolveStagePosition(stageIndex, out _, out _),
                Is.False);
        }

        private static Vector2 ResolveBackgroundWorldSize(StageSO stage)
        {
            Assert.That(stage.PresentationBackgroundSprite, Is.Not.Null, stage.name);
            var size = stage.PresentationBackgroundSprite.bounds.size;
            return new Vector2(size.x, size.y);
        }
    }
}
