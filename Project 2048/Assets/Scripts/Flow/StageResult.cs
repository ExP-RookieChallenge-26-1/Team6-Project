using Project2048.Combat;
using Project2048.Rewards;

namespace Project2048.Flow
{
    public readonly struct StageResult
    {
        public readonly int StageIndex;
        public readonly CombatResult CombatResult;
        public readonly RewardChoiceResult RewardResult;

        public StageResult(
            int stageIndex,
            CombatResult combatResult,
            RewardChoiceResult rewardResult)
        {
            StageIndex = stageIndex;
            CombatResult = combatResult;
            RewardResult = rewardResult;
        }
    }
}
