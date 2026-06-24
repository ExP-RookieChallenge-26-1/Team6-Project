using Project2048.Enemy;
using Project2048.Presentation;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Prototype
{
    [System.Serializable]
    public sealed class SkillVfxParticlePrefabBinding
    {
        public string objectName;
        public ParticleSystem prefab;
    }

    [System.Serializable]
    public sealed class SkillVfxDesignTimeBinding
    {
        public SkillVfxFamily family;
        public Sprite sprite;
        public GameObject prefab;
        public Vector3 localOffset = new(0f, 0.16f, 0f);
        public float radiusMultiplier = 1f;
        public float lifetimeSeconds = -1f;
        public int sortingOffset = 12;
        public float tintWhiteBlend = 0.18f;
        public float alpha = 0.9f;
        public float rotationDegrees = -12f;
    }

    [CreateAssetMenu(menuName = "Game/Combat World VFX Profile")]
    public class CombatWorldVfxProfileSO : ScriptableObject
    {
        public Sprite attackEffectSprite;
        public Sprite hitEffectSprite;
        public Sprite shieldEffectSprite;
        public Sprite thornShieldEffectSprite;
        public Sprite magicCircleEffectSprite;
        public Sprite flameEffectSprite;
        public Sprite chainAttackEffectSprite;
        public Sprite boundChainsEffectSprite;
        public GameObject attackEffectPrefab;
        public GameObject hitEffectPrefab;
        public GameObject shieldEffectPrefab;
        public GameObject thornShieldEffectPrefab;
        public GameObject magicCircleEffectPrefab;
        public GameObject flameEffectPrefab;
        public GameObject darkChainLaunchPrefab;
        public GameObject chainAttackEffectPrefab;
        public GameObject boundChainsEffectPrefab;
        public GameObject supportBuffVisualEffectPrefab;
        public ParticleSystem defaultSkillParticlePrefab;
        public ParticleSystem swirlSkillParticlePrefab;
        public SkillVfxParticlePrefabBinding[] particlePrefabBindings = System.Array.Empty<SkillVfxParticlePrefabBinding>();
        public SkillVfxDesignTimeBinding[] designTimeBindings = System.Array.Empty<SkillVfxDesignTimeBinding>();

        public CombatParticleEffectBinding shieldImpactEffect = new()
        {
            objectName = "ShieldImpactParticles",
            particleColor = new Color(0.62f, 0.92f, 1f, 0.96f),
            lifetimeSeconds = 0.8f,
            burstCount = 22,
            startSpeed = 0.78f,
            startSize = 0.22f,
        };

        public CombatParticleEffectBinding fearDebuffCastEffect = new()
        {
            objectName = "FearDebuffCastParticles",
            particleColor = new Color(0.75f, 0.05f, 0.16f, 0.95f),
            lifetimeSeconds = 0.9f,
            burstCount = 28,
            startSpeed = 0.62f,
            startSize = 0.28f,
            swirl = true,
        };

        public CombatParticleEffectBinding darknessDebuffCastEffect = new()
        {
            objectName = "DarknessDebuffCastParticles",
            particleColor = new Color(0.24f, 0.10f, 0.48f, 0.95f),
            lifetimeSeconds = 0.9f,
            burstCount = 28,
            startSpeed = 0.62f,
            startSize = 0.28f,
            swirl = true,
        };

        public CombatParticleEffectBinding ResolveDebuffCastEffect(DebuffType debuffType)
        {
            return debuffType switch
            {
                DebuffType.Fear => fearDebuffCastEffect,
                DebuffType.Darkness => darknessDebuffCastEffect,
                _ => null,
            };
        }

        public ParticleSystem ResolveParticlePrefab(string objectName, bool swirl)
        {
            if (!string.IsNullOrWhiteSpace(objectName) && particlePrefabBindings != null)
            {
                foreach (var binding in particlePrefabBindings)
                {
                    if (binding != null &&
                        binding.prefab != null &&
                        string.Equals(binding.objectName, objectName, System.StringComparison.Ordinal))
                    {
                        return binding.prefab;
                    }
                }
            }

            return swirl && swirlSkillParticlePrefab != null
                ? swirlSkillParticlePrefab
                : defaultSkillParticlePrefab;
        }

        public SkillVfxDesignTimeBinding ResolveDesignTimeBinding(SkillVfxFamily family)
        {
            if (family == SkillVfxFamily.None || designTimeBindings == null)
            {
                return null;
            }

            foreach (var binding in designTimeBindings)
            {
                if (binding != null && binding.family == family)
                {
                    return binding;
                }
            }

            return null;
        }
    }
}
