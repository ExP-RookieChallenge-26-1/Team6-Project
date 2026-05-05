using System;
using Project2048.Combat;
using UnityEngine;

namespace Project2048.Rewards
{
    [Serializable]
    public class RunProgress
    {
        [SerializeField] private bool hasCurrentHp;
        [SerializeField] private int currentHp;
        [SerializeField] private int extraBoardMoveCount;

        public bool HasCurrentHp => hasCurrentHp;
        public int CurrentHp => currentHp;
        public int ExtraBoardMoveCount => extraBoardMoveCount;

        public void Reset()
        {
            hasCurrentHp = false;
            currentHp = 0;
            extraBoardMoveCount = 0;
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

        public int HealByMaxHpPercent(int maxHp, float percentOfMaxHp)
        {
            maxHp = Mathf.Max(1, maxHp);
            var before = hasCurrentHp ? Mathf.Clamp(currentHp, 0, maxHp) : maxHp;
            var amount = Mathf.CeilToInt(maxHp * Mathf.Clamp01(percentOfMaxHp));
            currentHp = Mathf.Clamp(before + amount, 0, maxHp);
            hasCurrentHp = true;
            return currentHp - before;
        }

        public void AddBoardMoveCount(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            extraBoardMoveCount += amount;
        }
    }
}
