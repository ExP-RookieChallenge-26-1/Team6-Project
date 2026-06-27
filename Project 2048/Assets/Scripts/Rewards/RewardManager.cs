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
        public const float HealTierOneMaxHpPercent = 0.25f;
        public const float HealTierTwoMaxHpPercent = 0.5f;
        public const float HealTierThreeMaxHpPercent = 0.75f;
        private const int MinimumOfferedChoiceCategories = 2;
        private const int MaximumSkillChoicesPerOffer = 1;

        private enum RewardOfferCategory
        {
            Other,
            NextCombatBuff,
            Heal,
            Skill,
            PermanentStat,
        }

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
            pendingChoices.AddRange(ResolveRewardChoices(combatResult, player));
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
                    var maxHpForHeal = player != null
                        ? player.MaxHp
                        : Mathf.Max(1, RunProgress.CurrentHp);
                    appliedAmount = ResolveHealAmount(reward, maxHpForHeal);
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
                    RunProgress.AddNextCombatRankBuff(appliedAmount, 0, 0);
                    break;
                case RewardChoiceKind.TemporaryDefensePower:
                    appliedAmount = reward.temporaryDefensePowerBonus;
                    RunProgress.AddNextCombatRankBuff(0, appliedAmount, 0);
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
                    RunProgress.CapturePlayer(player);
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

        private List<BattleRewardSO> ResolveRewardChoices(CombatResult combatResult, PlayerCombatController player)
        {
            var tableRewardCount = rewardTable?.rewards != null ? rewardTable.rewards.Count : 0;
            var pool = rewardTable != null
                ? rewardTable.SelectRewards(
                    combatResult,
                    tableRewardCount,
                    reward => CanOfferRewardToPlayer(reward, player))
                : new List<BattleRewardSO>();

            if (pool.Count < OfferedChoiceCount ||
                CountPrimaryCategories(pool) < MinimumOfferedChoiceCategories)
            {
                AddUniqueRewards(pool, BuildRuntimeDefaultChoices(OfferedChoiceCount, player, pool));
            }

            if (pool.Count(reward => reward != null && !reward.IsSkillReward) < OfferedChoiceCount)
            {
                AddUniqueRewards(pool, BuildRuntimeDefaultNonSkillChoices(OfferedChoiceCount, player, pool));
            }

            var choices = SelectDiverseChoices(pool, OfferedChoiceCount);
            if (choices.Count < OfferedChoiceCount)
            {
                AddUniqueAllowedRewards(
                    choices,
                    BuildRuntimeDefaultChoices(OfferedChoiceCount - choices.Count, player, choices));
            }

            return choices.Take(OfferedChoiceCount).ToList();
        }

        private List<BattleRewardSO> BuildRuntimeDefaultChoices(
            int count,
            PlayerCombatController player,
            IEnumerable<BattleRewardSO> excludedRewards = null)
        {
            EnsureRuntimeDefaultRewards();
            var pool = runtimeDefaultRewards
                .Where(reward => CanOfferRewardToPlayer(reward, player))
                .Where(reward => !ContainsEquivalentReward(excludedRewards, reward))
                .ToList();

            return SelectDiverseChoices(pool, count);
        }

        private List<BattleRewardSO> BuildRuntimeDefaultNonSkillChoices(
            int count,
            PlayerCombatController player,
            IEnumerable<BattleRewardSO> excludedRewards = null)
        {
            EnsureRuntimeDefaultRewards();
            var pool = runtimeDefaultRewards
                .Where(reward => reward != null && !reward.IsSkillReward)
                .Where(reward => CanOfferRewardToPlayer(reward, player))
                .Where(reward => !ContainsEquivalentReward(excludedRewards, reward))
                .ToList();

            return SelectDiverseChoices(pool, count);
        }

        private void EnsureRuntimeDefaultRewards()
        {
            if (runtimeDefaultRewards.Count > 0)
            {
                return;
            }

            var lightShot = CreateRuntimeSkill("light-shot", "Light Shot", SkillType.Attack, SkillEffectKind.BasicAttack, cost: 20, power: 60);
            lightShot.availability = SkillAvailability.PlayerOnly;
            var shieldBash = CreateRuntimeSkill(
                "shield-bash",
                "Shield Bash",
                SkillType.Attack,
                SkillEffectKind.ShieldScalingAttack,
                cost: 20,
                power: 60,
                damageStatSource: DamageStatSource.ShieldHp);
            var ironWall = CreateRuntimeSkill(
                "iron-wall",
                "Iron Wall",
                SkillType.Defense,
                SkillEffectKind.DefenseStageUp,
                cost: 20,
                power: 0,
                selfDefenseStageModifier: 2);

            runtimeDefaultRewards.Add(CreateRuntimeReward("perm-defense", "Permanent Defense", RewardChoiceKind.PermanentDefensePower, permanentDefense: 1));
            runtimeDefaultRewards.Add(CreateRuntimeReward("perm-attack", "Permanent Attack", RewardChoiceKind.PermanentAttackPower, permanentAttack: 2));
            runtimeDefaultRewards.Add(CreateRuntimeReward(
                "learn-iron-wall",
                "Learn Skill",
                RewardChoiceKind.LearnSkill,
                skillToLearn: ironWall));
            runtimeDefaultRewards.Add(CreateRuntimeReward(
                "learn-shield-bash",
                "Learn Skill",
                RewardChoiceKind.LearnSkill,
                skillToLearn: shieldBash));
            runtimeDefaultRewards.Add(CreateRuntimeReward(
                "learn-light-shot",
                "Learn Skill",
                RewardChoiceKind.LearnSkill,
                skillToLearn: lightShot));
        }

        private bool CanOfferRewardToPlayer(BattleRewardSO reward, PlayerCombatController player)
        {
            if (reward == null)
            {
                return false;
            }

            if (!reward.IsSkillReward)
            {
                return true;
            }

            return reward.skillToLearn != null &&
                   reward.skillToLearn.CanAppearAsReward &&
                   !HasLearnedSkill(reward.skillToLearn, player);
        }

        private bool HasLearnedSkill(SkillSO skill, PlayerCombatController player)
        {
            return ContainsSkill(player?.Skills, skill) ||
                   ContainsSkill(RunProgress.EquippedSkills, skill);
        }

        private static bool ContainsSkill(IEnumerable<SkillSO> skills, SkillSO target)
        {
            if (skills == null || target == null)
            {
                return false;
            }

            foreach (var skill in skills)
            {
                if (IsSameSkill(skill, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameSkill(SkillSO left, SkillSO right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            if (ReferenceEquals(left, right))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(left.skillId) &&
                   left.skillId == right.skillId;
        }

        private static List<BattleRewardSO> SelectDiverseChoices(IEnumerable<BattleRewardSO> source, int count)
        {
            count = Mathf.Max(0, count);
            var remaining = source?.Where(reward => reward != null).ToList() ?? new List<BattleRewardSO>();
            var choices = new List<BattleRewardSO>(count);
            if (count == 0)
            {
                return choices;
            }

            var categories = remaining
                .Select(ResolveOfferCategory)
                .Where(IsPrimaryOfferCategory)
                .Distinct()
                .ToList();

            if (count >= MinimumOfferedChoiceCategories &&
                categories.Count >= MinimumOfferedChoiceCategories)
            {
                var firstCategory = ResolveOfferCategory(remaining[0]);
                if (!IsPrimaryOfferCategory(firstCategory))
                {
                    firstCategory = categories[0];
                }

                MoveFirstCategoryReward(remaining, choices, firstCategory);
                var secondCategory = categories.FirstOrDefault(category =>
                    category != firstCategory &&
                    remaining.Any(reward =>
                        ResolveOfferCategory(reward) == category &&
                        CanAddRewardChoice(choices, reward)));
                if (IsPrimaryOfferCategory(secondCategory))
                {
                    MoveFirstCategoryReward(remaining, choices, secondCategory);
                }
            }

            while (remaining.Count > 0 && choices.Count < count)
            {
                if (!MoveFirstAllowedReward(remaining, choices))
                {
                    break;
                }
            }

            return choices;
        }

        private static bool MoveFirstAllowedReward(
            List<BattleRewardSO> remaining,
            List<BattleRewardSO> choices)
        {
            var index = remaining.FindIndex(reward => CanAddRewardChoice(choices, reward));
            if (index < 0)
            {
                return false;
            }

            choices.Add(remaining[index]);
            remaining.RemoveAt(index);
            return true;
        }

        private static bool CanAddRewardChoice(
            IEnumerable<BattleRewardSO> choices,
            BattleRewardSO reward)
        {
            if (reward == null)
            {
                return false;
            }

            if (!reward.IsSkillReward)
            {
                return true;
            }

            return choices == null ||
                   choices.Count(choice => choice != null && choice.IsSkillReward) < MaximumSkillChoicesPerOffer;
        }

        private static void MoveFirstCategoryReward(
            List<BattleRewardSO> remaining,
            List<BattleRewardSO> choices,
            RewardOfferCategory category)
        {
            var index = remaining.FindIndex(reward =>
                ResolveOfferCategory(reward) == category &&
                CanAddRewardChoice(choices, reward));
            if (index < 0)
            {
                return;
            }

            choices.Add(remaining[index]);
            remaining.RemoveAt(index);
        }

        private static int CountPrimaryCategories(IEnumerable<BattleRewardSO> rewards)
        {
            return rewards?
                .Select(ResolveOfferCategory)
                .Where(IsPrimaryOfferCategory)
                .Distinct()
                .Count() ?? 0;
        }

        private static bool IsPrimaryOfferCategory(RewardOfferCategory category)
        {
            return category != RewardOfferCategory.Other;
        }

        private static RewardOfferCategory ResolveOfferCategory(BattleRewardSO reward)
        {
            if (reward == null)
            {
                return RewardOfferCategory.Other;
            }

            return reward.rewardKind switch
            {
                RewardChoiceKind.TemporaryAttackPower => RewardOfferCategory.NextCombatBuff,
                RewardChoiceKind.TemporaryDefensePower => RewardOfferCategory.NextCombatBuff,
                RewardChoiceKind.TemporaryBoardMoveCount => RewardOfferCategory.NextCombatBuff,
                RewardChoiceKind.Enhance => RewardOfferCategory.NextCombatBuff,
                RewardChoiceKind.HealOne => RewardOfferCategory.Heal,
                RewardChoiceKind.HealTwo => RewardOfferCategory.Heal,
                RewardChoiceKind.HealThree => RewardOfferCategory.Heal,
                RewardChoiceKind.Rest => RewardOfferCategory.Heal,
                RewardChoiceKind.LearnSkill => RewardOfferCategory.Skill,
                RewardChoiceKind.PermanentMaxHp => RewardOfferCategory.PermanentStat,
                RewardChoiceKind.PermanentAttackPower => RewardOfferCategory.PermanentStat,
                RewardChoiceKind.PermanentDefensePower => RewardOfferCategory.PermanentStat,
                RewardChoiceKind.PermanentCriticalChance => RewardOfferCategory.PermanentStat,
                RewardChoiceKind.PermanentCriticalDamageMultiplier => RewardOfferCategory.PermanentStat,
                _ => RewardOfferCategory.Other,
            };
        }

        private static void AddUniqueRewards(List<BattleRewardSO> target, IEnumerable<BattleRewardSO> rewards)
        {
            if (target == null || rewards == null)
            {
                return;
            }

            foreach (var reward in rewards)
            {
                if (reward != null && !ContainsEquivalentReward(target, reward))
                {
                    target.Add(reward);
                }
            }
        }

        private static void AddUniqueAllowedRewards(List<BattleRewardSO> target, IEnumerable<BattleRewardSO> rewards)
        {
            if (target == null || rewards == null)
            {
                return;
            }

            foreach (var reward in rewards)
            {
                if (reward != null &&
                    CanAddRewardChoice(target, reward) &&
                    !ContainsEquivalentReward(target, reward))
                {
                    target.Add(reward);
                }
            }
        }

        private static bool ContainsEquivalentReward(IEnumerable<BattleRewardSO> rewards, BattleRewardSO target)
        {
            if (rewards == null || target == null)
            {
                return false;
            }

            foreach (var reward in rewards)
            {
                if (IsEquivalentReward(reward, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsEquivalentReward(BattleRewardSO left, BattleRewardSO right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(left.rewardId) &&
                left.rewardId == right.rewardId)
            {
                return true;
            }

            return left.IsSkillReward &&
                   right.IsSkillReward &&
                   IsSameSkill(left.skillToLearn, right.skillToLearn);
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
            int permanentDefense = 0,
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
            reward.permanentDefensePowerBonus = permanentDefense;
            reward.skillToLearn = skillToLearn;
            return reward;
        }

        private static int ResolveHealAmount(BattleRewardSO reward, int maxHp)
        {
            maxHp = Mathf.Max(1, maxHp);
            var percentOfMaxHp = reward.rewardKind switch
            {
                RewardChoiceKind.HealOne => HealTierOneMaxHpPercent,
                RewardChoiceKind.HealTwo => HealTierTwoMaxHpPercent,
                RewardChoiceKind.HealThree => HealTierThreeMaxHpPercent,
                _ => 0f,
            };

            return percentOfMaxHp > 0f
                ? Mathf.CeilToInt(maxHp * percentOfMaxHp)
                : Mathf.Max(1, reward.healAmount);
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
