using System.Collections.Generic;
using System.Linq;
using Project2048.Presentation;
using Project2048.Prototype;
using Project2048.Skills;
using UnityEditor;
using UnityEngine;

namespace Project2048.Editor
{
    public static class SkillVfxDesignTimeAuthoringBuilder
    {
        private const string Root = "Assets/Art/Effects/SkillVFX";
        private const string Prefabs = Root + "/Prefabs";
        private const string Materials = Root + "/Materials";
        private const string Textures = Root + "/Textures";
        private const string Shaders = Root + "/Shaders";
        private const string Graphs = Root + "/Graphs";
        private const string Models = Root + "/Models";
        private const string ProfilePath = Root + "/Resources/PrototypeCombatWorldVfxProfile.asset";

        [MenuItem("Project2048/Rebuild Skill VFX Design-Time Prefabs")]
        public static void Rebuild()
        {
            EnsureFolders();
            MoveLiveFireballAssetsIntoSkillVfx();

            var defaultPrefab = CreateParticlePrefab(
                "SkillVfx_GenericParticleBurst",
                LoadMaterial(Materials + "/SkillVfx_BuffAura.mat"),
                sizeMultiplier: 1f,
                radiusMultiplier: 1f,
                preserveShape: false);
            var swirlPrefab = CreateParticlePrefab(
                "SkillVfx_SwirlParticleBurst",
                LoadMaterial(Materials + "/SkillVfx_LightBeam.mat"),
                sizeMultiplier: 1f,
                radiusMultiplier: 1.15f,
                preserveShape: false);

            var familyPrefabs = CreateFamilyPrefabs(defaultPrefab);
            var namedPrefabs = CreateNamedParticlePrefabs();

            PatchProfile(defaultPrefab, swirlPrefab, namedPrefabs);
            PatchSkillAssets(familyPrefabs, defaultPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Rebuilt Skill VFX design-time particle prefabs and references.");
        }

        private static void EnsureFolders()
        {
            foreach (var path in new[] { Prefabs, Materials, Textures, Shaders, Graphs, Models })
            {
                EnsureFolder(path);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = slash > 0 ? path[..slash] : "Assets";
            var name = slash > 0 ? path[(slash + 1)..] : path;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void MoveLiveFireballAssetsIntoSkillVfx()
        {
            MoveAssetIfNeeded("Assets/VFX Test/Prefab/vfx_Fireball_vfxgraph.prefab", Prefabs + "/SkillVfx_FireballProjectile.prefab");
            MoveAssetIfNeeded("Assets/VFX Test/Prefab/vfxgraph_Fireball.vfx", Graphs + "/SkillVfx_FireballProjectile.vfx");
            MoveAssetIfNeeded("Assets/VFX Test/Materials/Fireball/FireballTrail_AB.mat", Materials + "/SkillVfx_FireballTrail_AB.mat");
            MoveAssetIfNeeded("Assets/VFX Test/Materials/Fireball/FireballTrail_Add.mat", Materials + "/SkillVfx_FireballTrail_Add.mat");
            MoveAssetIfNeeded("Assets/VFX Test/Shader/Panshader_Unlit.shadergraph", Shaders + "/SkillVfx_Fireball_Unlit.shadergraph");
            MoveAssetIfNeeded("Assets/VFX Test/Texture/FireballFlare.png", Textures + "/SkillVfx_FireballFlare.png");
            MoveAssetIfNeeded("Assets/VFX Test/Texture/FireballTexture.png", Textures + "/SkillVfx_FireballTexture.png");
            MoveAssetIfNeeded("Assets/VFX Test/Texture/FireballTrail.png", Textures + "/SkillVfx_FireballTrail.png");
            MoveAssetIfNeeded("Assets/VFX Test/Models/FireballSphere.fbx", Models + "/SkillVfx_FireballSphere.fbx");
        }

        private static void MoveAssetIfNeeded(string source, string destination)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(destination)))
            {
                return;
            }

            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(source)))
            {
                Debug.LogWarning($"Skill VFX source asset missing, skipped move: {source}");
                return;
            }

