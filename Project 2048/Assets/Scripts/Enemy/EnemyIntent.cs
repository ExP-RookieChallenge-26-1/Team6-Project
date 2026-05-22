namespace Project2048.Enemy
{
    using Project2048.Skills;

    [System.Serializable]
    public class EnemyIntent
    {
        public string skillId;
        public string displayName;
        public SkillEffectKind skillEffectKind;
        public EnemyIntentType intentType;
        public int value;
        public DebuffType debuffType;
        public bool isThornGuard;
        public int retaliationDamage;
        public int targetAttackModifier;
        public int targetDefenseModifier;
        public int selfDefensePowerModifier;
        public float lifeStealPercent;
        public int hpCost;
        public bool hpCostLeavesOne = true;
        public int nextBoardMoveCountModifier;

        public EnemyIntent Clone()
        {
            return new EnemyIntent
            {
                skillId = skillId,
                displayName = displayName,
                skillEffectKind = skillEffectKind,
                intentType = intentType,
                value = value,
                debuffType = debuffType,
                isThornGuard = isThornGuard,
                retaliationDamage = retaliationDamage,
                targetAttackModifier = targetAttackModifier,
                targetDefenseModifier = targetDefenseModifier,
                selfDefensePowerModifier = selfDefensePowerModifier,
                lifeStealPercent = lifeStealPercent,
                hpCost = hpCost,
                hpCostLeavesOne = hpCostLeavesOne,
                nextBoardMoveCountModifier = nextBoardMoveCountModifier,
            };
        }
    }
}
