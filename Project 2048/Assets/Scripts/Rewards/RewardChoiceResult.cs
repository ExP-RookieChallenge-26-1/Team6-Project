using System;
using Project2048.Skills;

namespace Project2048.Rewards
{
    [Serializable]
    public struct RewardChoiceResult
    {
        public RewardChoiceKind Kind;
        public int AppliedAmount;
        public float AppliedFloatAmount;
        public int CurrentHp;
        public int ExtraBoardMoveCount;
        public int ChoiceIndex;
        public BattleRewardSO Reward;
        public SkillSO LearnedSkill;
        public SkillSO ForgottenSkill;
    }
}
