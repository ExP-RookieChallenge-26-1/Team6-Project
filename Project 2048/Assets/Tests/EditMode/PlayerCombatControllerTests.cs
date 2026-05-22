using NUnit.Framework;
using Project2048.Combat;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Tests
{
    public class PlayerCombatControllerTests
    {
        [Test]
        public void Init_StoresPlayerData_ForUiPortraitBinding()
        {
            var playerObject = new GameObject("Player");
            var playerData = ScriptableObject.CreateInstance<PlayerSO>();

            try
            {
                var player = playerObject.AddComponent<PlayerCombatController>();

                player.Init(playerData);

                Assert.That(player.Data, Is.SameAs(playerData));
            }
            finally
            {
                Object.DestroyImmediate(playerData);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void Init_EquipsOnlyFourStartingSkills()
        {
            var playerObject = new GameObject("Player");
            var playerData = ScriptableObject.CreateInstance<PlayerSO>();

            try
            {
                for (var index = 0; index < 6; index++)
                {
                    playerData.startingSkills.Add(CreateSkill($"skill-{index}"));
                }

                var player = playerObject.AddComponent<PlayerCombatController>();

                player.Init(playerData);

                Assert.That(player.Skills.Count, Is.EqualTo(PlayerCombatController.MaxEquippedSkillSlots));
                Assert.That(player.Skills[0].skillId, Is.EqualTo("skill-0"));
                Assert.That(player.Skills[3].skillId, Is.EqualTo("skill-3"));
            }
            finally
            {
                foreach (var skill in playerData.startingSkills)
                {
                    Object.DestroyImmediate(skill);
                }

                Object.DestroyImmediate(playerData);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void Init_UsesPokemonHpFormula_WhenEnabledOnPlayerData()
        {
            var playerObject = new GameObject("Player");
            var playerData = ScriptableObject.CreateInstance<PlayerSO>();

            try
            {
                playerData.usePokemonHpFormula = true;
                playerData.statLevel = 50;
                playerData.baseHp = 40;
                playerData.hpIndividualValue = 0;
                playerData.hpEffortValue = 0;

                var player = playerObject.AddComponent<PlayerCombatController>();

                player.Init(playerData);

                Assert.That(player.MaxHp, Is.EqualTo(100));
                Assert.That(player.CurrentHp, Is.EqualTo(100));
            }
            finally
            {
                Object.DestroyImmediate(playerData);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void TryLearnSkill_WhenSlotsAreFull_ReplacesSelectedSkill()
        {
            var playerObject = new GameObject("Player");
            var playerData = ScriptableObject.CreateInstance<PlayerSO>();
            var learnedSkill = CreateSkill("learned");

            try
            {
                for (var index = 0; index < 4; index++)
                {
                    playerData.startingSkills.Add(CreateSkill($"skill-{index}"));
                }

                var player = playerObject.AddComponent<PlayerCombatController>();
                player.Init(playerData);

                var learned = player.TryLearnSkill(learnedSkill, 2, out var forgottenSkill);

                Assert.That(learned, Is.True);
                Assert.That(forgottenSkill.skillId, Is.EqualTo("skill-2"));
                Assert.That(player.Skills[2], Is.SameAs(learnedSkill));
                Assert.That(player.Skills.Count, Is.EqualTo(4));
            }
            finally
            {
                foreach (var skill in playerData.startingSkills)
                {
                    Object.DestroyImmediate(skill);
                }

                Object.DestroyImmediate(learnedSkill);
                Object.DestroyImmediate(playerData);
                Object.DestroyImmediate(playerObject);
            }
        }

        private static SkillSO CreateSkill(string skillId)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.skillId = skillId;
            return skill;
        }
    }
}
