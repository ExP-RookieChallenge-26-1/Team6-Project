using System.Collections.Generic;
using System.Linq;
using Project2048.Combat;

namespace Project2048.Prototype
{
    // Prototype UI state only decides which combat panel is visible.
    // Skill availability and turn rules stay inside CombatManager.
    public enum PrototypeCombatScreenMode
    {
        Board,
        ActionSkills,
        EnemyTurn,
    }

    public class PrototypeCombatUiState
    {
        public PrototypeCombatScreenMode ScreenMode { get; private set; } = PrototypeCombatScreenMode.Board;

        public void Sync(CombatSnapshot snapshot)
        {
            if (snapshot == null)
            {
                ScreenMode = PrototypeCombatScreenMode.Board;
                return;
            }

            switch (snapshot.Phase)
            {
                case CombatPhase.BoardPhase:
                    ScreenMode = PrototypeCombatScreenMode.Board;
                    break;
                case CombatPhase.ActionPhase:
                    ScreenMode = PrototypeCombatScreenMode.ActionSkills;
                    break;
                case CombatPhase.EnemyTurn:
                    ScreenMode = PrototypeCombatScreenMode.EnemyTurn;
                    break;
                default:
                    if (snapshot.Phase != CombatPhase.Victory && snapshot.Phase != CombatPhase.Defeat)
                    {
                        ScreenMode = PrototypeCombatScreenMode.Board;
                    }
                    break;
            }
        }

        public List<SkillSnapshot> GetVisibleSkills(CombatSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Skills == null)
            {
                return new List<SkillSnapshot>();
            }

            return snapshot.Skills
                .Take(PlayerCombatController.MaxEquippedSkillSlots)
                .ToList();
        }
    }
}
