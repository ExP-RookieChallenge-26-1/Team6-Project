using Project2048.Enemy;
using Project2048.Presentation;
using UnityEngine;

namespace Project2048.Skills
{
    public enum SkillAvailability
    {
        Shared,
        PlayerOnly,
        EnemyOnly,
        Disabled,
    }

    [CreateAssetMenu(menuName = "Game/Skill")]
    public class SkillSO : ScriptableObject
    {
        public const int DefaultCost = 9;

        public string skillId;
        public string skillName;
        public SkillType skillType;
        public SkillEffectKind effectKind = SkillEffectKind.Default;
        public DamageStatSource damageStatSource = DamageStatSource.AttackPower;
        public int cost = DefaultCost;
        public int power;
        public int targetAttackStageModifier;
        public int targetDefenseStageModifier;
        public int selfDefenseStageModifier;
        public int selfCriticalStageModifier;
        public int targetAttackModifier;
        public int targetDefenseModifier;
        public DebuffType debuffType;
        public int debuffValue;
        public int selfDefenseBonus;
        public int selfDefensePowerModifier;
        public int selfThornRetaliationDamage;
        public int selfCounterPercent;
        public int selfEndureTurns;
        public int selfEchoDamageBonus;
        public int selfExtraAttackHits;
        [Range(0f, 1f)] public float selfCriticalChanceBonus;
        [Range(0f, 1f)] public float lifeStealPercent;
        public int hpCost;
        [Range(0f, 1f)] public float hpCostPercent;
        public bool hpCostLeavesOne = true;
        public int nextBoardMoveCountModifier;
        public int nextCostGainModifier;
        [Min(0f)] public float nextCostGainMultiplier = 1f;
        public int chargedPower;
        public bool consumesAllShield;
        [Min(0f)] public float nextAttackPowerMultiplier = 1f;
        public int nextAttackHitCount;
        [Min(0f)] public float nextAttackHitPowerMultiplier = 1f;
        [Range(0f, 1f)] public float healPercentOfMaxHp;
        public SkillAvailability availability = SkillAvailability.Shared;
        public Sprite icon;
        public CombatEffectBinding activationEffect = new();
        [TextArea] public string description;

        public bool CanAppearAsReward =>
            availability == SkillAvailability.Shared ||
            availability == SkillAvailability.PlayerOnly;

        public bool CanEnemyUse =>
            availability == SkillAvailability.Shared ||
            availability == SkillAvailability.EnemyOnly;

        public bool RequiresEnemyTarget => ResolveEffectKind() switch
        {
            SkillEffectKind.BasicAttack => true,
            SkillEffectKind.AttackStageDown => true,
            SkillEffectKind.DefenseStageDown => true,
            SkillEffectKind.LifeStealAttack => true,
            SkillEffectKind.SacrificeAttack => true,
            SkillEffectKind.ShieldScalingAttack => true,
            SkillEffectKind.ShieldBurstAttack => true,
            SkillEffectKind.DefenseScalingAttack => true,
            SkillEffectKind.BoardMoveBonusAttack => true,
            SkillEffectKind.BoardMovePenaltyAttack => true,
            _ => false,
        };

        public SkillEffectKind ResolveEffectKind()
        {
            if (effectKind != SkillEffectKind.Default)
            {
                return effectKind;
            }

            return skillType switch
            {
                SkillType.Attack => SkillEffectKind.BasicAttack,
                SkillType.Defense => SkillEffectKind.BasicDefense,
                SkillType.Heal => SkillEffectKind.Heal,
                _ => SkillEffectKind.Default,
            };
        }

        private void OnValidate()
        {
            cost = Mathf.Max(0, cost);
            power = Mathf.Max(0, power);
            targetAttackStageModifier = Mathf.Clamp(targetAttackStageModifier, -6, 6);
            targetDefenseStageModifier = Mathf.Clamp(targetDefenseStageModifier, -6, 6);
            debuffValue = Mathf.Max(0, debuffValue);
            selfDefenseStageModifier = Mathf.Clamp(selfDefenseStageModifier, -6, 6);
            selfCriticalStageModifier = Mathf.Clamp(selfCriticalStageModifier, 0, 4);
            selfCounterPercent = Mathf.Clamp(selfCounterPercent, 0, 400);
            selfEndureTurns = Mathf.Max(0, selfEndureTurns);
            selfExtraAttackHits = Mathf.Max(0, selfExtraAttackHits);
            selfCriticalChanceBonus = Mathf.Clamp01(selfCriticalChanceBonus);
            lifeStealPercent = Mathf.Clamp01(lifeStealPercent);
            hpCost = Mathf.Max(0, hpCost);
            hpCostPercent = Mathf.Clamp01(hpCostPercent);
            chargedPower = Mathf.Max(0, chargedPower);
            nextCostGainMultiplier = Mathf.Max(0f, nextCostGainMultiplier);
            nextAttackPowerMultiplier = Mathf.Max(0f, nextAttackPowerMultiplier);
            nextAttackHitCount = Mathf.Max(0, nextAttackHitCount);
            nextAttackHitPowerMultiplier = Mathf.Max(0f, nextAttackHitPowerMultiplier);
            healPercentOfMaxHp = Mathf.Clamp01(healPercentOfMaxHp);
        }
    }
}
