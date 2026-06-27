using System.Collections.Generic;
using System.Linq;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Rewards;
using Project2048.Skills;

namespace Project2048.Prototype
{
    public static class PrototypeCombatText
    {
        public static string FormatCost(int cost)
        {
            return $"보유 코스트 {cost}";
        }

        public static string FormatHp(int currentHp, int maxHp)
        {
            return $"{currentHp}/{maxHp}";
        }

        public static string FormatEnemyHp(int currentHp, int maxHp, int block)
        {
            return FormatHp(currentHp, maxHp);
        }

        public static string FormatPlayerHp(int currentHp, int maxHp, int block)
        {
            return FormatHp(currentHp, maxHp);
        }

        public static string FormatPlayerStatsTooltip(CombatSnapshot snapshot)
        {
            if (snapshot?.Player == null)
            {
                return "현재 플레이어 스탯\n전투 정보 없음";
            }

            var player = snapshot.Player;
            return string.Join(
                "\n",
                "현재 플레이어 스탯",
                $"체력 {player.CurrentHp}/{player.MaxHp}",
                $"공격력 {player.AttackPower} | 방어력 {player.DefensePower}",
                $"방어막 {player.Block} | 보호막 {player.ShieldHp}",
                $"AP {UnityEngine.Mathf.Max(0, snapshot.CurrentCost)} | 보드 이동 가능 {UnityEngine.Mathf.Max(0, snapshot.RemainingBoardMoves)}",
                $"치명타 확률 {FormatPercent(player.CriticalChance)} | 치명타 피해 {UnityEngine.Mathf.Max(1f, player.CriticalDamageMultiplier):0.##}배",
                $"상태 효과: {FormatStatusEffectSummary(player.StatusEffects)}");
        }

        public static string FormatActionDescription(string description)
        {
            return string.IsNullOrWhiteSpace(description)
                ? "최근 행동: 대기"
                : $"최근 행동: {description}";
        }

        public static string FormatEnemyTurnAction(string description)
        {
            return string.IsNullOrWhiteSpace(description)
                ? "적 행동 대기 중"
                : $"적 행동\n{description}";
        }

        public static string FormatEnemyHeader(string displayName, string aiProfileLabel)
        {
            return FormatEnemyHeader(displayName, aiProfileLabel, null);
        }

        public static string FormatEnemyHeader(string displayName, string aiProfileLabel, string fallbackStatusLine)
        {
            if (string.IsNullOrWhiteSpace(aiProfileLabel))
            {
                return string.IsNullOrWhiteSpace(fallbackStatusLine)
                    ? displayName ?? string.Empty
                    : $"{displayName}\n{fallbackStatusLine}";
            }

            return string.IsNullOrWhiteSpace(fallbackStatusLine)
                ? $"{displayName}\n{aiProfileLabel}"
                : $"{displayName}\n{aiProfileLabel}\n{fallbackStatusLine}";
        }

        public static string FormatRemainingMoves(int moves)
        {
            return $"이동 횟수 {moves}";
        }

        public static string FormatSkillHeader()
        {
            return "기술 선택";
        }

        public static string FormatEmptySkillSlotLabel(int slotIndex)
        {
            return $"{slotIndex + 1}. 빈 슬롯";
        }

        public static string FormatSkillLabel(int slotIndex, SkillSnapshot skill)
        {
            return FormatSkillLabel(slotIndex, skill, canAfford: true);
        }

        public static string FormatSkillLabel(int slotIndex, SkillSnapshot skill, bool canAfford)
        {
            return FormatSkillLabel(slotIndex, skill, canAfford, -1);
        }

        public static string FormatSkillLabel(int slotIndex, SkillSnapshot skill, bool canAfford, int currentCost)
        {
            if (skill == null)
            {
                return FormatEmptySkillSlotLabel(slotIndex);
            }

            var available = currentCost >= 0 ? currentCost : 0;
            var status = canAfford ? string.Empty : " - 부족";
            return $"{skill.DisplayName}\n코스트 {available}/{skill.Cost}{status}";
        }

        public static string FormatSkillReplacementLabel(int slotIndex, SkillSnapshot skill)
        {
            if (skill == null)
            {
                return FormatEmptySkillSlotLabel(slotIndex);
            }

            return $"{slotIndex + 1}. {skill.DisplayName}\n잊고 새 기술 배우기";
        }

