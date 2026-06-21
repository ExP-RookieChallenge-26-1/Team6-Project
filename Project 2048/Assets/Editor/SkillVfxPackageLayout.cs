using System;
using System.IO;
using System.Linq;
using Project2048.Prototype;
using Project2048.Skills;
using UnityEditor;
using UnityEngine;

namespace Project2048.EditorTools
{
    public static class SkillVfxPackageLayout
    {
        private const string SkillVfxRoot = "Assets/Art/Effects/SkillVFX";
        private const string EffectsRoot = SkillVfxRoot + "/Effects";
        private const string ResourcesRoot = SkillVfxRoot + "/Resources";
        private const string WorldVfxProfilePath = ResourcesRoot + "/PrototypeCombatWorldVfxProfile.asset";

        [MenuItem("Tools/Project2048/VFX/Apply Holy Fireball Style Layout")]
        public static void ApplyHolyFireballStyleLayout()
        {
            EnsureFolder(EffectsRoot);

            foreach (var move in Moves)
            {
                MoveAsset(move.Source, move.Target);
            }

            EnsureShieldDomePrefabs();
            EnsureTentacleWhipPrefab();
            CreateOrUpdatePackages();
            AssignWorldVfxProfile();
            AssignPackagesToSkills();

            DeleteFolderIfEmpty(SkillVfxRoot + "/Attack");
            DeleteFolderIfEmpty(SkillVfxRoot + "/Common");
            DeleteFolderIfEmpty(SkillVfxRoot + "/Shield");
            DeleteFolderIfEmpty(SkillVfxRoot + "/Materials");
            DeleteFolderIfEmpty(SkillVfxRoot + "/Prefabs");
            DeleteFolderIfEmpty(SkillVfxRoot + "/SkillSO/Materials");
            DeleteFolderIfEmpty(SkillVfxRoot + "/SkillSO");
            DeleteFolderIfEmpty(ResourcesRoot + "/VFX");
            DeleteFolderIfEmpty(ResourcesRoot + "/Effects");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Skill VFX assets are organized under HolyFireball-style effect packages.");
        }

