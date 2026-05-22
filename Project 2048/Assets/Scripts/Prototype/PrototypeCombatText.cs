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
            var status = canAfford ? string.Empty : " - COST LOW";
            return $"{skill.DisplayName}\nPP {available}/{skill.Cost}{status}";
        }

        public static string FormatSkillReplacementLabel(int slotIndex, SkillSnapshot skill)
        {
            if (skill == null)
            {
                return FormatEmptySkillSlotLabel(slotIndex);
            }

            return $"{slotIndex + 1}. {skill.DisplayName}\n잊고 새 기술 배우기";
        }

        public static string FormatIntent(EnemyIntent intent)
        {
            if (intent == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(intent.displayName))
            {
                return intent.displayName;
            }

            return intent.intentType switch
            {
                EnemyIntentType.Defense => "보호",
                EnemyIntentType.Attack => "공격",
                EnemyIntentType.Debuff => intent.debuffType switch
                {
                    DebuffType.Darkness => "섬광",
                    DebuffType.Fear => "공포",
                    _ => "약화",
                },
                _ => intent.intentType.ToString(),
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
                DebuffType.Fear => $"공포: 방어력 -{cue.Value}",
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
                ? $"전투 점수 {CalculatePrototypeScore(snapshot)}"
                : $"최종 점수 {CalculatePrototypeScore(snapshot)}";
        }

        public static string FormatResultDescription(CombatSnapshot snapshot, int totalScore)
        {
            if (snapshot == null)
            {
                return string.Empty;
            }

            return snapshot.Phase == CombatPhase.Victory
                ? $"전투 점수 {totalScore}"
                : $"최종 점수 {totalScore}";
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
                RewardChoiceKind.HealOne => "회복 1",
                RewardChoiceKind.HealTwo => "회복 2",
                RewardChoiceKind.HealThree => "회복 3",
                RewardChoiceKind.TemporaryAttackPower => $"다음 전투 공격력 +{reward.temporaryAttackPowerBonus}",
                RewardChoiceKind.TemporaryDefensePower => $"다음 전투 방어력 +{reward.temporaryDefensePowerBonus}",
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

        private static int CalculatePrototypeScore(CombatSnapshot snapshot)
        {
            var defeatedEnemies = snapshot.Enemies == null ? 0 : snapshot.Enemies.Count;
            return defeatedEnemies * 100 + snapshot.CurrentCost * 10;
        }
    }
}
