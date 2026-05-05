using System.Collections;
using System.Linq;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Presentation;
using UnityEngine;

namespace Project2048.Prototype
{
    public class CombatWorldSpriteView : MonoBehaviour
    {
        public const float EnemyDeathFadeDurationSeconds = 0.6f;

        [SerializeField] private PrototypeCombatBootstrap bootstrap;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer playerRenderer;
        [SerializeField] private SpriteRenderer enemyRenderer;
        [SerializeField] private Sprite defaultBackgroundSprite;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Animator enemyAnimator;

        private CombatManager combatManager;
        private CombatSnapshot snapshot;
        private Coroutine enemyDeathFadeCoroutine;
        private bool lastEnemyWasDead;

        public void Initialize(PrototypeCombatBootstrap owner)
        {
            bootstrap = owner;
            UnbindCombatEvents();
            combatManager = owner != null ? owner.CombatManager : null;

            ResolveMissingReferences();
            RenderBackground();

            if (combatManager == null)
            {
                return;
            }

            combatManager.OnCombatStateChanged -= HandleCombatStateChanged;
            combatManager.OnCombatStateChanged += HandleCombatStateChanged;

            snapshot = combatManager.GetSnapshot();
            lastEnemyWasDead = snapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false;
            Render(snapshot);
            SetEnemyRendererAlpha(lastEnemyWasDead ? 0f : 1f);
        }

        private void OnDestroy()
        {
            UnbindCombatEvents();
            ClearEnemyDeathFade();
        }

        private void UnbindCombatEvents()
        {
            if (combatManager == null)
            {
                return;
            }

            combatManager.OnCombatStateChanged -= HandleCombatStateChanged;
        }

        private void HandleCombatStateChanged(CombatSnapshot nextSnapshot)
        {
            var playerWasHit = PlayerWasHit(snapshot, nextSnapshot);
            var enemyWasHit = EnemyWasHit(snapshot, nextSnapshot);
            var nextEnemyDead = nextSnapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false;
            var enemyJustDied = !lastEnemyWasDead && nextEnemyDead;

            snapshot = nextSnapshot;
            Render(snapshot);
            PlayPlayerActionEffectIfNeeded(playerWasHit);
            PlayEnemyActionEffectIfNeeded(enemyWasHit, enemyJustDied);
            PlayEnemyDeathFadeIfNeeded(enemyJustDied, nextEnemyDead);
            lastEnemyWasDead = nextEnemyDead;
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
            if (enemyIsAlive && enemyDeathFadeCoroutine == null)
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
                    audioSource.PlayOneShot(effect.sfxClip, effect.EffectiveVolumeScale);
                }
            }

            if (effect.vfxPrefab != null)
            {
                var parent = anchor != null ? anchor : transform;
                var instance = Instantiate(effect.vfxPrefab, parent.position, Quaternion.identity, parent);
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

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
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
    }
}
