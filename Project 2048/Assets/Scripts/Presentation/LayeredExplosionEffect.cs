using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project2048.Presentation
{
    public sealed class LayeredExplosionEffect : MonoBehaviour
    {
        private const string FlashLayerName = "Flash";
        private const string FireballLayerName = "Fireball";
        private const string SmokeLayerName = "Smoke";
        private const string SparksLayerName = "Sparks";
        private const string ShockwaveLayerName = "Shockwave";
        private const string FlashLightName = "FlashLight";

        private const int TextureSize = 64;

        [Header("Playback")]
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool disableInsteadOfDestroy;
        [SerializeField, Min(0f)] private float destroyPaddingSeconds = 0.05f;

        [Header("Shape")]
        [SerializeField, Min(0.05f)] private float scaleMultiplier = 1.35f;
        [SerializeField] private bool includeShockwave = true;
        [SerializeField] private bool horizontalShockwave;

        [Header("Light")]
        [SerializeField] private bool includeFlashLight;
        [SerializeField, Min(0.01f)] private float lightDuration = 0.12f;
        [SerializeField, Min(0f)] private float peakLightIntensity = 8f;
        [SerializeField, Min(0f)] private float lightRange = 4f;

        [Header("Rendering")]
        [SerializeField] private int sortingOrder = 20;
        [SerializeField] private Material additiveMaterial;
        [SerializeField] private Material smokeMaterial;
        [SerializeField] private Material shockwaveMaterial;

        private ParticleSystem[] systems;
        private Light flashLight;
        private Material runtimeAdditiveMaterial;
        private Material runtimeSmokeMaterial;
        private Material runtimeShockwaveMaterial;
        private Texture2D softParticleTexture;
        private Texture2D shockwaveTexture;
        private int sortingLayerId;

        public float EstimatedLifetimeSeconds => 2.15f;

        private void Awake()
        {
            if (buildOnAwake)
            {
                EnsureBuilt();
            }

            CacheComponents();
            ResetFlashLight();
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDestroy()
        {
            DestroyOwnedObject(ref runtimeAdditiveMaterial);
            DestroyOwnedObject(ref runtimeSmokeMaterial);
            DestroyOwnedObject(ref runtimeShockwaveMaterial);
            DestroyOwnedObject(ref softParticleTexture);
            DestroyOwnedObject(ref shockwaveTexture);
        }

        public void PlayAt(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            Play();
        }

        public void Play()
        {
            EnsureBuilt();
            CacheComponents();
            StopAllCoroutines();

            foreach (var system in systems)
            {
                if (system == null)
                {
                    continue;
                }

                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Clear(false);
                system.Play(false);
            }

            if (flashLight != null)
            {
                StartCoroutine(AnimateLight());
            }

            StartCoroutine(FinishWhenParticlesDie());
        }

        public void ApplySorting(SpriteRenderer sortingReference, int orderOffset = 12)
        {
            if (sortingReference == null)
            {
                return;
            }

            sortingLayerId = sortingReference.sortingLayerID;
            sortingOrder = sortingReference.sortingOrder + orderOffset;
            ApplyRendererSorting();
        }

        [ContextMenu("Rebuild Child Systems")]
        private void RebuildChildSystems()
        {
            EnsureBuilt();
            CacheComponents();
            ResetFlashLight();
        }

        private void EnsureBuilt()
        {
            ConfigureFlash(EnsureParticleSystem(FlashLayerName));
            ConfigureFireball(EnsureParticleSystem(FireballLayerName));
            ConfigureSmoke(EnsureParticleSystem(SmokeLayerName));
            ConfigureSparks(EnsureParticleSystem(SparksLayerName));

            var shockwaveObject = FindDirectChild(ShockwaveLayerName);
            if (includeShockwave)
            {
                ConfigureShockwave(EnsureParticleSystem(ShockwaveLayerName));
                if (shockwaveObject != null)
                {
                    shockwaveObject.SetActive(true);
                }
            }
            else if (shockwaveObject != null)
            {
                shockwaveObject.SetActive(false);
            }

            var lightObject = FindDirectChild(FlashLightName);
            if (includeFlashLight)
            {
                flashLight = EnsureFlashLight();
                if (lightObject != null)
                {
                    lightObject.SetActive(true);
                }
            }
            else if (lightObject != null)
            {
                lightObject.SetActive(false);
            }
        }

        private ParticleSystem EnsureParticleSystem(string layerName)
        {
            var child = FindDirectChild(layerName);
            if (child == null)
            {
                child = new GameObject(layerName);
                var childTransform = child.transform;
                childTransform.SetParent(transform, false);
                childTransform.localPosition = Vector3.zero;
                childTransform.localRotation = Quaternion.identity;
                childTransform.localScale = Vector3.one;
            }

            var system = child.GetComponent<ParticleSystem>();
            if (system == null)
            {
                system = child.AddComponent<ParticleSystem>();
            }

            return system;
        }

        private GameObject FindDirectChild(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.gameObject : null;
        }

        private Light EnsureFlashLight()
        {
            var child = FindDirectChild(FlashLightName);
            if (child == null)
            {
                child = new GameObject(FlashLightName);
                child.transform.SetParent(transform, false);
                child.transform.localPosition = new Vector3(0f, 0f, -1f);
                child.transform.localRotation = Quaternion.identity;
                child.transform.localScale = Vector3.one;
            }

            var light = child.GetComponent<Light>();
            if (light == null)
            {
                light = child.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.range = lightRange * scaleMultiplier;
            light.color = new Color(1f, 0.46f, 0.08f, 1f);
            light.intensity = 0f;
            light.enabled = false;
            return light;
        }

        private void CacheComponents()
        {
            systems = GetComponentsInChildren<ParticleSystem>(true);
            flashLight = includeFlashLight ? GetComponentInChildren<Light>(true) : null;
            ApplyRendererSorting();
        }

        private void ConfigureFlash(ParticleSystem system)
        {
            ConfigureCommon(system, 0.1f, 1);
            var main = system.main;
            main.startLifetime = 0.08f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(3.5f * scaleMultiplier, 5f * scaleMultiplier);

            ConfigureBurst(system, 1, 1);
            var shape = system.shape;
            shape.enabled = false;
            ConfigureSizeOverLifetime(system, new[]
            {
                new Keyframe(0f, 0.2f),
                new Keyframe(0.08f, 1f),
                new Keyframe(1f, 0f),
            });
            ConfigureColorOverLifetime(system, CreateGradient(
                new Color(1f, 0.96f, 0.72f, 1f),
                new Color(1f, 0.58f, 0.08f, 1f),
                new Color(0.74f, 0.12f, 0.02f, 0f)));
            ConfigureRenderer(system, ResolveAdditiveMaterial(), ParticleSystemRenderMode.Billboard, 5);
        }

        private void ConfigureFireball(ParticleSystem system)
        {
            ConfigureCommon(system, 0.3f, 24);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * scaleMultiplier, 5f * scaleMultiplier);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f * scaleMultiplier, 1.6f * scaleMultiplier);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            ConfigureBurst(system, 14, 20);
            ConfigureSphereShape(system, 0.1f * scaleMultiplier, 0.3f);
            ConfigureSizeOverLifetime(system, new[]
            {
                new Keyframe(0f, 0.3f),
                new Keyframe(0.15f, 1.15f),
                new Keyframe(0.65f, 0.9f),
                new Keyframe(1f, 0f),
            });
            ConfigureColorOverLifetime(system, CreateGradient(
                new Color(1f, 0.98f, 0.78f, 1f),
                new Color(1f, 0.46f, 0.06f, 1f),
                new Color(0.82f, 0.12f, 0.02f, 0.9f),
                new Color(0.22f, 0.035f, 0.012f, 0.12f)));
            ConfigureNoise(system, 0.15f, 0.25f, 0.6f, 0.4f);
            ConfigureRenderer(system, ResolveAdditiveMaterial(), ParticleSystemRenderMode.Billboard, 4);
        }

        private void ConfigureSmoke(ParticleSystem system)
        {
            ConfigureCommon(system, 1.7f, 16);
            var main = system.main;
            main.startDelay = new ParticleSystem.MinMaxCurve(0.04f, 0.07f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f * scaleMultiplier, 1.3f * scaleMultiplier);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f * scaleMultiplier, 1.5f * scaleMultiplier);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            ConfigureBurst(system, 8, 12);
            ConfigureSphereShape(system, 0.2f * scaleMultiplier, 0f);
            ConfigureSizeOverLifetime(system, new[]
            {
                new Keyframe(0f, 0.35f),
                new Keyframe(0.25f, 1f),
                new Keyframe(1f, 1.8f),
            });
            ConfigureColorOverLifetime(system, CreateGradient(
                new Color(0.24f, 0.12f, 0.055f, 0f),
                new Color(0.18f, 0.09f, 0.04f, 0.42f),
                new Color(0.07f, 0.035f, 0.02f, 0f)));
            ConfigureVelocityY(system, 0.5f * scaleMultiplier, 1f * scaleMultiplier);
            ConfigureNoise(system, 0.4f, 0.7f, 0.5f, 0.25f);
            ConfigureRenderer(system, ResolveSmokeMaterial(), ParticleSystemRenderMode.Billboard, 2);
        }

        private void ConfigureSparks(ParticleSystem system)
        {
            ConfigureCommon(system, 0.75f, 40);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f * scaleMultiplier, 12f * scaleMultiplier);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f * scaleMultiplier, 0.07f * scaleMultiplier);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(1.75f);

            ConfigureBurst(system, 20, 35);
            ConfigureSphereShape(system, 0.05f * scaleMultiplier, 0.2f);
            ConfigureSizeOverLifetime(system, new[]
            {
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f),
            });
            ConfigureColorOverLifetime(system, CreateGradient(
                new Color(1f, 0.9f, 0.42f, 1f),
                new Color(1f, 0.42f, 0.04f, 0.85f),
                new Color(0.76f, 0.12f, 0.02f, 0f)));
            ConfigureSparkTrails(system);
            ConfigureRenderer(system, ResolveAdditiveMaterial(), ParticleSystemRenderMode.Stretch, 7);
        }

        private void ConfigureShockwave(ParticleSystem system)
        {
            ConfigureCommon(system, 0.35f, 1);
            var main = system.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 0f;
            main.startSize = scaleMultiplier;

            ConfigureBurst(system, 1, 1);
            var shape = system.shape;
            shape.enabled = false;
            ConfigureSizeOverLifetime(system, new[]
            {
                new Keyframe(0f, 0.1f),
                new Keyframe(0.15f, 2f),
                new Keyframe(1f, 5f),
            });
            ConfigureColorOverLifetime(system, CreateGradient(
                new Color(1f, 0.72f, 0.18f, 0.7f),
                new Color(0.9f, 0.28f, 0.04f, 0.35f),
                new Color(0.38f, 0.06f, 0.02f, 0f)));

            var renderMode = horizontalShockwave
                ? ParticleSystemRenderMode.HorizontalBillboard
                : ParticleSystemRenderMode.Billboard;
            ConfigureRenderer(system, ResolveShockwaveMaterial(), renderMode, 1);
        }

        private static void ConfigureCommon(ParticleSystem system, float duration, int maxParticles)
        {
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startColor = Color.white;
            main.maxParticles = maxParticles;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 0f;

            var velocity = system.velocityOverLifetime;
            velocity.enabled = false;
            var noise = system.noise;
            noise.enabled = false;
            var trails = system.trails;
            trails.enabled = false;
        }

        private static void ConfigureBurst(ParticleSystem system, short minCount, short maxCount)
        {
            var emission = system.emission;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, minCount, maxCount),
            });
        }

        private static void ConfigureSphereShape(ParticleSystem system, float radius, float randomDirectionAmount)
        {
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            shape.randomDirectionAmount = randomDirectionAmount;
        }

        private static void ConfigureSizeOverLifetime(ParticleSystem system, Keyframe[] keys)
        {
            var module = system.sizeOverLifetime;
            module.enabled = true;
            module.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));
        }

        private static void ConfigureColorOverLifetime(ParticleSystem system, Gradient gradient)
        {
            var module = system.colorOverLifetime;
            module.enabled = true;
            module.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void ConfigureVelocityY(ParticleSystem system, float minY, float maxY)
        {
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(minY, maxY);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        private static void ConfigureNoise(
            ParticleSystem system,
            float minStrength,
            float maxStrength,
            float frequency,
            float scrollSpeed)
        {
            var noise = system.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            noise.strength = new ParticleSystem.MinMaxCurve(minStrength, maxStrength);
            noise.frequency = frequency;
            noise.scrollSpeed = scrollSpeed;
            noise.damping = true;
        }

        private void ConfigureSparkTrails(ParticleSystem system)
        {
            var trails = system.trails;
            trails.enabled = true;
            trails.ratio = 0.6f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            trails.dieWithParticles = true;
            trails.inheritParticleColor = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f)));
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(CreateGradient(
                new Color(1f, 0.9f, 0.42f, 1f),
                new Color(1f, 0.36f, 0.04f, 0.65f),
                new Color(0.58f, 0.08f, 0.02f, 0f)));
        }

        private void ConfigureRenderer(
            ParticleSystem system,
            Material material,
            ParticleSystemRenderMode renderMode,
            int sortingOffset)
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = renderMode;
            renderer.sharedMaterial = material;
            renderer.trailMaterial = material;
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder + sortingOffset;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = 10f;

            if (renderMode == ParticleSystemRenderMode.Stretch)
            {
                renderer.lengthScale = 2.5f;
                renderer.velocityScale = 0.1f;
            }
            else
            {
                renderer.lengthScale = 1f;
                renderer.velocityScale = 0f;
            }
        }

        private void ApplyRendererSorting()
        {
            ApplyRendererSorting(FlashLayerName, 5);
            ApplyRendererSorting(FireballLayerName, 4);
            ApplyRendererSorting(SmokeLayerName, 2);
            ApplyRendererSorting(SparksLayerName, 7);
            ApplyRendererSorting(ShockwaveLayerName, 1);
        }

        private void ApplyRendererSorting(string childName, int sortingOffset)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                return;
            }

            var renderer = child.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder + sortingOffset;
        }

        private IEnumerator AnimateLight()
        {
            flashLight.enabled = true;

            var elapsed = 0f;
            while (elapsed < lightDuration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / lightDuration);
                var fade = 1f - normalizedTime;
                flashLight.intensity = peakLightIntensity * fade * fade;
                yield return null;
            }

            ResetFlashLight();
        }

        private IEnumerator FinishWhenParticlesDie()
        {
            yield return null;

            while (AnySystemAlive())
            {
                yield return null;
            }

            if (destroyPaddingSeconds > 0f)
            {
                yield return new WaitForSeconds(destroyPaddingSeconds);
            }

            if (disableInsteadOfDestroy)
            {
                gameObject.SetActive(false);
            }
            else if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }

        private bool AnySystemAlive()
        {
            foreach (var system in systems)
            {
                if (system != null && system.gameObject.activeInHierarchy && system.IsAlive(false))
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetFlashLight()
        {
            if (flashLight == null)
            {
                return;
            }

            flashLight.intensity = 0f;
            flashLight.enabled = false;
        }

        private Material ResolveAdditiveMaterial()
        {
            if (additiveMaterial != null)
            {
                return additiveMaterial;
            }

            runtimeAdditiveMaterial ??= CreateParticleMaterial(
                "LayeredExplosion_Additive_Runtime",
                true,
                ResolveSoftParticleTexture());
            return runtimeAdditiveMaterial;
        }

        private Material ResolveSmokeMaterial()
        {
            if (smokeMaterial != null)
            {
                return smokeMaterial;
            }

            runtimeSmokeMaterial ??= CreateParticleMaterial(
                "LayeredExplosion_Smoke_Runtime",
                false,
                ResolveSoftParticleTexture());
            return runtimeSmokeMaterial;
        }

        private Material ResolveShockwaveMaterial()
        {
            if (shockwaveMaterial != null)
            {
                return shockwaveMaterial;
            }

            runtimeShockwaveMaterial ??= CreateParticleMaterial(
                "LayeredExplosion_Shockwave_Runtime",
                true,
                ResolveShockwaveTexture());
            return runtimeShockwaveMaterial;
        }

        private Texture2D ResolveSoftParticleTexture()
        {
            if (softParticleTexture != null)
            {
                return softParticleTexture;
            }

            softParticleTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "LayeredExplosion_SoftParticle_Runtime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var center = (TextureSize - 1) * 0.5f;
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(1f - SmoothStepEdge(0.18f, 1f, distance));
                    softParticleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            softParticleTexture.Apply(false, true);
            return softParticleTexture;
        }

        private Texture2D ResolveShockwaveTexture()
        {
            if (shockwaveTexture != null)
            {
                return shockwaveTexture;
            }

            shockwaveTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "LayeredExplosion_Shockwave_Runtime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var center = (TextureSize - 1) * 0.5f;
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var outer = 1f - SmoothStepEdge(0.48f, 0.56f, distance);
                    var inner = SmoothStepEdge(0.34f, 0.44f, distance);
                    var alpha = Mathf.Clamp01(outer * inner);
                    shockwaveTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            shockwaveTexture.Apply(false, true);
            return shockwaveTexture;
        }

        private static Material CreateParticleMaterial(string materialName, bool additive, Texture texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = materialName,
                renderQueue = 3000,
                hideFlags = HideFlags.HideAndDontSave,
            };

            SetTextureIfPresent(material, "_BaseMap", texture);
            SetTextureIfPresent(material, "_MainTex", texture);
            SetColorIfPresent(material, "_BaseColor", Color.white);
            SetColorIfPresent(material, "_Color", Color.white);
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_ZWrite", 0f);
            SetFloatIfPresent(material, "_Cull", (float)CullMode.Off);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(
                material,
                "_DstBlend",
                additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_BlendOp", (float)BlendOp.Add);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }

        private static Gradient CreateGradient(params Color[] colors)
        {
            var gradient = new Gradient();
            var colorKeys = new GradientColorKey[colors.Length];
            var alphaKeys = new GradientAlphaKey[colors.Length];
            var maxIndex = Mathf.Max(1, colors.Length - 1);

            for (var index = 0; index < colors.Length; index++)
            {
                var time = index / (float)maxIndex;
                colorKeys[index] = new GradientColorKey(colors[index], time);
                alphaKeys[index] = new GradientAlphaKey(colors[index].a, time);
            }

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private static void SetTextureIfPresent(Material material, string propertyName, Texture texture)
        {
            if (texture != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetColorIfPresent(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static void SetFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static float SmoothStepEdge(float edge0, float edge1, float value)
        {
            var t = Mathf.Clamp01((value - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static void DestroyOwnedObject<T>(ref T target)
            where T : Object
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }

            target = null;
        }
    }
}
