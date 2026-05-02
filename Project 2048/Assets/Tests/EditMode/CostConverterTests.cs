using NUnit.Framework;
using Project2048.Board2048;

namespace Project2048.Tests
{
    public class CostConverterTests
    {
        [TestCase(2, 1)]
        [TestCase(4, 2)]
        [TestCase(8, 3)]
        [TestCase(16, 5)]
        [TestCase(32, 8)]
        [TestCase(64, 13)]
        [TestCase(128, 21)]
        [TestCase(256, 34)]
        [TestCase(512, 55)]
        [TestCase(1024, 89)]
        [TestCase(2048, 144)]
        public void ConvertTileToCost_CountsEveryPlayableTileValue(int tileValue, int expectedCost)
        {
            var converter = new CostConverter();

            Assert.That(converter.ConvertTileToCost(tileValue), Is.EqualTo(expectedCost));
        }

        [Test]
        public void ConvertBoardToCost_SumsAllPlayableTilesOnTheField()
        {
            var converter = new CostConverter();
            var board = new[,]
            {
                { 2, 4, 8, 16 },
                { 32, 64, 128, 256 },
                { 512, 1024, 2048, 0 },
                { Board2048Manager.ObstacleValue, 3, 6, 999 },
            };

            Assert.That(converter.ConvertBoardToCost(board), Is.EqualTo(375));
        }

        [TestCase(0)]
        [TestCase(Board2048Manager.ObstacleValue)]
        [TestCase(3)]
        [TestCase(6)]
        public void ConvertTileToCost_IgnoresEmptyObstacleAndInvalidValues(int tileValue)
        {
            var converter = new CostConverter();

            Assert.That(converter.ConvertTileToCost(tileValue), Is.Zero);
        }
    }
}
