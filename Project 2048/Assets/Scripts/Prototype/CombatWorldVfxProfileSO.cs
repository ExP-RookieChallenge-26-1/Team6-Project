using Project2048.Enemy;
using Project2048.Presentation;
using UnityEngine;

namespace Project2048.Prototype
{
    [CreateAssetMenu(menuName = "Game/Combat World VFX Profile")]
    public class CombatWorldVfxProfileSO : ScriptableObject
    {
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
    }
}
