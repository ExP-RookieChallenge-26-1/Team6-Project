namespace Project2048.Board2048
{
    /// <summary>
    /// Converts a 2048 board into action cost using the design team's tile-cost curve.
    /// Board cost keeps the existing total-value/largest-tile/fragments shape so merges
    /// remain better than scattered low tiles.
    /// </summary>
    public class CostConverter
    {
        private const int MaxSupportedTileCost = 40;
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

            var totalValueWeight = CalculateCostWeight(totalTileValue);
            var largestTileBonus = CalculateCostWeight(largestTileValue) * LargestTileBonusMultiplier;
            var fragmentationPenalty = (playableTileCount - 1) / FragmentationPenaltyDivisor;

            return System.Math.Max(
                0,
                (totalValueWeight + largestTileBonus) / (1 + LargestTileBonusMultiplier) - fragmentationPenalty);
        }

        public int ConvertTileToCost(int tileValue)
        {
            if (!IsPlayableTileValue(tileValue))
            {
                return 0;
            }

            var tier = CalculateLog2CostWeight(tileValue);
            if (tier <= 4)
            {
                return tier * 2;
            }

            return CalculateTileCost(tier);
        }

        private static bool IsPlayableTileValue(int tileValue)
        {
            return tileValue >= 2 && (tileValue & (tileValue - 1)) == 0;
        }

        private static int CalculateLog2CostWeight(long value)
        {
            var weight = 0;
            while (value > 1)
            {
                value >>= 1;
                weight++;
            }

            return weight;
        }

        private static int CalculateCostWeight(long value)
        {
            return CalculateTileCost(CalculateLog2CostWeight(value));
        }

        private static int CalculateTileCost(int tier)
        {
            if (tier <= 0)
            {
                return 0;
            }

            return tier <= 4
                ? tier * 2
                : System.Math.Min(MaxSupportedTileCost, (tier - 2) * 4);
        }
    }
}
