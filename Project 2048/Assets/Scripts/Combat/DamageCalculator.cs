using Project2048.Enemy;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Combat
{
    public class DamageCalculator
    {
        private const int MinimumDefenseStat = 1;
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

            return CalculatePlayerSkillDamage(player, skill.power, target);
        }

        public int CalculatePlayerSkillDamage(PlayerCombatController player, int skillPower, EnemyController target)
        {
            if (player == null)
            {
                return 0;
            }

            return CalculateMoveDamage(
                player.EffectiveAttackPower,
                skillPower + player.EchoDamageBonus,
                target?.EffectiveDefensePower ?? 0,
                player.CriticalChance,
                player.CriticalDamageMultiplier);
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
                enemy?.EffectiveAttackPower ?? 0,
                intent.movePower > 0 ? intent.movePower : intent.value,
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
            var baseDamage = movePower * attackDefenseRatio;
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
                movePower: 1,
                defensePower,
                criticalChance,
                criticalDamageMultiplier);
        }

        private float RollDamageVariance()
        {
            return Mathf.Lerp(MinimumDamageVariance, MaximumDamageVariance, (float)random.NextDouble());
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
