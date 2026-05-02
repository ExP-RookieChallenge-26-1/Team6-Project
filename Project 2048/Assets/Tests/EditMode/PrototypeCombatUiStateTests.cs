using System.Collections.Generic;
using NUnit.Framework;
using Project2048.Combat;
using Project2048.Prototype;
using Project2048.Skills;

namespace Project2048.Tests
{
    public class PrototypeCombatUiStateTests
    {
        [Test]
        public void Sync_BoardPhase_ShowsBoardOnly_AndClearsSelectedCategory()
        {
            var state = new PrototypeCombatUiState();
            state.SelectCategory(SkillType.Attack);

            state.Sync(CreateSnapshot(CombatPhase.BoardPhase));

            Assert.That(state.ScreenMode, Is.EqualTo(PrototypeCombatScreenMode.Board));
            Assert.That(state.SelectedCategory, Is.Null);
        }

        [Test]
        public void Sync_ActionPhase_ShowsCategorySelection_WhenNoCategoryChosen()
        {
            var state = new PrototypeCombatUiState();

            state.Sync(CreateSnapshot(CombatPhase.ActionPhase));

            Assert.That(state.ScreenMode, Is.EqualTo(PrototypeCombatScreenMode.ActionCategory));
        }

        [Test]
        public void GetVisibleSkills_ReturnsOnlyChosenCategorySkills()
        {
            var state = new PrototypeCombatUiState();
            var snapshot = CreateSnapshot(
                CombatPhase.ActionPhase,
                new SkillSnapshot { SkillId = "attack_1", SkillType = SkillType.Attack },
                new SkillSnapshot { SkillId = "attack_2", SkillType = SkillType.Attack },
                new SkillSnapshot { SkillId = "defense_1", SkillType = SkillType.Defense });

            state.Sync(snapshot);
            state.SelectCategory(SkillType.Defense);
            var visible = state.GetVisibleSkills(snapshot);

            Assert.That(state.ScreenMode, Is.EqualTo(PrototypeCombatScreenMode.ActionSkills));
            Assert.That(visible.Count, Is.EqualTo(1));
            Assert.That(visible[0].SkillId, Is.EqualTo("defense_1"));
        }

        private static CombatSnapshot CreateSnapshot(CombatPhase phase, params SkillSnapshot[] skills)
        {
            return new CombatSnapshot
            {
                Phase = phase,
                Skills = new List<SkillSnapshot>(skills),
                Board = new int[4, 4],
                Player = new PlayerCombatSnapshot(),
            };
        }
    }
}
