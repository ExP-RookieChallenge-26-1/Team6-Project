using System.Collections.Generic;
using System;
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
            return SelectRewards(combatResult, count, null);
        }

        public List<BattleRewardSO> SelectRewards(
            CombatResult combatResult,
            int count,
            Func<BattleRewardSO, bool> additionalFilter)
        {
            count = Mathf.Max(0, count);
            if (count == 0 || rewards == null)
            {
                return new List<BattleRewardSO>();
            }

            var validRewards = rewards
                .Where(CanOfferReward)
                .Where(reward => additionalFilter == null || additionalFilter(reward))
                .ToList();
            var selectedRewards = new List<BattleRewardSO>(count);
            while (validRewards.Count > 0 && selectedRewards.Count < count)
            {
                var index = SelectWeightedRewardIndex(validRewards, CountSkillRewards(validRewards));
                selectedRewards.Add(validRewards[index]);
                validRewards.RemoveAt(index);
            }

            return selectedRewards;
        }

        private static int SelectWeightedRewardIndex(IReadOnlyList<BattleRewardSO> validRewards, int skillRewardCount)
        {
            if (validRewards == null || validRewards.Count == 0)
            {
                return -1;
            }

            var totalWeight = 0f;
            for (var index = 0; index < validRewards.Count; index++)
            {
                totalWeight += ResolveOfferWeight(validRewards[index], skillRewardCount);
            }

            if (totalWeight <= 0f)
            {
                return UnityEngine.Random.Range(0, validRewards.Count);
            }

            var roll = UnityEngine.Random.Range(0f, totalWeight);
            for (var index = 0; index < validRewards.Count; index++)
            {
                roll -= ResolveOfferWeight(validRewards[index], skillRewardCount);
                if (roll <= 0f)
                {
                    return index;
                }
            }

            return validRewards.Count - 1;
        }

        private static float ResolveOfferWeight(BattleRewardSO reward, int skillRewardCount)
        {
            if (reward == null)
            {
                return 0f;
            }

            return reward.IsSkillReward
                ? Mathf.Clamp01(reward.enemySkillOfferChance) / Mathf.Max(1, skillRewardCount)
                : 1f;
        }

        private static int CountSkillRewards(IEnumerable<BattleRewardSO> rewards)
        {
            return rewards?.Count(reward => reward != null && reward.IsSkillReward) ?? 0;
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
