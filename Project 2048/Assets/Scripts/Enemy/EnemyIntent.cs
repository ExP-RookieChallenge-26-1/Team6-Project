namespace Project2048.Enemy
{
    using Project2048.Skills;

    [System.Serializable]
    public class EnemyIntent
    {
        public string skillId;
        public string displayName;
        public SkillType skillType;
        public SkillEffectKind skillEffectKind;
        public DamageStatSource damageStatSource = DamageStatSource.AttackPower;
        public EnemyIntentType intentType;
        public int value;
        public int movePower;
        public DebuffType debuffType;
        public bool isThornGuard;
        public int retaliationDamage;
        public int targetAttackModifier;
        public int targetDefenseModifier;
        public int selfDefensePowerModifier;
        public int selfCriticalStageModifier;
        public int selfEndureTurns;
        public float lifeStealPercent;
        public int hpCost;
        public float hpCostPercent;
        public bool hpCostLeavesOne = true;
        public int nextBoardMoveCountModifier;
        public int nextCostGainModifier;
        public float nextCostGainMultiplier = 1f;
        public int conditionalPowerBonus;
        public float conditionalHpThreshold;
        public int statusDuration;
        public int statusDamage;
        public float statusMaxHpDamagePercent;
        public int shieldPiercePercent;
        public float minSelfHpPercent;
        public float maxSelfHpPercent;

        public EnemyIntent Clone()
        {
            return new EnemyIntent
            {
                skillId = skillId,
                displayName = displayName,
                skillType = skillType,
                skillEffectKind = skillEffectKind,
                damageStatSource = damageStatSource,
                intentType = intentType,
                value = value,
                movePower = movePower,
                debuffType = debuffType,
                isThornGuard = isThornGuard,
                retaliationDamage = retaliationDamage,
                targetAttackModifier = targetAttackModifier,
                targetDefenseModifier = targetDefenseModifier,
                selfDefensePowerModifier = selfDefensePowerModifier,
                selfCriticalStageModifier = selfCriticalStageModifier,
                selfEndureTurns = selfEndureTurns,
                lifeStealPercent = lifeStealPercent,
                hpCost = hpCost,
                hpCostPercent = hpCostPercent,
                hpCostLeavesOne = hpCostLeavesOne,
                nextBoardMoveCountModifier = nextBoardMoveCountModifier,
                nextCostGainModifier = nextCostGainModifier,
                nextCostGainMultiplier = nextCostGainMultiplier,
                conditionalPowerBonus = conditionalPowerBonus,
                conditionalHpThreshold = conditionalHpThreshold,
                statusDuration = statusDuration,
                statusDamage = statusDamage,
                statusMaxHpDamagePercent = statusMaxHpDamagePercent,
                shieldPiercePercent = shieldPiercePercent,
                minSelfHpPercent = minSelfHpPercent,
                maxSelfHpPercent = maxSelfHpPercent,
            };
        }
    }
}
