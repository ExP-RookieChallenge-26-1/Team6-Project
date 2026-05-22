namespace Project2048.Skills
{
    public enum SkillEffectKind
    {
        Default = 0,

        BasicAttack = 1,
        BasicDefense = 2,

        AttackStageDown = 3,
        DefenseStageDown = 4,
        DefenseStageUp = 5,
        CriticalStageUp = 6,

        ChargeAttack = 7,
        Counter = 8,
        Endure = 9,

        LifeStealAttack = 10,
        SacrificeAttack = 11,

        ThornGuard = 12,

        ShieldScalingAttack = 13,
        ShieldBurstAttack = 14,
        DefenseScalingAttack = 15,

        BoardMoveBonusAttack = 16,
        BoardMovePenaltyAttack = 17,

        Heal = 18,
        NextAttackPowerMultiplier = 19,
        NextAttackSplit = 20,

        CostGainDown = 21,
        BoardObstacleDebuff = 22,
    }
}
