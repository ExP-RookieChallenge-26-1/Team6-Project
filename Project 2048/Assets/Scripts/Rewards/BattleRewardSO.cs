using UnityEngine;

namespace Project2048.Rewards
{
    [CreateAssetMenu(menuName = "Game/Rewards/Battle Reward")]
    public class BattleRewardSO : ScriptableObject
    {
        public string rewardId = "moth-basic";
        public string mothDisplayName = "나방";
        [Range(0f, 1f)] public float healPercentOfMaxHp = 0.3f;
        public int extraBoardMoveCount = 1;
        [TextArea] public string encounterText = "조력자가 다음 전투를 준비할 기회를 줍니다.";

        private void OnValidate()
        {
            healPercentOfMaxHp = Mathf.Clamp01(healPercentOfMaxHp);
            extraBoardMoveCount = Mathf.Max(0, extraBoardMoveCount);
        }
    }
}