        private static readonly AssetMove[] Moves =
        {
            new(SkillVfxRoot + "/HolyFireball", EffectsRoot + "/HolyFireball"),

            new(SkillVfxRoot + "/Attack/SkillVfx_AttackImpact.png", EffectsRoot + "/SlashArc/Textures/SkillVfx_AttackImpact.png"),
            new(SkillVfxRoot + "/Prefabs/SkillVfx_AttackImpact.prefab", EffectsRoot + "/SlashArc/Prefabs/SkillVfx_AttackImpact.prefab"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_SlashArc.mat", EffectsRoot + "/SlashArc/Materials/SkillVfx_SlashArc.mat"),

            new(SkillVfxRoot + "/Attack/SkillVfx_HitImpact.png", EffectsRoot + "/ImpactBurst/Textures/SkillVfx_HitImpact.png"),
            new(SkillVfxRoot + "/Prefabs/SkillVfx_HitImpact.prefab", EffectsRoot + "/ImpactBurst/Prefabs/SkillVfx_HitImpact.prefab"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_ImpactBurst.mat", EffectsRoot + "/ImpactBurst/Materials/SkillVfx_ImpactBurst.mat"),

            new(SkillVfxRoot + "/Common/SkillVfx_FlameBurst.png", EffectsRoot + "/FlameBurst/Textures/SkillVfx_FlameBurst.png"),
            new(SkillVfxRoot + "/Prefabs/SkillVfx_FlameBurst.prefab", EffectsRoot + "/FlameBurst/Prefabs/SkillVfx_FlameBurst.prefab"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_FlameBurst.mat", EffectsRoot + "/FlameBurst/Materials/SkillVfx_FlameBurst.mat"),

            new(SkillVfxRoot + "/Common/SkillVfx_ChainAttack.png", EffectsRoot + "/DarkChainBurst/Textures/SkillVfx_ChainAttack.png"),
            new(SkillVfxRoot + "/Common/SkillVfx_BoundChains.png", EffectsRoot + "/DarkChainBurst/Textures/SkillVfx_BoundChains.png"),
            new(SkillVfxRoot + "/Prefabs/SkillVfx_ChainAttack.prefab", EffectsRoot + "/DarkChainBurst/Prefabs/SkillVfx_ChainAttack.prefab"),
            new(SkillVfxRoot + "/Prefabs/SkillVfx_BoundChains.prefab", EffectsRoot + "/DarkChainBurst/Prefabs/SkillVfx_BoundChains.prefab"),
            new(SkillVfxRoot + "/Prefabs/SkillVfx_DarkShackleLaunch.prefab", EffectsRoot + "/DarkChainBurst/Prefabs/SkillVfx_DarkShackleLaunch.prefab"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_DarkChainBurst.mat", EffectsRoot + "/DarkChainBurst/Materials/SkillVfx_DarkChainBurst.mat"),

            new(SkillVfxRoot + "/Shield/SkillVfx_ShieldBarrier.png", EffectsRoot + "/ShieldDome/Textures/SkillVfx_ShieldBarrier.png"),
            new(SkillVfxRoot + "/Shield/SkillVfx_ThornShieldBarrier.png", EffectsRoot + "/ShieldDome/Textures/SkillVfx_ThornShieldBarrier.png"),
            new(SkillVfxRoot + "/Prefabs/SkillVfx_ShieldBarrier.prefab", EffectsRoot + "/ShieldDome/Prefabs/SkillVfx_ShieldBarrier.prefab"),
            new(SkillVfxRoot + "/Prefabs/SkillVfx_ThornShieldBarrier.prefab", EffectsRoot + "/ShieldDome/Prefabs/SkillVfx_ThornShieldBarrier.prefab"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_ShieldDome.mat", EffectsRoot + "/ShieldDome/Materials/SkillVfx_ShieldDome.mat"),

            new(SkillVfxRoot + "/Common/SkillVfx_MagicCircle.png", EffectsRoot + "/MagicCircle/Textures/SkillVfx_MagicCircle.png"),
            new(SkillVfxRoot + "/Prefabs/SkillVfx_MagicCircle.prefab", EffectsRoot + "/MagicCircle/Prefabs/SkillVfx_MagicCircle.prefab"),

            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_LightProjectile.mat", EffectsRoot + "/LightProjectile/Materials/SkillVfx_LightProjectile.mat"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_LightBeam.mat", EffectsRoot + "/LightBeam/Materials/SkillVfx_LightBeam.mat"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_BuffAura.mat", EffectsRoot + "/BuffAura/Materials/SkillVfx_BuffAura.mat"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_DebuffWave.mat", EffectsRoot + "/DebuffWave/Materials/SkillVfx_DebuffWave.mat"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_DrainTether.mat", EffectsRoot + "/DrainTether/Materials/SkillVfx_DrainTether.mat"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_CounterReady.mat", EffectsRoot + "/CounterReady/Materials/SkillVfx_CounterReady.mat"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_BoardDisturb.mat", EffectsRoot + "/BoardDisturb/Materials/SkillVfx_BoardDisturb.mat"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_SupportFire.mat", EffectsRoot + "/SupportFire/Materials/SkillVfx_SupportFire.mat"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_TentacleWhip.mat", EffectsRoot + "/TentacleWhip/Materials/SkillVfx_TentacleWhip.mat"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_SpikedBurst.mat", EffectsRoot + "/SpikedBurst/Materials/SkillVfx_SpikedBurst.mat"),
            new(SkillVfxRoot + "/SkillSO/Materials/SkillVfx_BloodFountainSlash.mat", EffectsRoot + "/BloodFountainSlash/Materials/SkillVfx_BloodFountainSlash.mat"),

            new(SkillVfxRoot + "/Materials/ShieldImpactParticles.mat", EffectsRoot + "/ShieldImpact/Materials/ShieldImpactParticles.mat"),
            new(SkillVfxRoot + "/Materials/FearDebuffParticles.mat", EffectsRoot + "/FearDebuff/Materials/FearDebuffParticles.mat"),
            new(SkillVfxRoot + "/Materials/DarknessDebuffParticles.mat", EffectsRoot + "/DarknessDebuff/Materials/DarknessDebuffParticles.mat"),

            new(ResourcesRoot + "/Effects/ClawSlash2D.shader", ResourcesRoot + "/Effects/ClawSlash2D/Shaders/ClawSlash2D.shader"),
        };

        private static readonly PackageDefinition[] Packages =
        {
            new(SkillVfxFamily.SlashArc, "SlashArc")
            {
                primarySpritePath = EffectsRoot + "/SlashArc/Textures/SkillVfx_AttackImpact.png",
                primaryPrefabPath = EffectsRoot + "/SlashArc/Prefabs/SkillVfx_AttackImpact.prefab",
                particleMaterialPath = EffectsRoot + "/SlashArc/Materials/SkillVfx_SlashArc.mat",
                localOffset = new Vector3(0f, 0.16f, 0f),
                radiusMultiplier = 1f,
                sortingOffset = 12,
                rotationDegrees = -12f,
            },
            new(SkillVfxFamily.LightProjectile, "LightProjectile")
            {
                projectilePrefabPath = EffectsRoot + "/HolyFireball/Prefabs/HolyFireball_Attack3.prefab",
                primarySpritePath = EffectsRoot + "/SlashArc/Textures/SkillVfx_AttackImpact.png",
                primaryPrefabPath = EffectsRoot + "/SlashArc/Prefabs/SkillVfx_AttackImpact.prefab",
                particleMaterialPath = EffectsRoot + "/LightProjectile/Materials/SkillVfx_LightProjectile.mat",
                radiusMultiplier = 0.92f,
                rotationDegrees = -8f,
            },
            new(SkillVfxFamily.ShieldDome, "ShieldDome")
            {
                primarySpritePath = EffectsRoot + "/ShieldDome/Textures/SkillVfx_ShieldBarrier.png",
                primaryPrefabPath = EffectsRoot + "/ShieldDome/Prefabs/SkillVfx_ShieldBarrier.prefab",
                secondarySpritePath = EffectsRoot + "/ShieldDome/Textures/SkillVfx_ThornShieldBarrier.png",
                secondaryPrefabPath = EffectsRoot + "/ShieldDome/Prefabs/SkillVfx_ThornShieldBarrier.prefab",
                particleMaterialPath = EffectsRoot + "/ShieldDome/Materials/SkillVfx_ShieldDome.mat",
                localOffset = new Vector3(0f, 0.12f, 0f),
            },
            new(SkillVfxFamily.ImpactBurst, "ImpactBurst")
            {
                primarySpritePath = EffectsRoot + "/ImpactBurst/Textures/SkillVfx_HitImpact.png",
                primaryPrefabPath = EffectsRoot + "/ImpactBurst/Prefabs/SkillVfx_HitImpact.prefab",
                particleMaterialPath = EffectsRoot + "/ImpactBurst/Materials/SkillVfx_ImpactBurst.mat",
                radiusMultiplier = 1.08f,
                rotationDegrees = 0f,
            },
            new(SkillVfxFamily.BuffAura, "BuffAura")
            {
                primarySpritePath = EffectsRoot + "/MagicCircle/Textures/SkillVfx_MagicCircle.png",
                primaryPrefabPath = EffectsRoot + "/MagicCircle/Prefabs/SkillVfx_MagicCircle.prefab",
                particleMaterialPath = EffectsRoot + "/BuffAura/Materials/SkillVfx_BuffAura.mat",
                localOffset = new Vector3(0f, 0.08f, 0f),
                rotationDegrees = -8f,
            },
            new(SkillVfxFamily.DebuffWave, "DebuffWave")
            {
                primarySpritePath = EffectsRoot + "/MagicCircle/Textures/SkillVfx_MagicCircle.png",
                primaryPrefabPath = EffectsRoot + "/MagicCircle/Prefabs/SkillVfx_MagicCircle.prefab",
                particleMaterialPath = EffectsRoot + "/DebuffWave/Materials/SkillVfx_DebuffWave.mat",
                localOffset = new Vector3(0f, 0.08f, 0f),
                rotationDegrees = -8f,
            },
            new(SkillVfxFamily.DrainTether, "DrainTether")
            {
                primarySpritePath = EffectsRoot + "/MagicCircle/Textures/SkillVfx_MagicCircle.png",
                primaryPrefabPath = EffectsRoot + "/MagicCircle/Prefabs/SkillVfx_MagicCircle.prefab",
                particleMaterialPath = EffectsRoot + "/DrainTether/Materials/SkillVfx_DrainTether.mat",
                localOffset = new Vector3(0f, 0.08f, 0f),
                rotationDegrees = -8f,
            },
            new(SkillVfxFamily.CounterReady, "CounterReady")
            {
                primarySpritePath = EffectsRoot + "/MagicCircle/Textures/SkillVfx_MagicCircle.png",
                primaryPrefabPath = EffectsRoot + "/MagicCircle/Prefabs/SkillVfx_MagicCircle.prefab",
                particleMaterialPath = EffectsRoot + "/CounterReady/Materials/SkillVfx_CounterReady.mat",
                localOffset = new Vector3(0f, 0.08f, 0f),
                rotationDegrees = -8f,
            },
            new(SkillVfxFamily.BoardDisturb, "BoardDisturb")
            {
                primarySpritePath = EffectsRoot + "/MagicCircle/Textures/SkillVfx_MagicCircle.png",
                primaryPrefabPath = EffectsRoot + "/MagicCircle/Prefabs/SkillVfx_MagicCircle.prefab",
                particleMaterialPath = EffectsRoot + "/BoardDisturb/Materials/SkillVfx_BoardDisturb.mat",
                localOffset = new Vector3(0f, 0.08f, 0f),
                rotationDegrees = -8f,
            },
            new(SkillVfxFamily.SupportFire, "SupportFire")
            {
                primarySpritePath = EffectsRoot + "/SlashArc/Textures/SkillVfx_AttackImpact.png",
                primaryPrefabPath = EffectsRoot + "/SlashArc/Prefabs/SkillVfx_AttackImpact.prefab",
                secondarySpritePath = EffectsRoot + "/MagicCircle/Textures/SkillVfx_MagicCircle.png",
                secondaryPrefabPath = EffectsRoot + "/MagicCircle/Prefabs/SkillVfx_MagicCircle.prefab",
                particleMaterialPath = EffectsRoot + "/SupportFire/Materials/SkillVfx_SupportFire.mat",
                localOffset = new Vector3(0f, 0.48f, 0f),
                radiusMultiplier = 0.82f,
                tintWhiteBlend = 0.2f,
                alpha = 0.86f,
                rotationDegrees = -6f,
            },
            new(SkillVfxFamily.LightBeam, "LightBeam")
            {
                projectilePrefabPath = EffectsRoot + "/HolyFireball/Prefabs/HolyFireball_Attack3.prefab",
                primarySpritePath = EffectsRoot + "/SlashArc/Textures/SkillVfx_AttackImpact.png",
                primaryPrefabPath = EffectsRoot + "/SlashArc/Prefabs/SkillVfx_AttackImpact.prefab",
                particleMaterialPath = EffectsRoot + "/LightBeam/Materials/SkillVfx_LightBeam.mat",
                localOffset = new Vector3(0f, 0.14f, 0f),
                radiusMultiplier = 1.18f,
                rotationDegrees = -10f,
            },
            new(SkillVfxFamily.TentacleWhip, "TentacleWhip")
            {
                primarySpritePath = EffectsRoot + "/TentacleWhip/Textures/SkillVfx_TentacleWhip.png",
                primaryPrefabPath = EffectsRoot + "/TentacleWhip/Prefabs/SkillVfx_TentacleWhip.prefab",
                particleMaterialPath = EffectsRoot + "/TentacleWhip/Materials/SkillVfx_TentacleWhip.mat",
                localOffset = new Vector3(0f, 0.14f, 0f),
                radiusMultiplier = 0.92f,
                rotationDegrees = -8f,
            },
            new(SkillVfxFamily.SpikedBurst, "SpikedBurst")
            {
                primarySpritePath = EffectsRoot + "/ImpactBurst/Textures/SkillVfx_HitImpact.png",
                primaryPrefabPath = EffectsRoot + "/ImpactBurst/Prefabs/SkillVfx_HitImpact.prefab",
                particleMaterialPath = EffectsRoot + "/SpikedBurst/Materials/SkillVfx_SpikedBurst.mat",
                localOffset = new Vector3(0f, 0.14f, 0f),
                radiusMultiplier = 1.12f,
                rotationDegrees = 0f,
            },
            new(SkillVfxFamily.BloodFountainSlash, "BloodFountainSlash")
            {
                primarySpritePath = EffectsRoot + "/SlashArc/Textures/SkillVfx_AttackImpact.png",
                primaryPrefabPath = EffectsRoot + "/SlashArc/Prefabs/SkillVfx_AttackImpact.prefab",
                particleMaterialPath = EffectsRoot + "/BloodFountainSlash/Materials/SkillVfx_BloodFountainSlash.mat",
                localOffset = new Vector3(0f, 0.14f, 0f),
                radiusMultiplier = 1.08f,
                rotationDegrees = -16f,
            },
            new(SkillVfxFamily.FlameBurst, "FlameBurst")
            {
                primarySpritePath = EffectsRoot + "/FlameBurst/Textures/SkillVfx_FlameBurst.png",
                primaryPrefabPath = EffectsRoot + "/FlameBurst/Prefabs/SkillVfx_FlameBurst.prefab",
                particleMaterialPath = EffectsRoot + "/FlameBurst/Materials/SkillVfx_FlameBurst.mat",
                localOffset = new Vector3(0f, -0.42f, 0f),
                radiusMultiplier = 1.1f,
                sortingOffset = 11,
                tintWhiteBlend = 0.16f,
                alpha = 0.84f,
                rotationDegrees = -8f,
            },
            new(SkillVfxFamily.DarkChainBurst, "DarkChainBurst")
            {
                primarySpritePath = EffectsRoot + "/DarkChainBurst/Textures/SkillVfx_ChainAttack.png",
                primaryPrefabPath = EffectsRoot + "/DarkChainBurst/Prefabs/SkillVfx_ChainAttack.prefab",
                projectilePrefabPath = EffectsRoot + "/DarkChainBurst/Prefabs/SkillVfx_DarkShackleLaunch.prefab",
                secondarySpritePath = EffectsRoot + "/DarkChainBurst/Textures/SkillVfx_BoundChains.png",
                secondaryPrefabPath = EffectsRoot + "/DarkChainBurst/Prefabs/SkillVfx_BoundChains.prefab",
                particleMaterialPath = EffectsRoot + "/DarkChainBurst/Materials/SkillVfx_DarkChainBurst.mat",
                localOffset = new Vector3(0f, 0.04f, 0f),
                radiusMultiplier = 0.95f,
                tintWhiteBlend = 0.14f,
                alpha = 0.78f,
                rotationDegrees = -4f,
            },
        };

        private static void CreateOrUpdatePackages()
        {
            foreach (var definition in Packages)
            {
                var packagePath = EffectsRoot + $"/{definition.folderName}/SkillVfx_{definition.folderName}Package.asset";
                EnsureFolder(Path.GetDirectoryName(packagePath)?.Replace('\\', '/'));
                var package = AssetDatabase.LoadAssetAtPath<SkillVfxPackageSO>(packagePath);
                if (package == null)
                {
                    package = ScriptableObject.CreateInstance<SkillVfxPackageSO>();
                    AssetDatabase.CreateAsset(package, packagePath);
                }

                package.family = definition.family;
                package.primarySprite = LoadAsset<Sprite>(definition.primarySpritePath);
                package.primaryPrefab = LoadAsset<GameObject>(definition.primaryPrefabPath);
                package.projectilePrefab = LoadAsset<GameObject>(definition.projectilePrefabPath);
                package.secondarySprite = LoadAsset<Sprite>(definition.secondarySpritePath);
                package.secondaryPrefab = LoadAsset<GameObject>(definition.secondaryPrefabPath);
                package.particleMaterial = LoadAsset<Material>(definition.particleMaterialPath);
                package.localOffset = definition.localOffset;
                package.radiusMultiplier = definition.radiusMultiplier;
                package.lifetimeSeconds = definition.lifetimeSeconds;
                package.sortingOffset = definition.sortingOffset;
                package.tintWhiteBlend = definition.tintWhiteBlend;
                package.alpha = definition.alpha;
                package.rotationDegrees = definition.rotationDegrees;
                EditorUtility.SetDirty(package);
            }
        }

        private static void AssignWorldVfxProfile()
        {
            EnsureFolder(ResourcesRoot);
            var profile = AssetDatabase.LoadAssetAtPath<CombatWorldVfxProfileSO>(WorldVfxProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CombatWorldVfxProfileSO>();
                AssetDatabase.CreateAsset(profile, WorldVfxProfilePath);
            }

            var slashArc = FindPackageDefinition(SkillVfxFamily.SlashArc);
            var impactBurst = FindPackageDefinition(SkillVfxFamily.ImpactBurst);
            var shieldDome = FindPackageDefinition(SkillVfxFamily.ShieldDome);
            var magicCircle = FindPackageDefinition(SkillVfxFamily.BuffAura);
            var flameBurst = FindPackageDefinition(SkillVfxFamily.FlameBurst);
            var darkChain = FindPackageDefinition(SkillVfxFamily.DarkChainBurst);

            profile.attackEffectSprite = LoadAsset<Sprite>(slashArc.primarySpritePath);
            profile.attackEffectPrefab = LoadAsset<GameObject>(slashArc.primaryPrefabPath);
            profile.hitEffectSprite = LoadAsset<Sprite>(impactBurst.primarySpritePath);
            profile.hitEffectPrefab = LoadAsset<GameObject>(impactBurst.primaryPrefabPath);
            profile.shieldEffectSprite = LoadAsset<Sprite>(shieldDome.primarySpritePath);
            profile.shieldEffectPrefab = LoadAsset<GameObject>(shieldDome.primaryPrefabPath);
            profile.thornShieldEffectSprite = LoadAsset<Sprite>(shieldDome.secondarySpritePath);
            profile.thornShieldEffectPrefab = LoadAsset<GameObject>(shieldDome.secondaryPrefabPath);
            profile.magicCircleEffectSprite = LoadAsset<Sprite>(magicCircle.primarySpritePath);
            profile.magicCircleEffectPrefab = LoadAsset<GameObject>(magicCircle.primaryPrefabPath);
            profile.flameEffectSprite = LoadAsset<Sprite>(flameBurst.primarySpritePath);
            profile.flameEffectPrefab = LoadAsset<GameObject>(flameBurst.primaryPrefabPath);
            profile.chainAttackEffectSprite = LoadAsset<Sprite>(darkChain.primarySpritePath);
            profile.chainAttackEffectPrefab = LoadAsset<GameObject>(darkChain.primaryPrefabPath);
            profile.boundChainsEffectSprite = LoadAsset<Sprite>(darkChain.secondarySpritePath);
            profile.boundChainsEffectPrefab = LoadAsset<GameObject>(darkChain.secondaryPrefabPath);
            profile.darkChainLaunchPrefab = LoadAsset<GameObject>(darkChain.projectilePrefabPath);
            profile.designTimeBindings = Packages
                .Select(definition => new SkillVfxDesignTimeBinding
                {
                    family = definition.family,
                    sprite = LoadAsset<Sprite>(definition.primarySpritePath),
                    prefab = LoadAsset<GameObject>(definition.primaryPrefabPath),
                    localOffset = definition.localOffset,
                    radiusMultiplier = definition.radiusMultiplier,
                    lifetimeSeconds = definition.lifetimeSeconds,
                    sortingOffset = definition.sortingOffset,
                    tintWhiteBlend = definition.tintWhiteBlend,
                    alpha = definition.alpha,
                    rotationDegrees = definition.rotationDegrees,
                })
                .ToArray();

            EditorUtility.SetDirty(profile);
        }

        private static PackageDefinition FindPackageDefinition(SkillVfxFamily family)
        {
            foreach (var package in Packages)
            {
                if (package.family == family)
                {
                    return package;
                }
            }

            throw new InvalidOperationException($"Missing VFX package definition for {family}.");
        }

        private static void AssignPackagesToSkills()
        {
            var packagesByFamily = new System.Collections.Generic.Dictionary<SkillVfxFamily, SkillVfxPackageSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:SkillVfxPackageSO", new[] { EffectsRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var package = AssetDatabase.LoadAssetAtPath<SkillVfxPackageSO>(path);
                if (package != null && package.family != SkillVfxFamily.None)
                {
                    packagesByFamily[package.family] = package;
                }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:SkillSO", new[] { "Assets/Data/Skills" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var skill = AssetDatabase.LoadAssetAtPath<SkillSO>(path);
                if (skill == null)
                {
                    continue;
                }

                var family = skill.ResolveVfxFamily();
                if (family == SkillVfxFamily.None || !packagesByFamily.TryGetValue(family, out var package))
                {
                    continue;
                }

                skill.vfxPackage = package;
                if (skill.activationEffect?.particleEffect != null && package.particleMaterial != null)
                {
                    skill.activationEffect.particleEffect.particleMaterial = package.particleMaterial;
                }

                EditorUtility.SetDirty(skill);
            }
        }

        private static void EnsureShieldDomePrefabs()
        {
            EnsureShieldDomePrefab(
                EffectsRoot + "/ShieldDome/Prefabs/SkillVfx_ShieldBarrier.prefab",
                "SkillVfx_ShieldBarrier",
                EffectsRoot + "/ShieldDome/Textures/SkillVfx_ShieldBarrier.png",
                "ShieldGuardSparkles",
                new Color(0.78f, 0.94f, 1f, 0.62f),
                new Color(1f, 1f, 1f, 0.9f),
                14);

            EnsureShieldDomePrefab(
                EffectsRoot + "/ShieldDome/Prefabs/SkillVfx_ThornShieldBarrier.prefab",
                "SkillVfx_ThornShieldBarrier",
                EffectsRoot + "/ShieldDome/Textures/SkillVfx_ThornShieldBarrier.png",
                "ThornGuardShieldSparkles",
                new Color(0.78f, 0.96f, 0.86f, 0.58f),
                new Color(1f, 0.78f, 0.72f, 0.86f),
                15);
        }

        private static void EnsureTentacleWhipPrefab()
        {
            EnsureSpriteEffectPrefab(
                EffectsRoot + "/TentacleWhip/Prefabs/SkillVfx_TentacleWhip.prefab",
                "SkillVfx_TentacleWhip",
                EffectsRoot + "/TentacleWhip/Textures/SkillVfx_TentacleWhip.png",
                EffectsRoot + "/TentacleWhip/Materials/SkillVfx_TentacleWhip.mat",
                new Color(1f, 1f, 1f, 0.82f));
        }

        private static void EnsureSpriteEffectPrefab(
            string prefabPath,
            string prefabName,
            string spritePath,
            string materialPath,
            Color color)
        {
            EnsureFolder(Path.GetDirectoryName(prefabPath)?.Replace('\\', '/'));
            var sprite = LoadAsset<Sprite>(spritePath);
            var material = LoadAsset<Material>(materialPath);
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var root = existingPrefab != null
                ? PrefabUtility.LoadPrefabContents(prefabPath)
                : new GameObject(prefabName);
            root.name = prefabName;

            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = root.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.color = color;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            if (existingPrefab != null)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void EnsureShieldDomePrefab(
            string prefabPath,
            string prefabName,
            string spritePath,
            string sparkleName,
            Color sparkleStart,
            Color sparkleEnd,
            int sparkleSortingOrder)
        {
            EnsureFolder(Path.GetDirectoryName(prefabPath)?.Replace('\\', '/'));
            var sprite = LoadAsset<Sprite>(spritePath);
            var material = LoadAsset<Material>(EffectsRoot + "/ShieldDome/Materials/SkillVfx_ShieldDome.mat");
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var root = existingPrefab != null
                ? PrefabUtility.LoadPrefabContents(prefabPath)
                : new GameObject(prefabName);
            root.name = prefabName;

            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = root.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.color = new Color(1f, 1f, 1f, 0.72f);
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            RemoveLegacyShieldChildren(root.transform);

            var sparkles = FindDirectChild(root.transform, sparkleName);
            if (sparkles == null)
            {
                sparkles = new GameObject(sparkleName).transform;
                sparkles.SetParent(root.transform, false);
            }

            var particles = sparkles.GetComponent<ParticleSystem>();
            if (particles == null)
            {
                particles = sparkles.gameObject.AddComponent<ParticleSystem>();
            }

            ConfigureShieldSparklePrefabParticles(particles, material, sparkleStart, sparkleEnd, sparkleSortingOrder);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            if (existingPrefab != null)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void RemoveLegacyShieldChildren(Transform root)
        {
            var legacyNames = new[]
            {
                "ShieldLightCircleRing",
                "ShieldLightCircleHalo",
                "ShieldLightCircleVfxGraph",
                "ShieldLightDomeParticles",
                "ShieldLightCircleParticles",
                "ThornGuardSpikedCircleRing",
                "ThornGuardDarkInnerCircle",
                "ThornGuardTriangleSpikes",
                "ThornGuardSpikedCircleVfxGraph",
                "ThornGuardSpikeParticles",
                "ThornGuardSpikedCircleParticles",
            };

            foreach (var legacyName in legacyNames)
            {
                var child = FindDirectChild(root, legacyName);
                if (child != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static void ConfigureShieldSparklePrefabParticles(
            ParticleSystem particles,
            Material material,
            Color startColor,
            Color endColor,
            int sortingOrder)
        {
            var transform = particles.transform;
            transform.localPosition = new Vector3(0f, 0.08f, 0f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            var main = particles.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.46f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.052f);
            main.startColor = new ParticleSystem.MinMaxGradient(startColor, endColor);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 48;

            var emission = particles.emission;
            emission.rateOverTime = 12f;
            emission.SetBursts(Array.Empty<ParticleSystem.Burst>());

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.64f;
            shape.radiusThickness = 0.18f;
            shape.arc = 360f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.035f, 0.035f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, 1f),
                new Keyframe(1f, 0f)));

            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.72f, 0.18f),
                    new GradientAlphaKey(0f, 1f),
                },
            });

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.alignment = ParticleSystemRenderSpace.View;
                renderer.sharedMaterial = material;
                renderer.sortingOrder = sortingOrder;
                renderer.minParticleSize = 0f;
                renderer.maxParticleSize = 1.4f;
            }
        }

