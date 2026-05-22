using System;
using Project2048.Combat;
using UnityEngine;

namespace Project2048.Rewards
{
    [Serializable]
    public readonly struct NextCombatBuff
    {
        public NextCombatBuff(int attackPowerBonus, int defensePowerBonus, int boardMoveCountBonus)
        {
            AttackPowerBonus = attackPowerBonus;
            DefensePowerBonus = defensePowerBonus;
            BoardMoveCountBonus = boardMoveCountBonus;
        }

        public int AttackPowerBonus { get; }
        public int DefensePowerBonus { get; }
        public int BoardMoveCountBonus { get; }
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

        public bool HasCurrentHp => hasCurrentHp;
        public int CurrentHp => currentHp;
        public int ExtraBoardMoveCount => extraBoardMoveCount;
        public int PermanentMaxHpBonus => permanentMaxHpBonus;
        public int PermanentAttackPowerBonus => permanentAttackPowerBonus;
        public int PermanentDefensePowerBonus => permanentDefensePowerBonus;
        public float PermanentCriticalChanceBonus => permanentCriticalChanceBonus;
        public float PermanentCriticalDamageMultiplierBonus => permanentCriticalDamageMultiplierBonus;
        public int NextCombatAttackPowerBonus => nextCombatAttackPowerBonus;
        public int NextCombatDefensePowerBonus => nextCombatDefensePowerBonus;
        public int NextCombatBoardMoveCountBonus => nextCombatBoardMoveCountBonus;

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
        }

        public void CapturePlayer(PlayerCombatController player)
        {
            if (player == null)
            {
                return;
            }

            hasCurrentHp = true;
            currentHp = Mathf.Clamp(player.CurrentHp, 0, player.MaxHp);
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
            nextCombatAttackPowerBonus += Mathf.Max(0, attackPowerBonus);
            nextCombatDefensePowerBonus += Mathf.Max(0, defensePowerBonus);
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
