using System;

namespace Project2048.Rewards
{
    [Serializable]
    public struct RewardChoiceResult
    {
        public RewardChoiceKind Kind;
        public int AppliedAmount;
        public int CurrentHp;
        public int ExtraBoardMoveCount;
        public BattleRewardSO Reward;
    }
}