        private static void MoveAsset(string source, string target)
        {
            if (!AssetExists(source))
            {
                return;
            }

            if (AssetExists(target))
            {
                Debug.LogWarning($"VFX layout target already exists, skipping move: {target}");
                return;
            }

            EnsureFolder(Path.GetDirectoryName(target)?.Replace('\\', '/'));
            var error = AssetDatabase.MoveAsset(source, target);
            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException($"Failed to move VFX asset from {source} to {target}: {error}");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static bool AssetExists(string path)
        {
            return AssetDatabase.IsValidFolder(path) || AssetDatabase.LoadMainAssetAtPath(path) != null;
        }

        private static T LoadAsset<T>(string path)
            where T : UnityEngine.Object
        {
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void DeleteFolderIfEmpty(string assetPath)
        {
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            if (!Directory.Exists(absolutePath))
            {
                return;
            }

            foreach (var entry in Directory.EnumerateFileSystemEntries(absolutePath))
            {
                if (!entry.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            AssetDatabase.DeleteAsset(assetPath);
        }

        private readonly struct AssetMove
        {
            public readonly string Source;
            public readonly string Target;

            public AssetMove(string source, string target)
            {
                Source = source;
                Target = target;
            }
        }

        private sealed class PackageDefinition
        {
            public readonly SkillVfxFamily family;
            public readonly string folderName;
            public string primarySpritePath;
            public string primaryPrefabPath;
            public string projectilePrefabPath;
            public string secondarySpritePath;
            public string secondaryPrefabPath;
            public string particleMaterialPath;
            public Vector3 localOffset = new(0f, 0.16f, 0f);
            public float radiusMultiplier = 1f;
            public float lifetimeSeconds = -1f;
            public int sortingOffset = 12;
            public float tintWhiteBlend = 0.18f;
            public float alpha = 0.9f;
            public float rotationDegrees = -12f;

            public PackageDefinition(SkillVfxFamily family, string folderName)
            {
                this.family = family;
                this.folderName = folderName;
            }
        }
    }
}
