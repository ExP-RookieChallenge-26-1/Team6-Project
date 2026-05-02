namespace Project2048.Enemy
{
    public static class EnemyAiProfileFormatter
    {
        public static string Format(
            EnemyAiActionBias actionBias,
            EnemyDebuffPattern debuffPattern,
            EnemyAiStrength strength)
        {
            return $"AI: {FormatActionBias(actionBias)} / {FormatDebuffPattern(debuffPattern)} / {FormatStrength(strength)}";
        }

        private static string FormatActionBias(EnemyAiActionBias actionBias)
        {
            return actionBias switch
            {
                EnemyAiActionBias.AttackHeavy => "공격 몰빵",
                EnemyAiActionBias.DefenseHeavy => "방어 몰빵",
                _ => "밸런스",
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
    }
}
