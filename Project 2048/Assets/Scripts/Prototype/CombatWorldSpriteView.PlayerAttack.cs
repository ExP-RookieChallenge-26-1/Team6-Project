using System.Collections;
using Project2048.Presentation;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Prototype
{
    public partial class CombatWorldSpriteView
    {
        private const float PlayerCloseRangeAttackAdvanceSeconds = 0.12f;
        private const float PlayerCloseRangeAttackWindupSeconds = 0.035f;
        private const float PlayerCloseRangeAttackHoldSeconds = 0.05f;
        private const float PlayerCloseRangeAttackRecoverSeconds = 0.14f;
        private const float PlayerCloseRangeAttackStopDistance = 0.62f;
        private const float PlayerCloseRangeAttackMaxLungeDistance = 1.45f;
        private const float PlayerCloseRangeAttackScalePop = 0.035f;
        private const float CloseRangeImpactTowardSourceOffset = 0.2f;

        private Coroutine playerCloseRangeAttackCoroutine;
        private Transform playerCloseRangeAttackTransform;
        private Vector3 playerCloseRangeAttackRestLocalPosition;
        private Vector3 playerCloseRangeAttackRestLocalScale = Vector3.one;
        private bool hasPlayerCloseRangeAttackRestTransform;

        private static float PlayerCloseRangeAttackTotalSeconds =>
            PlayerCloseRangeAttackAdvanceSeconds +
            PlayerCloseRangeAttackWindupSeconds +
            PlayerCloseRangeAttackHoldSeconds +
            PlayerCloseRangeAttackRecoverSeconds;

        private bool TryPlayCloseRangePlayerAttackSkillEffect(
            SkillSO skill,
            CombatEffectBinding effect,
            Transform sourceAnchor,
            Transform targetAnchor,
            out float lifetimeSeconds)
        {
            lifetimeSeconds = 0f;
            var family = ResolveSkillVfxFamily(skill);
            if (!UsesCloseRangePlayerLunge(family) || sourceAnchor == null || targetAnchor == null)
            {
                return false;
            }

            lifetimeSeconds = PlayerCloseRangeAttackTotalSeconds + ResolveCloseRangeImpactLifetimeSeconds(family);
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                PlayPlayerAttackAnimation(ResolvePlayerAttackAnimationSpeedMultiplier(skill));
                PlayCloseRangePlayerAttackImpactEffects(skill, effect, sourceAnchor, targetAnchor);
                return true;
            }

            ClearPlayerCloseRangeAttackLunge(restoreTransform: true);
            CachePlayerCloseRangeAttackRestTransform(sourceAnchor);
            playerCloseRangeAttackCoroutine = StartCoroutine(PlayerCloseRangeAttackRoutine(
                skill,
                effect,
                sourceAnchor,
                targetAnchor));
            return true;
        }

        private IEnumerator PlayerCloseRangeAttackRoutine(
            SkillSO skill,
            CombatEffectBinding effect,
            Transform sourceAnchor,
            Transform targetAnchor)
        {
            var actor = sourceAnchor;
            CachePlayerCloseRangeAttackRestTransform(actor);
            var startPosition = playerCloseRangeAttackRestLocalPosition;
            var baseScale = playerCloseRangeAttackRestLocalScale;
            var targetPosition = ResolvePlayerCloseRangeAttackTargetLocalPosition(actor, targetAnchor, startPosition);

            yield return MovePlayerCloseRangeAttackActorRoutine(
                actor,
                startPosition,
                targetPosition,
                baseScale,
                PlayerCloseRangeAttackAdvanceSeconds,
                easeOut: true);

            PlayPlayerAttackAnimation(ResolvePlayerAttackAnimationSpeedMultiplier(skill));
            if (PlayerCloseRangeAttackWindupSeconds > 0f)
            {
                yield return new WaitForSeconds(PlayerCloseRangeAttackWindupSeconds);
            }

            PlayCloseRangePlayerAttackImpactEffects(skill, effect, sourceAnchor, targetAnchor);

            if (PlayerCloseRangeAttackHoldSeconds > 0f)
            {
                yield return new WaitForSeconds(PlayerCloseRangeAttackHoldSeconds);
            }

            yield return MovePlayerCloseRangeAttackActorRoutine(
                actor,
                targetPosition,
                startPosition,
                baseScale,
                PlayerCloseRangeAttackRecoverSeconds,
                easeOut: false);

            RestorePlayerCloseRangeAttackTransform();
            playerCloseRangeAttackCoroutine = null;
        }

        private IEnumerator MovePlayerCloseRangeAttackActorRoutine(
            Transform actor,
            Vector3 from,
            Vector3 to,
            Vector3 baseScale,
            float durationSeconds,
            bool easeOut)
        {
            if (actor == null)
            {
                yield break;
            }

            var startTime = Time.realtimeSinceStartup;
            while (true)
            {
                var elapsed = Time.realtimeSinceStartup - startTime;
                var t = durationSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / durationSeconds);
                var eased = easeOut
                    ? 1f - Mathf.Pow(1f - t, 3f)
                    : Mathf.SmoothStep(0f, 1f, t);
                actor.localPosition = Vector3.Lerp(from, to, eased);
                actor.localScale = baseScale * (1f + Mathf.Sin(t * Mathf.PI) * PlayerCloseRangeAttackScalePop);

                if (t >= 1f)
                {
                    break;
                }

                yield return null;
            }
        }

        private void PlayCloseRangePlayerAttackImpactEffects(
            SkillSO skill,
            CombatEffectBinding effect,
            Transform sourceAnchor,
            Transform targetAnchor)
        {
            PlayCombatantActionAudioEffect(effect);

            switch (ResolveSkillVfxFamily(skill))
            {
                case SkillVfxFamily.SpikedBurst:
                    PlaySpecializedSkillArtLayer(skill, targetAnchor, sourceAnchor);
                    PlaySpikedBurstSkillEffect(skill, targetAnchor, sourceAnchor);
                    PlayCombatantActionParticleEffect(effect, targetAnchor);
                    break;
                case SkillVfxFamily.BloodFountainSlash:
                    PlaySpecializedSkillArtLayer(skill, targetAnchor, sourceAnchor);
                    PlayBloodFountainSlashSkillEffect(skill, sourceAnchor, targetAnchor);
                    PlayCombatantActionParticleEffect(effect, targetAnchor);
                    break;
                case SkillVfxFamily.SlashArc:
                case SkillVfxFamily.ImpactBurst:
                    PlayCloseRangeReusableAttackImpactArt(skill, sourceAnchor, targetAnchor);
                    SpawnCloseRangeReusableAttackImpactParticles(skill, sourceAnchor, targetAnchor);
                    break;
            }
        }

        private void PlayCloseRangeReusableAttackImpactArt(
            SkillSO skill,
            Transform sourceAnchor,
            Transform targetAnchor)
        {
            var family = ResolveSkillVfxFamily(skill);
            if (skill == null || (family != SkillVfxFamily.SlashArc && family != SkillVfxFamily.ImpactBurst))
            {
                return;
            }

            var scale = Mathf.Max(0.01f, skill.vfxScale);
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);
            var fallbackSprite = family == SkillVfxFamily.ImpactBurst
                ? ResolveHitEffectSprite()
                : ResolveAttackEffectSprite();
            var explicitSprite = family == SkillVfxFamily.ImpactBurst ? hitEffectSprite : attackEffectSprite;
            var fallbackPrefab = family == SkillVfxFamily.ImpactBurst
                ? ResolveHitEffectPrefab()
                : ResolveAttackEffectPrefab();
            var localOffset = ResolveCloseRangeImpactLocalOffset(
                targetAnchor,
                sourceAnchor,
                ResolveDesignTimeLocalOffset(
                    tuning,
                    package,
                    designTimeBinding,
                    new Vector3(0f, 0.16f, 0f)));

            var art = SpawnAttackArtSpriteLayer(
                targetAnchor,
                family == SkillVfxFamily.ImpactBurst ? "HitImpactEffectArt" : "AttackEffectArt",
                ResolveDesignTimeArtColor(
                    ResolveReusableSkillParticleColor(skill),
                    tuning,
                    package,
                    designTimeBinding,
                    0.18f,
                    0.9f),
                AttackArtBaseRadius *
                    Mathf.Clamp(Mathf.Sqrt(scale), 0.7f, 1.42f) *
                    ResolveDesignTimeRadiusMultiplier(
                        tuning,
                        package,
                        designTimeBinding,
                        family == SkillVfxFamily.ImpactBurst
                            ? 1.08f * HitEffectArtSizeMultiplier
                            : AttackEffectArtSizeMultiplier),
                ResolveDesignTimeLifetime(tuning, package, designTimeBinding, AttackArtLifetimeSeconds),
                localOffset,
                ResolveDesignTimeSortingOffset(tuning, package, designTimeBinding, 12),
                spriteOverride: ResolveDesignTimeSprite(
                    tuning,
                    package,
                    designTimeBinding,
                    null,
                    explicitSprite != null ? explicitSprite : fallbackSprite),
                prefabOverride: ResolveDesignTimePrefab(tuning, package, designTimeBinding, fallbackPrefab));
            if (art == null)
            {
                return;
            }

            var facingSign = ResolveAttackFacingSign(sourceAnchor, targetAnchor);
            art.transform.localRotation = Quaternion.Euler(
                0f,
                facingSign >= 0f ? 0f : 180f,
                ResolveDesignTimeRotationDegrees(tuning, package, designTimeBinding, -12f) * facingSign);
        }

        private void SpawnCloseRangeReusableAttackImpactParticles(
            SkillSO skill,
            Transform sourceAnchor,
            Transform targetAnchor)
        {
            var family = ResolveSkillVfxFamily(skill);
            if (skill == null || (family != SkillVfxFamily.SlashArc && family != SkillVfxFamily.ImpactBurst))
            {
                return;
            }

            ResolveReusableSkillParticleDefaults(
                family,
                out var lifetimeSeconds,
                out var burstCount,
                out var startSpeed,
                out var startSize,
                out _);

            var scale = Mathf.Max(0.01f, skill.vfxScale);
            var intensity = Mathf.Max(0.1f, skill.vfxIntensity);
            var facingSign = ResolveAttackFacingSign(sourceAnchor, targetAnchor);
            var color = ResolveSkillTintedColor(
                ResolveReusableSkillSecondaryParticleColor(skill),
                family == SkillVfxFamily.ImpactBurst
                    ? new Color(1f, 0.82f, 0.18f, 0.82f)
                    : new Color(1f, 1f, 1f, 0.74f),
                0.32f,
                0.58f);

            SpawnParticleBurst(
                null,
                targetAnchor,
                $"{family}CloseRangeImpactParticles",
                color,
                null,
                Mathf.Min(lifetimeSeconds, 0.42f),
                Mathf.RoundToInt(burstCount * intensity * 0.45f),
                startSpeed * Mathf.Sqrt(scale) * 0.62f,
                startSize * Mathf.Clamp(scale, 0.72f, 1.35f) * 0.58f,
                false,
                ResolveCloseRangeImpactLocalOffset(targetAnchor, sourceAnchor, new Vector3(0f, 0.16f, 0f)),
                particles => ConfigureCloseRangeImpactParticles(particles, scale, facingSign));
        }

        private void CachePlayerCloseRangeAttackRestTransform(Transform actor)
        {
            if (actor == null)
            {
                return;
            }

            if (hasPlayerCloseRangeAttackRestTransform && playerCloseRangeAttackTransform == actor)
            {
                return;
            }

            playerCloseRangeAttackTransform = actor;
            playerCloseRangeAttackRestLocalPosition = actor.localPosition;
            playerCloseRangeAttackRestLocalScale = actor.localScale;
            hasPlayerCloseRangeAttackRestTransform = true;
        }

        private void ClearPlayerCloseRangeAttackLunge(bool restoreTransform = false)
        {
            if (playerCloseRangeAttackCoroutine != null)
            {
                StopCoroutine(playerCloseRangeAttackCoroutine);
                playerCloseRangeAttackCoroutine = null;
            }

            if (restoreTransform)
            {
                RestorePlayerCloseRangeAttackTransform();
            }
        }

        private void RestorePlayerCloseRangeAttackTransform()
        {
            if (!hasPlayerCloseRangeAttackRestTransform || playerCloseRangeAttackTransform == null)
            {
                return;
            }

            playerCloseRangeAttackTransform.localPosition = playerCloseRangeAttackRestLocalPosition;
            playerCloseRangeAttackTransform.localScale = playerCloseRangeAttackRestLocalScale;
        }

        private static Vector3 ResolvePlayerCloseRangeAttackTargetLocalPosition(
            Transform actor,
            Transform targetAnchor,
            Vector3 fallbackLocalPosition)
        {
            if (actor == null || targetAnchor == null)
            {
                return fallbackLocalPosition;
            }

            var sourceCenter = ResolveAnchorVisualCenterWorldPosition(actor);
            var targetCenter = ResolveAnchorVisualCenterWorldPosition(targetAnchor);
            var direction = targetCenter - sourceCenter;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return fallbackLocalPosition;
            }

            var moveDistance = Mathf.Clamp(
                direction.magnitude - PlayerCloseRangeAttackStopDistance,
                0f,
                PlayerCloseRangeAttackMaxLungeDistance);
            var desiredWorldPosition = actor.position + direction.normalized * moveDistance;
            var desiredLocalPosition = actor.parent != null
                ? actor.parent.InverseTransformPoint(desiredWorldPosition)
                : desiredWorldPosition;
            desiredLocalPosition.z = fallbackLocalPosition.z;
            return desiredLocalPosition;
        }

        private static Vector3 ResolveCloseRangeImpactLocalOffset(
            Transform targetAnchor,
            Transform sourceAnchor,
            Vector3 localOffsetFromVisualCenter)
        {
            if (targetAnchor == null)
            {
                return localOffsetFromVisualCenter;
            }

            var targetCenter = ResolveAnchorVisualCenterWorldPosition(targetAnchor);
            var impactWorldPosition = targetCenter + targetAnchor.TransformVector(localOffsetFromVisualCenter);
            if (sourceAnchor != null)
            {
                var direction = targetCenter - ResolveAnchorVisualCenterWorldPosition(sourceAnchor);
                direction.z = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    impactWorldPosition -= direction.normalized * CloseRangeImpactTowardSourceOffset;
                }
            }

            return targetAnchor.InverseTransformPoint(impactWorldPosition);
        }

        private static bool UsesCloseRangePlayerLunge(SkillVfxFamily family)
        {
            return family == SkillVfxFamily.SlashArc ||
                family == SkillVfxFamily.ImpactBurst ||
                family == SkillVfxFamily.SpikedBurst ||
                family == SkillVfxFamily.BloodFountainSlash;
        }

        private static float ResolveCloseRangeImpactLifetimeSeconds(SkillVfxFamily family)
        {
            return family switch
            {
                SkillVfxFamily.SpikedBurst => HeavyStrikeSpikedBurstDurationSeconds,
                SkillVfxFamily.BloodFountainSlash => BloodFountainSlashDurationSeconds,
                SkillVfxFamily.ImpactBurst => 0.55f,
                SkillVfxFamily.SlashArc => 0.55f,
                _ => AttackArtLifetimeSeconds,
            };
        }

        private static void ConfigureCloseRangeImpactParticles(
            ParticleSystem particles,
            float scale,
            float facingSign)
        {
            if (particles == null)
            {
                return;
            }

            scale = Mathf.Max(0.01f, scale);
            var direction = Mathf.Sign(facingSign == 0f ? 1f : facingSign);
            var main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f * scale, 0.72f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f * scale, 0.075f * scale);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.18f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f * scale;
            shape.radiusThickness = 0.45f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = direction > 0f
                ? new ParticleSystem.MinMaxCurve(0.08f * scale, 0.62f * scale)
                : new ParticleSystem.MinMaxCurve(-0.62f * scale, -0.08f * scale);
            velocity.y = new ParticleSystem.MinMaxCurve(0.06f * scale, 0.42f * scale);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(0.22f, 1f),
                new Keyframe(1f, 0f)));
        }
    }
}
