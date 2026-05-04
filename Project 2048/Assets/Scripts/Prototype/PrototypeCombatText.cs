using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Skills;

namespace Project2048.Prototype
{
    public static class PrototypeCombatText
    {
        public static string FormatCost(int cost)
        {
            return $"보유 코스트: {cost}";
        }

        public static string FormatHp(int currentHp, int maxHp)
        {
            return $"체력 {currentHp}/{maxHp}";
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
                ? "적 턴\n행동 대기 중"
                : $"적 턴\n{description}";
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
            return $"제한 턴 : {moves}회";
        }

        public static string FormatSkillHeader(SkillType? skillType)
        {
            return skillType == SkillType.Defense ? "방어 스킬 선택" : "공격 스킬 선택";
        }

        public static string FormatSkillLabel(int tierIndex, SkillSnapshot skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            return $"{tierIndex + 1}단계 · {skill.DisplayName} (코스트 {skill.Cost} / 위력 {skill.Power})";
        }

        public static string FormatIntent(EnemyIntent intent)
        {
            if (intent == null)
            {
                return string.Empty;
            }

            return intent.intentType switch
            {
                EnemyIntentType.Defense => "방어",
                EnemyIntentType.Attack => "공격",
                EnemyIntentType.Debuff => intent.debuffType switch
                {
                    DebuffType.Darkness => "암흑",
                    DebuffType.Fear => "공포",
                    _ => "디버프",
                },
                _ => intent.intentType.ToString(),
            };
        }

        public static string FormatDebuffVfxLabel(CombatVfxCue cue)
        {
            if (cue == null)
            {
                return string.Empty;
            }

            return cue.DebuffType switch
            {
                DebuffType.Fear => "공포: 방어도 획득 절반",
                DebuffType.Darkness => $"암흑: 방해 블록 +{cue.Value}",
                _ => $"디버프: {cue.Value}",
            };
        }

        public static string FormatResultTitle(CombatPhase phase)
        {
            return phase == CombatPhase.Victory ? "클리어!" : "죽었습니다!";
        }

        public static string FormatResultDescription(CombatSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return string.Empty;
            }

            return snapshot.Phase == CombatPhase.Victory
                ? $"얻은 스코어 : {CalculatePrototypeScore(snapshot)}"
                : $"스코어 : {CalculatePrototypeScore(snapshot)}";
        }

        private static int CalculatePrototypeScore(CombatSnapshot snapshot)
        {
            var defeatedEnemies = snapshot.Enemies == null ? 0 : snapshot.Enemies.Count;
            return defeatedEnemies * 100 + snapshot.CurrentCost * 10;
        }
    }
}
