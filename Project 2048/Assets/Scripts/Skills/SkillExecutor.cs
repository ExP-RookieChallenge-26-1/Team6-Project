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
                    ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, skill.damageStatSource, false, player, target, damageCalculator);
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.BoardMoveBonusAttack:
                case SkillEffectKind.BoardMovePenaltyAttack:
                    ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, skill.damageStatSource, false, player, target, damageCalculator);
                    player.ApplyNextTurnBoardMoveCountModifier(skill.nextBoardMoveCountModifier);
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.LifeStealAttack:
                    ExecuteAttack(skill.power, skill.selfExtraAttackHits, ResolveLifeStealPercent(skill), skill.damageStatSource, false, player, target, damageCalculator);
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.SacrificeAttack:
                    SpendSkillHpCost(skill, player);
                    ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, skill.damageStatSource, false, player, target, damageCalculator);
                    ApplyTargetModifiers(skill, target);
                    break;
                case SkillEffectKind.ShieldScalingAttack:
                    ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, DamageStatSource.ShieldHp, false, player, target, damageCalculator);
                    break;
                case SkillEffectKind.ShieldBurstAttack:
                    ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, DamageStatSource.ShieldHp, true, player, target, damageCalculator);
                    break;
                case SkillEffectKind.DefenseScalingAttack:
                    ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, DamageStatSource.DefensePower, false, player, target, damageCalculator);
                    break;
                case SkillEffectKind.BasicDefense:
                    player.AddBlock(skill.power);
                    break;
                case SkillEffectKind.ThornGuard:
                    player.ApplyThornGuard(ResolveDefenseGain(skill, player), skill.selfThornRetaliationDamage);
                    break;
                case SkillEffectKind.AttackStageDown:
                    ExecuteHybridDamage(skill, player, target, damageCalculator);
                    if (target != null)
                    {
                        target.ApplyAttackModifier(ResolveStageModifier(skill.targetAttackStageModifier, skill.targetAttackModifier, -1));
                    }
                    break;
                case SkillEffectKind.DefenseStageDown:
                    ExecuteHybridDamage(skill, player, target, damageCalculator);
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
                default:
                    if (skill.skillType == SkillType.Attack)
                    {
                        ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, skill.damageStatSource, false, player, target, damageCalculator);
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
            return ExecuteAttack(skillPower, 0, 0f, DamageStatSource.AttackPower, false, player, target, damageCalculator);
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
            return ExecuteAttack(skillPower, 0, 0f, statSource, false, player, target, damageCalculator);
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

        private static int ExecuteAttack(
            int skillPower,
            int skillExtraHits,
            float lifeStealPercent,
            DamageStatSource statSource,
            bool consumeAllShieldAfterAttack,
            PlayerCombatController player,
            EnemyController target,
            DamageCalculator damageCalculator)
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
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var targetShieldBeforeHit = target.ShieldHp;
                var shouldRetaliate = targetShieldBeforeHit > 0 && target.ThornRetaliationDamage > 0;
                var retaliationDamage = target.ThornRetaliationDamage;
                var damage = damageCalculator.CalculatePlayerSkillDamageFromStat(
                    attackStat,
                    effectivePower,
                    target,
                    player.CriticalChance,
                    player.CriticalDamageMultiplier);
                totalHpDamage += target.TakeDamage(damage);
                if (shouldRetaliate && retaliationDamage > 0)
                {
                    player.TakeDamage(damageCalculator.CalculateMoveDamage(
                        targetShieldBeforeHit,
                        retaliationDamage,
                        player.EffectiveDefensePower,
                        target.CriticalChance,
                        target.CriticalDamageMultiplier));
                }

                if (target.IsDead)
                {
                    break;
                }
            }

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

            return ExecuteAttack(skill.power, skill.selfExtraAttackHits, skill.lifeStealPercent, skill.damageStatSource, false, player, target, damageCalculator);
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
