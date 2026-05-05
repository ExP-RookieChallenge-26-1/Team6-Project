using System.Collections.Generic;
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
            if (rewards == null)
            {
                return null;
            }

            foreach (var reward in rewards)
            {
                if (reward != null)
                {
                    return reward;
                }
            }

            return null;
        }
    }
}
