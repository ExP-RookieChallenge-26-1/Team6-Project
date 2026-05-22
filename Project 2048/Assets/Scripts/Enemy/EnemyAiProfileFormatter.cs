namespace Project2048.Enemy
{
    public static class EnemyAiProfileFormatter
    {
        public static string Format(
            EnemyAiActionBias actionBias,
            EnemyDebuffPattern debuffPattern,
            EnemyAiStrength strength,
            EnemyAiComplexity complexity)
        {
            return $"AI: {FormatComplexity(complexity)} / {FormatActionBias(actionBias)} / {FormatDebuffPattern(debuffPattern)} / {FormatStrength(strength)}";
        }

        public static string Format(
            EnemyAiActionBias actionBias,
            EnemyDebuffPattern debuffPattern,
            EnemyAiStrength strength)
        {
            return Format(actionBias, debuffPattern, strength, EnemyAiComplexity.Simple);
        }

        private static string FormatActionBias(EnemyAiActionBias actionBias)
        {
            return actionBias switch
            {
                EnemyAiActionBias.AttackHeavy => "공격 위주",
                EnemyAiActionBias.DefenseHeavy => "방어 위주",
                _ => "균형",
            };
        }

        private static string FormatDebuffPattern(EnemyDebuffPattern debuffPattern)
        {
            return debuffPattern switch
            {
                EnemyDebuffPattern.DarknessThenFear => "암흑->공포",
                _ => "공포->암흑",
            };
        }

        private static string FormatStrength(EnemyAiStrength strength)
        {
            return strength == EnemyAiStrength.Enhanced ? "강화" : "일반";
        }

        private static string FormatComplexity(EnemyAiComplexity complexity)
        {
            return complexity switch
            {
                EnemyAiComplexity.Complex => "복잡",
                EnemyAiComplexity.Normal => "보통",
                _ => "단순",
            };
        }
    }
}
