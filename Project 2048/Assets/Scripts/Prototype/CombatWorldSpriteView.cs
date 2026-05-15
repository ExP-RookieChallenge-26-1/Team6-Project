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

        private const float EnemyAppearIntroRightOffset = 2.25f;
        private const float EnemyAppearIntroJumpHeight = 0.7f;
        private const float EnemyAppearIntroScalePop = 0.08f;

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

        private CombatManager combatManager;
        private CombatSnapshot snapshot;
        private Coroutine enemyDeathFadeCoroutine;
        private Coroutine enemyAppearIntroCoroutine;
        private Vector3 enemyRendererRestLocalPosition;
        private Vector3 enemyRendererRestLocalScale = Vector3.one;
        private bool hasEnemyRendererRestTransform;
        private bool lastEnemyWasDead;

        public void Initialize(PrototypeCombatBootstrap owner)
        {
            bootstrap = owner;
            UnbindCombatEvents();
            combatManager = owner != null ? owner.CombatManager : null;

            ResolveMissingReferences();
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
            Render(snapshot);
            SetEnemyRendererAlpha(lastEnemyWasDead ? 0f : 1f);
        }

        private void OnDestroy()
        {
            UnbindCombatEvents();
            ClearEnemyDeathFade();
            ClearEnemyAppearIntro();
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
            var enemyUsedDefense = EnemyUsedDefense(snapshot, nextSnapshot);
            var enemyAppeared = EnemyAppeared(snapshot, nextSnapshot);
            var nextEnemyDead = nextSnapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false;
            var enemyJustDied = !lastEnemyWasDead && nextEnemyDead;

            snapshot = nextSnapshot;
            Render(snapshot);
            PlayEnemyAppearEffectIfNeeded(enemyAppeared);
            PlayPlayerActionEffectIfNeeded(playerWasHit);
            PlayEnemyActionEffectIfNeeded(enemyWasHit, enemyJustDied);
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
                PlayCombatantActionEffect(effect, transform, enemyAnimator);
                return;
            }

            CacheEnemyRendererRestTransform();
            ClearEnemyAppearIntro(restoreTransform: false);

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                RestoreEnemyRendererTransform();
                PlayCombatantActionEffect(effect, enemyRenderer.transform, enemyAnimator);
                return;
            }

            enemyAppearIntroCoroutine = StartCoroutine(EnemyAppearIntroRoutine(effect));
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

            return next.Player.CurrentHp < previous.Player.CurrentHp ||
                next.Player.Block < previous.Player.Block;
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
                nextEnemy.Block < previousEnemy.Block;
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
