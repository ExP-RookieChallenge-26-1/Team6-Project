namespace Project2048.Board2048
{
    /// <summary>
    /// 2048 보드에 남은 모든 숫자를 행동 코스트로 바꿉니다.
    /// 전체 타일 양, 가장 큰 타일 보너스, 조각난 타일 페널티를 함께 봅니다.
    /// 타일값 자체가 아니라 2048 단계(log2)를 쓰기 때문에 고레벨 타일도 과하게 폭증하지 않습니다.
    /// </summary>
    public class CostConverter
    {
        private const int LargestTileBonusMultiplier = 2;
        private const int FragmentationPenaltyDivisor = 2;

        public int ConvertBoardToCost(int[,] board)
        {
            long totalTileValue = 0;
            var largestTileValue = 0;
            var playableTileCount = 0;

            foreach (var tileValue in board)
            {
                if (!IsPlayableTileValue(tileValue))
                {
                    continue;
                }

                totalTileValue += tileValue;
                playableTileCount++;
                if (tileValue > largestTileValue)
                {
                    largestTileValue = tileValue;
                }
            }

            if (playableTileCount == 0)
            {
                return 0;
            }

            var totalValueScore = CalculateLog2Score(totalTileValue);
            var largestTileBonus = CalculateLog2Score(largestTileValue) * LargestTileBonusMultiplier;
            var fragmentationPenalty = (playableTileCount - 1) / FragmentationPenaltyDivisor;

            return System.Math.Max(0, totalValueScore + largestTileBonus - fragmentationPenalty);
        }

        public int ConvertTileToCost(int tileValue)
        {
            return IsPlayableTileValue(tileValue)
                ? CalculateLog2Score(tileValue) * (1 + LargestTileBonusMultiplier)
                : 0;
        }

        private static bool IsPlayableTileValue(int tileValue)
        {
            return tileValue >= 2 && (tileValue & (tileValue - 1)) == 0;
        }

        private static int CalculateLog2Score(long value)
        {
            var score = 0;
            while (value > 1)
            {
                value >>= 1;
                score++;
            }

            return score;
        }
    }
}
