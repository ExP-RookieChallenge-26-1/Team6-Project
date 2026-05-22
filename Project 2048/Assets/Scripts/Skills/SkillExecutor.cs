using Project2048.Combat;
using Project2048.Enemy;
using UnityEngine;

namespace Project2048.Skills
{
    public class SkillExecutor
    {
        public void Execute(
            SkillSO skill,
            PlayerCombatController player,
            EnemyController target,
            DamageCalculator damageCalculator)
        {
            if (skill == null || player == null)
            {
                return;
            }

            damageCalculator ??= new DamageCalculator();

            switch (skill.ResolveEffectKind())
            {
                case SkillEffectKind.BasicAttack:
                    ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, player, target, damageCalculator);
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.BoardMovePenaltyAttack:
                    ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, player, target, damageCalculator);
                    player.ApplyNextTurnBoardMoveCountModifier(skill.nextBoardMoveCountModifier);
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.LifeSteal:
                    ExecuteAttack(skill.power, skill.selfExtraAttackHits, ResolveLifeStealPercent(skill), player, target, damageCalculator);
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.SacrificeAttack:
                    player.SpendHp(skill.hpCost, skill.hpCostLeavesOne);
                    ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, player, target, damageCalculator);
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.BasicDefense:
                    player.GainBlockWithBonus(skill.power);
                    if (skill.selfDefenseBonus != 0)
                    {
                        player.ApplyDefenseBonus(skill.selfDefenseBonus);
                    }
                    break;
                case SkillEffectKind.ThornGuard:
                    player.ApplyThornGuard(ResolveDefenseGain(skill, player), skill.selfThornRetaliationDamage);
                    break;
                case SkillEffectKind.LightGuard:
                    player.GainBlockWithBonus(skill.power);
                    break;
                case SkillEffectKind.AttackDown:
                    ExecuteHybridDamage(skill, player, target, damageCalculator);
                    if (target != null)
                    {
                        target.ApplyAttackModifier(skill.targetAttackModifier != 0 ? skill.targetAttackModifier : -skill.power);
                    }
                    break;
                case SkillEffectKind.DefenseDown:
                    ExecuteHybridDamage(skill, player, target, damageCalculator);
                    if (target != null)
                    {
                        target.ApplyDefenseModifier(skill.targetDefenseModifier != 0 ? skill.targetDefenseModifier : -skill.power);
                    }
                    break;
                case SkillEffectKind.ChargeAttack:
                    player.QueueChargedAttack(GetSkillDisplayName(skill), skill.chargedPower > 0 ? skill.chargedPower : skill.power);
                    break;
                case SkillEffectKind.Counter:
                    player.ApplyCounter(skill.selfCounterPercent > 0 ? skill.selfCounterPercent : skill.power);
                    break;
                case SkillEffectKind.Endure:
                    player.ApplyEndure(skill.selfEndureTurns > 0 ? skill.selfEndureTurns : 1);
                    break;
                case SkillEffectKind.CriticalFocus:
                    player.ApplyCriticalChanceBonus(skill.selfCriticalChanceBonus);
                    break;
                case SkillEffectKind.SplitAttack:
                    player.ApplyExtraAttackHits(skill.selfExtraAttackHits > 0 ? skill.selfExtraAttackHits : 1);
                    break;
                case SkillEffectKind.EchoDamage:
                    player.ApplyEchoDamageBonus(skill.selfEchoDamageBonus > 0 ? skill.selfEchoDamageBonus : skill.power);
                    break;
                case SkillEffectKind.IronWall:
                    player.ApplyDefensePowerModifier(skill.selfDefensePowerModifier != 0 ? skill.selfDefensePowerModifier : skill.power);
                    break;
                default:
                    if (skill.skillType == SkillType.Attack)
                    {
                        ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, player, target, damageCalculator);
                    }

                    ApplyTargetModifiers(skill, target);
                    break;
            }
        }

        public int ExecuteChargedAttack(
            PlayerCombatController player,
            EnemyController target,
            int skillPower,
            DamageCalculator damageCalculator)
        {
            if (player == null || target == null)
            {
                return 0;
            }

            damageCalculator ??= new DamageCalculator();
            return ExecuteAttack(skillPower, 0, 0f, player, target, damageCalculator);
        }

        private static int ExecuteAttack(
            int skillPower,
            int skillExtraHits,
            float lifeStealPercent,
            PlayerCombatController player,
            EnemyController target,
            DamageCalculator damageCalculator)
        {
            if (player == null || target == null)
            {
                return 0;
            }

            var hitCount = Mathf.Max(1, 1 + player.ExtraAttackHits + Mathf.Max(0, skillExtraHits));
            var totalHpDamage = 0;
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var shouldRetaliate = target.ShieldHp > 0 && target.ThornRetaliationDamage > 0;
                var retaliationDamage = target.ThornRetaliationDamage;
                var damage = damageCalculator.CalculatePlayerSkillDamage(player, skillPower, target);
                totalHpDamage += target.TakeDamage(damage);
                if (shouldRetaliate && retaliationDamage > 0)
                {
                    player.TakeDamage(retaliationDamage);
                }

                if (target.IsDead)
                {
                    break;
                }
            }

            if (lifeStealPercent > 0f && totalHpDamage > 0)
            {
                player.RestoreHp(Mathf.CeilToInt(totalHpDamage * Mathf.Clamp01(lifeStealPercent)));
            }

            return totalHpDamage;
        }

        private static int ExecuteHybridDamage(
            SkillSO skill,
            PlayerCombatController player,
            EnemyController target,
            DamageCalculator damageCalculator)
        {
            if (skill == null || skill.power <= 0)
            {
                return 0;
            }

            return ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, player, target, damageCalculator);
        }

        private static void ApplyTargetModifiers(SkillSO skill, EnemyController target)
        {
            if (skill == null || target == null)
            {
                return;
            }

            if (skill.targetAttackModifier != 0)
            {
                target.ApplyAttackModifier(skill.targetAttackModifier);
            }

            if (skill.targetDefenseModifier != 0)
            {
                target.ApplyDefenseModifier(skill.targetDefenseModifier);
            }
        }

        private static int ResolveDefenseGain(SkillSO skill, PlayerCombatController player)
        {
            return Mathf.Max(0, skill.power + player.DefenseBonus - player.FearStacks);
        }

        private static float ResolveLifeStealPercent(SkillSO skill)
        {
            return skill.lifeStealPercent > 0f ? skill.lifeStealPercent : 0.5f;
        }

        private static string GetSkillDisplayName(SkillSO skill)
        {
            return string.IsNullOrWhiteSpace(skill.skillName) ? skill.skillId : skill.skillName;
        }
    }
}
