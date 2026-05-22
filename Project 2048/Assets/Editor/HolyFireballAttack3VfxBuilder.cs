using System.IO;
using Project2048.Presentation;
using Project2048.Skills;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project2048.EditorTools
{
    public static class HolyFireballAttack3VfxBuilder
    {
        private const string ShaderGraphTemplatePath =
            "Packages/com.unity.shadergraph/GraphTemplates/Cross Pipeline/0_Particle Unlit.shadergraph";

        private const string ShaderGraphPath = "Assets/Shaders/Effects/HolyFireParticle.shadergraph";
        private const string PrefabPath = "Assets/Prefabs/Effects/HolyFireball_Attack3.prefab";
        private const float VisualScale = 3f;

        private static readonly string[] LightProjectileSkillPaths =
        {
            "Assets/Data/Skills/LightShot.asset",
            "Assets/Data/Skills/GatherLight.asset",
        };

        [MenuItem("Tools/Project2048/VFX/Rebuild Holy Fireball Attack 3")]
        public static void Rebuild()
        {
            EnsureFolder("Assets/Shaders/Effects");
            EnsureFolder("Assets/Art/Effects/HolyFireball");
            EnsureFolder("Assets/Materials/Effects");
            EnsureFolder("Assets/Prefabs/Effects");

            EnsureShaderGraph();
            var orbTexture = CreateTexture("Assets/Art/Effects/HolyFireball/HolyFire_Orb.png", BuildSoftOrb);
            var wispTexture = CreateTexture("Assets/Art/Effects/HolyFireball/HolyFire_Wisp.png", BuildWisp);
            var haloTexture = CreateTexture("Assets/Art/Effects/HolyFireball/HolyFire_Halo.png", BuildHalo);
            var sparkTexture = CreateTexture("Assets/Art/Effects/HolyFireball/HolyFire_Spark.png", BuildSpark);

            AssetDatabase.ImportAsset(ShaderGraphPath, ImportAssetOptions.ForceUpdate);
            var shader = LoadHolyParticleShader();
            var coreMaterial = CreateMaterial("Assets/Materials/Effects/HolyFireball_Core.mat", shader, orbTexture);
            var wispMaterial = CreateMaterial("Assets/Materials/Effects/HolyFireball_Wisp.mat", shader, wispTexture);
            var haloMaterial = CreateMaterial("Assets/Materials/Effects/HolyFireball_Halo.mat", shader, haloTexture);
            var sparkMaterial = CreateMaterial("Assets/Materials/Effects/HolyFireball_Spark.mat", shader, sparkTexture);

            var prefab = BuildPrefab(coreMaterial, wispMaterial, haloMaterial, sparkMaterial);
            AssignLightProjectileSkills(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Rebuilt holy fireball VFX and assigned it to light projectile skills: {PrefabPath}");
        }

        private static void EnsureFolder(string path)
        {
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

        private static void EnsureShaderGraph()
        {
            if (File.Exists(ShaderGraphPath))
            {
                return;
            }

            if (!AssetDatabase.CopyAsset(ShaderGraphTemplatePath, ShaderGraphPath))
            {
                Debug.LogWarning("Could not copy Shader Graph template. Falling back to URP particle shader for materials.");
            }
        }

        private static Shader LoadHolyParticleShader()
        {
            return Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? AssetDatabase.LoadAssetAtPath<Shader>(ShaderGraphPath)
                ?? Shader.Find("Sprites/Default");
        }

        private static Texture2D CreateTexture(string path, System.Func<int, int, Color[]> builder)
        {
            const int width = 128;
            const int height = 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = Path.GetFileNameWithoutExtension(path),
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            texture.SetPixels(builder(width, height));
            texture.Apply(false, false);

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Color[] BuildSoftOrb(int width, int height)
        {
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var uv = PixelUv(x, y, width, height);
                    var radius = uv.magnitude;
                    var core = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(radius / 0.42f));
                    var corona = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(radius / 0.86f));
                    var alpha = Mathf.Pow(Mathf.Clamp01(core + corona * 0.16f), 1.18f);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            return pixels;
        }

        private static Color[] BuildWisp(int width, int height)
        {
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var u = (x + 0.5f) / width;
                    var v = (y + 0.5f) / height;
                    var centeredX = (u - 0.5f) * 2f;
                    var vertical = v * 2f - 1f;
                    var curl = Mathf.Sin(v * 17.5f) * 0.085f + Mathf.Sin(v * 34f + 1.7f) * 0.028f;
                    var widthAtHeight = Mathf.Lerp(0.34f, 0.055f, Mathf.SmoothStep(0f, 1f, v));
                    var body = Mathf.SmoothStep(1f, 0f, Mathf.Abs(centeredX - curl) / widthAtHeight);
                    var spine = Mathf.SmoothStep(1f, 0f, Mathf.Abs(centeredX - curl) / (widthAtHeight * 0.35f));
                    var tip = Mathf.SmoothStep(1.08f, -0.12f, vertical);
                    var baseFade = Mathf.SmoothStep(-1f, -0.55f, vertical);
                    var alpha = Mathf.Clamp01((body * 0.72f + spine * 0.38f) * tip * baseFade);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, Mathf.Pow(alpha, 1.35f));
                }
            }

            return pixels;
        }

        private static Color[] BuildHalo(int width, int height)
        {
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var radius = PixelUv(x, y, width, height).magnitude;
                    var ring = Mathf.Exp(-Mathf.Pow((radius - 0.52f) * 9.2f, 2f));
                    var glow = Mathf.SmoothStep(1f, 0f, radius) * 0.08f;
                    pixels[y * width + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(ring * 0.72f + glow));
                }
            }

            return pixels;
        }

        private static Color[] BuildSpark(int width, int height)
        {
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var uv = PixelUv(x, y, width, height);
                    var core = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(uv.magnitude / 0.16f));
                    var horizontal = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(Mathf.Abs(uv.y) / 0.045f))
                        * Mathf.SmoothStep(1f, 0f, Mathf.Abs(uv.x));
                    var vertical = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(Mathf.Abs(uv.x) / 0.045f))
                        * Mathf.SmoothStep(1f, 0f, Mathf.Abs(uv.y));
                    var alpha = Mathf.Clamp01(core + horizontal * 0.72f + vertical * 0.72f);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            return pixels;
        }

        private static Vector2 PixelUv(int x, int y, int width, int height)
        {
            return new Vector2(((x + 0.5f) / width - 0.5f) * 2f, ((y + 0.5f) / height - 0.5f) * 2f);
        }

        private static Material CreateMaterial(string path, Shader shader, Texture2D texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.name = Path.GetFileNameWithoutExtension(path);
            if (material.HasProperty("_Base_Map"))
            {
                material.SetTexture("_Base_Map", texture);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            SetColorIfPresent(material, "_BaseColor", Color.white);
            SetColorIfPresent(material, "_Color", Color.white);
            SetColorIfPresent(material, "_EmissionColor", Color.black);
            ConfigureAdditiveParticleMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureAdditiveParticleMaterial(Material material)
        {
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            SetFloatIfPresent(material, "_SurfaceType", 1f);
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_BlendMode", 2f);
            SetFloatIfPresent(material, "_Blend", 2f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.One);
            SetFloatIfPresent(material, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloatIfPresent(material, "_DstBlendAlpha", (float)BlendMode.One);
            SetFloatIfPresent(material, "_AlphaSrcBlend", (float)BlendMode.One);
            SetFloatIfPresent(material, "_AlphaDstBlend", (float)BlendMode.One);
            SetFloatIfPresent(material, "_ZWrite", 0f);
            SetFloatIfPresent(material, "_Cull", (float)CullMode.Off);
            SetFloatIfPresent(material, "_ColorMode", 0f);
            SetFloatIfPresent(material, "_SoftParticlesEnabled", 0f);
            SetFloatIfPresent(material, "_CameraFadingEnabled", 0f);
            SetFloatIfPresent(material, "_DistortionEnabled", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);
            SetVectorIfPresent(material, "_BaseColorAddSubDiff", Vector4.zero);
            SetVectorIfPresent(material, "_CameraFadeParams", Vector4.zero);
            SetVectorIfPresent(material, "_SoftParticleFadeParams", Vector4.zero);
            SetFloatIfPresent(material, "_DistortionStrengthScaled", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
            material.DisableKeyword("_COLOROVERLAY_ON");
            material.DisableKeyword("_COLORCOLOR_ON");
            material.DisableKeyword("_COLORADDSUBDIFF_ON");
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("SHADOWCASTER", false);
            material.SetShaderPassEnabled("DepthNormalsOnly", false);
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void SetColorIfPresent(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, value);
            }
        }

        private static void SetVectorIfPresent(Material material, string property, Vector4 value)
        {
            if (material.HasProperty(property))
            {
                material.SetVector(property, value);
            }
        }

        private static GameObject BuildPrefab(
            Material coreMaterial,
            Material wispMaterial,
            Material haloMaterial,
            Material sparkMaterial)
        {
            AssetDatabase.DeleteAsset(PrefabPath);

            var root = new GameObject("HolyFireball_Attack3");
            root.transform.localScale = Vector3.one * VisualScale;
            var projectile = root.AddComponent<CombatProjectileEffect>();

            var core = CreateCore(root.transform, coreMaterial);
            var flame = CreateInnerFlame(root.transform, wispMaterial);
            var halo = CreateTravelHalo(root.transform, haloMaterial);
            var trail = CreateTrail(root.transform, wispMaterial);
            var sparks = CreateTravelSparks(root.transform, sparkMaterial);
            var impactFlash = CreateImpactFlash(root.transform, coreMaterial);
            var impactRing = CreateImpactRing(root.transform, haloMaterial);
            var impactSparks = CreateImpactSparks(root.transform, sparkMaterial);
            var impactWisps = CreateImpactWisps(root.transform, wispMaterial);

            var serialized = new SerializedObject(projectile);
            serialized.FindProperty("sourceLocalOffset").vector3Value = new Vector3(0.48f, 0.28f, 0f);
            serialized.FindProperty("targetLocalOffset").vector3Value = new Vector3(-0.2f, 0.34f, 0f);
            serialized.FindProperty("travelSeconds").floatValue = 0.48f;
            serialized.FindProperty("arcHeight").floatValue = 0.32f;
            serialized.FindProperty("impactLifetimeSeconds").floatValue = 0.5f;
            SetParticleArray(serialized.FindProperty("travelParticles"), core, flame, halo, trail, sparks);
            SetParticleArray(serialized.FindProperty("impactParticles"), impactFlash, impactRing, impactSparks, impactWisps);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void SetParticleArray(SerializedProperty property, params ParticleSystem[] systems)
        {
            property.arraySize = systems.Length;
            for (var i = 0; i < systems.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = systems[i];
            }
        }

        private static ParticleSystem CreateCore(Transform parent, Material material)
        {
            var ps = CreateParticleObject(parent, "Core", material, 52);
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.17f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.94f),
                new Color(0.8f, 0.96f, 1f, 0.78f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 52;

            var emission = ps.emission;
            emission.rateOverTime = 40f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;

            ApplySizeOverLife(ps, new AnimationCurve(
                new Keyframe(0f, 0.72f),
                new Keyframe(0.28f, 1f),
                new Keyframe(1f, 0f)));
            ApplyColorOverLife(ps, HolyWhiteGradient(0.98f, 0f));
            ApplyNoise(ps, 0.08f, 1.8f);
            return ps;
        }

        private static ParticleSystem CreateInnerFlame(Transform parent, Material material)
        {
            var ps = CreateParticleObject(parent, "InnerFlameWisps", material, 51);
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.26f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(-0.75f, 0.75f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.86f),
                new Color(1f, 0.82f, 0.34f, 0.58f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 64;

            var emission = ps.emission;
            emission.rateOverTime = 48f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.09f;

            ApplyVelocityOverLifeY(ps, ParticleSystemSimulationSpace.Local, 0.08f, 0.3f);

            ApplySizeOverLife(ps, new AnimationCurve(
                new Keyframe(0f, 0.24f),
                new Keyframe(0.34f, 1f),
                new Keyframe(1f, 0f)));
            ApplyColorOverLife(ps, HolyWhiteGradient(0.86f, 0f));
            ApplyNoise(ps, 0.18f, 2.6f);
            return ps;
        }

        private static ParticleSystem CreateTravelHalo(Transform parent, Material material)
        {
            var ps = CreateParticleObject(parent, "RadiantHalo", material, 50);
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.22f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.46f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI);
            main.startColor = new Color(0.72f, 0.94f, 1f, 0.2f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 16;

            var emission = ps.emission;
            emission.rateOverTime = 7f;

            var shape = ps.shape;
            shape.enabled = false;

            ApplySizeOverLife(ps, new AnimationCurve(
                new Keyframe(0f, 0.58f),
                new Keyframe(0.55f, 1f),
                new Keyframe(1f, 1.15f)));
            ApplyColorOverLife(ps, FadeOutGradient(new Color(0.78f, 0.96f, 1f, 0.2f), 0f));
            return ps;
        }

        private static ParticleSystem CreateTrail(Transform parent, Material material)
        {
            var ps = CreateParticleObject(parent, "TrailingSacredFlame", material, 49);
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.36f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(-1.1f, 1.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.62f),
                new Color(0.95f, 0.72f, 0.28f, 0.42f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;

            var emission = ps.emission;
            emission.rateOverTime = 78f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;

            var inherit = ps.inheritVelocity;
            inherit.enabled = true;
            inherit.mode = ParticleSystemInheritVelocityMode.Current;
            inherit.curve = new ParticleSystem.MinMaxCurve(-0.85f);

            ApplySizeOverLife(ps, new AnimationCurve(
                new Keyframe(0f, 0.8f),
                new Keyframe(0.42f, 1f),
                new Keyframe(1f, 0f)));
            ApplyColorOverLife(ps, HolyWhiteGradient(0.72f, 0f));
            ApplyNoise(ps, 0.34f, 1.2f);
            return ps;
        }

        private static ParticleSystem CreateTravelSparks(Transform parent, Material material)
        {
            var ps = CreateParticleObject(parent, "GoldCyanSparks", material, 53);
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.34f, 0.95f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.026f, 0.058f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.45f, 0.9f),
                new Color(0.64f, 0.95f, 1f, 0.72f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            var emission = ps.emission;
            emission.rateOverTime = 28f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;

            var inherit = ps.inheritVelocity;
            inherit.enabled = true;
            inherit.mode = ParticleSystemInheritVelocityMode.Current;
            inherit.curve = new ParticleSystem.MinMaxCurve(-0.35f);

            ApplySizeOverLife(ps, new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.25f, 1f),
                new Keyframe(1f, 0f)));
            ApplyColorOverLife(ps, FadeOutGradient(new Color(1f, 0.88f, 0.46f, 0.88f), 0f));
            return ps;
        }

        private static ParticleSystem CreateImpactFlash(Transform parent, Material material)
        {
            var ps = CreateParticleObject(parent, "ImpactWhiteFlash", material, 55);
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.16f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.42f);
            main.startColor = new Color(1f, 1f, 1f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 14;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 7) });
            var shape = ps.shape;
            shape.enabled = false;
            ApplySizeOverLife(ps, new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.22f, 1f),
                new Keyframe(1f, 1.35f)));
            ApplyColorOverLife(ps, FadeOutGradient(new Color(1f, 1f, 1f, 0.95f), 0f));
            return ps;
        }

        private static ParticleSystem CreateImpactRing(Transform parent, Material material)
        {
            var ps = CreateParticleObject(parent, "ImpactConsecrationRing", material, 54);
            var main = ps.main;
            main.duration = 0.12f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.32f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.72f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI);
            main.startColor = new Color(0.76f, 0.94f, 1f, 0.32f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 14;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });
            var shape = ps.shape;
            shape.enabled = false;
            ApplySizeOverLife(ps, new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(1f, 1.1f)));
            ApplyColorOverLife(ps, FadeOutGradient(new Color(0.78f, 0.95f, 1f, 0.32f), 0f));
            return ps;
        }

        private static ParticleSystem CreateImpactSparks(Transform parent, Material material)
        {
            var ps = CreateParticleObject(parent, "ImpactRadiantSparks", material, 56);
            var main = ps.main;
            main.duration = 0.12f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.72f, 1.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.034f, 0.082f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.88f, 0.36f, 0.95f),
                new Color(0.68f, 0.95f, 1f, 0.9f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;

            ApplySizeOverLife(ps, new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.18f, 1f),
                new Keyframe(1f, 0f)));
            ApplyColorOverLife(ps, FadeOutGradient(new Color(1f, 0.9f, 0.42f, 0.9f), 0f));
            return ps;
        }

        private static ParticleSystem CreateImpactWisps(Transform parent, Material material)
        {
            var ps = CreateParticleObject(parent, "ImpactAscendingWisps", material, 52);
            var main = ps.main;
            main.duration = 0.16f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.24f, 0.62f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.26f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(-0.95f, 0.95f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.62f),
                new Color(0.86f, 0.96f, 1f, 0.48f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 42;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;

            ApplyVelocityOverLifeY(ps, ParticleSystemSimulationSpace.World, 0.2f, 0.56f);

            ApplySizeOverLife(ps, new AnimationCurve(
                new Keyframe(0f, 0.25f),
                new Keyframe(0.38f, 1f),
                new Keyframe(1f, 0f)));
            ApplyColorOverLife(ps, HolyWhiteGradient(0.66f, 0f));
            ApplyNoise(ps, 0.28f, 1.9f);
            return ps;
        }

        private static ParticleSystem CreateParticleObject(Transform parent, string name, Material material, int sortingOrder)
        {
            var child = new GameObject(name, typeof(ParticleSystem));
            child.transform.SetParent(parent, false);
            var ps = child.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingOrder = sortingOrder;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = 2f;
            return ps;
        }

        private static void ApplySizeOverLife(ParticleSystem ps, AnimationCurve curve)
        {
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void ApplyColorOverLife(ParticleSystem ps, Gradient gradient)
        {
            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void ApplyVelocityOverLifeY(
            ParticleSystem ps,
            ParticleSystemSimulationSpace space,
            float minY,
            float maxY)
        {
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = space;
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(minY, maxY);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        private static void ApplyNoise(ParticleSystem ps, float strength, float frequency)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = strength;
            noise.frequency = frequency;
            noise.scrollSpeed = 0.15f;
            noise.damping = true;
        }

        private static Gradient HolyWhiteGradient(float startAlpha, float endAlpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                    new GradientColorKey(new Color(0.74f, 0.95f, 1f), 0.42f),
                    new GradientColorKey(new Color(1f, 0.78f, 0.3f), 0.76f),
                    new GradientColorKey(new Color(1f, 1f, 1f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(startAlpha, 0f),
                    new GradientAlphaKey(startAlpha * 0.82f, 0.35f),
                    new GradientAlphaKey(startAlpha * 0.42f, 0.72f),
                    new GradientAlphaKey(endAlpha, 1f),
                });
            return gradient;
        }

        private static Gradient FadeOutGradient(Color color, float endAlpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(new Color(1f, 1f, 1f), 0.65f),
                    new GradientColorKey(color, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(color.a, 0f),
                    new GradientAlphaKey(color.a * 0.65f, 0.45f),
                    new GradientAlphaKey(endAlpha, 1f),
                });
            return gradient;
        }

        private static void AssignLightProjectileSkills(GameObject prefab)
        {
            foreach (var skillPath in LightProjectileSkillPaths)
            {
                var skill = AssetDatabase.LoadAssetAtPath<SkillSO>(skillPath);
                if (skill == null)
                {
                    Debug.LogError($"Could not find light projectile skill asset at {skillPath}");
                    continue;
                }

                skill.activationEffect ??= new CombatEffectBinding();
                skill.activationEffect.vfxPrefab = prefab;
                skill.activationEffect.localOffset = Vector3.zero;
                skill.activationEffect.autoDestroySeconds = 1.55f;
                skill.activationEffect.volumeScale = 1.05f;
                skill.activationEffect.minPitch = 0.96f;
                skill.activationEffect.maxPitch = 1.04f;
                skill.activationEffect.sfxDelaySeconds = 0.3f;
                skill.vfxFamily = SkillVfxFamily.LightProjectile;
                skill.vfxScale = skill.skillId == "gather-light" ? 1.8f : 1f;
                skill.vfxIntensity = skill.skillId == "gather-light" ? 1.4f : 1.1f;
                EditorUtility.SetDirty(skill);
            }
        }
    }
}
