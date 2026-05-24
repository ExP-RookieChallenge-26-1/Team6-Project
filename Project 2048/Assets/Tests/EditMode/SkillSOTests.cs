using System.Reflection;
using System.Linq;
using Project2048.Enemy;
using NUnit.Framework;
using Project2048.Combat;
using Project2048.Rewards;
using Project2048.Skills;
using UnityEditor;
using UnityEngine;

namespace Project2048.Tests
{
    public class SkillSOTests
    {
        [Test]
        public void OnValidate_ClampsReusableVfxParameters()
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            try
            {
                skill.vfxScale = 0f;
                skill.vfxIntensity = -1f;
                skill.vfxRepeatCount = 0;

                typeof(SkillSO)
                    .GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(skill, null);

                Assert.That(skill.vfxScale, Is.EqualTo(0.01f).Within(0.0001f));
                Assert.That(skill.vfxIntensity, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(skill.vfxRepeatCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void PrototypeSkillAssets_MatchCurrentBalancePool()
        {
            var skills = LoadPrototypeSkills();

            Assert.That(skills.ContainsKey("counter"), Is.False);
            Assert.That(skills.ContainsKey("guard-break"), Is.False);
            Assert.That(skills.ContainsKey("feint-strike"), Is.False);
            var bloodFang = skills["blood-fang"];
            Assert.That(bloodFang.ResolveEffectKind(), Is.EqualTo(SkillEffectKind.SacrificeAttack));
            Assert.That(bloodFang.power, Is.EqualTo(100));
            Assert.That(bloodFang.lifeStealPercent, Is.Zero);
            Assert.That(bloodFang.description, Does.Not.Contain("회복"));

            var rewardTable = AssetDatabase.LoadAssetAtPath<RewardTableSO>("Assets/Data/Rewards/PrototypeRewardTable.asset");
            Assert.That(rewardTable, Is.Not.Null);
            var rewardSkillIds = new System.Collections.Generic.HashSet<string>();
            foreach (var reward in rewardTable.rewards)
            {
                if (reward != null && reward.skillToLearn != null)
                {
                    rewardSkillIds.Add(reward.skillToLearn.skillId);
                    Assert.That(reward.skillToLearn.CanAppearAsReward, reward.rewardId);
                }
            }

            Assert.That(rewardSkillIds, Does.Not.Contain("counter"));
            Assert.That(rewardSkillIds, Does.Not.Contain("guard-break"));
            Assert.That(rewardSkillIds, Does.Not.Contain("feint-strike"));
            Assert.That(skills.Keys, Is.SupersetOf(new[]
            {
                "bleeding-cut",
                "poison-coat",
                "open-wound",
                "execute",
                "overburn",
                "seal-skill",
                "taunt",
                "crack-brand",
                "afterglow-save",
                "cleanse-hand",
                "black-corrosion",
            }));
            Assert.That(rewardSkillIds, Does.Contain("bleeding-cut"));
            Assert.That(rewardSkillIds, Does.Contain("cleanse-hand"));
            Assert.That(rewardSkillIds, Does.Not.Contain("black-corrosion"));
        }

        [Test]
        public void PrototypePlayerAsset_UsesRequestedStartingSkills()
        {
            var player = AssetDatabase.LoadAssetAtPath<PlayerSO>("Assets/Data/PrototypePlayer.asset");

            Assert.That(player, Is.Not.Null);
            Assert.That(player.startingSkills.ConvertAll(skill => skill.skillId), Is.EqualTo(new[]
            {
                "light-shot",
                "low-stance",
                "flash",
            }));
        }

        [Test]
        public void PrototypeEnemyAssets_EquipCurrentSharedStatusSkills()
        {
            var enemyGuids = AssetDatabase.FindAssets("t:EnemySO", new[] { "Assets/Data/Enemies" });
            var assignedSkillIds = new System.Collections.Generic.HashSet<string>();

            Assert.That(enemyGuids.Length, Is.EqualTo(12));
            foreach (var guid in enemyGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemySO>(path);

                Assert.That(enemy, Is.Not.Null, path);
                Assert.That(enemy.AssignedSkillCount, Is.EqualTo(EnemySO.MaxEquippedSkillSlots), path);
                foreach (var skill in enemy.skills.Take(EnemySO.MaxEquippedSkillSlots))
                {
                    Assert.That(skill, Is.Not.Null, path);
                    Assert.That(skill.CanEnemyUse, Is.True, $"{path}:{skill.skillId}");
                    assignedSkillIds.Add(skill.skillId);
                }
            }

            Assert.That(assignedSkillIds, Is.SupersetOf(new[]
            {
                "bleeding-cut",
                "poison-coat",
                "open-wound",
                "execute",
                "seal-skill",
                "crack-brand",
                "black-corrosion",
            }));
        }

        private static System.Collections.Generic.Dictionary<string, SkillSO> LoadPrototypeSkills()
        {
            var skills = new System.Collections.Generic.Dictionary<string, SkillSO>();
            var skillGuids = AssetDatabase.FindAssets("t:SkillSO", new[] { "Assets/Data/Skills" });
            foreach (var guid in skillGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var skill = AssetDatabase.LoadAssetAtPath<SkillSO>(path);
                Assert.That(skill, Is.Not.Null, path);
                skills[skill.skillId] = skill;
            }

            return skills;
        }
    }
}