        public static string FormatSkillTooltip(SkillSnapshot skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            return FormatSkillTooltip(
                skill.DisplayName,
                skill.SkillId,
                skill.SkillType,
                SkillEffectKind.Default,
                skill.Cost,
                skill.Power,
                chargedPower: 0,
                skill.Description,
                DebuffType.None);
        }

        public static string FormatSkillTooltip(SkillSO skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            var displayName = string.IsNullOrWhiteSpace(skill.skillName) ? skill.skillId : skill.skillName;
            return FormatSkillTooltip(
                displayName,
                skill.skillId,
                skill.skillType,
                skill.ResolveEffectKind(),
                skill.cost,
                skill.power,
                skill.chargedPower,
                skill.description,
                skill.debuffType);
        }

        private static string FormatSkillTooltip(
            string displayName,
            string skillId,
            SkillType skillType,
            SkillEffectKind effectKind,
            int cost,
            int power,
            int chargedPower,
            string description,
            DebuffType debuffType)
        {
            var lines = new List<string>
            {
                displayName,
                FormatSkillMetaLine(skillId, displayName, skillType, effectKind, cost, power, chargedPower, debuffType),
            };

            if (!string.IsNullOrWhiteSpace(description))
            {
                lines.Add(description);
            }

            return string.Join("\n", lines.Where(line => !string.IsNullOrWhiteSpace(line)));
        }

        private static string FormatSkillMetaLine(
            string skillId,
            string displayName,
            SkillType skillType,
            SkillEffectKind effectKind,
            int cost,
            int power,
            int chargedPower,
            DebuffType debuffType)
        {
            var effectivePower = chargedPower > 0 && chargedPower != power
                ? chargedPower
                : power;
            return $"{ResolveSkillElement(skillId, displayName, debuffType)} / {ResolveSkillCategory(skillType, effectKind)}   위력 {FormatPower(effectivePower)}   명중 100   코스트 {cost}";
        }

        private static string ResolveSkillElement(string skillId, string displayName, DebuffType debuffType)
        {
            var normalizedId = skillId ?? string.Empty;
            var normalizedName = displayName ?? string.Empty;
            if (debuffType == DebuffType.Darkness ||
                normalizedId.Contains("dark") ||
                normalizedId.Contains("black") ||
                normalizedName.Contains("암흑") ||
                normalizedName.Contains("어둠"))
            {
                return "어둠";
            }

            if (normalizedId.Contains("light") ||
                normalizedName.Contains("빛"))
            {
                return "빛";
            }

            return "전투";
        }

        private static string ResolveSkillCategory(SkillType skillType, SkillEffectKind effectKind)
        {
            if (skillType == SkillType.Debuff ||
                effectKind == SkillEffectKind.AttackStageDown ||
                effectKind == SkillEffectKind.DefenseStageDown ||
                effectKind == SkillEffectKind.CostGainDown ||
                effectKind == SkillEffectKind.BoardObstacleDebuff)
            {
                return "방해";
            }

            if (skillType == SkillType.Heal ||
                effectKind == SkillEffectKind.BasicDefense ||
                effectKind == SkillEffectKind.ThornGuard ||
                effectKind == SkillEffectKind.Counter ||
                effectKind == SkillEffectKind.Endure ||
                effectKind == SkillEffectKind.DefenseStageUp ||
                effectKind == SkillEffectKind.CriticalStageUp ||
                effectKind == SkillEffectKind.NextAttackPowerMultiplier ||
                effectKind == SkillEffectKind.NextAttackSplit)
            {
                return "보조";
            }

            return "공격";
        }

        private static string FormatPower(int power)
        {
            return power > 0 ? power.ToString() : "-";
        }

        private static string FormatPercent(float value)
        {
            return $"{UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Clamp01(value) * 100f)}%";
        }

        private static string FormatStatusEffectSummary(IEnumerable<CombatStatusEffectSnapshot> effects)
        {
            if (effects == null)
            {
                return "없음";
            }

            var names = effects
                .Where(effect => effect != null && !string.IsNullOrWhiteSpace(effect.DisplayName))
                .Select(effect => effect.DisplayName)
                .Distinct()
                .ToList();
            if (names.Count == 0)
            {
                return "없음";
            }

            var shown = names.Take(3).ToList();
            var summary = string.Join(", ", shown);
            var hiddenCount = names.Count - shown.Count;
            return hiddenCount > 0 ? $"{summary} 외 {hiddenCount}개" : summary;
        }

