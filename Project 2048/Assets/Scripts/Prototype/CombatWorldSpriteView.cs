using System.Collections;
using System.Linq;
using Project2048.Audio;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Presentation;
using Project2048.Skills;
using UnityEngine;
using UnityEngine.Audio;

namespace Project2048.Prototype
{
    public class CombatWorldSpriteView : MonoBehaviour
    {
        public const float EnemyDeathFadeDurationSeconds = 0.6f;
        public const float EnemyAppearIntroDurationSeconds = 0.45f;
        public const float EnemyAttackLungeDurationSeconds = 0.32f;
        public const float EnemyAppearScreenShakeDurationSeconds = 0.34f;
        public const float ShieldImpactParticleLifetimeSeconds = 0.8f;
        public const float DebuffCastParticleLifetimeSeconds = 0.9f;

        private const float EnemyAppearIntroRightOffset = 2.25f;
        private const float EnemyAppearIntroJumpHeight = 0.7f;
        private const float EnemyAppearIntroScalePop = 0.08f;
        private const float EnemyAttackLungeDistance = 0.72f;
        private const float EnemyAttackLungeImpactTime = 0.45f;
        private const float EnemyAttackLungeScalePop = 0.05f;
        private const float EnemyAppearScreenShakeMagnitude = 0.22f;
        private const int ShieldImpactParticleCount = 22;
        private const int DebuffCastParticleCount = 28;

        [SerializeField] private PrototypeCombatBootstrap bootstrap;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer playerRenderer;
        [SerializeField] private SpriteRenderer enemyRenderer;
        [SerializeField] private Sprite defaultBackgroundSprite;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private SimpleBgmDucker bgmDucker;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Animator enemyAnimator;
        [SerializeField] private ParticleSystem shieldImpactParticlePrefab;
        [SerializeField] private ParticleSystem debuffCastParticlePrefab;
        [SerializeField] private Transform screenShakeTarget;
        [SerializeField] private Color shieldImpactParticleColor = new(0.62f, 0.92f, 1f, 0.96f);
        [SerializeField] private Color fearDebuffParticleColor = new(0.75f, 0.05f, 0.16f, 0.95f);
        [SerializeField] private Color darknessDebuffParticleColor = new(0.40f, 0.12f, 0.78f, 0.95f);

        private CombatManager combatManager;
        private CombatSnapshot snapshot;
        private Coroutine enemyDeathFadeCoroutine;
        private Coroutine enemyAppearIntroCoroutine;
        private Coroutine enemyAttackLungeCoroutine;
        private Coroutine screenShakeCoroutine;
        private Transform activeScreenShakeTarget;
        private Transform foregroundScreenShakeRoot;
        private Vector3 screenShakeRestLocalPosition;
        private Vector3 enemyRendererRestLocalPosition;
        private Vector3 enemyRendererRestLocalScale = Vector3.one;
        private bool hasEnemyRendererRestTransform;
        private bool lastEnemyWasDead;
        private int lastPlayedEnemyDebuffVfxSequence;

        public void Initialize(PrototypeCombatBootstrap owner)
        {
            bootstrap = owner;
            UnbindCombatEvents();
            combatManager = owner != null ? owner.CombatManager : null;

            ResolveMissingReferences();
            ResolveScreenShakeTarget();
            CacheEnemyRendererRestTransform();
            RenderBackground();

            if (combatManager == null)
            {
                return;
            }

            combatManager.OnCombatStateChanged -= HandleCombatStateChanged;
            combatManager.OnCombatStateChanged += HandleCombatStateChanged;
            combatManager.OnPlayerSkillUsed -= HandlePlayerSkillUsed;
            combatManager.OnPlayerSkillUsed += HandlePlayerSkillUsed;

            snapshot = combatManager.GetSnapshot();
            lastEnemyWasDead = snapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false;
            lastPlayedEnemyDebuffVfxSequence = 0;
            Render(snapshot);
            SetEnemyRendererAlpha(lastEnemyWasDead ? 0f : 1f);
        }

