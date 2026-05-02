using Project2048.Enemy;
using Project2048.Skills;

namespace Project2048.Combat
{
    public class DamageCalculator
    {
        public int CalculatePlayerSkillDamage(PlayerCombatController player, SkillSO skill)
        {
            return player.AttackPower + skill.power;
        }

        public int CalculateEnemyDamage(EnemyIntent intent)
        {
            return intent?.value ?? 0;
        }
    }
}
