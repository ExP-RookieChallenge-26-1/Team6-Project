using System;
using System.Collections.Generic;
using Project2048.Combat;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Rewards
{
    [Serializable]
    public readonly struct NextCombatBuff
    {
        public NextCombatBuff(int attackStageBonus, int defenseStageBonus, int boardMoveCountBonus)
        {
            AttackStageBonus = attackStageBonus;
            DefenseStageBonus = defenseStageBonus;
            BoardMoveCountBonus = boardMoveCountBonus;
        }

        public int AttackStageBonus { get; }
        public int DefenseStageBonus { get; }
        public int BoardMoveCountBonus { get; }

        public int AttackPowerBonus => AttackStageBonus;
        public int DefensePowerBonus => DefenseStageBonus;
    }

    [Serializable]
    public class RunProgress
    {
        [SerializeField] private bool hasCurrentHp;
        [SerializeField] private int currentHp;
        [SerializeField] private int extraBoardMoveCount;
        [SerializeField] private int permanentMaxHpBonus;
        [SerializeField] private int permanentAttackPowerBonus;
        [SerializeField] private int permanentDefensePowerBonus;
        [SerializeField] private float permanentCriticalChanceBonus;
        [SerializeField] private float permanentCriticalDamageMultiplierBonus;
        [SerializeField] private int nextCombatAttackPowerBonus;
        [SerializeField] private int nextCombatDefensePowerBonus;
        [SerializeField] private int nextCombatBoardMoveCountBonus;
        [SerializeField] private List<SkillSO> equippedSkills = new();

        public bool HasCurrentHp => hasCurrentHp;
        public int CurrentHp => currentHp;
        public int ExtraBoardMoveCount => extraBoardMoveCount;
        public int PermanentMaxHpBonus => permanentMaxHpBonus;
        public int PermanentAttackPowerBonus => permanentAttackPowerBonus;
        public int PermanentDefensePowerBonus => permanentDefensePowerBonus;
        public float PermanentCriticalChanceBonus => permanentCriticalChanceBonus;
        public float PermanentCriticalDamageMultiplierBonus => permanentCriticalDamageMultiplierBonus;
        public int NextCombatAttackStageBonus => nextCombatAttackPowerBonus;
        public int NextCombatDefenseStageBonus => nextCombatDefensePowerBonus;
        public int NextCombatAttackPowerBonus => nextCombatAttackPowerBonus;
        public int NextCombatDefensePowerBonus => nextCombatDefensePowerBonus;
        public int NextCombatBoardMoveCountBonus => nextCombatBoardMoveCountBonus;
        public IReadOnlyList<SkillSO> EquippedSkills => equippedSkills ??= new List<SkillSO>();
        public bool HasEquippedSkills => EquippedSkills.Count > 0;

        public void Reset()
        {
            hasCurrentHp = false;
            currentHp = 0;
            extraBoardMoveCount = 0;
            permanentMaxHpBonus = 0;
            permanentAttackPowerBonus = 0;
            permanentDefensePowerBonus = 0;
            permanentCriticalChanceBonus = 0f;
            permanentCriticalDamageMultiplierBonus = 0f;
            nextCombatAttackPowerBonus = 0;
            nextCombatDefensePowerBonus = 0;
            nextCombatBoardMoveCountBonus = 0;
            equippedSkills?.Clear();
        }

        public void CapturePlayer(PlayerCombatController player)
        {
            if (player == null)
            {
                return;
            }

            hasCurrentHp = true;
            currentHp = Mathf.Clamp(player.CurrentHp, 0, player.MaxHp);
            CapturePlayerSkills(player.Skills);
        }

        public void CapturePlayerSkills(IEnumerable<SkillSO> skills)
        {
            equippedSkills ??= new List<SkillSO>();
            equippedSkills.Clear();
            if (skills == null)
            {
                return;
            }

            foreach (var skill in skills)
            {
                if (skill == null)
                {
                    continue;
                }

                equippedSkills.Add(skill);
                if (equippedSkills.Count >= PlayerCombatController.MaxEquippedSkillSlots)
                {
                    break;
                }
            }
        }

        public int ResolveStartingHp(int maxHp)
        {
            maxHp = Mathf.Max(1, maxHp);
            return hasCurrentHp
                ? Mathf.Clamp(currentHp, 1, maxHp)
                : maxHp;
        }

        public int HealByFlatAmount(int maxHp, int amount)
        {
            maxHp = Mathf.Max(1, maxHp);
            var before = hasCurrentHp ? Mathf.Clamp(currentHp, 0, maxHp) : maxHp;
            currentHp = Mathf.Clamp(before + Mathf.Max(0, amount), 0, maxHp);
            hasCurrentHp = true;
            return currentHp - before;
        }

        public int HealByMaxHpPercent(int maxHp, float percentOfMaxHp)
        {
            maxHp = Mathf.Max(1, maxHp);
            var amount = Mathf.CeilToInt(maxHp * Mathf.Clamp01(percentOfMaxHp));
            return HealByFlatAmount(maxHp, amount);
        }

        public void AddBoardMoveCount(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            extraBoardMoveCount += amount;
        }

        public void AddPermanentStats(
            int maxHpBonus,
            int attackPowerBonus,
            int defensePowerBonus,
            float criticalChanceBonus,
            float criticalDamageMultiplierBonus)
        {
            permanentMaxHpBonus += Mathf.Max(0, maxHpBonus);
            permanentAttackPowerBonus += Mathf.Max(0, attackPowerBonus);
            permanentDefensePowerBonus += Mathf.Max(0, defensePowerBonus);
            permanentCriticalChanceBonus = Mathf.Clamp01(permanentCriticalChanceBonus + Mathf.Max(0f, criticalChanceBonus));
            permanentCriticalDamageMultiplierBonus += Mathf.Max(0f, criticalDamageMultiplierBonus);
        }

        public void AddNextCombatBuff(int attackPowerBonus, int defensePowerBonus, int boardMoveCountBonus)
        {
            AddNextCombatRankBuff(attackPowerBonus, defensePowerBonus, boardMoveCountBonus);
        }

        public void AddNextCombatRankBuff(int attackStageBonus, int defenseStageBonus, int boardMoveCountBonus)
        {
            nextCombatAttackPowerBonus += Mathf.Max(0, attackStageBonus);
            nextCombatDefensePowerBonus += Mathf.Max(0, defenseStageBonus);
            nextCombatBoardMoveCountBonus += Mathf.Max(0, boardMoveCountBonus);
        }

        public NextCombatBuff ConsumeNextCombatBuffs()
        {
            var buff = new NextCombatBuff(
                nextCombatAttackPowerBonus,
                nextCombatDefensePowerBonus,
                nextCombatBoardMoveCountBonus);

            nextCombatAttackPowerBonus = 0;
            nextCombatDefensePowerBonus = 0;
            nextCombatBoardMoveCountBonus = 0;
            return buff;
        }
    }
}
