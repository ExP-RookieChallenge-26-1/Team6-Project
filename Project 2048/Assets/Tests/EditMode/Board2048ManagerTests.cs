using NUnit.Framework;
using Project2048.Board2048;

namespace Project2048.Tests
{
    public class Board2048ManagerTests
    {
        [Test]
        public void MoveLeft_MergesOncePerTile_WithoutDoubleMerge()
        {
            var board = new Board2048Manager();
            board.InitBoard(1, spawnInitialTiles: false);
            board.SetBoardState(
                new[,]
                {
                    { 2, 2, 4, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                1);

            var moved = board.Move(Direction.Left, spawnTile: false);
            var state = board.GetBoardSnapshot();

            Assert.That(moved, Is.True);
            Assert.That(board.MoveCount, Is.EqualTo(0));
            Assert.That(GetRow(state, 0), Is.EqualTo(new[] { 4, 4, 0, 0 }));
        }

        [Test]
        public void MoveLeft_DoesNotMergeAcrossObstacle()
        {
            var board = new Board2048Manager();
            board.InitBoard(1, spawnInitialTiles: false);
            board.SetBoardState(
                new[,]
                {
                    { 2, Board2048Manager.ObstacleValue, 2, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                1);

            var moved = board.Move(Direction.Left, spawnTile: false);
            var state = board.GetBoardSnapshot();

            Assert.That(moved, Is.False);
            Assert.That(GetRow(state, 0), Is.EqualTo(new[] { 2, Board2048Manager.ObstacleValue, 2, 0 }));
        }

        [Test]
        public void MoveLeft_MergesWithinSegmentBeforeObstacle()
        {
            var board = new Board2048Manager();
            board.InitBoard(1, spawnInitialTiles: false);
            board.SetBoardState(
                new[,]
                {
                    { 2, 2, Board2048Manager.ObstacleValue, 4 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                1);

            var moved = board.Move(Direction.Left, spawnTile: false);
            var state = board.GetBoardSnapshot();

            Assert.That(moved, Is.True);
            Assert.That(GetRow(state, 0), Is.EqualTo(new[] { 4, 0, Board2048Manager.ObstacleValue, 4 }));
        }

        [Test]
        public void InitBoard_ConsumesPendingObstacles_AndPlacesThemOnEmptyCells()
        {
            var board = new Board2048Manager();
            board.QueueObstacles(2);
            board.InitBoard(2, spawnInitialTiles: false);

            Assert.That(board.PendingObstacleCount, Is.EqualTo(0));
            var snapshot = board.GetBoardSnapshot();
            var obstacleCount = 0;
            for (var row = 0; row < 4; row++)
            {
                for (var col = 0; col < 4; col++)
                {
                    if (Board2048Manager.IsObstacle(snapshot[row, col]))
                    {
                        obstacleCount++;
                    }
                }
            }

            Assert.That(obstacleCount, Is.EqualTo(2));
        }

        private static int[] GetRow(int[,] board, int row)
        {
            return new[]
            {
                board[row, 0],
                board[row, 1],
                board[row, 2],
                board[row, 3],
            };
        }
    }
}
