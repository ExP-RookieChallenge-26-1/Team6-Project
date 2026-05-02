using System.Collections.Generic;

namespace Project2048.Board2048
{
    /// <summary>
    /// 2048 보드에 남은 모든 숫자를 행동 코스트로 바꿉니다.
    /// 가장 큰 타일 하나만 보는 방식이 아니라, 보드 전체 합산 방식입니다.
    /// </summary>
    public class CostConverter
    {
        private readonly Dictionary<int, int> tileCostMap = new()
        {
            { 2, 1 },
            { 4, 2 },
            { 8, 3 },
            { 16, 5 },
            { 32, 8 },
            { 64, 13 },
            { 128, 21 },
            { 256, 34 },
            { 512, 55 },
            { 1024, 89 },
            { 2048, 144 },
        };

        public int ConvertBoardToCost(int[,] board)
        {
            var total = 0;

            foreach (var tileValue in board)
            {
                total += ConvertTileToCost(tileValue);
            }

            return total;
        }

        public int ConvertTileToCost(int tileValue)
        {
            return tileCostMap.TryGetValue(tileValue, out var cost)
                ? cost
                : 0;
        }
    }
}