        private void OnDestroy()
        {
            UnbindCombatEvents();
            ClearEnemyDeathFade();
            ClearEnemyAppearIntro();
            ClearEnemyAttackLunge();
            ClearScreenShake(restoreTransform: true);
        }

        private void UnbindCombatEvents()
        {
            if (combatManager == null)
            {
                return;
            }

            combatManager.OnCombatStateChanged -= HandleCombatStateChanged;
            combatManager.OnPlayerSkillUsed -= HandlePlayerSkillUsed;
        }

        private void HandleCombatStateChanged(CombatSnapshot nextSnapshot)
        {
            var playerWasHit = PlayerWasHit(snapshot, nextSnapshot);
            var enemyWasHit = EnemyWasHit(snapshot, nextSnapshot);
            var playerShieldWasHit = PlayerShieldWasHit(snapshot, nextSnapshot);
            var enemyShieldWasHit = EnemyShieldWasHit(snapshot, nextSnapshot);
            var enemyUsedAttack = EnemyUsedAttack(snapshot, nextSnapshot, playerWasHit);
            var enemyUsedDefense = EnemyUsedDefense(snapshot, nextSnapshot);
            var enemyAppeared = EnemyAppeared(snapshot, nextSnapshot);
            var nextEnemyDead = nextSnapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false;
            var enemyJustDied = !lastEnemyWasDead && nextEnemyDead;

            snapshot = nextSnapshot;
            Render(snapshot);
            PlayEnemyAppearEffectIfNeeded(enemyAppeared);
            PlayEnemyAttackEffectIfNeeded(enemyUsedAttack);
            PlayShieldImpactEffectIfNeeded(playerShieldWasHit, playerRenderer != null ? playerRenderer.transform : transform);
            PlayShieldImpactEffectIfNeeded(enemyShieldWasHit, enemyRenderer != null ? enemyRenderer.transform : transform);
            PlayPlayerActionEffectIfNeeded(playerWasHit);
            PlayEnemyActionEffectIfNeeded(enemyWasHit, enemyJustDied);
            PlayEnemyDebuffCastEffectIfNeeded(snapshot?.LastVfxCue);
            PlayEnemyDefenseEffectIfNeeded(enemyUsedDefense);
            PlayEnemyDeathFadeIfNeeded(enemyJustDied, nextEnemyDead);
            lastEnemyWasDead = nextEnemyDead;
        }

        private void HandlePlayerSkillUsed(SkillSO skill, EnemyController target)
        {
            if (skill?.activationEffect == null || !skill.activationEffect.HasAnyAsset)
            {
                return;
            }

            var isAttack = skill.skillType == SkillType.Attack;
            var anchor = isAttack && target != null && enemyRenderer != null
                ? enemyRenderer.transform
                : playerRenderer != null
                    ? playerRenderer.transform
                    : transform;
            var animator = isAttack ? enemyAnimator : playerAnimator;
            PlayCombatantActionEffect(skill.activationEffect, anchor, animator);
        }

        private void Render(CombatSnapshot currentSnapshot)
        {
            RenderBackground();

            if (playerRenderer != null)
            {
                playerRenderer.sprite = combatManager?.Player?.Data?.portrait;
            }

            if (enemyRenderer == null)
            {
                return;
            }

            enemyRenderer.sprite = ResolveEnemySprite(currentSnapshot);
            var enemyIsAlive = !(currentSnapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false);
            if (enemyIsAlive && enemyDeathFadeCoroutine == null && enemyAppearIntroCoroutine == null)
            {
                SetEnemyRendererAlpha(1f);
            }
        }