        public static string FormatIntent(EnemyIntent intent)
        {
            if (intent == null)
            {
                return string.Empty;
            }

            return intent.intentType switch
            {
                EnemyIntentType.Attack => "공격",
                EnemyIntentType.Defense => "방어",
                EnemyIntentType.Debuff => "변화",
                _ => "변화",
            };
        }

        public static string FormatIntents(IEnumerable<EnemyIntent> intents)
        {
            if (intents == null)
            {
                return string.Empty;
            }

            return string.Join(
                "\n",
                intents
                    .Where(intent => intent != null)
                    .Take(EnemySO.MaximumActionsPerTurn)
                    .Select(FormatIntent));
        }

        public static string FormatDebuffVfxLabel(CombatVfxCue cue)
        {
            if (cue == null)
            {
                return string.Empty;
            }

            return cue.DebuffType switch
            {
                DebuffType.Fear => $"공포: 공격 랭크 -{cue.Value}",
                DebuffType.Darkness => $"섬광: 방해 블록 +{cue.Value}",
                _ => $"약화 {cue.Value}",
            };
        }

        public static string FormatResultTitle(CombatPhase phase)
        {
            return phase == CombatPhase.Victory ? "승리" : "패배";
        }

        public static string FormatResultDescription(CombatSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return string.Empty;
            }

            return snapshot.Phase == CombatPhase.Victory
                ? "보상을 선택하세요"
                : "전투를 다시 시도하세요";
        }

        public static string FormatRewardTitle(BattleRewardSO reward)
        {
            return reward != null && !string.IsNullOrWhiteSpace(reward.mothDisplayName)
                ? reward.mothDisplayName
                : "보상";
        }

        public static string FormatRewardDescription(BattleRewardSO reward)
        {
            return reward != null && !string.IsNullOrWhiteSpace(reward.encounterText)
                ? reward.encounterText
                : "다음 전투를 준비할 보상을 선택합니다.";
        }

        public static string FormatRestReward(BattleRewardSO reward)
        {
            var percent = reward != null
                ? UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Clamp01(reward.healPercentOfMaxHp) * 100f)
                : 30;
            return $"회복: 최대 체력 {percent}% 회복";
        }

        public static string FormatEnhanceReward(BattleRewardSO reward)
        {
            var count = reward != null ? UnityEngine.Mathf.Max(0, reward.extraBoardMoveCount) : 1;
            return $"강화: 보드 이동 횟수 +{count}";
        }

        public static string FormatRewardChoice(BattleRewardSO reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            return reward.rewardKind switch
            {
                RewardChoiceKind.HealOne => "회복 1단계",
                RewardChoiceKind.HealTwo => "회복",
                RewardChoiceKind.HealThree => "회복 3단계",
                RewardChoiceKind.TemporaryAttackPower => $"다음 전투 공격 랭크 +{reward.temporaryAttackPowerBonus}",
                RewardChoiceKind.TemporaryDefensePower => $"다음 전투 방어 랭크 +{reward.temporaryDefensePowerBonus}",
                RewardChoiceKind.TemporaryBoardMoveCount => $"다음 전투 이동 횟수 +{reward.temporaryBoardMoveCountBonus}",
                RewardChoiceKind.PermanentMaxHp => $"최대 체력 +{reward.permanentMaxHpBonus}",
                RewardChoiceKind.PermanentAttackPower => $"공격력 +{reward.permanentAttackPowerBonus}",
                RewardChoiceKind.PermanentDefensePower => $"방어력 +{reward.permanentDefensePowerBonus}",
                RewardChoiceKind.PermanentCriticalChance => $"치명타 확률 +{UnityEngine.Mathf.RoundToInt(reward.permanentCriticalChanceBonus * 100f)}%",
                RewardChoiceKind.PermanentCriticalDamageMultiplier => $"치명타 배율 +{reward.permanentCriticalDamageMultiplierBonus:0.##}배",
                RewardChoiceKind.LearnSkill => reward.skillToLearn != null
                    ? $"기술 습득: {reward.skillToLearn.skillName}"
                    : "기술 습득",
                RewardChoiceKind.Rest => FormatRestReward(reward),
                RewardChoiceKind.Enhance => FormatEnhanceReward(reward),
                _ => FormatRewardTitle(reward),
            };
        }

        public static string FormatRewardTooltip(BattleRewardSO reward)
        {
            if (reward == null || !reward.IsSkillReward)
            {
                return string.Empty;
            }

            return FormatSkillTooltip(reward.skillToLearn);
        }

    }
}
