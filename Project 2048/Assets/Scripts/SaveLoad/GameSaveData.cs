using System;
using Project2048.Core;
using Project2048.Rewards;

namespace Project2048.Save
{
    [Serializable]
    public class GameSaveData
    {
        public int version = 1;
        public string savedAtUtc;
        public bool hasActiveRun;
        public int currentStageIndex = 1;

        public bool hasCurrentHp;
        public int currentHp;
        public int extraBoardMoveCount;
        public int permanentMaxHpBonus;
        public int permanentAttackPowerBonus;
        public int permanentDefensePowerBonus;
        public float permanentCriticalChanceBonus;
        public float permanentCriticalDamageMultiplierBonus;
        public int nextCombatAttackPowerBonus;
        public int nextCombatDefensePowerBonus;
        public int nextCombatBoardMoveCountBonus;

        public static GameSaveData From(GameContext context, RunProgress runProgress)
        {
            var progress = runProgress ?? new RunProgress();
            return new GameSaveData
            {
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                hasActiveRun = context != null && context.IsRunActive,
                currentStageIndex = context != null ? context.CurrentStageIndex : 1,
                hasCurrentHp = progress.HasCurrentHp,
                currentHp = progress.CurrentHp,
                extraBoardMoveCount = progress.ExtraBoardMoveCount,
                permanentMaxHpBonus = progress.PermanentMaxHpBonus,
                permanentAttackPowerBonus = progress.PermanentAttackPowerBonus,
                permanentDefensePowerBonus = progress.PermanentDefensePowerBonus,
                permanentCriticalChanceBonus = progress.PermanentCriticalChanceBonus,
                permanentCriticalDamageMultiplierBonus = progress.PermanentCriticalDamageMultiplierBonus,
                nextCombatAttackPowerBonus = progress.NextCombatAttackPowerBonus,
                nextCombatDefensePowerBonus = progress.NextCombatDefensePowerBonus,
                nextCombatBoardMoveCountBonus = progress.NextCombatBoardMoveCountBonus,
            };
        }

        public void ApplyTo(GameContext context)
        {
            if (context == null)
            {
                return;
            }

            context.SetRunActive(hasActiveRun);
            context.SetStageIndex(currentStageIndex);
        }

        public void ApplyTo(RunProgress runProgress)
        {
            runProgress?.RestoreState(
                hasCurrentHp,
                currentHp,
                extraBoardMoveCount,
                permanentMaxHpBonus,
                permanentAttackPowerBonus,
                permanentDefensePowerBonus,
                permanentCriticalChanceBonus,
                permanentCriticalDamageMultiplierBonus,
                nextCombatAttackPowerBonus,
                nextCombatDefensePowerBonus,
                nextCombatBoardMoveCountBonus);
        }
    }
}
