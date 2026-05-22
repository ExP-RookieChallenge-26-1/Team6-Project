using Project2048.Enemy;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Combat
{
    public class DamageCalculator
    {
        private const float DefenseScale = 100f;
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

            var attackValue = Mathf.Max(0, player.EffectiveAttackPower + skillPower + player.EchoDamageBonus);
            return CalculateDamage(
                attackValue,
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

            return CalculateDamage(
                Mathf.Max(0, intent.value),
                target?.EffectiveDefensePower ?? 0,
                enemy?.CriticalChance ?? 0f,
                enemy?.CriticalDamageMultiplier ?? 1f);
        }

        public int CalculateDamage(
            int attackValue,
            int defensePower,
            float criticalChance,
            float criticalDamageMultiplier)
        {
            attackValue = Mathf.Max(0, attackValue);
            if (attackValue == 0)
            {
                return 0;
            }

            var mitigated = attackValue * DefenseScale / (DefenseScale + Mathf.Max(0, defensePower));
            var varied = mitigated * RollDamageVariance();
            if (RollCritical(criticalChance))
            {
                varied *= Mathf.Max(1f, criticalDamageMultiplier);
            }

            return Mathf.Max(1, Mathf.CeilToInt(varied));
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
