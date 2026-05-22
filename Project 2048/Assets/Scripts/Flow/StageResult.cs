using Project2048.Combat;
using Project2048.Rewards;

namespace Project2048.Flow
{
    public enum StageEncounterType
    {
        Normal,
        Elite,
        Boss,
        FinalBoss,
    }

    public readonly struct StageResult
    {
        public readonly int StageIndex;
        public readonly StageEncounterType EncounterType;
        public readonly bool RunCompleted;
        public readonly CombatResult CombatResult;
        public readonly RewardChoiceResult RewardResult;

        public StageResult(
            int stageIndex,
            CombatResult combatResult,
            RewardChoiceResult rewardResult)
            : this(stageIndex, StageEncounterType.Normal, false, combatResult, rewardResult)
        {
        }

        public StageResult(
            int stageIndex,
            StageEncounterType encounterType,
            bool runCompleted,
            CombatResult combatResult,
            RewardChoiceResult rewardResult)
        {
            StageIndex = stageIndex;
            EncounterType = encounterType;
            RunCompleted = runCompleted;
            CombatResult = combatResult;
            RewardResult = rewardResult;
        }
    }
}
