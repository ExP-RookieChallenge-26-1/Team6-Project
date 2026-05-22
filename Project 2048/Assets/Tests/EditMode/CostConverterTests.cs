using NUnit.Framework;
using Project2048.Board2048;

namespace Project2048.Tests
{
    public class CostConverterTests
    {
        [TestCase(2, 3)]
        [TestCase(4, 6)]
        [TestCase(8, 9)]
        [TestCase(16, 12)]
        [TestCase(32, 15)]
        [TestCase(64, 18)]
        [TestCase(128, 21)]
        [TestCase(256, 24)]
        [TestCase(512, 27)]
        [TestCase(1024, 30)]
        [TestCase(2048, 33)]
        [TestCase(4096, 36)]
        public void ConvertTileToCost_CountsEveryPlayableTileValue(int tileValue, int expectedCost)
        {
            var converter = new CostConverter();

            Assert.That(converter.ConvertTileToCost(tileValue), Is.EqualTo(expectedCost));
        }

        [Test]
        public void ConvertBoardToCost_CombinesTotalValueLargestTileAndFragmentation()
        {
            var converter = new CostConverter();
            var board = new[,]
            {
                { 2, 4, 8, 16 },
                { 32, 64, 128, 256 },
                { 512, 1024, 2048, 0 },
                { Board2048Manager.ObstacleValue, 3, 6, 999 },
            };

            Assert.That(converter.ConvertBoardToCost(board), Is.EqualTo(28));
        }

        [TestCase(2, 4)]
        [TestCase(4, 8)]
        [TestCase(8, 16)]
        [TestCase(16, 32)]
        [TestCase(32, 64)]
        [TestCase(64, 128)]
        [TestCase(128, 256)]
        [TestCase(256, 512)]
        [TestCase(512, 1024)]
        [TestCase(1024, 2048)]
        [TestCase(2048, 4096)]
        public void ConvertBoardToCost_RewardsMergingIntoLargerTiles(int sourceTile, int mergedTile)
        {
            var converter = new CostConverter();
            var splitBoard = new[,]
            {
                { sourceTile, sourceTile, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            };
            var mergedBoard = new[,]
            {
                { mergedTile, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            };

            Assert.That(converter.ConvertBoardToCost(mergedBoard), Is.GreaterThan(converter.ConvertBoardToCost(splitBoard)));
        }

        [TestCase(0)]
        [TestCase(1)]
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
