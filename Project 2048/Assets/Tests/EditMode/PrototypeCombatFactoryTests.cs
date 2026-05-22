using System.Linq;
using NUnit.Framework;
using Project2048.Enemy;
using Project2048.Prototype;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Tests
{
    public class PrototypeCombatFactoryTests
    {
        [Test]
        public void CreateDefaultLoadout_EquipsFourPrototypeSkills()
        {
            var loadout = PrototypeCombatFactory.CreateDefaultLoadout();

            try
            {
                Assert.That(loadout.PlayerData, Is.Not.Null);
                Assert.That(loadout.EnemyData, Is.Not.Null);
                Assert.That(loadout.PlayerData.maxHp, Is.EqualTo(240));
                Assert.That(loadout.PlayerData.attackPower, Is.EqualTo(10));
                Assert.That(loadout.Skills.Count, Is.EqualTo(11));
                Assert.That(loadout.Skills.Select(skill => skill.skillId), Is.EqualTo(new[]
                {
                    "quick-stab",
                    "light-shot",
                    "heavy-strike",
                    "gather-light",
                    "low-stance",
                    "light-guard",
                    "shield-bash",
                    "shield-burst",
                    "iron-wall",
                    "body-press",
                    "flash",
                }));
                Assert.That(loadout.PlayerData.startingSkills.Count, Is.EqualTo(4));
                Assert.That(loadout.PlayerData.startingSkills.Select(skill => skill.cost), Is.EqualTo(new[] { 6, 6, 5, 4 }));
            }
            finally
            {
                loadout.Dispose();
            }
        }

        [Test]
        public void CreateDefaultLoadout_UsesCurrentPrototypeSkillNames()
        {
            var loadout = PrototypeCombatFactory.CreateDefaultLoadout();

            try
            {
                Assert.That(loadout.Skills.Select(skill => skill.skillName), Is.EqualTo(new[]
                {
                    "Quick Stab",
                    "Light Shot",
                    "Heavy Strike",
                    "Gather Light",
                    "Low Stance",
                    "Light Guard",
                    "Shield Bash",
                    "Shield Burst",
                    "Iron Wall",
                    "Body Press",
                    "Flash",
                }));
            }
            finally
            {
                loadout.Dispose();
            }
        }

        [Test]
        public void CreateDefaultLoadout_AssignsReusableVfxFamilies()
        {
            var loadout = PrototypeCombatFactory.CreateDefaultLoadout();

            try
            {
                Assert.That(loadout.Skills.Select(skill => skill.vfxFamily), Is.EqualTo(new[]
                {
                    SkillVfxFamily.SlashArc,
                    SkillVfxFamily.LightProjectile,
                    SkillVfxFamily.SlashArc,
                    SkillVfxFamily.LightProjectile,
                    SkillVfxFamily.ShieldDome,
                    SkillVfxFamily.ShieldDome,
                    SkillVfxFamily.ShieldDome,
                    SkillVfxFamily.ShieldDome,
                    SkillVfxFamily.BuffAura,
                    SkillVfxFamily.ImpactBurst,
                    SkillVfxFamily.DebuffWave,
                }));
                Assert.That(loadout.Skills.All(skill => skill.vfxScale > 0f), Is.True);
                Assert.That(loadout.Skills.All(skill => skill.vfxIntensity > 0f), Is.True);
                Assert.That(loadout.Skills.All(skill => skill.vfxRepeatCount >= 1), Is.True);
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
                Assert.That(roster.Where(enemy => enemy.aiStrength == EnemyAiStrength.Normal).Select(enemy => enemy.maxHp), Is.All.EqualTo(160));
                Assert.That(roster.Where(enemy => enemy.aiStrength == EnemyAiStrength.Enhanced).Select(enemy => enemy.maxHp), Is.All.EqualTo(210));
                Assert.That(roster.SelectMany(enemy => enemy.skills).All(skill => skill.vfxFamily != SkillVfxFamily.None), Is.True);
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
