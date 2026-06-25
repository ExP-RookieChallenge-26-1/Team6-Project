using System.Collections;
using System.Linq;
using Project2048.Presentation;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.Playables;

namespace Project2048.Prototype
{
    public partial class CombatWorldSpriteView
    {
        public const float EnemyDeathFadeDurationSeconds = 0.6f;
        public const float EnemyAppearIntroDurationSeconds = 0.45f;
        public const float EnemyAttackLungeDurationSeconds = 0.32f;
        public const float EnemyAppearWorldShakeDurationSeconds = 1.5f;

        private const float EnemyAppearIntroRightOffset = 2.25f;
        private const float EnemyAppearIntroJumpHeight = 0.7f;
        private const float EnemyAppearIntroScalePop = 0.08f;
        private const float EnemyAttackLungeDistance = 0.72f;
        private const float EnemyAttackLungeImpactTime = 0.45f;
        private const float EnemyAttackLungeScalePop = 0.05f;
        private const float EnemyAppearWorldShakeMagnitude = 0.13f;

        private Coroutine enemyDeathFadeCoroutine;
        private Coroutine enemyDeathFadeDelayCoroutine;
        private Coroutine enemyAppearIntroCoroutine;
        private Coroutine enemyAttackLungeCoroutine;
        private Vector3 enemyRendererRestLocalPosition;
        private Vector3 enemyRendererRestLocalScale = Vector3.one;
        private bool hasEnemyRendererRestTransform;
        private bool lastEnemyWasDead;
        private float delayEnemyDeathFadeUntilRealtime;
        private Coroutine enemyAnimationReturnToIdleCoroutine;
        private PlayableGraph enemyAnimationGraph;
        private AnimationClip currentEnemyDirectAnimationClip;
        private bool currentEnemyDirectAnimationLoops;

        private void PlayEnemyAppearIntro(CombatEffectBinding effect)
        {
            if (enemyRenderer == null)
            {
                PlayCombatantActionEffect(effect, transform, enemyAnimator);
                PlayEnemyAppearWorldShake();
                return;
            }

            CacheEnemyRendererRestTransform();
            ClearEnemyAttackLunge(restoreTransform: true);
            ClearEnemyAppearIntro(restoreTransform: false);

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                RestoreEnemyRendererTransform();
                PlayCombatantActionEffect(effect, enemyRenderer.transform, enemyAnimator);
                return;
            }

            enemyAppearIntroCoroutine = StartCoroutine(EnemyAppearIntroRoutine(effect));
        }

        private void PlayEnemyAttackLunge(CombatEffectBinding effect)
        {
            if (enemyRenderer == null)
            {
                PlayEnemyAttackImpactEffects(effect);
                return;
            }

            CacheEnemyRendererRestTransform();
            ClearEnemyAppearIntro(restoreTransform: true);
            ClearEnemyAttackLunge(restoreTransform: true);

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                RestoreEnemyRendererTransform();
                PlayEnemyAttackImpactEffects(effect);
                return;
            }

