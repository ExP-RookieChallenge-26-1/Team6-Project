using System;
using System.Collections.Generic;
using System.Linq;
using Project2048.Core;
using Project2048.Rewards;
using Project2048.Skills;

namespace Project2048.Save
{
    [Serializable]
    public class GameSaveData
    {
        public int version = 2;
        public string savedAtUtc;
        public bool hasActiveRun;
        public int currentStageIndex = 1;
        public List<string> equippedSkillIds = new();

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
                equippedSkillIds = ExtractEquippedSkillIds(progress),
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

        public void ApplyTo(RunProgress runProgress, IEnumerable<SkillSO> knownSkills)
        {
            ApplyTo(runProgress);

            if (knownSkills != null)
            {
                runProgress?.RestoreEquippedSkills(ResolveEquippedSkills(knownSkills));
            }
        }

        private static List<string> ExtractEquippedSkillIds(RunProgress progress)
        {
            return progress.EquippedSkills
                .Where(skill => skill != null && !string.IsNullOrWhiteSpace(skill.skillId))
                .Select(skill => skill.skillId)
                .Distinct()
                .ToList();
        }

        private IEnumerable<SkillSO> ResolveEquippedSkills(IEnumerable<SkillSO> knownSkills)
        {
            if (equippedSkillIds == null || equippedSkillIds.Count == 0 || knownSkills == null)
            {
                return Array.Empty<SkillSO>();
            }

            var skillsById = knownSkills
                .Where(skill => skill != null && !string.IsNullOrWhiteSpace(skill.skillId))
                .GroupBy(skill => skill.skillId)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var resolvedSkills = new List<SkillSO>();
            var restoredIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var skillId in equippedSkillIds)
            {
                if (string.IsNullOrWhiteSpace(skillId) || !restoredIds.Add(skillId))
                {
                    continue;
                }

                if (skillsById.TryGetValue(skillId, out var skill))
                {
                    resolvedSkills.Add(skill);
                }
            }

            return resolvedSkills;
        }
    }
}
