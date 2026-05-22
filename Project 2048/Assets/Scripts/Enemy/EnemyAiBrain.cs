using Project2048.Skills;
using UnityEngine;

namespace Project2048.Enemy
{
    public class EnemyAiBrain
    {
        private readonly System.Random random;

        public EnemyAiBrain(System.Random random = null)
        {
            this.random = random ?? new System.Random();
        }

        public EnemyIntent ChooseIntent(EnemySO data, int turnIndex)
        {
            if (data == null)
            {
                return null;
            }

            if (CanUseSpecialActions(data) &&
                data.canUseBullRush &&
                data.bullRushInterval > 0 &&
                (turnIndex + 1) % data.bullRushInterval == 0)
            {
                return BuildBullRushIntent(data);
            }

            var debuffInterval = Mathf.Max(0, data.aiDebuffInterval);
            if (debuffInterval > 0 && (turnIndex + 1) % debuffInterval == 0)
            {
                return ChooseSkillIntent(data, EnemyIntentType.Debuff) ??
                       BuildDebuffIntent(data, ((turnIndex + 1) / debuffInterval) - 1);
            }

            var selectedType = ChooseAttackOrDefenseType(data);
            if (selectedType == EnemyIntentType.Defense && CanUseSpecialActions(data) && data.canUseThornGuard)
            {
                return BuildDefenseIntent(data);
            }

            return ChooseSkillIntent(data, selectedType) ?? BuildFallbackIntent(data, selectedType);
        }

        private EnemyIntent ChooseSkillIntent(EnemySO data, EnemyIntentType requestedType)
        {
            if (data.skills == null || data.skills.Count == 0)
            {
                return null;
            }

            var matchingSkills = new SkillSO[EnemySO.MaxEquippedSkillSlots];
            var count = 0;
            for (var index = 0; index < data.skills.Count && index < EnemySO.MaxEquippedSkillSlots; index++)
            {
                var skill = data.skills[index];
                if (skill == null || ResolveIntentType(skill) != requestedType)
                {
                    continue;
                }

                matchingSkills[count++] = skill;
            }

            if (count == 0)
            {
                return null;
            }

            return BuildSkillIntent(data, matchingSkills[random.Next(count)]);
        }

        private EnemyIntentType ChooseAttackOrDefenseType(EnemySO data)
        {
            var (attackWeight, defenseWeight) = GetActionWeights(data.aiActionBias);
            var totalWeight = attackWeight + defenseWeight;
            if (totalWeight <= 0)
            {
                return EnemyIntentType.Attack;
            }

            var roll = random.NextDouble() * totalWeight;
            return roll < attackWeight ? EnemyIntentType.Attack : EnemyIntentType.Defense;
        }

        private EnemyIntent BuildFallbackIntent(EnemySO data, EnemyIntentType intentType)
        {
            return intentType == EnemyIntentType.Defense
                ? BuildDefenseIntent(data)
                : BuildAttackIntent(data);
        }

        private EnemyIntent BuildSkillIntent(EnemySO data, SkillSO skill)
        {
            var effectKind = skill.ResolveEffectKind();
            var intent = new EnemyIntent
            {
                skillId = skill.skillId,
                displayName = string.IsNullOrWhiteSpace(skill.skillName) ? skill.skillId : skill.skillName,
                skillEffectKind = effectKind,
                hpCost = skill.hpCost,
                hpCostLeavesOne = skill.hpCostLeavesOne,
                lifeStealPercent = skill.lifeStealPercent,
                nextBoardMoveCountModifier = skill.nextBoardMoveCountModifier,
                targetAttackModifier = skill.targetAttackModifier,
                targetDefenseModifier = skill.targetDefenseModifier,
                selfDefensePowerModifier = skill.selfDefensePowerModifier,
            };

            switch (effectKind)
            {
                case SkillEffectKind.AttackDown:
                    intent.intentType = EnemyIntentType.Debuff;
                    intent.value = Mathf.Abs(skill.targetAttackModifier != 0 ? skill.targetAttackModifier : skill.power);
                    intent.targetAttackModifier = skill.targetAttackModifier != 0 ? skill.targetAttackModifier : -skill.power;
                    return intent;
                case SkillEffectKind.DefenseDown:
                    intent.intentType = EnemyIntentType.Debuff;
                    intent.value = Mathf.Abs(skill.targetDefenseModifier != 0 ? skill.targetDefenseModifier : skill.power);
                    intent.targetDefenseModifier = skill.targetDefenseModifier != 0 ? skill.targetDefenseModifier : -skill.power;
                    return intent;
                case SkillEffectKind.ThornGuard:
                    intent.intentType = EnemyIntentType.Defense;
                    intent.value = ScaleByStrength(skill.power, data.aiStrength);
                    intent.isThornGuard = true;
                    intent.retaliationDamage = ScaleByStrength(skill.selfThornRetaliationDamage, data.aiStrength);
                    return intent;
                case SkillEffectKind.LightGuard:
                case SkillEffectKind.BasicDefense:
                    intent.intentType = EnemyIntentType.Defense;
                    intent.value = ScaleByStrength(skill.power, data.aiStrength);
                    return intent;
                case SkillEffectKind.IronWall:
                    intent.intentType = EnemyIntentType.Defense;
                    intent.value = 0;
                    intent.selfDefensePowerModifier = skill.selfDefensePowerModifier != 0
                        ? skill.selfDefensePowerModifier
                        : skill.power;
                    return intent;
                case SkillEffectKind.ChargeAttack:
                    intent.intentType = EnemyIntentType.Attack;
                    intent.movePower = ScaleByStrength(Mathf.Max(skill.chargedPower, skill.power), data.aiStrength);
                    intent.value = ScaleByStrength(data.attackPower + Mathf.Max(skill.chargedPower, skill.power), data.aiStrength);
                    return intent;
                default:
                    intent.intentType = EnemyIntentType.Attack;
                    intent.movePower = ScaleByStrength(skill.power, data.aiStrength);
                    intent.value = ScaleByStrength(data.attackPower + skill.power, data.aiStrength);
                    return intent;
            }
        }