            var error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"Failed to move Skill VFX asset from {source} to {destination}: {error}");
            }
        }

        private static Dictionary<SkillVfxFamily, ParticleSystem> CreateFamilyPrefabs(ParticleSystem defaultPrefab)
        {
            var prefabs = new Dictionary<SkillVfxFamily, ParticleSystem>();
            foreach (SkillVfxFamily family in System.Enum.GetValues(typeof(SkillVfxFamily)))
            {
                if (family == SkillVfxFamily.None)
                {
                    continue;
                }

                var material = LoadMaterial(Materials + $"/SkillVfx_{family}.mat") ??
                    LoadMaterial(Materials + "/SkillVfx_BuffAura.mat");
                prefabs[family] = CreateParticlePrefab(
                    $"SkillVfx_{family}Particles",
                    material,
                    sizeMultiplier: ResolveFamilySizeMultiplier(family),
                    radiusMultiplier: ResolveFamilyRadiusMultiplier(family),
                    preserveShape: false) ?? defaultPrefab;
            }

            return prefabs;
        }

        private static Dictionary<string, ParticleSystem> CreateNamedParticlePrefabs()
        {
            var bindings = new Dictionary<string, ParticleSystem>
            {
                ["ShieldImpactParticles"] = CreateParticlePrefab("SkillVfx_ShieldImpactParticles", LoadMaterial(Materials + "/ShieldImpactParticles.mat"), 0.9f, 1.05f, false),
                ["FearDebuffCastParticles"] = CreateParticlePrefab("SkillVfx_FearDebuffCastParticles", LoadMaterial(Materials + "/FearDebuffParticles.mat"), 1f, 1.25f, false),
                ["DarknessDebuffCastParticles"] = CreateParticlePrefab("SkillVfx_DarknessDebuffCastParticles", LoadMaterial(Materials + "/DarknessDebuffParticles.mat"), 1f, 1.25f, false),
                ["ShieldBashLaunchParticles"] = CreateParticlePrefab("SkillVfx_ShieldBashLaunchParticles", LoadMaterial(Materials + "/SkillVfx_ShieldDome.mat"), 0.9f, 0.9f, false),
                ["ShieldBurstShardParticles"] = CreateParticlePrefab("SkillVfx_ShieldBurstShardParticles", LoadMaterial(Materials + "/SkillVfx_ShieldDome.mat"), 1.15f, 1.15f, false),
                ["FlameBurstFlameParticles"] = CreateParticlePrefab("SkillVfx_FlameBurstFlameParticles", LoadMaterial(Materials + "/SkillVfx_FlameBurst.mat"), 1.1f, 0.9f, false),
                ["FlameBurstEmbers"] = CreateParticlePrefab("SkillVfx_FlameBurstEmbers", LoadMaterial(Materials + "/SkillVfx_FlameBurst.mat"), 0.72f, 0.85f, false),
                ["FlameBurstSmoke"] = CreateParticlePrefab("SkillVfx_FlameBurstSmoke", LoadMaterial(Materials + "/SkillVfx_FlameBurst.mat"), 1.35f, 1.1f, false),
                ["LightEchoSupportFireParticles"] = CreateParticlePrefab("SkillVfx_LightEchoSupportFireParticles", LoadMaterial(Materials + "/SkillVfx_SupportFire.mat"), 0.9f, 0.85f, false),
                ["HeavyStrikeSpikedExplosionParticles"] = CreateParticlePrefab("SkillVfx_HeavyStrikeSpikedExplosionParticles", LoadMaterial(Materials + "/SkillVfx_SpikedBurst.mat"), 1.25f, 1.15f, false),
                ["HeavyStrikeSpikeShards"] = CreateParticlePrefab("SkillVfx_HeavyStrikeSpikeShards", LoadMaterial(Materials + "/SkillVfx_SpikedBurst.mat"), 0.85f, 0.85f, false),
                ["FlameBurstFireFountain"] = CreateParticlePrefab("SkillVfx_FlameBurstFireFountain", LoadMaterial(Materials + "/SkillVfx_BloodFountainSlash.mat"), 1.15f, 0.95f, false),
                ["FlameBurstFireMist"] = CreateParticlePrefab("SkillVfx_FlameBurstFireMist", LoadMaterial(Materials + "/SkillVfx_BloodFountainSlash.mat"), 1.35f, 1.05f, false),
                ["DarkShackleMuzzleParticles"] = CreateParticlePrefab("SkillVfx_DarkShackleMuzzleParticles", LoadMaterial(Materials + "/SkillVfx_DarkChainBurst.mat"), 0.9f, 1.05f, false),
                ["LanternSkillMuzzleParticles"] = CreateParticlePrefab("SkillVfx_LanternSkillMuzzleParticles", LoadMaterial(Materials + "/SkillVfx_LightProjectile.mat"), 0.85f, 1.05f, false),
                ["ChargedLightBeamMuzzleParticles"] = CreateParticlePrefab("SkillVfx_ChargedLightBeamMuzzleParticles", LoadMaterial(Materials + "/SkillVfx_LightBeam.mat"), 0.9f, 1.1f, false),
                ["ChargedLightBeamImpactParticles"] = CreateParticlePrefab("SkillVfx_ChargedLightBeamImpactParticles", LoadMaterial(Materials + "/SkillVfx_LightBeam.mat"), 1.05f, 1.2f, false),
                ["TentacleStrikeImpactParticles"] = CreateParticlePrefab("SkillVfx_TentacleStrikeImpactParticles", LoadMaterial(Materials + "/SkillVfx_TentacleWhip.mat"), 1f, 1.1f, false),
            };

            return bindings.Where(pair => pair.Value != null).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private static ParticleSystem CreateParticlePrefab(
            string assetName,
            Material material,
            float sizeMultiplier,
            float radiusMultiplier,
            bool preserveShape)
        {
            var path = $"{Prefabs}/{assetName}.prefab";
            var root = new GameObject(assetName);
            root.transform.localScale = Vector3.one;

            var authoring = root.AddComponent<SkillVfxParticleBurstPrefab>();
            authoring.sizeMultiplier = Mathf.Max(0.01f, sizeMultiplier);
            authoring.radiusMultiplier = Mathf.Max(0.01f, radiusMultiplier);
            authoring.preserveAuthoredShape = preserveShape;

            var particles = root.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.28f;
            main.startLifetime = 0.8f;
            main.startSpeed = 0.6f;
            main.startSize = 0.12f;
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.36f;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 12;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab != null ? prefab.GetComponent<ParticleSystem>() : null;
        }

        private static void PatchProfile(
            ParticleSystem defaultPrefab,
            ParticleSystem swirlPrefab,
            IReadOnlyDictionary<string, ParticleSystem> namedPrefabs)
        {
            var profile = AssetDatabase.LoadAssetAtPath<CombatWorldVfxProfileSO>(ProfilePath);
            if (profile == null)
            {
                Debug.LogError($"Missing Skill VFX profile: {ProfilePath}");
                return;
            }

            profile.defaultSkillParticlePrefab = defaultPrefab;
            profile.swirlSkillParticlePrefab = swirlPrefab;
            profile.particlePrefabBindings = namedPrefabs
                .Select(pair => new SkillVfxParticlePrefabBinding
                {
                    objectName = pair.Key,
                    prefab = pair.Value,
                })
                .ToArray();

            if (namedPrefabs.TryGetValue("ShieldImpactParticles", out var shieldImpact))
            {
                profile.shieldImpactEffect.particlePrefab = shieldImpact;
            }

            if (namedPrefabs.TryGetValue("FearDebuffCastParticles", out var fear))
            {
                profile.fearDebuffCastEffect.particlePrefab = fear;
            }

            if (namedPrefabs.TryGetValue("DarknessDebuffCastParticles", out var darkness))
            {
                profile.darknessDebuffCastEffect.particlePrefab = darkness;
            }

            EditorUtility.SetDirty(profile);
        }

        private static void PatchSkillAssets(
            IReadOnlyDictionary<SkillVfxFamily, ParticleSystem> familyPrefabs,
            ParticleSystem defaultPrefab)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:SkillSO", new[] { "Assets/Data/Skills" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var skill = AssetDatabase.LoadAssetAtPath<SkillSO>(path);
                if (skill == null)
                {
                    continue;
                }

                skill.activationEffect ??= new CombatEffectBinding();
                skill.activationEffect.particleEffect ??= new CombatParticleEffectBinding();
                var family = skill.ResolveVfxFamily();
                var prefab = familyPrefabs.TryGetValue(family, out var resolvedPrefab)
                    ? resolvedPrefab
                    : defaultPrefab;
                skill.activationEffect.particleEffect.particlePrefab = prefab;
                skill.vfx ??= new SkillVfxTuning();
                if (skill.vfx.family == SkillVfxFamily.None)
                {
                    skill.vfx.family = family;
                }

                skill.vfx.particlePrefab = prefab;
                EditorUtility.SetDirty(skill);
            }
        }

        private static Material LoadMaterial(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static float ResolveFamilySizeMultiplier(SkillVfxFamily family)
        {
            return family switch
            {
                SkillVfxFamily.ShieldDome => 1.05f,
                SkillVfxFamily.SpikedBurst => 1.2f,
                SkillVfxFamily.BloodFountainSlash => 1.15f,
                SkillVfxFamily.FlameBurst => 1.1f,
                SkillVfxFamily.LightBeam => 1.05f,
                _ => 1f,
            };
        }

        private static float ResolveFamilyRadiusMultiplier(SkillVfxFamily family)
        {
            return family switch
            {
                SkillVfxFamily.BuffAura => 1.18f,
                SkillVfxFamily.CounterReady => 1.12f,
                SkillVfxFamily.DebuffWave => 1.12f,
                SkillVfxFamily.DarkChainBurst => 1.08f,
                SkillVfxFamily.LightBeam => 1.18f,
                _ => 1f,
            };
        }
    }
}
