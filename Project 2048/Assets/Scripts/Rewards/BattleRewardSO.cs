using Project2048.Skills;
using UnityEngine;

namespace Project2048.Rewards
{
    [CreateAssetMenu(menuName = "Game/Rewards/Battle Reward")]
    public class BattleRewardSO : ScriptableObject
    {
        public string rewardId = "reward";
        public string mothDisplayName = "Reward";
        public RewardChoiceKind rewardKind = RewardChoiceKind.HealTwo;
        [Range(0f, 1f)] public float healPercentOfMaxHp = 0.3f;
        public int healAmount = 2;
        public int extraBoardMoveCount = 1;
        public int temporaryAttackPowerBonus = 3;
        public int temporaryDefensePowerBonus = 3;
        public int temporaryBoardMoveCountBonus = 2;
        public int permanentMaxHpBonus;
        public int permanentAttackPowerBonus;
        public int permanentDefensePowerBonus;
        [Range(0f, 1f)] public float permanentCriticalChanceBonus;
        [Min(0f)] public float permanentCriticalDamageMultiplierBonus;
        public SkillSO skillToLearn;
        [Range(0f, 1f)] public float enemySkillOfferChance = 0.1f;
        [TextArea] public string encounterText = "Choose one reward.";

        public bool IsSkillReward => rewardKind == RewardChoiceKind.LearnSkill;

        private void OnValidate()
        {
            healPercentOfMaxHp = Mathf.Clamp01(healPercentOfMaxHp);
            healAmount = Mathf.Max(0, healAmount);
            extraBoardMoveCount = Mathf.Max(0, extraBoardMoveCount);
            temporaryAttackPowerBonus = Mathf.Max(0, temporaryAttackPowerBonus);
            temporaryDefensePowerBonus = Mathf.Max(0, temporaryDefensePowerBonus);
            temporaryBoardMoveCountBonus = Mathf.Max(0, temporaryBoardMoveCountBonus);
            permanentMaxHpBonus = Mathf.Max(0, permanentMaxHpBonus);
            permanentAttackPowerBonus = Mathf.Max(0, permanentAttackPowerBonus);
            permanentDefensePowerBonus = Mathf.Max(0, permanentDefensePowerBonus);
            permanentCriticalChanceBonus = Mathf.Clamp01(permanentCriticalChanceBonus);
            permanentCriticalDamageMultiplierBonus = Mathf.Max(0f, permanentCriticalDamageMultiplierBonus);
            enemySkillOfferChance = Mathf.Clamp01(enemySkillOfferChance);
        }
    }
}
