using Project2048.Presentation;
using UnityEngine;

namespace Project2048.Skills
{
    [CreateAssetMenu(menuName = "Game/Skill")]
    public class SkillSO : ScriptableObject
    {
        public const int DefaultCost = 9;

        public string skillId;
        public string skillName;
        public SkillType skillType;
        public SkillEffectKind effectKind = SkillEffectKind.Default;
        public int cost = DefaultCost;
        public int power;
        public int targetAttackModifier;
        public int targetDefenseModifier;
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
        public bool hpCostLeavesOne = true;
        public int nextBoardMoveCountModifier;
        public int chargedPower;
        public bool canAppearAsReward = true;
        public bool isEnemySkill;
        public Sprite icon;
        public CombatEffectBinding activationEffect = new();
        [TextArea] public string description;

        public bool RequiresEnemyTarget => ResolveEffectKind() switch
        {
            SkillEffectKind.BasicAttack => true,
            SkillEffectKind.AttackDown => true,
            SkillEffectKind.DefenseDown => true,
            SkillEffectKind.SacrificeAttack => true,
            SkillEffectKind.LifeSteal => true,
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
                _ => SkillEffectKind.Default,
            };
        }

        private void OnValidate()
        {
            cost = Mathf.Max(0, cost);
            power = Mathf.Max(0, power);
            selfCounterPercent = Mathf.Clamp(selfCounterPercent, 0, 100);
            selfEndureTurns = Mathf.Max(0, selfEndureTurns);
            selfExtraAttackHits = Mathf.Max(0, selfExtraAttackHits);
            selfCriticalChanceBonus = Mathf.Clamp01(selfCriticalChanceBonus);
            lifeStealPercent = Mathf.Clamp01(lifeStealPercent);
            hpCost = Mathf.Max(0, hpCost);
            chargedPower = Mathf.Max(0, chargedPower);
        }
    }
}
