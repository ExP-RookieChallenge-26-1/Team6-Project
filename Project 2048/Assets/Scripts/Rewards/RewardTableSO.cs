using System.Collections.Generic;
using System.Linq;
using Project2048.Combat;
using UnityEngine;

namespace Project2048.Rewards
{
    [CreateAssetMenu(menuName = "Game/Rewards/Reward Table")]
    public class RewardTableSO : ScriptableObject
    {
        public List<BattleRewardSO> rewards = new();

        public BattleRewardSO SelectReward(CombatResult combatResult)
        {
            var selectedRewards = SelectRewards(combatResult, 1);
            return selectedRewards.Count > 0 ? selectedRewards[0] : null;
        }

        public List<BattleRewardSO> SelectRewards(CombatResult combatResult, int count)
        {
            count = Mathf.Max(0, count);
            if (count == 0 || rewards == null)
            {
                return new List<BattleRewardSO>();
            }

            var validRewards = rewards.Where(CanOfferReward).ToList();
            var selectedRewards = new List<BattleRewardSO>(count);
            while (validRewards.Count > 0 && selectedRewards.Count < count)
            {
                var index = Random.Range(0, validRewards.Count);
                selectedRewards.Add(validRewards[index]);
                validRewards.RemoveAt(index);
            }

            return selectedRewards;
        }

        private static bool CanOfferReward(BattleRewardSO reward)
        {
            if (reward == null)
            {
                return false;
            }

            return !reward.IsSkillReward ||
                   (reward.skillToLearn != null && reward.skillToLearn.CanAppearAsReward);
        }
    }
}
