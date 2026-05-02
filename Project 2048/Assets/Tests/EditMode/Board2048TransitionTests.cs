using System.Linq;
using NUnit.Framework;
using Project2048.Board2048;
using UnityEngine;

namespace Project2048.Tests
{
    public class Board2048TransitionTests
    {
        [Test]
        public void MoveLeft_PublishesTileMovementAndMergeDetails()
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

            BoardTransition transition = null;
            board.OnBoardTransitioned += next => transition = next;

            var moved = board.Move(Direction.Left, spawnTile: false);

            Assert.That(moved, Is.True);
            Assert.That(transition, Is.Not.Null);
            Assert.That(transition.Direction, Is.EqualTo(Direction.Left));
            Assert.That(transition.Spawns, Is.Empty);
            Assert.That(transition.Movements.Count(m =>
                m.Value == 2 &&
                m.To == new Vector2Int(0, 0) &&
                m.IsMergeParticipant &&
                m.ResultValue == 4), Is.EqualTo(2));
            Assert.That(transition.Movements.Any(m =>
                m.Value == 4 &&
                m.From == new Vector2Int(2, 0) &&
                m.To == new Vector2Int(1, 0) &&
                !m.IsMergeParticipant &&
                m.ResultValue == 4), Is.True);
        }

        [Test]
        public void MoveLeft_DoesNotPublishTransitionWhenBoardDoesNotMove()
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

            BoardTransition transition = null;
            board.OnBoardTransitioned += next => transition = next;

            var moved = board.Move(Direction.Left, spawnTile: false);

            Assert.That(moved, Is.False);
            Assert.That(transition, Is.Null);
        }
    }
}
