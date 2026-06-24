using Project2048.Combat;
using Project2048.Board2048;
using Project2048.Cost;
using Project2048.Enemy;
using UnityEngine;

namespace Project2048.Skills
{
    public sealed class SkillExecutionContext
    {
        public ActionCostWallet CostWallet { get; set; }
        public Board2048Manager BoardManager { get; set; }
    }

    public class SkillExecutor
    {
        public void Execute(
            SkillSO skill,
            PlayerCombatController player,
            EnemyController target,
            DamageCalculator damageCalculator,
            SkillExecutionContext context = null)
        {
            if (skill == null || player == null)
            {
                return;
            }

            damageCalculator ??= new DamageCalculator();

            switch (skill.ResolveEffectKind())
            {
                case SkillEffectKind.BasicAttack:
                    SkillAttackExecutor.ExecuteSkillAttack(skill, player, target, damageCalculator);
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.BoardMoveBonusAttack:
                case SkillEffectKind.BoardMovePenaltyAttack:
                    SkillAttackExecutor.ExecuteSkillAttack(skill, player, target, damageCalculator);
                    player.ApplyNextTurnBoardMoveCountModifier(skill.nextBoardMoveCountModifier);
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.LifeStealAttack:
                    SkillAttackExecutor.ExecuteSkillAttack(
                        skill,
                        player,
                        target,
                        damageCalculator,
                        lifeStealPercentOverride: SkillAttackExecutor.ResolveLifeStealPercent(skill));
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.SacrificeAttack:
                    SpendSkillHpCost(skill, player);
                    SkillAttackExecutor.ExecuteSkillAttack(skill, player, target, damageCalculator);
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.BleedAttack:
                    SkillAttackExecutor.ExecuteSkillAttack(skill, player, target, damageCalculator);
                    target?.ApplyBleed(ResolveStatusDuration(skill, 2), ResolveStatusDamage(skill, 20));
                    break;
                case SkillEffectKind.PoisonAttack:
                    SkillAttackExecutor.ExecuteSkillAttack(skill, player, target, damageCalculator);
                    target?.ApplyPoison(ResolveStatusDuration(skill, 3), ResolveStatusPercent(skill, 0.05f));
                    break;
                case SkillEffectKind.OpenWoundAttack:
                    SkillAttackExecutor.ExecuteOpenWoundAttack(skill, player, target, damageCalculator);
                    break;
                case SkillEffectKind.ExecuteAttack:
                    SkillAttackExecutor.ExecuteSkillAttack(
                        skill,
                        player,
                        target,
                        damageCalculator,
                        powerOverride: SkillAttackExecutor.ResolveExecutePower(skill, target));
                    break;
                case SkillEffectKind.OverburnAttack:
                    SkillAttackExecutor.ExecuteSkillAttack(
                        skill,
                        player,
                        target,
                        damageCalculator,
                        powerOverride: SkillAttackExecutor.ResolveOverburnPower(skill, context));
                    break;
                case SkillEffectKind.ShieldScalingAttack:
                    SkillAttackExecutor.ExecuteSkillAttack(
                        skill,
                        player,
                        target,
                        damageCalculator,
                        statSourceOverride: DamageStatSource.ShieldHp);
                    break;
                case SkillEffectKind.ShieldBurstAttack:
                    SkillAttackExecutor.ExecuteSkillAttack(
                        skill,
                        player,
                        target,
                        damageCalculator,
                        statSourceOverride: DamageStatSource.ShieldHp,
                        consumeAllShieldAfterAttack: true);
                    break;
                case SkillEffectKind.DefenseScalingAttack:
                    SkillAttackExecutor.ExecuteSkillAttack(
                        skill,
                        player,
                        target,
                        damageCalculator,
                        statSourceOverride: DamageStatSource.DefensePower);
                    break;
                case SkillEffectKind.BasicDefense:
                    player.AddBlock(skill.power);
                    break;
                case SkillEffectKind.ThornGuard:
                    player.ApplyThornGuard(ResolveDefenseGain(skill, player), skill.selfThornRetaliationDamage);
                    break;
                case SkillEffectKind.AttackStageDown:
                    SkillAttackExecutor.ExecuteHybridDamage(skill, player, target, damageCalculator);
                    if (target != null)
                    {
                        target.ApplyAttackModifier(ResolveStageModifier(skill.targetAttackStageModifier, skill.targetAttackModifier, -1));
                    }
                    break;
                case SkillEffectKind.DefenseStageDown:
                    SkillAttackExecutor.ExecuteHybridDamage(skill, player, target, damageCalculator);
                    if (target != null)
                    {
                        target.ApplyDefenseModifier(ResolveStageModifier(skill.targetDefenseStageModifier, skill.targetDefenseModifier, -1));
                    }
                    break;
                case SkillEffectKind.DefenseStageUp:
                    player.ApplyDefensePowerModifier(ResolveStageModifier(skill.selfDefenseStageModifier, skill.selfDefensePowerModifier, 1));
                    break;
                case SkillEffectKind.CriticalStageUp:
                    player.ApplyCriticalStageModifier(skill.selfCriticalStageModifier > 0
                        ? skill.selfCriticalStageModifier
                        : Mathf.Max(1, Mathf.RoundToInt(skill.selfCriticalChanceBonus / 0.2f)));
                    break;
                case SkillEffectKind.ChargeAttack:
                    player.QueueChargedAttack(GetSkillDisplayName(skill), skill.chargedPower > 0 ? skill.chargedPower : skill.power, skill.damageStatSource);
                    break;
                case SkillEffectKind.Counter:
                    player.ApplyCounter(skill.selfCounterPercent > 0 ? skill.selfCounterPercent : 200);
                    break;
                case SkillEffectKind.Endure:
                    player.ApplyEndure(skill.selfEndureTurns > 0 ? skill.selfEndureTurns : 1);
                    break;
                case SkillEffectKind.Heal:
                    player.RestoreHpByMaxHpPercent(skill.healPercentOfMaxHp > 0f ? skill.healPercentOfMaxHp : 0.25f);
                    break;
                case SkillEffectKind.NextAttackPowerMultiplier:
                    player.ApplyNextAttackPowerMultiplier(skill.nextAttackPowerMultiplier > 0f ? skill.nextAttackPowerMultiplier : 1.3f);
                    break;
                case SkillEffectKind.NextAttackSplit:
                    player.ApplyNextAttackSplit(
                        skill.nextAttackHitCount > 0 ? skill.nextAttackHitCount : 2,
                        skill.nextAttackHitPowerMultiplier > 0f ? skill.nextAttackHitPowerMultiplier : 0.6f);
                    break;
                case SkillEffectKind.SealSkill:
                    target?.ApplySealFromLastUsedSkill(ResolveStatusDuration(skill, 1));
                    break;
                case SkillEffectKind.Taunt:
                    if (target != null)
                    {
                        target.ApplyTaunt(ResolveStatusDuration(skill, 1));
                        target.ApplyAttackModifier(ResolveStageModifier(skill.targetAttackStageModifier, skill.targetAttackModifier, 2));
                    }
                    break;
                case SkillEffectKind.CrackBrand:
                    target?.ApplyBrand(ResolveStatusDamage(skill, 40));
                    break;
                case SkillEffectKind.CostCarry:
                    if (skill.nextCostGainModifier > 0)
                    {
                        player.ApplyNextTurnCostGainModifier(skill.nextCostGainModifier);
                    }
                    else
                    {
                        player.ApplyCostCarry(skill.maxCostCarry > 0 ? skill.maxCostCarry : 4);
                    }
                    break;
                case SkillEffectKind.DarknessCleanse:
                    if (context?.BoardManager != null &&
                        context.BoardManager.RemoveOneObstacle())
                    {
                        context.CostWallet?.AddCost(skill.costRefund > 0 ? skill.costRefund : 2);
                    }
                    break;
                default:
                    if (skill.skillType == SkillType.Attack)
                    {
                        SkillAttackExecutor.ExecuteSkillAttack(skill, player, target, damageCalculator);
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
            return SkillAttackExecutor.ExecuteAttack(skillPower, 0, 0f, DamageStatSource.AttackPower, false, player, target, damageCalculator);
        }

        public int ExecuteChargedAttack(
            PlayerCombatController player,
            EnemyController target,
            int skillPower,
            DamageStatSource statSource,
            DamageCalculator damageCalculator)
        {
            if (player == null || target == null)
            {
                return 0;
            }

            damageCalculator ??= new DamageCalculator();
            return SkillAttackExecutor.ExecuteAttack(skillPower, 0, 0f, statSource, false, player, target, damageCalculator);
        }

        public bool CanExecute(SkillSO skill, PlayerCombatController player)
        {
            if (skill == null || player == null)
            {
                return false;
            }

            return skill.ResolveEffectKind() switch
            {
                SkillEffectKind.ShieldScalingAttack => player.ShieldHp > 0,
                SkillEffectKind.ShieldBurstAttack => player.ShieldHp > 0,
                _ => true,
            };
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
            return Mathf.Max(0, skill.power);
        }

        private static int ResolveStageModifier(int stageModifier, int legacyModifier, int fallback)
        {
            if (stageModifier != 0)
            {
                return stageModifier;
            }

            if (legacyModifier != 0)
            {
                return legacyModifier;
            }

            return fallback;
        }

        private static void SpendSkillHpCost(SkillSO skill, PlayerCombatController player)
        {
            if (skill == null || player == null)
            {
                return;
            }

            var percentCost = skill.hpCostPercent > 0f
                ? Mathf.CeilToInt(player.MaxHp * Mathf.Clamp01(skill.hpCostPercent))
                : 0;
            player.SpendHp(Mathf.Max(skill.hpCost, percentCost), skill.hpCostLeavesOne);
        }

        private static int ResolveStatusDuration(SkillSO skill, int fallback)
        {
            return skill != null && skill.statusDuration > 0 ? skill.statusDuration : fallback;
        }

        private static int ResolveStatusDamage(SkillSO skill, int fallback)
        {
            return skill != null && skill.statusDamage > 0 ? skill.statusDamage : fallback;
        }

        private static float ResolveStatusPercent(SkillSO skill, float fallback)
        {
            return skill != null && skill.statusMaxHpDamagePercent > 0f ? skill.statusMaxHpDamagePercent : fallback;
        }

        private static string GetSkillDisplayName(SkillSO skill)
        {
            return string.IsNullOrWhiteSpace(skill.skillName) ? skill.skillId : skill.skillName;
        }
    }
}
