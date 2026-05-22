using Project2048.Enemy;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Combat
{
    public class DamageCalculator
    {
        private const int MinimumDefenseStat = 1;
        private const float MovePowerScale = 10f;
        private const float MinimumDamageVariance = 0.85f;
        private const float MaximumDamageVariance = 1f;

        private readonly System.Random random;

        public DamageCalculator(System.Random random = null)
        {
            this.random = random ?? new System.Random();
        }

        public int CalculatePlayerSkillDamage(PlayerCombatController player, SkillSO skill)
        {
            return CalculatePlayerSkillDamage(player, skill, null);
        }

        public int CalculatePlayerSkillDamage(PlayerCombatController player, SkillSO skill, EnemyController target)
        {
            if (player == null || skill == null)
            {
                return 0;
            }

            return CalculatePlayerSkillDamage(player, skill.power, target, skill.damageStatSource);
        }

        public int CalculatePlayerSkillDamage(PlayerCombatController player, int skillPower, EnemyController target)
        {
            return CalculatePlayerSkillDamage(player, skillPower, target, DamageStatSource.AttackPower);
        }

        public int CalculatePlayerSkillDamage(
            PlayerCombatController player,
            int skillPower,
            EnemyController target,
            DamageStatSource statSource)
        {
            if (player == null)
            {
                return 0;
            }

            return CalculateMoveDamage(
                ResolvePlayerDamageStat(player, statSource),
                skillPower,
                target?.EffectiveDefensePower ?? 0,
                player.CriticalChance,
                player.CriticalDamageMultiplier);
        }

        public int CalculatePlayerSkillDamageFromStat(
            int attackStat,
            int skillPower,
            EnemyController target,
            float criticalChance,
            float criticalDamageMultiplier)
        {
            return CalculateMoveDamage(
                attackStat,
                skillPower,
                target?.EffectiveDefensePower ?? 0,
                criticalChance,
                criticalDamageMultiplier);
        }

        public int CalculateEnemyDamage(EnemyIntent intent)
        {
            return intent?.value ?? 0;
        }

        public int CalculateEnemyDamage(EnemyController enemy, EnemyIntent intent, PlayerCombatController target)
        {
            if (intent == null)
            {
                return 0;
            }

            return CalculateMoveDamage(
                ResolveEnemyDamageStat(enemy, intent.damageStatSource),
                intent.movePower > 0 ? intent.movePower : Mathf.Max(0, intent.value) * (int)MovePowerScale,
                target?.EffectiveDefensePower ?? 0,
                enemy?.CriticalChance ?? 0f,
                enemy?.CriticalDamageMultiplier ?? 1f);
        }

        public int CalculateMoveDamage(
            int attackPower,
            int movePower,
            int defensePower,
            float criticalChance,
            float criticalDamageMultiplier)
        {
            movePower = Mathf.Max(0, movePower);
            attackPower = Mathf.Max(0, attackPower);
            if (attackPower == 0 || movePower == 0)
            {
                return 0;
            }

            var attackDefenseRatio = attackPower / (float)Mathf.Max(MinimumDefenseStat, defensePower);
            var baseDamage = (movePower / MovePowerScale) * attackDefenseRatio;
            var varied = baseDamage * RollDamageVariance();
            if (RollCritical(criticalChance))
            {
                varied *= Mathf.Max(1f, criticalDamageMultiplier);
            }

            return Mathf.Max(1, Mathf.CeilToInt(varied));
        }

        public int CalculateDamage(
            int attackValue,
            int defensePower,
            float criticalChance,
            float criticalDamageMultiplier)
        {
            return CalculateMoveDamage(
                attackValue,
                movePower: (int)MovePowerScale,
                defensePower,
                criticalChance,
                criticalDamageMultiplier);
        }

        private float RollDamageVariance()
        {
            return Mathf.Lerp(MinimumDamageVariance, MaximumDamageVariance, (float)random.NextDouble());
        }

        private static int ResolvePlayerDamageStat(PlayerCombatController player, DamageStatSource statSource)
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

        private static int ResolveEnemyDamageStat(EnemyController enemy, DamageStatSource statSource)
        {
            if (enemy == null)
            {
                return 0;
            }

            return statSource switch
            {
                DamageStatSource.DefensePower => enemy.EffectiveDefensePower,
                DamageStatSource.ShieldHp => enemy.ShieldHp,
                _ => enemy.EffectiveAttackPower,
            };
        }

        private bool RollCritical(float criticalChance)
        {
            criticalChance = Mathf.Clamp01(criticalChance);
            if (criticalChance <= 0f)
            {
                return false;
            }

            if (criticalChance >= 1f)
            {
                return true;
            }

            return random.NextDouble() < criticalChance;
        }
    }
}
