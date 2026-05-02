using System.Collections.Generic;
using System.Linq;
using Project2048.Combat;
using Project2048.Skills;

namespace Project2048.Prototype
{
    // 프로토타입 UI가 지금 어느 패널을 보여줄지 나타냅니다.
    // 전투 규칙은 CombatManager가 결정하고, 이 값은 화면 전환에만 씁니다.
    public enum PrototypeCombatScreenMode
    {
        Board,
        ActionCategory,
        ActionSkills,
        EnemyTurn,
    }

    public class PrototypeCombatUiState
    {
        public PrototypeCombatScreenMode ScreenMode { get; private set; } = PrototypeCombatScreenMode.Board;
        public SkillType? SelectedCategory { get; private set; }

        public void Sync(CombatSnapshot snapshot)
        {
            if (snapshot == null)
            {
                ScreenMode = PrototypeCombatScreenMode.Board;
                SelectedCategory = null;
                return;
            }

            // snapshot의 전투 phase를 화면 모드로 번역합니다.
            // ActionPhase에서는 사용자가 고른 공격/방어 카테고리를 유지해야 하므로 SelectedCategory를 봅니다.
            switch (snapshot.Phase)
            {
                case CombatPhase.BoardPhase:
                    ScreenMode = PrototypeCombatScreenMode.Board;
                    SelectedCategory = null;
                    break;
                case CombatPhase.ActionPhase:
                    ScreenMode = SelectedCategory.HasValue
                        ? PrototypeCombatScreenMode.ActionSkills
                        : PrototypeCombatScreenMode.ActionCategory;
                    break;
                case CombatPhase.EnemyTurn:
                    ScreenMode = PrototypeCombatScreenMode.EnemyTurn;
                    SelectedCategory = null;
                    break;
                default:
                    if (snapshot.Phase != CombatPhase.Victory && snapshot.Phase != CombatPhase.Defeat)
                    {
                        ScreenMode = PrototypeCombatScreenMode.Board;
                        SelectedCategory = null;
                    }
                    break;
            }
        }

        public void SelectCategory(SkillType skillType)
        {
            SelectedCategory = skillType;
            ScreenMode = PrototypeCombatScreenMode.ActionSkills;
        }

        public void ClearCategory()
        {
            SelectedCategory = null;
            ScreenMode = PrototypeCombatScreenMode.ActionCategory;
        }

        public List<SkillSnapshot> GetVisibleSkills(CombatSnapshot snapshot)
        {
            if (snapshot == null || !SelectedCategory.HasValue)
            {
                return new List<SkillSnapshot>();
            }

            return snapshot.Skills
                .Where(skill => skill.SkillType == SelectedCategory.Value)
                .ToList();
        }
    }
}
