using System.Linq;
using System.Reflection;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Presentation;
using Project2048.Rewards;
using Project2048.Skills;
using NUnit.Framework;
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

            var fireball = skills["fireball"];
            var burstFireball = skills["burst-fireball"];
            var burnOut = skills["burn-out"];
            var overburn = skills["overburn"];
            var recklessBlow = skills["reckless-blow"];
            var afterglowSave = skills["afterglow-save"];
            var cleanseHand = skills["cleanse-hand"];
            var blackCorrosion = skills["black-corrosion"];
            var lightShot = skills["light-shot"];
            var lowStance = skills["low-stance"];
            var lightGuard = skills["light-guard"];
            var bodyPress = skills["body-press"];

            Assert.That(fireball.skillName, Is.EqualTo("\uD654\uC5FC\uAD6C"));
            Assert.That(fireball.cost, Is.EqualTo(20));
            Assert.That(fireball.ResolveEffectKind(), Is.EqualTo(SkillEffectKind.BleedAttack));
            Assert.That(fireball.statusDuration, Is.EqualTo(2));
            Assert.That(fireball.statusDamage, Is.EqualTo(20));

            Assert.That(burstFireball.skillName, Is.EqualTo("\uD3ED\uBC1C \uD654\uC5FC\uAD6C"));
            Assert.That(burstFireball.cost, Is.EqualTo(30));
            Assert.That(burstFireball.ResolveEffectKind(), Is.EqualTo(SkillEffectKind.OpenWoundAttack));
            Assert.That(burstFireball.conditionalPowerBonus, Is.EqualTo(50));

            Assert.That(burnOut.skillName, Is.EqualTo("\uD654\uC5FC \uC18C\uBAA8"));
            Assert.That(burnOut.cost, Is.EqualTo(30));
            Assert.That(burnOut.ResolveEffectKind(), Is.EqualTo(SkillEffectKind.SacrificeAttack));
            Assert.That(burnOut.power, Is.EqualTo(100));
            Assert.That(burnOut.lifeStealPercent, Is.Zero);

            Assert.That(overburn.cost, Is.EqualTo(40));
            Assert.That(overburn.ResolveEffectKind(), Is.EqualTo(SkillEffectKind.OverburnAttack));
            Assert.That(overburn.extraPowerPerConsumedCost, Is.EqualTo(1));
            Assert.That(afterglowSave.cost, Is.EqualTo(10));
            Assert.That(afterglowSave.nextCostGainModifier, Is.EqualTo(15));
            Assert.That(afterglowSave.maxCostCarry, Is.Zero);
            Assert.That(cleanseHand.cost, Is.EqualTo(10));
            Assert.That(cleanseHand.costRefund, Is.EqualTo(10));
            Assert.That(blackCorrosion.cost, Is.EqualTo(20));
            Assert.That(blackCorrosion.nextCostGainModifier, Is.EqualTo(-10));
            Assert.That(lightShot.power, Is.EqualTo(60));
            Assert.That(lowStance.power, Is.EqualTo(20));
            Assert.That(lightGuard.power, Is.EqualTo(40));
            Assert.That(bodyPress.ResolveEffectKind(), Is.EqualTo(SkillEffectKind.DefenseScalingAttack));
            Assert.That(bodyPress.damageStatSource, Is.EqualTo(DamageStatSource.DefensePower));

            var expectedCosts = new System.Collections.Generic.Dictionary<string, int>
            {
                ["quick-stab"] = 10,
                ["flash"] = 10,
                ["howl"] = 10,
                ["taunt"] = 10,
                ["low-stance"] = 10,
                ["focus-breath"] = 10,
                ["cleanse-hand"] = 10,
                ["afterglow-save"] = 10,
                ["light-shot"] = 20,
                ["fireball"] = 20,
                ["poison-coat"] = 20,
                ["execute"] = 20,
                ["shield-bash"] = 20,
                ["crack-brand"] = 20,
                ["seal-skill"] = 20,
                ["black-corrosion"] = 20,
                ["dark-shackle"] = 20,
                ["darkness"] = 20,
                ["light-echo"] = 20,
                ["iron-wall"] = 20,
                ["light-guard"] = 20,
                ["sharp-senses"] = 20,
                ["flow-strike"] = 30,
                ["intimidating-shot"] = 30,
                ["life-drain"] = 30,
                ["body-press"] = 30,
                ["burst-fireball"] = 30,
                ["shield-burst"] = 30,
                ["burn-out"] = 30,
                ["heavy-strike"] = 30,
                ["gather-light"] = 30,
                ["black-pressure"] = 30,
                ["deep-darkness"] = 30,
                ["endure"] = 30,
                ["light-split"] = 30,
                ["thorn-guard"] = 30,
                ["light-recover"] = 30,
                ["tentacle-strike"] = 40,
                ["bioluminescence"] = 40,
                ["reckless-blow"] = 40,
                ["overburn"] = 40,
            };
            foreach (var expectedCost in expectedCosts)
            {
                Assert.That(skills[expectedCost.Key].cost, Is.EqualTo(expectedCost.Value), expectedCost.Key);
            }

            const string ExpFlameSpritePath = "Assets/Art/Source/ExP/Effects/Effect_Flame.png";
            const string FlameBurstPrefabPath = "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_FlameBurst.prefab";
            const string FlameImagePrefabPath = "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_FlameImage.prefab";
            const string RawVfxTestFirePath = "Assets/VFX Test/Prefab/vfx_Fire.prefab";

            foreach (var flameSkill in new[] { fireball, burstFireball, burnOut, overburn, recklessBlow })
            {
                Assert.That(flameSkill.ResolveVfxFamily(), Is.EqualTo(SkillVfxFamily.FlameBurst), flameSkill.skillId);
                Assert.That(flameSkill.vfxDefinition.HasAnyCue, Is.True, flameSkill.skillId);
                Assert.That(AssetDatabase.GetAssetPath(flameSkill.vfx.primarySprite), Is.EqualTo(ExpFlameSpritePath), flameSkill.skillId);
                Assert.That(AssetDatabase.GetAssetPath(flameSkill.vfx.primaryPrefab), Is.EqualTo(FlameBurstPrefabPath), flameSkill.skillId);

                var cuePaths = flameSkill.vfxDefinition
                    .cues
                    .Select(cue => AssetDatabase.GetAssetPath(cue.prefab))
                    .ToArray();
                Assert.That(cuePaths, Does.Contain(FlameImagePrefabPath), flameSkill.skillId);
                Assert.That(cuePaths, Does.Not.Contain(RawVfxTestFirePath), flameSkill.skillId);
            }

            foreach (var fireballSkill in new[] { fireball, burstFireball, burnOut })
            {
                Assert.That(fireballSkill.vfxPrimaryColor, Is.EqualTo(new Color(1f, 0.42f, 0.06f, 1f)), fireballSkill.skillId);
                var cuePaths = fireballSkill.vfxDefinition
                    .cues
                    .Select(cue => AssetDatabase.GetAssetPath(cue.prefab))
                    .ToArray();
                Assert.That(
                    cuePaths,
                    Does.Contain("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_TealEasyExplosion.prefab"),
                    fireballSkill.skillId);
                Assert.That(cuePaths, Does.Not.Contain("Assets/VFX Test/Prefab/vfx_EasyExplosion.prefab"), fireballSkill.skillId);
                Assert.That(fireballSkill.activationEffect?.vfxPrefab, Is.Not.Null, fireballSkill.skillId);
                Assert.That(
                    AssetDatabase.GetAssetPath(fireballSkill.activationEffect.vfxPrefab),
                    Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_FireballProjectile.prefab"),
                    fireballSkill.skillId);
                Assert.That(
                    fireballSkill.activationEffect.vfxPrefab.GetComponentInChildren<CombatProjectileEffect>(true),
                    Is.Not.Null,
                    fireballSkill.skillId);
            }

            foreach (var supportSkill in new[] { skills["light-recover"], skills["focus-breath"] })
            {
                Assert.That(supportSkill.vfxDefinition.HasAnyCue, Is.True, supportSkill.skillId);
                var cuePaths = supportSkill.vfxDefinition
                    .CuesFor(SkillVfxTrigger.Activate)
                    .Select(cue => AssetDatabase.GetAssetPath(cue.prefab))
                    .ToArray();
                Assert.That(cuePaths, Does.Contain("Assets/VFX Test/Prefab/vfx_Healing.prefab"), supportSkill.skillId);
            }

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

            Assert.That(skills.Keys, Is.SupersetOf(new[]
            {
                "fireball",
                "poison-coat",
                "burst-fireball",
                "execute",
                "overburn",
                "seal-skill",
                "taunt",
                "crack-brand",
                "afterglow-save",
                "cleanse-hand",
                "black-corrosion",
            }));
            Assert.That(rewardSkillIds, Does.Contain("fireball"));
            Assert.That(rewardSkillIds, Does.Contain("cleanse-hand"));
            Assert.That(rewardSkillIds, Does.Not.Contain("black-corrosion"));
        }

        [Test]
        public void PrototypeSkillAssets_UseExpectedImpactPrefabsInsteadOfRawVfxTestPrefabs()
        {
            var skills = LoadPrototypeSkills();
            const string RawEasyExplosionPath = "Assets/VFX Test/Prefab/vfx_EasyExplosion.prefab";
            const string TealEasyExplosionPath = "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_TealEasyExplosion.prefab";
            const string RawVfxTestFirePath = "Assets/VFX Test/Prefab/vfx_Fire.prefab";
            const string FlameImagePrefabPath = "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_FlameImage.prefab";

            foreach (var skillId in new[] { "fireball", "burst-fireball", "burn-out" })
            {
                var cuePaths = skills[skillId].vfxDefinition
                    .cues
                    .Select(cue => AssetDatabase.GetAssetPath(cue.prefab))
                    .ToArray();
                Assert.That(cuePaths, Does.Contain(TealEasyExplosionPath), skillId);
                Assert.That(cuePaths, Does.Not.Contain(RawEasyExplosionPath), skillId);
            }

            foreach (var skillId in new[] { "fireball", "burst-fireball", "burn-out", "overburn", "reckless-blow" })
            {
                var cuePaths = skills[skillId].vfxDefinition
                    .cues
                    .Select(cue => AssetDatabase.GetAssetPath(cue.prefab))
                    .ToArray();
                Assert.That(cuePaths, Does.Contain(FlameImagePrefabPath), skillId);
                Assert.That(cuePaths, Does.Not.Contain(RawVfxTestFirePath), skillId);
            }

            foreach (var skillId in new[] { "shield-bash", "shield-burst" })
            {
                Assert.That(
                    AssetDatabase.GetAssetPath(skills[skillId].vfx.secondaryPrefab),
                    Is.EqualTo(TealEasyExplosionPath),
                    skillId);
            }
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
                "gather-light",
            }));
        }

        [Test]
        public void PrototypeEnemyAssets_EquipCurrentSharedStatusSkills()
        {
            var enemyGuids = AssetDatabase.FindAssets("t:EnemySO", new[] { "Assets/Data/Enemies" });
            var assignedSkillIds = new System.Collections.Generic.HashSet<string>();

            Assert.That(enemyGuids.Length, Is.EqualTo(16));
            foreach (var guid in enemyGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemySO>(path);

                Assert.That(enemy, Is.Not.Null, path);
                Assert.That(enemy.AssignedSkillCount, Is.EqualTo(EnemySO.MaxEquippedSkillSlots), path);
                Assert.That(enemy.intentPattern, Is.Not.Null.And.Not.Empty, path);
                foreach (var skill in enemy.skills.Take(EnemySO.MaxEquippedSkillSlots))
                {
                    Assert.That(skill, Is.Not.Null, path);
                    Assert.That(skill.CanEnemyUse, Is.True, $"{path}:{skill.skillId}");
                    assignedSkillIds.Add(skill.skillId);
                }

                foreach (var intent in enemy.intentPattern)
                {
                    Assert.That(intent, Is.Not.Null, path);
                    Assert.That(intent.skillId, Is.Not.Empty, path);
                    Assert.That(enemy.skills.Take(EnemySO.MaxEquippedSkillSlots).Any(skill => skill != null && skill.skillId == intent.skillId), Is.True, $"{path}:{intent.skillId}");
                }

                if (path.EndsWith("08.asset", System.StringComparison.Ordinal) ||
                    path.EndsWith("08_Enhanced.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(enemy.intentPattern.Any(intent => intent.intentType == EnemyIntentType.Attack), Is.True, path);
                }
            }

            Assert.That(assignedSkillIds, Is.SupersetOf(new[]
            {
                "quick-stab",
                "low-stance",
                "fireball",
                "thorn-guard",
                "flash",
                "poison-coat",
                "execute",
                "focus-breath",
                "dark-shackle",
                "sharp-senses",
                "iron-wall",
                "seal-skill",
                "crack-brand",
                "intimidating-shot",
                "burst-fireball",
                "black-pressure",
                "overburn",
                "howl",
                "body-press",
                "endure",
                "heavy-strike",
                "burn-out",
                "reckless-blow",
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
