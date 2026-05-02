using System.Linq;
using NUnit.Framework;
using Project2048.Enemy;
using Project2048.Prototype;
using UnityEngine;

namespace Project2048.Tests
{
    public class PrototypeCombatFactoryTests
    {
        [Test]
        public void CreateDefaultLoadout_BuildsSixPrototypeSkills_ForTemporaryUi()
        {
            var loadout = PrototypeCombatFactory.CreateDefaultLoadout();

            try
            {
                Assert.That(loadout.PlayerData, Is.Not.Null);
                Assert.That(loadout.EnemyData, Is.Not.Null);
                Assert.That(loadout.Skills.Count, Is.EqualTo(6));
                Assert.That(loadout.Skills.Select(skill => skill.skillId), Is.EqualTo(new[]
                {
                    "attack_1",
                    "attack_2",
                    "attack_3",
                    "defense_1",
                    "defense_2",
                    "defense_3",
                }));
                Assert.That(loadout.PlayerData.startingSkills.Count, Is.EqualTo(6));
            }
            finally
            {
                loadout.Dispose();
            }
        }

        [Test]
        public void CreateDefaultLoadout_UsesKoreanTieredAttackAndDefenseSkillNames()
        {
            var loadout = PrototypeCombatFactory.CreateDefaultLoadout();

            try
            {
                Assert.That(loadout.Skills.Select(skill => skill.skillName), Is.EqualTo(new[]
                {
                    "1단계 공격",
                    "2단계 공격",
                    "3단계 공격",
                    "1단계 방어",
                    "2단계 방어",
                    "3단계 방어",
                }));
            }
            finally
            {
                loadout.Dispose();
            }
        }

        [Test]
        public void CreatePrototypeEnemyRoster_BuildsTwelveTemporaryAiProfiles()
        {
            var roster = PrototypeCombatFactory.CreatePrototypeEnemyRoster();

            try
            {
                Assert.That(roster.Count, Is.EqualTo(12));
                Assert.That(roster.Count(enemy => enemy.aiStrength == EnemyAiStrength.Normal), Is.EqualTo(8));
                Assert.That(roster.Count(enemy => enemy.aiStrength == EnemyAiStrength.Enhanced), Is.EqualTo(4));
                Assert.That(roster.Select(enemy => enemy.aiActionBias).Distinct(), Is.EquivalentTo(new[]
                {
                    EnemyAiActionBias.AttackHeavy,
                    EnemyAiActionBias.DefenseHeavy,
                    EnemyAiActionBias.Balanced,
                }));
                Assert.That(roster.Select(enemy => enemy.aiDebuffPattern).Distinct(), Is.EquivalentTo(new[]
                {
                    EnemyDebuffPattern.FearThenDarkness,
                    EnemyDebuffPattern.DarknessThenFear,
                }));
                Assert.That(roster.All(enemy => enemy.intentPattern.Count == 0), Is.True);
                Assert.That(roster.All(enemy => !string.IsNullOrWhiteSpace(enemy.GetAiProfileLabel())), Is.True);
            }
            finally
            {
                foreach (var enemy in roster)
                {
                    Object.DestroyImmediate(enemy);
                }
            }
        }
    }
}
