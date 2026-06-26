using Project2048.Combat;
using Project2048.Enemy;
using UnityEngine;

namespace Project2048.Skills
{
    internal static class SkillAttackExecutor
    {
        internal static int ExecuteSkillAttack(
            SkillSO skill,
            PlayerCombatController player,
            EnemyController target,
            DamageCalculator damageCalculator,
            int? powerOverride = null,
            float? lifeStealPercentOverride = null,
            DamageStatSource? statSourceOverride = null,
            bool consumeAllShieldAfterAttack = false,
            SkillExecutionContext context = null)
        {
            if (skill == null)
            {
                return 0;
            }

            return ExecuteAttack(
                powerOverride ?? skill.power,
                skill.selfExtraAttackHits,
                lifeStealPercentOverride ?? skill.lifeStealPercent,
                statSourceOverride ?? skill.damageStatSource,
                consumeAllShieldAfterAttack,
                player,
                target,
                damageCalculator,
                skill.shieldPiercePercent,
                context);
        }

        internal static int ExecuteAttack(
            int skillPower,
            int skillExtraHits,
            float lifeStealPercent,
            DamageStatSource statSource,
            bool consumeAllShieldAfterAttack,
            PlayerCombatController player,
            EnemyController target,
            DamageCalculator damageCalculator,
            int shieldPiercePercent = 0,
            SkillExecutionContext context = null)
        {
            if (player == null || target == null)
            {
                return 0;
            }

            var attackStat = ResolveDamageStat(player, statSource);
            player.TryConsumeNextAttackModifiers(out var powerMultiplier, out var nextAttackHitCount, out var nextAttackHitPowerMultiplier);
            var hitCount = Mathf.Max(1, 1 + Mathf.Max(0, skillExtraHits));
            if (nextAttackHitCount > 1)
            {
                hitCount = Mathf.Max(hitCount, nextAttackHitCount);
            }

            var hitPowerMultiplier = hitCount > 1 ? nextAttackHitPowerMultiplier : 1f;
            var effectivePower = Mathf.Max(0, Mathf.RoundToInt(skillPower * powerMultiplier * hitPowerMultiplier));
            var totalHpDamage = 0;
            var anyCriticalHpDamage = false;
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var targetThornShieldBeforeHit = target.ThornRetaliationShieldHp;
                var shouldRetaliate = targetThornShieldBeforeHit > 0 && target.ThornRetaliationDamage > 0;
                var retaliationDamage = target.ThornRetaliationDamage;
                var damageResult = damageCalculator.CalculatePlayerSkillDamageResultFromStat(
                    attackStat,
                    effectivePower,
                    target,
                    player.CriticalChance,
                    player.CriticalDamageMultiplier);
                var hpDamage = target.TakeDamage(damageResult.Amount, shieldPiercePercent / 100f);
                totalHpDamage += hpDamage;
                anyCriticalHpDamage |= hpDamage > 0 && damageResult.IsCritical;
                target.TriggerOnAttackedStatusDamage();
                if (shouldRetaliate && retaliationDamage > 0)
                {
                    player.TakeDamage(damageCalculator.CalculateMoveDamage(
                        targetThornShieldBeforeHit,
                        retaliationDamage,
                        player.EffectiveDefensePower,
                        target.CriticalChance,
                        target.CriticalDamageMultiplier));
                }

                if (player.IsDead || target.IsDead)
                {
                    break;
                }
            }

            context?.ReportEnemyDamagePopup(target, totalHpDamage, anyCriticalHpDamage);

            if (consumeAllShieldAfterAttack)
            {
                player.ConsumeAllShield();
            }

            if (lifeStealPercent > 0f && totalHpDamage > 0)
            {
                player.RestoreHp(Mathf.CeilToInt(totalHpDamage * Mathf.Clamp01(lifeStealPercent)));
            }

            return totalHpDamage;
        }

        internal static void ExecuteOpenWoundAttack(
            SkillSO skill,
            PlayerCombatController player,
            EnemyController target,
            DamageCalculator damageCalculator,
            SkillExecutionContext context = null)
        {
            var targetHasStatus = target != null && target.HasPoisonOrBleed;
            var power = skill.power + (targetHasStatus ? ResolveConditionalPowerBonus(skill, 50) : 0);
            ExecuteSkillAttack(skill, player, target, damageCalculator, powerOverride: power, context: context);
            if (targetHasStatus)
            {
                target.ExtendPoisonAndBleed(1);
            }
        }

        internal static int ExecuteHybridDamage(
            SkillSO skill,
            PlayerCombatController player,
            EnemyController target,
            DamageCalculator damageCalculator,
            SkillExecutionContext context = null)
        {
            if (skill == null || skill.power <= 0)
            {
                return 0;
            }

            return ExecuteSkillAttack(skill, player, target, damageCalculator, context: context);
        }

        internal static float ResolveLifeStealPercent(SkillSO skill)
        {
            return skill.lifeStealPercent > 0f ? skill.lifeStealPercent : 0.5f;
        }

        internal static int ResolveExecutePower(SkillSO skill, EnemyController target)
        {
            if (skill == null || target == null)
            {
                return skill != null ? skill.power : 0;
            }

            var threshold = skill.conditionalHpThreshold > 0f ? skill.conditionalHpThreshold : 0.3f;
            return target.CurrentHp <= Mathf.CeilToInt(target.MaxHp * threshold)
                ? skill.power * 2
                : skill.power;
        }

        internal static int ResolveOverburnPower(SkillSO skill, SkillExecutionContext context)
        {
            if (skill == null)
            {
                return 0;
            }

            var extraCost = context?.CostWallet?.CurrentCost ?? 0;
            if (extraCost > 0)
            {
                context.CostWallet.Spend(extraCost);
            }

            var extraPowerPerCost = skill.extraPowerPerConsumedCost > 0 ? skill.extraPowerPerConsumedCost : 10;
            return skill.power + extraCost / 10 * extraPowerPerCost;
        }

        private static int ResolveDamageStat(PlayerCombatController player, DamageStatSource statSource)
        {
            if (player == null)
            {
                return 0;
            }

            return statSource switch
            {
                DamageStatSource.DefensePower => player.EffectiveDefensePower,
                DamageStatSource.ShieldHp => player.ShieldHp,
                _ => player.EffectiveAttackPower,
            };
        }

        private static int ResolveConditionalPowerBonus(SkillSO skill, int fallback)
        {
            return skill != null && skill.conditionalPowerBonus > 0 ? skill.conditionalPowerBonus : fallback;
        }
    }
}