        private void PlayPlayerActionEffectIfNeeded(bool playerWasHit)
        {
            if (!playerWasHit)
            {
                return;
            }

            PlayCombatantActionEffect(
                combatManager?.Player?.Data?.FindActionEffect(CombatActionIds.Hit),
                playerRenderer != null ? playerRenderer.transform : transform,
                playerAnimator);
        }

        private void PlayEnemyAttackEffectIfNeeded(bool enemyUsedAttack)
        {
            if (!enemyUsedAttack)
            {
                return;
            }

            PlayEnemyAttackLunge(ResolveCurrentEnemyData()?.FindActionEffect(CombatActionIds.Attack));
        }

        private void PlayEnemyAppearEffectIfNeeded(bool enemyAppeared)
        {
            if (!enemyAppeared)
            {
                return;
            }

            PlayEnemyAppearIntro(ResolveCurrentEnemyData()?.FindActionEffect(CombatActionIds.Appear));
        }

        private void PlayEnemyActionEffectIfNeeded(bool enemyWasHit, bool enemyJustDied)
        {
            var enemyData = ResolveCurrentEnemyData();
            if (enemyJustDied)
            {
                PlayCombatantActionEffect(
                    enemyData?.FindActionEffect(CombatActionIds.Death),
                    enemyRenderer != null ? enemyRenderer.transform : transform,
                    enemyAnimator);
                return;
            }

            if (!enemyWasHit)
            {
                return;
            }

            PlayCombatantActionEffect(
                enemyData?.FindActionEffect(CombatActionIds.Hit),
                enemyRenderer != null ? enemyRenderer.transform : transform,
                enemyAnimator);
        }

        private void PlayEnemyDefenseEffectIfNeeded(bool enemyUsedDefense)
        {
            if (!enemyUsedDefense)
            {
                return;
            }

            PlayCombatantActionEffect(
                ResolveCurrentEnemyData()?.FindActionEffect(CombatActionIds.Defend),
                enemyRenderer != null ? enemyRenderer.transform : transform,
                enemyAnimator);
        }

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
            var effect = ResolveCurrentEnemyData()?.FindActionEffect(ResolveDebuffActionId(cue.DebuffType));
            var hasAuthoredVisual = effect?.vfxPrefab != null || effect?.animationClip != null;
            PlayCombatantActionEffect(
                effect,
                enemyRenderer != null ? enemyRenderer.transform : transform,
                enemyAnimator);

            if (!hasAuthoredVisual)
            {
                SpawnDebuffCastParticles(cue.DebuffType);
            }
        }

        private void PlayShieldImpactEffectIfNeeded(bool shieldWasHit, Transform anchor)
        {
            if (!shieldWasHit)
            {
                return;
            }

            SpawnParticleBurst(
                shieldImpactParticlePrefab,
                anchor,
                "ShieldImpactParticles",
                shieldImpactParticleColor,
                ShieldImpactParticleLifetimeSeconds,
                ShieldImpactParticleCount,
                0.78f,
                0.13f);
        }

        private EnemySO ResolveCurrentEnemyData()
        {
            var enemyIndex = snapshot?.Enemies?.FirstOrDefault()?.EnemyIndex ?? 0;
            var enemies = combatManager?.Enemies;
            if (enemies == null || enemyIndex < 0 || enemyIndex >= enemies.Count)
            {
                return null;
            }

            return enemies[enemyIndex]?.Data;
        }

        private void PlayCombatantActionEffect(CombatEffectBinding effect, Transform anchor, Animator animator)
        {
            if (effect == null || !effect.HasAnyAsset)
            {
                return;
            }

            if (effect.sfxClip != null)
            {
                EnsureAudioSource();
                if (audioSource != null)
                {
                    DuckBgmForImportantSfx();
                    CombatEffectAudioPlayer.PlayOneShot(audioSource, effect, 1f, transform);
                }
            }

            if (effect.vfxPrefab != null)
            {
                var parent = anchor != null ? anchor : transform;
                var instance = Instantiate(effect.vfxPrefab, parent.position, Quaternion.identity, parent);
                instance.transform.localPosition += effect.localOffset;
                var lifetime = effect.EffectiveAutoDestroySeconds;
                if (lifetime > 0f)
                {
                    Destroy(instance, lifetime);
                }
            }

            if (effect.animationClip != null && animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Play(effect.animationClip.name, 0, 0f);
            }
        }