            enemyAttackLungeCoroutine = StartCoroutine(EnemyAttackLungeRoutine(effect));
        }

        private void PlayEnemyIdleAnimationIfNeeded()
        {
            if (showingRewardPresenter || (snapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false))
            {
                return;
            }

            var idleClip = ResolveCurrentEnemyData()?.idleAnimation;
            if (idleClip == null)
            {
                if (currentEnemyDirectAnimationLoops)
                {
                    ClearEnemyDirectAnimation(restoreCurrentSprite: true);
                }

                return;
            }

            if (currentEnemyDirectAnimationLoops &&
                currentEnemyDirectAnimationClip == idleClip &&
                enemyAnimationGraph.IsValid())
            {
                return;
            }

            PlayEnemyAnimationClip(idleClip, loop: true);
        }

        private float PlayEnemyOneShotAnimation(AnimationClip clip, bool returnToIdle)
        {
            if (clip == null)
            {
                return 0f;
            }

            if (!PlayEnemyAnimationClip(clip, loop: false))
            {
                return 0f;
            }

            var duration = Mathf.Max(0f, clip.length);
            if (returnToIdle && Application.isPlaying && isActiveAndEnabled)
            {
                enemyAnimationReturnToIdleCoroutine = StartCoroutine(ReturnToEnemyIdleAnimationAfterDelay(duration));
            }

            return duration;
        }

        private bool PlayEnemyAnimationClip(AnimationClip clip, bool loop)
        {
            var animator = ResolveEnemyAnimatorForDirectAnimation();
            if (clip == null || animator == null)
            {
                return false;
            }

            StopEnemyAnimationReturnToIdle();
            DestroyEnemyAnimationGraph();

            enemyAnimationGraph = PlayableGraph.Create("EnemyDirectAnimation");
            enemyAnimationGraph.SetTimeUpdateMode(Application.isPlaying
                ? DirectorUpdateMode.GameTime
                : DirectorUpdateMode.Manual);

            var clipPlayable = AnimationClipPlayable.Create(enemyAnimationGraph, clip);
            clipPlayable.SetTime(0d);
            clipPlayable.SetSpeed(1d);

            var output = AnimationPlayableOutput.Create(enemyAnimationGraph, "EnemyDirectAnimation", animator);
            output.SetSourcePlayable(clipPlayable);

            currentEnemyDirectAnimationClip = clip;
            currentEnemyDirectAnimationLoops = loop;
            enemyAnimationGraph.Play();
            if (!Application.isPlaying)
            {
                enemyAnimationGraph.Evaluate(0f);
            }

            return true;
        }

        private Animator ResolveEnemyAnimatorForDirectAnimation()
        {
            ResolveMissingReferences();
            if (enemyAnimator != null)
            {
                return enemyAnimator;
            }

            if (enemyRenderer == null)
            {
                return null;
            }

            enemyAnimator = enemyRenderer.GetComponent<Animator>();
            if (enemyAnimator == null)
            {
                enemyAnimator = enemyRenderer.gameObject.AddComponent<Animator>();
            }

            return enemyAnimator;
        }

        private IEnumerator ReturnToEnemyIdleAnimationAfterDelay(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, delaySeconds));
            enemyAnimationReturnToIdleCoroutine = null;
            PlayEnemyIdleAnimationIfNeeded();
            if (!currentEnemyDirectAnimationLoops)
            {
                ClearEnemyDirectAnimation(restoreCurrentSprite: true);
            }
        }

        private void ClearEnemyDirectAnimation(bool restoreCurrentSprite = false)
        {
            StopEnemyAnimationReturnToIdle();
            DestroyEnemyAnimationGraph();
            currentEnemyDirectAnimationClip = null;
            currentEnemyDirectAnimationLoops = false;

            if (restoreCurrentSprite &&
                enemyRenderer != null &&
                activeEnemyBattleActor == null &&
                !showingRewardPresenter)
            {
                enemyRenderer.sprite = ResolveEnemySprite(snapshot);
            }
        }

        private void StopEnemyAnimationReturnToIdle()
        {
            if (enemyAnimationReturnToIdleCoroutine == null)
            {
                return;
            }

            StopCoroutine(enemyAnimationReturnToIdleCoroutine);
            enemyAnimationReturnToIdleCoroutine = null;
        }

        private void DestroyEnemyAnimationGraph()
        {
            if (!enemyAnimationGraph.IsValid())
            {
                return;
            }

            enemyAnimationGraph.Destroy();
        }

        private void PlayEnemyAttackImpactEffects(CombatEffectBinding effect)
        {
            PlayEnemyClawSlashEffect();
            PlayCombatantActionEffect(
                effect,
                enemyRenderer != null ? enemyRenderer.transform : transform,
                enemyAnimator);
        }

        private void PlayEnemyClawSlashEffect()
        {
            if (!playEnemyClawSlashEffect)
            {
                return;
            }

            var playerAnchor = ResolvePlayerAnchor();
            var sortingReference = playerRenderer != null ? playerRenderer : enemyRenderer;
            var parent = playerAnchor != null
                ? playerAnchor
                : sortingReference != null
                    ? sortingReference.transform
                    : transform;
            var localOffset = ResolveVisualCenterLocalOffset(parent, enemyClawSlashLocalOffset);
            var attackArt = SpawnAttackArtSpriteLayer(
                parent,
                "EnemyAttackArt",
                Color.white,
                AttackArtBaseRadius * 1.08f * AttackEffectArtSizeMultiplier,
                AttackArtLifetimeSeconds,
                localOffset,
                sortingOffset: 12);
            if (attackArt != null)
            {
                attackArt.transform.localRotation = Quaternion.Euler(enemyClawSlashLocalEulerAngles);
                attackArt.transform.localScale *= Mathf.Max(0.01f, enemyClawSlashScale);
                return;
            }

            var slash = enemyClawSlashEffectPrefab != null
                ? Instantiate(enemyClawSlashEffectPrefab, parent)
                : CreateRuntimeEnemyClawSlashEffect(parent);
            if (slash == null)
            {
                return;
            }

            slash.gameObject.name = "EnemyClawSlash2D";
            slash.transform.localPosition = localOffset;
            slash.transform.localRotation = Quaternion.Euler(enemyClawSlashLocalEulerAngles);
            slash.transform.localScale = Vector3.one * Mathf.Max(0.01f, enemyClawSlashScale);
            slash.Play(
                ResolveEnemyAttackDirectionSign(),
                sortingReference,
                previewComplete: !Application.isPlaying || !isActiveAndEnabled);
        }

        private static CombatClawSlash2DEffect CreateRuntimeEnemyClawSlashEffect(Transform parent)
        {
            var slashObject = new GameObject("EnemyClawSlash2D");
            slashObject.transform.SetParent(parent, false);
            return slashObject.AddComponent<CombatClawSlash2DEffect>();
        }

        private float ResolveEnemyAttackDirectionSign()
        {
            var playerAnchor = ResolvePlayerAnchor();
            if (enemyRenderer == null || playerAnchor == null)
            {
                return -1f;
            }

            var deltaX = ResolveAnchorVisualCenterWorldPosition(playerAnchor).x -
                ResolveAnchorVisualCenterWorldPosition(enemyRenderer.transform).x;
            return deltaX >= 0f ? 1f : -1f;
        }

        private IEnumerator EnemyAppearIntroRoutine(CombatEffectBinding effect)
        {
            var targetPosition = enemyRendererRestLocalPosition;
            var baseScale = enemyRendererRestLocalScale;
            var startPosition = targetPosition + (Vector3.right * EnemyAppearIntroRightOffset);
            var startTime = Time.realtimeSinceStartup;

            enemyRenderer.transform.localPosition = startPosition;
            enemyRenderer.transform.localScale = baseScale * (1f - EnemyAppearIntroScalePop);
            SetEnemyRendererAlpha(1f);

            while (true)
            {
                var elapsed = Time.realtimeSinceStartup - startTime;
                var t = Mathf.Clamp01(elapsed / EnemyAppearIntroDurationSeconds);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                var position = Vector3.Lerp(startPosition, targetPosition, eased);
                position.y += Mathf.Sin(t * Mathf.PI) * EnemyAppearIntroJumpHeight;
                enemyRenderer.transform.localPosition = position;

                var scalePop = 1f + Mathf.Sin(t * Mathf.PI) * EnemyAppearIntroScalePop;
                enemyRenderer.transform.localScale = baseScale * scalePop;

                if (t >= 1f)
                {
                    break;
                }

                yield return null;
            }

            RestoreEnemyRendererTransform();
            enemyAppearIntroCoroutine = null;
            PlayCombatantActionEffect(effect, enemyRenderer.transform, enemyAnimator);
            PlayEnemyAppearWorldShake();
        }

        private IEnumerator EnemyAttackLungeRoutine(CombatEffectBinding effect)
        {
            var startPosition = enemyRendererRestLocalPosition;
            var targetPosition = ResolveEnemyAttackLungeTarget(startPosition);
            var baseScale = enemyRendererRestLocalScale;
            var startTime = Time.realtimeSinceStartup;
            var playedImpactEffect = false;

            while (true)
            {
                var elapsed = Time.realtimeSinceStartup - startTime;
                var t = Mathf.Clamp01(elapsed / EnemyAttackLungeDurationSeconds);
                if (!playedImpactEffect && t >= EnemyAttackLungeImpactTime)
                {
                    playedImpactEffect = true;
                    PlayEnemyAttackImpactEffects(effect);
                }

                Vector3 position;
                if (t < EnemyAttackLungeImpactTime)
                {
                    var attackT = Mathf.Clamp01(t / EnemyAttackLungeImpactTime);
                    var eased = 1f - Mathf.Pow(1f - attackT, 3f);
                    position = Vector3.Lerp(startPosition, targetPosition, eased);
                }
                else
                {
                    var recoverT = Mathf.Clamp01((t - EnemyAttackLungeImpactTime) / (1f - EnemyAttackLungeImpactTime));
                    var eased = Mathf.SmoothStep(0f, 1f, recoverT);
                    position = Vector3.Lerp(targetPosition, startPosition, eased);
                }

                enemyRenderer.transform.localPosition = position;
                var scalePop = 1f + Mathf.Sin(t * Mathf.PI) * EnemyAttackLungeScalePop;
                enemyRenderer.transform.localScale = baseScale * scalePop;

                if (t >= 1f)
                {
                    break;
                }

                yield return null;
            }

            RestoreEnemyRendererTransform();
            if (!playedImpactEffect)
            {
                PlayEnemyAttackImpactEffects(effect);
            }

            enemyAttackLungeCoroutine = null;
        }

        private void PlayEnemyDeathFadeIfNeeded(bool enemyJustDied, bool nextEnemyDead)
        {
            if ((enemyJustDied || nextEnemyDead) &&
                enemyRenderer != null &&
                enemyDeathFadeCoroutine == null &&
                enemyDeathFadeDelayCoroutine == null &&
                enemyRenderer.color.a > 0.001f)
            {
                var delaySeconds = Mathf.Max(0f, delayEnemyDeathFadeUntilRealtime - Time.realtimeSinceStartup);
                if (delaySeconds > 0f && isActiveAndEnabled)
                {
                    enemyDeathFadeDelayCoroutine = StartCoroutine(EnemyDeathFadeDelayRoutine(delaySeconds));
                }
                else
                {
                    PlayEnemyDeathFade();
                }

                return;
            }

            if (!nextEnemyDead && enemyRenderer != null)
            {
                ClearEnemyDeathFade();
                delayEnemyDeathFadeUntilRealtime = 0f;
                SetEnemyRendererAlpha(1f);
            }
        }

        private void PlayEnemyDeathFade()
        {
            if (enemyRenderer == null)
            {
                return;
            }

            ClearEnemyAppearIntro(restoreTransform: true);
            ClearEnemyAttackLunge(restoreTransform: true);
            ClearEnemyDeathFade();
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                SetEnemyRendererAlpha(0f);
                return;
            }

            enemyDeathFadeCoroutine = StartCoroutine(EnemyDeathFadeRoutine());
        }

        private IEnumerator EnemyDeathFadeDelayRoutine(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delaySeconds));
            enemyDeathFadeDelayCoroutine = null;
            delayEnemyDeathFadeUntilRealtime = 0f;
            PlayEnemyDeathFade();
        }

        private IEnumerator EnemyDeathFadeRoutine()
        {
            var fromAlpha = enemyRenderer != null ? Mathf.Clamp01(enemyRenderer.color.a) : 1f;
            var startTime = Time.realtimeSinceStartup;

            while (true)
            {
                var elapsed = Time.realtimeSinceStartup - startTime;
                var t = Mathf.Clamp01(elapsed / EnemyDeathFadeDurationSeconds);
                SetEnemyRendererAlpha(Mathf.Lerp(fromAlpha, 0f, t));

                if (t >= 1f)
                {
                    break;
                }

                yield return null;
            }

            SetEnemyRendererAlpha(0f);
            enemyDeathFadeCoroutine = null;
        }

        private void ClearEnemyDeathFade()
        {
            if (enemyDeathFadeDelayCoroutine != null)
            {
                StopCoroutine(enemyDeathFadeDelayCoroutine);
                enemyDeathFadeDelayCoroutine = null;
            }

            if (enemyDeathFadeCoroutine != null)
            {
                StopCoroutine(enemyDeathFadeCoroutine);
                enemyDeathFadeCoroutine = null;
            }
        }

        private void ClearEnemyAppearIntro(bool restoreTransform = false)
        {
            if (enemyAppearIntroCoroutine != null)
            {
                StopCoroutine(enemyAppearIntroCoroutine);
                enemyAppearIntroCoroutine = null;
            }

            if (restoreTransform)
            {
                RestoreEnemyRendererTransform();
            }
        }

        private void ClearEnemyAttackLunge(bool restoreTransform = false)
        {
            if (enemyAttackLungeCoroutine != null)
            {
                StopCoroutine(enemyAttackLungeCoroutine);
                enemyAttackLungeCoroutine = null;
            }

            if (restoreTransform)
            {
                RestoreEnemyRendererTransform();
            }
        }

        private void PlayEnemyAppearWorldShake()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            var shake = ResolveWorldShake();
            if (shake == null)
            {
                return;
            }

            shake.Shake(EnemyAppearWorldShakeDurationSeconds, EnemyAppearWorldShakeMagnitude);
        }

        private WorldShake ResolveWorldShake()
        {
            if (worldShake != null && IsUsableShakeTarget(worldShake.transform))
            {
                worldShake.ResetRestPosition();
                return worldShake;
            }

            var root = ResolveForegroundShakeRoot();
            if (root == null || !IsUsableShakeTarget(root))
            {
                return null;
            }

            worldShake = root.GetComponent<WorldShake>();
            if (worldShake == null)
            {
                worldShake = root.gameObject.AddComponent<WorldShake>();
            }

            worldShake.ResetRestPosition();
            return worldShake;
        }

        private Transform ResolveForegroundShakeRoot()
        {
            if (foregroundShakeRoot != null)
            {
                return IsUsableShakeTarget(foregroundShakeRoot) ? foregroundShakeRoot : null;
            }

            if (playerRenderer == null && enemyRenderer == null)
            {
                return null;
            }

            var rootObject = new GameObject("ForegroundShakeRoot");
            foregroundShakeRoot = rootObject.transform;
            foregroundShakeRoot.SetParent(transform, false);
            foregroundShakeRoot.localPosition = Vector3.zero;
            foregroundShakeRoot.localRotation = Quaternion.identity;
            foregroundShakeRoot.localScale = Vector3.one;

            var reparentedCount = 0;
            reparentedCount += ReparentRendererForWorldShake(playerRenderer) ? 1 : 0;
            reparentedCount += ReparentRendererForWorldShake(enemyRenderer) ? 1 : 0;
            if (reparentedCount <= 0)
            {
                DestroyGeneratedShakeRoot(rootObject);
                foregroundShakeRoot = null;
                return null;
            }

            hasEnemyRendererRestTransform = false;
            return foregroundShakeRoot;
        }

        private bool ReparentRendererForWorldShake(SpriteRenderer renderer)
        {
            if (renderer == null || foregroundShakeRoot == null)
            {
                return false;
            }

            var rendererTransform = renderer.transform;
            if (rendererTransform == foregroundShakeRoot || rendererTransform.IsChildOf(foregroundShakeRoot))
            {
                return false;
            }

            if (!CanAutoReparentForWorldShake(rendererTransform))
            {
                return false;
            }

            rendererTransform.SetParent(foregroundShakeRoot, true);
            return true;
        }

        private void ClearWorldShake()
        {
            if (worldShake != null)
            {
                worldShake.StopShake(restorePosition: true);
            }
        }

        private bool CanAutoReparentForWorldShake(Transform target)
        {
            if (target == null || target == transform)
            {
                return false;
            }

            if (target.parent != null && target.parent != transform)
            {
                return false;
            }

            return IsUsableShakeTarget(target);
        }

        private bool IsUsableShakeTarget(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            if (backgroundRenderer != null && backgroundRenderer.transform.IsChildOf(target))
            {
                return false;
            }

            return target.GetComponentInChildren<Rigidbody2D>(includeInactive: true) == null &&
                target.GetComponentInChildren<Collider2D>(includeInactive: true) == null &&
                target.GetComponentInChildren<Camera>(includeInactive: true) == null &&
                target.GetComponentInChildren<AudioListener>(includeInactive: true) == null &&
                target.GetComponentInChildren<Canvas>(includeInactive: true) == null &&
                target.GetComponentInChildren<EventSystem>(includeInactive: true) == null;
        }

        private static void DestroyGeneratedShakeRoot(GameObject rootObject)
        {
            if (rootObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(rootObject);
            }
            else
            {
                DestroyImmediate(rootObject);
            }
        }

        private Vector3 ResolveEnemyAttackLungeTarget(Vector3 restLocalPosition)
        {
            if (enemyRenderer == null)
            {
                return restLocalPosition;
            }

            var enemyTransform = enemyRenderer.transform;
            var enemyWorldPosition = ResolveAnchorVisualCenterWorldPosition(enemyTransform);
            var playerAnchor = ResolvePlayerAnchor();
            var targetWorldPosition = playerAnchor != null
                ? ResolveAnchorVisualCenterWorldPosition(playerAnchor)
                : enemyWorldPosition + Vector3.left;
            var direction = targetWorldPosition - enemyWorldPosition;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.left;
            }

            var lungeWorldPosition = enemyWorldPosition + direction.normalized * EnemyAttackLungeDistance;
            return enemyTransform.parent != null
                ? enemyTransform.parent.InverseTransformPoint(lungeWorldPosition)
                : lungeWorldPosition;
        }

        private void CacheEnemyRendererRestTransform()
        {
            if (enemyRenderer == null || hasEnemyRendererRestTransform)
            {
                return;
            }

            enemyRendererRestLocalPosition = enemyRenderer.transform.localPosition;
            enemyRendererRestLocalScale = enemyRenderer.transform.localScale;
            hasEnemyRendererRestTransform = true;
        }

        private void RestoreEnemyRendererTransform()
        {
            if (enemyRenderer == null || !hasEnemyRendererRestTransform)
            {
                return;
            }

            enemyRenderer.transform.localPosition = enemyRendererRestLocalPosition;
            enemyRenderer.transform.localScale = enemyRendererRestLocalScale;
        }

        private void SetEnemyRendererAlpha(float alpha)
        {
            SetEnemyBattleActorAlpha(alpha);
            if (enemyRenderer == null)
            {
                return;
            }

            var color = enemyRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            enemyRenderer.color = color;
        }

        private void SetEnemyBattleActorAlpha(float alpha)
        {
            if (activeEnemyBattleActor == null)
            {
                return;
            }

            var renderers = activeEnemyBattleActor.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                var color = renderer.color;
                color.a = Mathf.Clamp01(alpha);
                renderer.color = color;
            }
        }
    }
}
