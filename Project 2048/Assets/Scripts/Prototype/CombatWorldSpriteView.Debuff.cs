using System.Collections;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Presentation;
using UnityEngine;

namespace Project2048.Prototype
{
    public partial class CombatWorldSpriteView
    {
        public const float DebuffCastParticleLifetimeSeconds = 0.9f;
        public const float DebuffTargetParticleDelaySeconds = DebuffCastParticleLifetimeSeconds;

        private const int DebuffCastParticleCount = 28;

        private int lastPlayedEnemyDebuffVfxSequence;
        private Material runtimeFearDebuffParticleMaterial;
        private Material runtimeDarknessDebuffParticleMaterial;

        private void PlayEnemyDebuffCastEffectIfNeeded(CombatVfxCue cue)
        {
            if (cue == null ||
                cue.Sequence <= 0 ||
                cue.Sequence == lastPlayedEnemyDebuffVfxSequence ||
                cue.DebuffType == DebuffType.None)
            {
                return;
            }

            lastPlayedEnemyDebuffVfxSequence = cue.Sequence;
            var enemyData = ResolveCurrentEnemyData();
            var effect = enemyData?.FindActionEffect(ResolveDebuffActionId(cue.DebuffType));
            PlayCombatantActionEffect(
                effect,
                enemyRenderer != null ? enemyRenderer.transform : transform,
                enemyAnimator);
            if (effect?.sfxClip == null)
            {
                PlayCombatantActionAudioEffect(enemyData?.FindActionEffect(CombatActionIds.Attack));
            }

            SpawnDebuffCastParticles(
                cue.DebuffType,
                enemyRenderer != null ? enemyRenderer.transform : transform);
            PlayDebuffTargetEffectAfterCast(cue.DebuffType, ResolveDebuffParticleLifetimeSeconds(cue.DebuffType));
        }

        private void PlayDebuffTargetEffectAfterCast(DebuffType debuffType, float delaySeconds)
        {
            var target = playerRenderer != null ? playerRenderer.transform : transform;
            if (!isActiveAndEnabled)
            {
                SpawnDebuffCastParticles(debuffType, target);
                return;
            }

            StartCoroutine(SpawnDebuffTargetParticlesAfterDelay(debuffType, target, delaySeconds));
        }

        private IEnumerator SpawnDebuffTargetParticlesAfterDelay(DebuffType debuffType, Transform target, float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, delaySeconds));
            SpawnDebuffCastParticles(debuffType, target != null ? target : transform);
        }

        private void SpawnDebuffCastParticles(DebuffType debuffType, Transform anchor)
        {
            var effect = ResolveDebuffParticleEffect(debuffType);
            var material = effect?.particleMaterial != null
                ? effect.particleMaterial
                : ResolveDebuffParticleMaterial(debuffType);
            var color = material != null ? Color.white : ResolveDebuffParticleColor(debuffType);
            SpawnParticleBurst(
                effect?.particlePrefab != null ? effect.particlePrefab : debuffCastParticlePrefab,
                anchor,
                $"{debuffType}DebuffCastParticles",
                color,
                material,
                effect != null ? effect.EffectiveLifetimeSeconds : DebuffCastParticleLifetimeSeconds,
                effect != null ? effect.EffectiveBurstCount : DebuffCastParticleCount,
                effect != null ? effect.EffectiveStartSpeed : 0.62f,
                effect != null ? effect.EffectiveStartSize : 0.28f,
                swirl: true);
        }

        private Color ResolveDebuffParticleColor(DebuffType debuffType)
        {
            return debuffType switch
            {
                DebuffType.Fear => fearDebuffParticleColor,
                DebuffType.Darkness => darknessDebuffParticleColor,
                _ => shieldImpactParticleColor,
            };
        }

        private CombatParticleEffectBinding ResolveDebuffParticleEffect(DebuffType debuffType)
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.ResolveDebuffCastEffect(debuffType) : null;
        }

        private float ResolveDebuffParticleLifetimeSeconds(DebuffType debuffType)
        {
            return ResolveDebuffParticleEffect(debuffType)?.EffectiveLifetimeSeconds ?? DebuffTargetParticleDelaySeconds;
        }

        private Material ResolveDebuffParticleMaterial(DebuffType debuffType)
        {
            return debuffType switch
            {
                DebuffType.Fear => fearDebuffParticleMaterial != null
                    ? fearDebuffParticleMaterial
                    : runtimeFearDebuffParticleMaterial ??= CreateParticleMaterial(
                        "FearDebuffParticleMaterial",
                        fearDebuffParticleColor),
                DebuffType.Darkness => darknessDebuffParticleMaterial != null
                    ? darknessDebuffParticleMaterial
                    : runtimeDarknessDebuffParticleMaterial ??= CreateParticleMaterial(
                        "DarknessDebuffParticleMaterial",
                        darknessDebuffParticleColor),
                _ => ResolveShieldImpactParticleMaterial(),
            };
        }

        private static string ResolveDebuffActionId(DebuffType debuffType)
        {
            return debuffType switch
            {
                DebuffType.Fear => CombatActionIds.DebuffFear,
                DebuffType.Darkness => CombatActionIds.DebuffDarkness,
                _ => null,
            };
        }
    }
}
