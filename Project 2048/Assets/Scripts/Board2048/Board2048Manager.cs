using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Project2048.Board2048
{
    /// <summary>
    /// 순수 2048 보드 규칙입니다. UI 오브젝트를 직접 움직이지 않고,
    /// 보드 값과 전환 데이터만 밖으로 내보냅니다.
    /// </summary>
    [Serializable]
    public class Board2048Manager
    {
        private const int BoardSize = 4;
        // -1은 암흑 디버프로 생기는 방해 블록입니다. 숫자 타일처럼 움직이거나 합쳐지지 않습니다.
        public const int ObstacleValue = -1;
        private int[,] board = new int[BoardSize, BoardSize];
        private int pendingObstacleCount;

        public int MoveCount { get; private set; }
        public int PendingObstacleCount => pendingObstacleCount;

        public event Action<int[,]> OnBoardChanged;
        public event Action<int> OnMoveCountChanged;
        public event Action OnBoardFinished;
        public event Action<BoardTransition> OnBoardTransitioned;

        public void InitBoard(int moveCount, bool spawnInitialTiles = true)
        {
            board = new int[BoardSize, BoardSize];
            MoveCount = Mathf.Max(0, moveCount);

            var queued = pendingObstacleCount;
            pendingObstacleCount = 0;
            PlaceRandomObstacles(queued);

            if (spawnInitialTiles)
            {
                SpawnRandomTile();
                SpawnRandomTile();
            }

            PublishBoardChanged();
            OnMoveCountChanged?.Invoke(MoveCount);
        }

        public void QueueObstacles(int count)
        {
            if (count <= 0)
            {
                return;
            }

            pendingObstacleCount += count;
        }

        public static bool IsObstacle(int value)
        {
            return value == ObstacleValue;
        }

        public void SetBoardState(int[,] state, int moveCount)
        {
            ValidateBoard(state);
            board = CloneBoard(state);
            MoveCount = Mathf.Max(0, moveCount);

            PublishBoardChanged();
            OnMoveCountChanged?.Invoke(MoveCount);
        }

        public int[,] GetBoardSnapshot()
        {
            return CloneBoard(board);
        }

        public bool Move(Direction direction, bool spawnTile = true)
        {
            if (MoveCount <= 0)
            {
                return false;
            }

            var before = CloneBoard(board);
            var moved = TryMove(direction, out var movements);
            if (!moved)
            {
                return false;
            }

            // 실제로 보드가 변한 경우에만 이동 횟수를 씁니다. 막힌 방향 입력은 턴을 소비하지 않습니다.
            MoveCount--;

            BoardTileSpawn spawn = null;
            if (spawnTile)
            {
                spawn = SpawnRandomTile();
            }

            var transition = new BoardTransition
            {
                Direction = direction,
                Before = before,
                After = CloneBoard(board),
            };
            transition.Movements.AddRange(movements);
            if (spawn != null)
            {
                transition.Spawns.Add(spawn);
            }

            OnBoardTransitioned?.Invoke(transition);
            PublishBoardChanged();
            OnMoveCountChanged?.Invoke(MoveCount);

            if (MoveCount <= 0)
            {
                OnBoardFinished?.Invoke();
            }

            return true;
        }

        private bool TryMove(Direction direction, out List<BoardTileMovement> movements)
        {
            movements = new List<BoardTileMovement>();
            var moved = false;

            for (var index = 0; index < BoardSize; index++)
            {
                // 한 줄씩 꺼내서 왼쪽으로 미는 모양으로 정규화한 뒤 병합합니다.
                // Right/Down은 뒤집어서 처리하고 다시 원래 방향으로 되돌립니다.
                var original = ExtractLine(index, direction);
                var originalPositions = ExtractLinePositions(index, direction);
                var working = NeedsReverse(direction) ? original.Reverse().ToArray() : original;
                var positions = NeedsReverse(direction) ? originalPositions.Reverse().ToArray() : originalPositions;
                var merged = MergeLineWithMovements(working, positions, movements);
                var final = NeedsReverse(direction) ? merged.Reverse().ToArray() : merged;

                if (!LinesEqual(original, final))
                {
                    WriteLine(index, direction, final);
                    moved = true;
                }
            }

            return moved;
        }

        private Vector2Int[] ExtractLinePositions(int index, Direction direction)
        {
            var positions = new Vector2Int[BoardSize];
            for (var offset = 0; offset < BoardSize; offset++)
            {
                positions[offset] = direction switch
                {
                    Direction.Left or Direction.Right => new Vector2Int(offset, index),
                    Direction.Up or Direction.Down => new Vector2Int(index, offset),
                    _ => positions[offset],
                };
            }

            return positions;
        }

        private int[] ExtractLine(int index, Direction direction)
        {
            var line = new int[BoardSize];
            for (var offset = 0; offset < BoardSize; offset++)
            {
                switch (direction)
                {
                    case Direction.Left:
                    case Direction.Right:
                        line[offset] = board[index, offset];
                        break;
                    case Direction.Up:
                    case Direction.Down:
                        line[offset] = board[offset, index];
                        break;
                }
            }

            return line;
        }

        private void WriteLine(int index, Direction direction, int[] line)
        {
            for (var offset = 0; offset < BoardSize; offset++)
            {
                switch (direction)
                {
                    case Direction.Left:
                    case Direction.Right:
                        board[index, offset] = line[offset];
                        break;
                    case Direction.Up:
                    case Direction.Down:
                        board[offset, index] = line[offset];
                        break;
                }
            }
        }

        private static bool NeedsReverse(Direction direction)
        {
            return direction == Direction.Right || direction == Direction.Down;
        }

        private static bool LinesEqual(int[] left, int[] right)
        {
            for (var index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private BoardTileSpawn SpawnRandomTile()
        {
            var emptyCells = CollectEmptyCells();
            if (emptyCells.Count == 0)
            {
                return null;
            }

            var selected = emptyCells[UnityEngine.Random.Range(0, emptyCells.Count)];
            var value = UnityEngine.Random.value < 0.9f ? 2 : 4;
            board[selected.y, selected.x] = value;
            return new BoardTileSpawn
            {
                Position = selected,
                Value = value,
            };
        }

        private void PlaceRandomObstacles(int count)
        {
            if (count <= 0)
            {
                return;
            }

            var emptyCells = CollectEmptyCells();
            for (var i = 0; i < count && emptyCells.Count > 0; i++)
            {
                var pickIndex = UnityEngine.Random.Range(0, emptyCells.Count);
                var selected = emptyCells[pickIndex];
                board[selected.y, selected.x] = ObstacleValue;
                emptyCells.RemoveAt(pickIndex);
            }
        }

        private List<Vector2Int> CollectEmptyCells()
        {
            var cells = new List<Vector2Int>();
            for (var row = 0; row < BoardSize; row++)
            {
                for (var col = 0; col < BoardSize; col++)
                {
                    if (board[row, col] == 0)
                    {
                        cells.Add(new Vector2Int(col, row));
                    }
                }
            }

            return cells;
        }

        private static int[,] CloneBoard(int[,] source)
        {
            var clone = new int[BoardSize, BoardSize];
            for (var row = 0; row < BoardSize; row++)
            {
                for (var col = 0; col < BoardSize; col++)
                {
                    clone[row, col] = source[row, col];
                }
            }

            return clone;
        }

        private static void ValidateBoard(int[,] state)
        {
            if (state == null || state.GetLength(0) != BoardSize || state.GetLength(1) != BoardSize)
            {
                throw new ArgumentException("Board state must be a 4x4 grid.", nameof(state));
            }
        }

        private void PublishBoardChanged()
        {
            OnBoardChanged?.Invoke(CloneBoard(board));
        }

        private static int[] MergeLine(int[] line)
        {
            var result = new int[line.Length];
            var writeIndex = 0;
            var segmentStart = 0;

            for (var i = 0; i <= line.Length; i++)
            {
                var endOfSegment = i == line.Length || line[i] == ObstacleValue;
                if (!endOfSegment)
                {
                    continue;
                }

                var segmentLength = i - segmentStart;
                if (segmentLength > 0)
                {
                    var segment = new int[segmentLength];
                    Array.Copy(line, segmentStart, segment, 0, segmentLength);
                    var merged = MergeSegment(segment);
                    Array.Copy(merged, 0, result, writeIndex, segmentLength);
                    writeIndex += segmentLength;
                }

                if (i < line.Length)
                {
                    result[writeIndex++] = ObstacleValue;
                }

                segmentStart = i + 1;
            }

            return result;
        }

        private static int[] MergeSegment(int[] segment)
        {
            var values = segment.Where(value => value != 0).ToList();
            var merged = new List<int>(segment.Length);

            for (var index = 0; index < values.Count; index++)
            {
                if (index < values.Count - 1 && values[index] == values[index + 1])
                {
                    merged.Add(values[index] * 2);
                    index++;
                }
                else
                {
                    merged.Add(values[index]);
                }
            }

            while (merged.Count < segment.Length)
            {
                merged.Add(0);
            }

            return merged.ToArray();
        }

        private static int[] MergeLineWithMovements(
            int[] line,
            Vector2Int[] positions,
            ICollection<BoardTileMovement> movements)
        {
            var result = new int[line.Length];
            var segmentStart = 0;

            for (var i = 0; i <= line.Length; i++)
            {
                // 방해 블록은 벽처럼 취급합니다. 벽을 기준으로 줄을 나누면
                // 숫자 타일이 방해 블록을 넘어가거나 그 너머의 타일과 합쳐지지 않습니다.
                var endOfSegment = i == line.Length || line[i] == ObstacleValue;
                if (!endOfSegment)
                {
                    continue;
                }

                if (segmentStart < i)
                {
                    MergeSegmentWithMovements(line, positions, segmentStart, i, result, movements);
                }

                if (i < line.Length)
                {
                    result[i] = ObstacleValue;
                }

                segmentStart = i + 1;
            }

            return result;
        }

        private static void MergeSegmentWithMovements(
            int[] line,
            Vector2Int[] positions,
            int segmentStart,
            int segmentEnd,
            int[] result,
            ICollection<BoardTileMovement> movements)
        {
            var tiles = new List<LineTile>();
            for (var index = segmentStart; index < segmentEnd; index++)
            {
                if (line[index] > 0)
                {
                    tiles.Add(new LineTile(line[index], index));
                }
            }

            var targetIndex = segmentStart;
            for (var index = 0; index < tiles.Count; index++)
            {
                var tile = tiles[index];
                if (index < tiles.Count - 1 && tile.Value == tiles[index + 1].Value)
                {
                    // 2048 원칙대로 한 번 병합된 타일은 같은 이동 안에서 다시 병합되지 않습니다.
                    var nextTile = tiles[index + 1];
                    var resultValue = tile.Value * 2;
                    result[targetIndex] = resultValue;
                    AddMovement(tile, positions, targetIndex, true, resultValue, movements);
                    AddMovement(nextTile, positions, targetIndex, true, resultValue, movements);
                    index++;
                }
                else
                {
                    result[targetIndex] = tile.Value;
                    AddMovement(tile, positions, targetIndex, false, tile.Value, movements);
                }

                targetIndex++;
            }
        }

        private static void AddMovement(
            LineTile tile,
            Vector2Int[] positions,
            int targetIndex,
            bool isMergeParticipant,
            int resultValue,
            ICollection<BoardTileMovement> movements)
        {
            if (!isMergeParticipant && tile.SourceIndex == targetIndex)
            {
                return;
            }

            movements.Add(new BoardTileMovement
            {
                Value = tile.Value,
                From = positions[tile.SourceIndex],
                To = positions[targetIndex],
                IsMergeParticipant = isMergeParticipant,
                ResultValue = resultValue,
            });
        }

        private readonly struct LineTile
        {
            public LineTile(int value, int sourceIndex)
            {
                Value = value;
                SourceIndex = sourceIndex;
            }

            public int Value { get; }
            public int SourceIndex { get; }
        }
    }
}
