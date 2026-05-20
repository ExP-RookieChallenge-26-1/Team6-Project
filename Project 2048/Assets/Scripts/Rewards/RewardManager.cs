using System;
using Project2048.Combat;
using UnityEngine;

namespace Project2048.Rewards
{
    public class RewardManager : MonoBehaviour
    {
        [SerializeField] private RewardTableSO rewardTable;
        [SerializeField] private RunProgress runProgress = new();

        private BattleRewardSO pendingReward;
        private BattleRewardSO runtimeDefaultReward;
        private bool rewardClaimed = true;

        public RunProgress RunProgress => runProgress ??= new RunProgress();
        public BattleRewardSO PendingReward => pendingReward;
        public bool HasPendingReward => pendingReward != null;
        public bool HasUnclaimedReward => pendingReward != null && !rewardClaimed;
        public RewardChoiceResult LastChoiceResult { get; private set; }

        public event Action<BattleRewardSO> OnRewardOffered;
        public event Action<RewardChoiceResult> OnRewardClaimed;

        private void OnDestroy()
        {
            if (runtimeDefaultReward != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(runtimeDefaultReward);
                }
                else
                {
                    DestroyImmediate(runtimeDefaultReward);
                }
            }
        }

        public void Initialize(RunProgress progress, RewardTableSO table)
        {
            runProgress = progress ?? new RunProgress();
            if (table != null)
            {
                rewardTable = table;
            }
        }

        public void OfferReward(CombatResult combatResult, PlayerCombatController player)
        {
            RunProgress.CapturePlayer(player);
            pendingReward = ResolveReward(combatResult);
            rewardClaimed = pendingReward == null;
            LastChoiceResult = default;
            OnRewardOffered?.Invoke(pendingReward);
        }

        public RewardChoiceResult ChooseRest(PlayerCombatController player)
        {
            if (pendingReward == null)
            {
                return default;
            }

            var applied = 0;
            if (player != null)
            {
                applied = player.RestoreHpByMaxHpPercent(pendingReward.healPercentOfMaxHp);
                RunProgress.CapturePlayer(player);
            }
            else
            {
                applied = RunProgress.HealByMaxHpPercent(RunProgress.CurrentHp, pendingReward.healPercentOfMaxHp);
            }

            return CompleteChoice(RewardChoiceKind.Rest, applied);
        }

        public RewardChoiceResult ChooseEnhance(PlayerCombatController player)
        {
            if (pendingReward == null)
            {
                return default;
            }

            RunProgress.CapturePlayer(player);
            RunProgress.AddBoardMoveCount(pendingReward.extraBoardMoveCount);
            return CompleteChoice(RewardChoiceKind.Enhance, pendingReward.extraBoardMoveCount);
        }

        public void ClearReward(PlayerCombatController player)
        {
            RunProgress.CapturePlayer(player);
            pendingReward = null;
            rewardClaimed = true;
            LastChoiceResult = default;
        }

        private RewardChoiceResult CompleteChoice(RewardChoiceKind kind, int appliedAmount)
        {
            rewardClaimed = true;
            LastChoiceResult = new RewardChoiceResult
            {
                Kind = kind,
                AppliedAmount = appliedAmount,
                CurrentHp = RunProgress.CurrentHp,
                ExtraBoardMoveCount = RunProgress.ExtraBoardMoveCount,
                Reward = pendingReward,
            };

            OnRewardClaimed?.Invoke(LastChoiceResult);
            return LastChoiceResult;
        }

        private BattleRewardSO ResolveReward(CombatResult combatResult)
        {
            var selected = rewardTable != null ? rewardTable.SelectReward(combatResult) : null;
            if (selected != null)
            {
                return selected;
            }

            runtimeDefaultReward ??= ScriptableObject.CreateInstance<BattleRewardSO>();
            runtimeDefaultReward.hideFlags = HideFlags.DontSave;
            return runtimeDefaultReward;
        }
    }
}