        private static EnemyIntentType ResolveIntentType(SkillSO skill)
        {
            if (skill == null)
            {
                return EnemyIntentType.Attack;
            }

            return skill.ResolveEffectKind() switch
            {
                SkillEffectKind.AttackDown => EnemyIntentType.Debuff,
                SkillEffectKind.DefenseDown => EnemyIntentType.Debuff,
                SkillEffectKind.BasicDefense => EnemyIntentType.Defense,
                SkillEffectKind.ThornGuard => EnemyIntentType.Defense,
                SkillEffectKind.LightGuard => EnemyIntentType.Defense,
                SkillEffectKind.Counter => EnemyIntentType.Defense,
                SkillEffectKind.Endure => EnemyIntentType.Defense,
                SkillEffectKind.CriticalFocus => EnemyIntentType.Defense,
                SkillEffectKind.SplitAttack => EnemyIntentType.Defense,
                SkillEffectKind.EchoDamage => EnemyIntentType.Defense,
                SkillEffectKind.IronWall => EnemyIntentType.Defense,
                _ => EnemyIntentType.Attack,
            };
        }

        private static (int AttackWeight, int DefenseWeight) GetActionWeights(EnemyAiActionBias bias)
        {
            return bias switch
            {
                EnemyAiActionBias.AttackHeavy => (80, 20),
                EnemyAiActionBias.DefenseHeavy => (20, 80),
                _ => (50, 50),
            };
        }

        private static EnemyIntent BuildAttackIntent(EnemySO data)
        {
            return new EnemyIntent
            {
                intentType = EnemyIntentType.Attack,
                movePower = ScaleByStrength(data.attackPower, data.aiStrength),
                value = ScaleByStrength(data.attackPower, data.aiStrength),
            };
        }

        private static EnemyIntent BuildDefenseIntent(EnemySO data)
        {
            if (CanUseSpecialActions(data) && data.canUseThornGuard)
            {
                return new EnemyIntent
                {
                    displayName = "가시 방어",
                    skillEffectKind = SkillEffectKind.ThornGuard,
                    intentType = EnemyIntentType.Defense,
                    value = ScaleByStrength(data.thornGuardShieldHp, data.aiStrength),
                    isThornGuard = true,
                    retaliationDamage = ScaleByStrength(data.thornGuardRetaliationDamage, data.aiStrength),
                };
            }

            return new EnemyIntent
            {
                intentType = EnemyIntentType.Defense,
                value = ScaleByStrength(data.defensePower, data.aiStrength),
            };
        }

        private static EnemyIntent BuildBullRushIntent(EnemySO data)
        {
            return new EnemyIntent
            {
                displayName = "황소 돌진",
                intentType = EnemyIntentType.Attack,
                movePower = ScaleByStrength(data.bullRushBonusDamage, data.aiStrength),
                value = ScaleByStrength(data.attackPower + data.bullRushBonusDamage, data.aiStrength),
            };
        }

        private static EnemyIntent BuildDebuffIntent(EnemySO data, int debuffIndex)
        {
            return new EnemyIntent
            {
                intentType = EnemyIntentType.Debuff,
                debuffType = ResolveDebuffType(data.aiDebuffPattern, debuffIndex),
                value = ScaleByStrength(data.debuffPower, data.aiStrength),
            };
        }

        private static DebuffType ResolveDebuffType(EnemyDebuffPattern pattern, int debuffIndex)
        {
            var even = debuffIndex % 2 == 0;
            return pattern switch
            {
                EnemyDebuffPattern.DarknessThenFear => even ? DebuffType.Darkness : DebuffType.Fear,
                _ => even ? DebuffType.Fear : DebuffType.Darkness,
            };
        }

        private static int ScaleByStrength(int value, EnemyAiStrength strength)
        {
            var baseValue = Mathf.Max(0, value);
            return strength == EnemyAiStrength.Enhanced
                ? Mathf.CeilToInt(baseValue * 1.5f)
                : baseValue;
        }

        private static bool CanUseSpecialActions(EnemySO data)
        {
            return data.encounterRank != EnemyEncounterRank.Normal;
        }
    }
}