        private void PlayEnemyAppearIntro(CombatEffectBinding effect)
        {
            if (enemyRenderer == null)
            {
                PlayEnemyAppearScreenShake();
                PlayCombatantActionEffect(effect, transform, enemyAnimator);
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

            PlayEnemyAppearScreenShake();
            enemyAppearIntroCoroutine = StartCoroutine(EnemyAppearIntroRoutine(effect));
        }

        private void PlayEnemyAttackLunge(CombatEffectBinding effect)
        {
            if (enemyRenderer == null)
            {
                PlayCombatantActionEffect(effect, transform, enemyAnimator);
                return;
            }

            CacheEnemyRendererRestTransform();
            ClearEnemyAppearIntro(restoreTransform: true);
            ClearEnemyAttackLunge(restoreTransform: true);

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                RestoreEnemyRendererTransform();
                PlayCombatantActionEffect(effect, enemyRenderer.transform, enemyAnimator);
                return;
            }

            enemyAttackLungeCoroutine = StartCoroutine(EnemyAttackLungeRoutine(effect));
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
                    PlayCombatantActionEffect(effect, enemyRenderer.transform, enemyAnimator);
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
                PlayCombatantActionEffect(effect, enemyRenderer.transform, enemyAnimator);
            }

            enemyAttackLungeCoroutine = null;
        }

        private void RenderBackground()
        {
            if (backgroundRenderer != null && defaultBackgroundSprite != null)
            {
                backgroundRenderer.sprite = defaultBackgroundSprite;
            }
        }

        private Sprite ResolveEnemySprite(CombatSnapshot currentSnapshot)
        {
            var enemyIndex = currentSnapshot?.Enemies?.FirstOrDefault()?.EnemyIndex ?? 0;
            var enemies = combatManager?.Enemies;
            if (enemies == null || enemyIndex < 0 || enemyIndex >= enemies.Count)
            {
                return null;
            }

            return enemies[enemyIndex]?.Data?.portrait;
        }

        private void PlayEnemyDeathFadeIfNeeded(bool enemyJustDied, bool nextEnemyDead)
        {
            if ((enemyJustDied || nextEnemyDead) &&
                enemyRenderer != null &&
                enemyDeathFadeCoroutine == null &&
                enemyRenderer.color.a > 0.001f)
            {
                PlayEnemyDeathFade();
                return;
            }

            if (!nextEnemyDead && enemyRenderer != null)
            {
                ClearEnemyDeathFade();
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

        private void PlayEnemyAppearScreenShake()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            var target = ResolveScreenShakeTarget();
            if (target == null)
            {
                return;
            }

            ClearScreenShake(restoreTransform: true);
            activeScreenShakeTarget = target;
            screenShakeRestLocalPosition = target.localPosition;
            screenShakeCoroutine = StartCoroutine(ScreenShakeRoutine(target, screenShakeRestLocalPosition));
        }

        private IEnumerator ScreenShakeRoutine(Transform target, Vector3 restLocalPosition)
        {
            var startTime = Time.realtimeSinceStartup;
            while (true)
            {
                if (target == null)
                {
                    yield break;
                }

                var elapsed = Time.realtimeSinceStartup - startTime;
                var t = Mathf.Clamp01(elapsed / EnemyAppearScreenShakeDurationSeconds);
                var decay = 1f - t;
                var offset = new Vector3(
                    Mathf.Sin((t * Mathf.PI * 13f) + 0.35f) * EnemyAppearScreenShakeMagnitude * decay,
                    Mathf.Sin((t * Mathf.PI * 17f) + 1.1f) * EnemyAppearScreenShakeMagnitude * 0.55f * decay,
                    0f);
                target.localPosition = restLocalPosition + offset;

                if (t >= 1f)
                {
                    break;
                }

                yield return null;
            }

            target.localPosition = restLocalPosition;
            screenShakeCoroutine = null;
            activeScreenShakeTarget = null;
        }

        private Transform ResolveScreenShakeTarget()
        {
            if (screenShakeTarget != null)
            {
                return screenShakeTarget;
            }

            var foregroundRoot = ResolveForegroundScreenShakeRoot();
            if (foregroundRoot != null)
            {
                screenShakeTarget = foregroundRoot;
                return screenShakeTarget;
            }

            return transform;
        }

        private Transform ResolveForegroundScreenShakeRoot()
        {
            if (foregroundScreenShakeRoot != null)
            {
                return foregroundScreenShakeRoot;
            }

            if (playerRenderer == null && enemyRenderer == null)
            {
                return null;
            }

            var rootObject = new GameObject("ForegroundScreenShakeRoot");
            foregroundScreenShakeRoot = rootObject.transform;
            foregroundScreenShakeRoot.SetParent(transform, false);
            foregroundScreenShakeRoot.localPosition = Vector3.zero;
            foregroundScreenShakeRoot.localRotation = Quaternion.identity;
            foregroundScreenShakeRoot.localScale = Vector3.one;

            ReparentRendererForScreenShake(playerRenderer);
            ReparentRendererForScreenShake(enemyRenderer);
            hasEnemyRendererRestTransform = false;
            return foregroundScreenShakeRoot;
        }

        private void ReparentRendererForScreenShake(SpriteRenderer renderer)
        {
            if (renderer == null || foregroundScreenShakeRoot == null)
            {
                return;
            }

            var rendererTransform = renderer.transform;
            if (rendererTransform == foregroundScreenShakeRoot || rendererTransform.IsChildOf(foregroundScreenShakeRoot))
            {
                return;
            }

            if (rendererTransform.parent != null && rendererTransform.parent != transform)
            {
                return;
            }

            rendererTransform.SetParent(foregroundScreenShakeRoot, true);
        }

        private void ClearScreenShake(bool restoreTransform = false)
        {
            if (screenShakeCoroutine != null)
            {
                StopCoroutine(screenShakeCoroutine);
                screenShakeCoroutine = null;
            }

            if (restoreTransform && activeScreenShakeTarget != null)
            {
                activeScreenShakeTarget.localPosition = screenShakeRestLocalPosition;
            }

            activeScreenShakeTarget = null;
        }

        private Vector3 ResolveEnemyAttackLungeTarget(Vector3 restLocalPosition)
        {
            if (enemyRenderer == null)
            {
                return restLocalPosition;
            }

            var enemyTransform = enemyRenderer.transform;
            var enemyWorldPosition = enemyTransform.position;
            var targetWorldPosition = playerRenderer != null
                ? playerRenderer.transform.position
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

        private void SpawnDebuffCastParticles(DebuffType debuffType)
        {
            var color = ResolveDebuffParticleColor(debuffType);
            SpawnParticleBurst(
                debuffCastParticlePrefab,
                enemyRenderer != null ? enemyRenderer.transform : transform,
                $"{debuffType}DebuffCastParticles",
                color,
                DebuffCastParticleLifetimeSeconds,
                DebuffCastParticleCount,
                0.62f,
                0.16f);
        }

        private void SpawnParticleBurst(
            ParticleSystem prefab,
            Transform anchor,
            string objectName,
            Color color,
            float lifetimeSeconds,
            int burstCount,
            float startSpeed,
            float startSize)
        {
            var parent = anchor != null ? anchor : transform;
            var particles = prefab != null
                ? Instantiate(prefab, parent.position, Quaternion.identity, parent)
                : CreateFallbackParticleSystem(parent, objectName);
            if (particles == null)
            {
                return;
            }

            particles.gameObject.name = objectName;
            particles.transform.localPosition = Vector3.zero;
            ConfigureParticleBurst(particles, color, lifetimeSeconds, burstCount, startSpeed, startSize, parent);
            particles.Play(true);
            if (lifetimeSeconds > 0f && Application.isPlaying)
            {
                Destroy(particles.gameObject, lifetimeSeconds + 0.2f);
            }
        }

        private ParticleSystem CreateFallbackParticleSystem(Transform parent, string objectName)
        {
            var particleObject = new GameObject(objectName, typeof(ParticleSystem));
            particleObject.transform.SetParent(parent, false);
            return particleObject.GetComponent<ParticleSystem>();
        }

        private static void ConfigureParticleBurst(
            ParticleSystem particles,
            Color color,
            float lifetimeSeconds,
            int burstCount,
            float startSpeed,
            float startSize,
            Transform anchor)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Clear(true);

            var main = particles.main;
            main.duration = Mathf.Max(0.05f, lifetimeSeconds * 0.35f);
            main.loop = false;
            main.startLifetime = Mathf.Max(0.05f, lifetimeSeconds);
            main.startSpeed = Mathf.Max(0f, startSpeed);
            main.startSize = Mathf.Max(0.01f, startSize);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.Clamp(burstCount, 1, short.MaxValue)),
            });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.22f;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer == null || anchor == null)
            {
                return;
            }

            var anchorRenderer = anchor.GetComponent<SpriteRenderer>();
            if (anchorRenderer == null)
            {
                return;
            }

            renderer.sortingLayerID = anchorRenderer.sortingLayerID;
            renderer.sortingOrder = anchorRenderer.sortingOrder + 2;
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

        private static string ResolveDebuffActionId(DebuffType debuffType)
        {
            return debuffType switch
            {
                DebuffType.Fear => CombatActionIds.DebuffFear,
                DebuffType.Darkness => CombatActionIds.DebuffDarkness,
                _ => null,
            };
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
            if (enemyRenderer == null)
            {
                return;
            }

            var color = enemyRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            enemyRenderer.color = color;
        }

        private void ResolveMissingReferences()
        {
            if (backgroundRenderer == null)
            {
                backgroundRenderer = FindRendererByName("BackgroundSprite");
            }

            if (playerRenderer == null)
            {
                playerRenderer = FindRendererByName("PlayerSprite");
            }

            if (enemyRenderer == null)
            {
                enemyRenderer = FindRendererByName("EnemySprite");
            }

            if (playerAnimator == null && playerRenderer != null)
            {
                playerAnimator = playerRenderer.GetComponent<Animator>();
            }

            if (enemyAnimator == null && enemyRenderer != null)
            {
                enemyAnimator = enemyRenderer.GetComponent<Animator>();
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            ResolveAudioRouting();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            if (sfxMixerGroup != null)
            {
                audioSource.outputAudioMixerGroup = sfxMixerGroup;
            }
        }

        private void ResolveAudioRouting()
        {
            var settings = Project2048AudioSettings.LoadDefault();
            if (sfxMixerGroup == null)
            {
                sfxMixerGroup = settings != null ? settings.SfxGroup : null;
            }

            if (bgmDucker == null)
            {
                bgmDucker = SimpleBgmDucker.Active != null
                    ? SimpleBgmDucker.Active
                    : FindAnyObjectByType<SimpleBgmDucker>(FindObjectsInactive.Include);
            }
        }

        private void DuckBgmForImportantSfx()
        {
            ResolveAudioRouting();
            bgmDucker?.DuckBgm();
        }

        private static SpriteRenderer FindRendererByName(string objectName)
        {
            var target = GameObject.Find(objectName);
            return target != null ? target.GetComponent<SpriteRenderer>() : null;
        }

        private static bool PlayerWasHit(CombatSnapshot previous, CombatSnapshot next)
        {
            if (previous?.Player == null || next?.Player == null)
            {
                return false;
            }

            return next.Phase == CombatPhase.EnemyTurn &&
                (next.Player.CurrentHp < previous.Player.CurrentHp ||
                next.Player.Block < previous.Player.Block);
        }

        private static bool PlayerShieldWasHit(CombatSnapshot previous, CombatSnapshot next)
        {
            if (previous?.Player == null || next?.Player == null || next.Phase != CombatPhase.EnemyTurn)
            {
                return false;
            }

            return previous.Player.Block > 0 && next.Player.Block < previous.Player.Block;
        }

        private static bool EnemyWasHit(CombatSnapshot previous, CombatSnapshot next)
        {
            var previousEnemy = previous?.Enemies?.FirstOrDefault();
            var nextEnemy = next?.Enemies?.FirstOrDefault();
            if (previousEnemy == null || nextEnemy == null)
            {
                return false;
            }

            return nextEnemy.CurrentHp < previousEnemy.CurrentHp ||
                (next.Phase == CombatPhase.ActionPhase && nextEnemy.Block < previousEnemy.Block);
        }

        private static bool EnemyShieldWasHit(CombatSnapshot previous, CombatSnapshot next)
        {
            var previousEnemy = previous?.Enemies?.FirstOrDefault();
            var nextEnemy = next?.Enemies?.FirstOrDefault();
            if (previousEnemy == null || nextEnemy == null || next.Phase != CombatPhase.ActionPhase)
            {
                return false;
            }

            return previousEnemy.Block > 0 && nextEnemy.Block < previousEnemy.Block;
        }

        private static bool EnemyUsedAttack(CombatSnapshot previous, CombatSnapshot next, bool playerWasHit)
        {
            if (!playerWasHit || next?.Phase != CombatPhase.EnemyTurn)
            {
                return false;
            }

            var nextEnemy = next.Enemies?.FirstOrDefault();
            return EnemyHasAttackIntent(nextEnemy);
        }

        private static bool EnemyUsedDefense(CombatSnapshot previous, CombatSnapshot next)
        {
            var previousEnemy = previous?.Enemies?.FirstOrDefault();
            var nextEnemy = next?.Enemies?.FirstOrDefault();
            if (previousEnemy == null || nextEnemy == null || nextEnemy.IsDead)
            {
                return false;
            }

            return next.Phase == CombatPhase.EnemyTurn &&
                EnemyHasDefenseIntent(nextEnemy) &&
                nextEnemy.Block > previousEnemy.Block;
        }

        private static bool EnemyHasAttackIntent(EnemyCombatSnapshot enemy)
        {
            if (enemy?.Intents != null && enemy.Intents.Any(intent => intent?.intentType == EnemyIntentType.Attack))
            {
                return true;
            }

            return enemy?.Intent?.intentType == EnemyIntentType.Attack;
        }

        private static bool EnemyHasDefenseIntent(EnemyCombatSnapshot enemy)
        {
            if (enemy?.Intents != null && enemy.Intents.Any(intent => intent?.intentType == EnemyIntentType.Defense))
            {
                return true;
            }

            return enemy?.Intent?.intentType == EnemyIntentType.Defense;
        }

        private static bool EnemyAppeared(CombatSnapshot previous, CombatSnapshot next)
        {
            var nextEnemy = next?.Enemies?.FirstOrDefault();
            if (nextEnemy == null || nextEnemy.IsDead)
            {
                return false;
            }

            var previousEnemy = previous?.Enemies?.FirstOrDefault();
            return previousEnemy == null ||
                previousEnemy.IsDead ||
                previous.Phase == CombatPhase.Victory ||
                previous.Phase == CombatPhase.Defeat ||
                previousEnemy.EnemyIndex != nextEnemy.EnemyIndex;
        }
    }
}
