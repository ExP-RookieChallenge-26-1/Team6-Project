using Project2048.Combat;
using Project2048.Enemy;

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

            switch (skill.skillType)
            {
                case SkillType.Attack:
                    if (target != null)
                    {
                        var damage = damageCalculator.CalculatePlayerSkillDamage(player, skill);
                        target.TakeDamage(damage);
                        if (skill.targetAttackModifier != 0)
                        {
                            target.ApplyAttackModifier(skill.targetAttackModifier);
                        }
                    }
                    break;
                case SkillType.Defense:
                    player.GainBlockWithBonus(skill.power);
                    if (skill.selfDefenseBonus != 0)
                    {
                        player.ApplyDefenseBonus(skill.selfDefenseBonus);
                    }
                    break;
            }
        }
    }
}
