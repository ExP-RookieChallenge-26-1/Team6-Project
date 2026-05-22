using System;
using System.Collections.Generic;
using System.Linq;
using Project2048.Combat;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Rewards
{
    public class RewardManager : MonoBehaviour
    {
        public const int OfferedChoiceCount = 3;

        [SerializeField] private RewardTableSO rewardTable;
        [SerializeField] private RunProgress runProgress = new();

        private readonly List<BattleRewardSO> pendingChoices = new();
        private readonly List<BattleRewardSO> runtimeDefaultRewards = new();
        private readonly List<SkillSO> runtimeRewardSkills = new();
        private BattleRewardSO pendingReward;
        private bool rewardClaimed = true;

        public RunProgress RunProgress => runProgress ??= new RunProgress();
        public BattleRewardSO PendingReward => pendingReward;
        public IReadOnlyList<BattleRewardSO> PendingChoices => pendingChoices;
        public bool HasPendingReward => pendingChoices.Count > 0;
        public bool HasUnclaimedReward => pendingChoices.Count > 0 && !rewardClaimed;
        public RewardChoiceResult LastChoiceResult { get; private set; }

        public event Action<BattleRewardSO> OnRewardOffered;
        public event Action<IReadOnlyList<BattleRewardSO>> OnRewardChoicesOffered;
        public event Action<RewardChoiceResult> OnRewardClaimed;

        private void OnDestroy()
        {
            DestroyRuntimeObjects();
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
            pendingChoices.Clear();
            pendingChoices.AddRange(ResolveRewardChoices(combatResult));
            pendingReward = pendingChoices.FirstOrDefault();
            rewardClaimed = pendingChoices.Count == 0;
            LastChoiceResult = default;
            OnRewardOffered?.Invoke(pendingReward);
            OnRewardChoicesOffered?.Invoke(pendingChoices);
        }

        public RewardChoiceResult ChooseReward(int choiceIndex, PlayerCombatController player, int replacementSkillIndex = -1)
        {
            if (choiceIndex < 0 || choiceIndex >= pendingChoices.Count)
            {
                return default;
            }

            return ChooseReward(pendingChoices[choiceIndex], player, choiceIndex, replacementSkillIndex);
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

            return CompleteChoice(RewardChoiceKind.Rest, applied, 0f, -1, pendingReward, null, null);
        }

        public RewardChoiceResult ChooseEnhance(PlayerCombatController player)
        {
            if (pendingReward == null)
            {
                return default;
            }

            RunProgress.CapturePlayer(player);
            RunProgress.AddBoardMoveCount(pendingReward.extraBoardMoveCount);
            return CompleteChoice(RewardChoiceKind.Enhance, pendingReward.extraBoardMoveCount, 0f, -1, pendingReward, null, null);
        }

        public void ClearReward(PlayerCombatController player)
        {
            RunProgress.CapturePlayer(player);
            pendingChoices.Clear();
            pendingReward = null;
            rewardClaimed = true;
            LastChoiceResult = default;
        }

        private RewardChoiceResult ChooseReward(
            BattleRewardSO reward,
            PlayerCombatController player,
            int choiceIndex,
            int replacementSkillIndex)
        {
            if (reward == null)
            {
                return default;
            }

            var appliedAmount = 0;
            var appliedFloatAmount = 0f;
            SkillSO forgottenSkill = null;
            SkillSO learnedSkill = null;

            switch (reward.rewardKind)
            {
                case RewardChoiceKind.HealOne:
                case RewardChoiceKind.HealTwo:
                case RewardChoiceKind.HealThree:
                    appliedAmount = ResolveHealAmount(reward);
                    if (player != null)
                    {
                        appliedAmount = player.RestoreHp(appliedAmount);
                        RunProgress.CapturePlayer(player);
                    }
                    else
                    {
                        appliedAmount = RunProgress.HealByFlatAmount(RunProgress.CurrentHp, appliedAmount);
                    }
                    break;
                case RewardChoiceKind.TemporaryAttackPower:
                    appliedAmount = reward.temporaryAttackPowerBonus;
                    RunProgress.AddNextCombatBuff(appliedAmount, 0, 0);
                    break;
                case RewardChoiceKind.TemporaryDefensePower:
                    appliedAmount = reward.temporaryDefensePowerBonus;
                    RunProgress.AddNextCombatBuff(0, appliedAmount, 0);
                    break;
                case RewardChoiceKind.TemporaryBoardMoveCount:
                    appliedAmount = reward.temporaryBoardMoveCountBonus;
                    RunProgress.AddNextCombatBuff(0, 0, appliedAmount);
                    break;
                case RewardChoiceKind.PermanentMaxHp:
                    appliedAmount = reward.permanentMaxHpBonus;
                    RunProgress.AddPermanentStats(appliedAmount, 0, 0, 0f, 0f);
                    player?.ApplyPermanentStatBonuses(appliedAmount, 0, 0, 0f, 0f);
                    if (player != null)
                    {
                        RunProgress.CapturePlayer(player);
                    }
                    break;
                case RewardChoiceKind.PermanentAttackPower:
                    appliedAmount = reward.permanentAttackPowerBonus;
                    RunProgress.AddPermanentStats(0, appliedAmount, 0, 0f, 0f);
                    break;
                case RewardChoiceKind.PermanentDefensePower:
                    appliedAmount = reward.permanentDefensePowerBonus;
                    RunProgress.AddPermanentStats(0, 0, appliedAmount, 0f, 0f);
                    break;
                case RewardChoiceKind.PermanentCriticalChance:
                    appliedFloatAmount = reward.permanentCriticalChanceBonus;
                    RunProgress.AddPermanentStats(0, 0, 0, appliedFloatAmount, 0f);
                    break;
                case RewardChoiceKind.PermanentCriticalDamageMultiplier:
                    appliedFloatAmount = reward.permanentCriticalDamageMultiplierBonus;
                    RunProgress.AddPermanentStats(0, 0, 0, 0f, appliedFloatAmount);
                    break;
                case RewardChoiceKind.LearnSkill:
                    learnedSkill = reward.skillToLearn;
                    if (player == null || !player.TryLearnSkill(learnedSkill, replacementSkillIndex, out forgottenSkill))
                    {
                        return default;
                    }
                    appliedAmount = 1;
                    break;
                case RewardChoiceKind.Rest:
                    return ChooseRest(player);
                case RewardChoiceKind.Enhance:
                    return ChooseEnhance(player);
            }

            return CompleteChoice(reward.rewardKind, appliedAmount, appliedFloatAmount, choiceIndex, reward, learnedSkill, forgottenSkill);
        }

        private RewardChoiceResult CompleteChoice(
            RewardChoiceKind kind,
            int appliedAmount,
            float appliedFloatAmount,
            int choiceIndex,
            BattleRewardSO reward,
            SkillSO learnedSkill,
            SkillSO forgottenSkill)
        {
            rewardClaimed = true;
            LastChoiceResult = new RewardChoiceResult
            {
                Kind = kind,
                AppliedAmount = appliedAmount,
                AppliedFloatAmount = appliedFloatAmount,
                CurrentHp = RunProgress.CurrentHp,
                ExtraBoardMoveCount = RunProgress.ExtraBoardMoveCount,
                ChoiceIndex = choiceIndex,
                Reward = reward,
                LearnedSkill = learnedSkill,
                ForgottenSkill = forgottenSkill,
            };

            OnRewardClaimed?.Invoke(LastChoiceResult);
            return LastChoiceResult;
        }

        private List<BattleRewardSO> ResolveRewardChoices(CombatResult combatResult)
        {
            var choices = rewardTable != null
                ? rewardTable.SelectRewards(combatResult, OfferedChoiceCount)
                : new List<BattleRewardSO>();

            if (choices.Count < OfferedChoiceCount)
            {
                choices.AddRange(BuildRuntimeDefaultChoices(OfferedChoiceCount - choices.Count));
            }

            return choices.Take(OfferedChoiceCount).ToList();
        }

        private List<BattleRewardSO> BuildRuntimeDefaultChoices(int count)
        {
            EnsureRuntimeDefaultRewards();
            var pool = new List<BattleRewardSO>(runtimeDefaultRewards);
            var choices = new List<BattleRewardSO>(count);
            while (pool.Count > 0 && choices.Count < count)
            {
                var index = UnityEngine.Random.Range(0, pool.Count);
                choices.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return choices;
        }

        private void EnsureRuntimeDefaultRewards()
        {
            if (runtimeDefaultRewards.Count > 0)
            {
                return;
            }

            var lightShot = CreateRuntimeSkill("light-shot", "빛 발사", SkillType.Attack, SkillEffectKind.BasicAttack, cost: 6, power: 60);
            var shieldBash = CreateRuntimeSkill(
                "shield-bash",
                "방패 밀치기",
                SkillType.Attack,
                SkillEffectKind.ShieldScalingAttack,
                cost: 5,
                power: 60,
                damageStatSource: DamageStatSource.ShieldHp);
            var ironWall = CreateRuntimeSkill(
                "iron-wall",
                "철벽",
                SkillType.Defense,
                SkillEffectKind.DefenseStageUp,
                cost: 6,
                power: 0,
                selfDefenseStageModifier: 2);

            runtimeDefaultRewards.Add(CreateRuntimeReward("heal-2", "회복 2", RewardChoiceKind.HealTwo, healAmount: 2));
            runtimeDefaultRewards.Add(CreateRuntimeReward("next-attack", "다음 전투 공격", RewardChoiceKind.TemporaryAttackPower, temporaryAttack: 3));
            runtimeDefaultRewards.Add(CreateRuntimeReward("perm-attack", "공격력 영구 증가", RewardChoiceKind.PermanentAttackPower, permanentAttack: 1));
            runtimeDefaultRewards.Add(CreateRuntimeReward(
                "learn-core-skill",
                "기술 습득",
                RewardChoiceKind.LearnSkill,
                skillToLearn: UnityEngine.Random.value switch
                {
                    < 0.33f => ironWall,
                    < 0.66f => shieldBash,
                    _ => lightShot,
                }));
        }

        private SkillSO CreateRuntimeSkill(
            string skillId,
            string skillName,
            SkillType skillType,
            SkillEffectKind effectKind,
            int cost,
            int power,
            int selfDefenseStageModifier = 0,
            DamageStatSource damageStatSource = DamageStatSource.AttackPower)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.hideFlags = HideFlags.DontSave;
            skill.skillId = skillId;
            skill.skillName = skillName;
            skill.skillType = skillType;
            skill.effectKind = effectKind;
            skill.damageStatSource = damageStatSource;
            skill.cost = Mathf.Max(0, cost);
            skill.power = Mathf.Max(0, power);
            skill.selfDefenseStageModifier = Mathf.Clamp(selfDefenseStageModifier, -6, 6);
            runtimeRewardSkills.Add(skill);
            return skill;
        }

        private BattleRewardSO CreateRuntimeReward(
            string rewardId,
            string displayName,
            RewardChoiceKind kind,
            int healAmount = 0,
            int temporaryAttack = 0,
            int permanentAttack = 0,
            SkillSO skillToLearn = null)
        {
            var reward = ScriptableObject.CreateInstance<BattleRewardSO>();
            reward.hideFlags = HideFlags.DontSave;
            reward.rewardId = rewardId;
            reward.mothDisplayName = displayName;
            reward.rewardKind = kind;
            reward.healAmount = healAmount;
            reward.temporaryAttackPowerBonus = temporaryAttack;
            reward.permanentAttackPowerBonus = permanentAttack;
            reward.skillToLearn = skillToLearn;
            return reward;
        }

        private static int ResolveHealAmount(BattleRewardSO reward)
        {
            return reward.rewardKind switch
            {
                RewardChoiceKind.HealOne => 1,
                RewardChoiceKind.HealThree => 3,
                _ => Mathf.Max(1, reward.healAmount),
            };
        }

        private void DestroyRuntimeObjects()
        {
            foreach (var reward in runtimeDefaultRewards)
            {
                DestroyRuntimeObject(reward);
            }

            foreach (var skill in runtimeRewardSkills)
            {
                DestroyRuntimeObject(skill);
            }

            runtimeDefaultRewards.Clear();
            runtimeRewardSkills.Clear();
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
