using System.Collections;
using System.Linq;
using Project2048.Audio;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Presentation;
using Project2048.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace Project2048.Prototype
{
    public class CombatWorldSpriteView : MonoBehaviour
    {
        public const float EnemyDeathFadeDurationSeconds = 0.6f;
        public const float EnemyAppearIntroDurationSeconds = 0.45f;
        public const float EnemyAttackLungeDurationSeconds = 0.32f;
        public const float EnemyAppearWorldShakeDurationSeconds = 1.5f;
        public const float ShieldImpactParticleLifetimeSeconds = 0.8f;
        public const float DebuffCastParticleLifetimeSeconds = 0.9f;
        public const float DebuffTargetParticleDelaySeconds = DebuffCastParticleLifetimeSeconds;
        public const float DamageNumberPopupDurationSeconds = 0.55f;

        private const float EnemyAppearIntroRightOffset = 2.25f;
        private const float EnemyAppearIntroJumpHeight = 0.7f;
        private const float EnemyAppearIntroScalePop = 0.08f;
        private const float EnemyAttackLungeDistance = 0.72f;
        private const float EnemyAttackLungeImpactTime = 0.45f;
        private const float EnemyAttackLungeScalePop = 0.05f;
        private const float EnemyAppearWorldShakeMagnitude = 0.13f;
        private const float DamageNumberPopupRiseDistance = 0.62f;
        private const float DamageNumberPopupWorldFontSize = 2.6f;
        private const float DamageNumberPopupUiFontSize = 34f;
        private const int DamageNumberPopupSortingOrderOffset = 32;
        private const int ShieldImpactParticleCount = 22;
        private const int DebuffCastParticleCount = 28;
        private const string DefaultWorldVfxProfileResourceName = "PrototypeCombatWorldVfxProfile";

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
        [SerializeField] private CombatWorldVfxProfileSO worldVfxProfile;
        [SerializeField] private WorldShake worldShake;
        [SerializeField] private Transform foregroundShakeRoot;
        [SerializeField] private Color shieldImpactParticleColor = new(0.62f, 0.92f, 1f, 0.96f);
        [SerializeField] private Color fearDebuffParticleColor = new(0.75f, 0.05f, 0.16f, 0.95f);
        [SerializeField] private Color darknessDebuffParticleColor = new(0.24f, 0.10f, 0.48f, 0.95f);
        [SerializeField] private Material shieldImpactParticleMaterial;
        [SerializeField] private Material fearDebuffParticleMaterial;
        [SerializeField] private Material darknessDebuffParticleMaterial;

        private CombatManager combatManager;
        private CombatSnapshot snapshot;
        private Coroutine enemyDeathFadeCoroutine;
        private Coroutine enemyAppearIntroCoroutine;
        private Coroutine enemyAttackLungeCoroutine;
        private Vector3 enemyRendererRestLocalPosition;
        private Vector3 enemyRendererRestLocalScale = Vector3.one;
        private bool hasEnemyRendererRestTransform;
        private bool lastEnemyWasDead;
        private int lastPlayedEnemyDebuffVfxSequence;
        private Material runtimeShieldImpactParticleMaterial;
        private Material runtimeFearDebuffParticleMaterial;
        private Material runtimeDarknessDebuffParticleMaterial;
        private RectTransform damageNumberPopupLayer;
        private readonly System.Collections.Generic.List<GameObject> damageNumberPopups = new();

        public void Initialize(PrototypeCombatBootstrap owner)
        {
            bootstrap = owner;
            UnbindCombatEvents();
            combatManager = owner != null ? owner.CombatManager : null;

            ResolveMissingReferences();
            ResolveWorldVfxProfile();
            ResolveWorldShake();
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
            ClearWorldShake();
            ClearDamageNumberPopups();
            DestroyRuntimeParticleMaterials();
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
            var playerHpDamage = ResolvePlayerHpDamage(snapshot, nextSnapshot);
            var enemyHpDamage = ResolveEnemyHpDamage(snapshot, nextSnapshot);
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
            PlayDamageNumberPopupIfNeeded(playerHpDamage, playerRenderer);
            PlayDamageNumberPopupIfNeeded(enemyHpDamage, enemyRenderer);
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
            PlayCombatantActionEffect(
                effect,
                enemyRenderer != null ? enemyRenderer.transform : transform,
                enemyAnimator);

            SpawnDebuffCastParticles(
                cue.DebuffType,
                enemyRenderer != null ? enemyRenderer.transform : transform);
            PlayDebuffTargetEffectAfterCast(cue.DebuffType, ResolveDebuffParticleLifetimeSeconds(cue.DebuffType));
        }

        private void PlayShieldImpactEffectIfNeeded(bool shieldWasHit, Transform anchor)
        {
            if (!shieldWasHit)
            {
                return;
            }

            var effect = ResolveShieldImpactParticleEffect();
            SpawnParticleBurst(
                effect,
                anchor,
                "ShieldImpactParticles",
                shieldImpactParticlePrefab,
                shieldImpactParticleColor,
                effect?.particleMaterial != null ? null : ResolveShieldImpactParticleMaterial(),
                ShieldImpactParticleLifetimeSeconds,
                ShieldImpactParticleCount,
                0.78f,
                0.22f,
                swirl: false);
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

            if (effect.particleEffect?.HasParticleVisual == true)
            {
                SpawnParticleBurst(
                    effect.particleEffect,
                    anchor,
                    "CombatActionParticles",
                    fallbackPrefab: null,
                    fallbackColor: Color.white,
                    fallbackMaterial: null,
                    fallbackLifetimeSeconds: effect.EffectiveAutoDestroySeconds,
                    fallbackBurstCount: 16,
                    fallbackStartSpeed: 0.6f,
                    fallbackStartSize: 0.12f,
                    swirl: false);
            }

            if (effect.animationClip != null && animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Play(effect.animationClip.name, 0, 0f);
            }
        }

        private void PlayDamageNumberPopupIfNeeded(int damageAmount, SpriteRenderer targetRenderer)
        {
            if (damageAmount <= 0 || targetRenderer == null)
            {
                return;
            }

            var popupLayer = ResolveDamageNumberPopupLayer();
            if (popupLayer != null)
            {
                PlayDamageNumberUiPopup(damageAmount, targetRenderer, popupLayer);
                return;
            }

            PlayDamageNumberWorldPopup(damageAmount, targetRenderer);
        }

        private void PlayDamageNumberUiPopup(int damageAmount, SpriteRenderer targetRenderer, RectTransform popupLayer)
        {
            var popupObject = new GameObject("DamageNumberPopup", typeof(RectTransform), typeof(TextMeshProUGUI));
            popupObject.transform.SetParent(popupLayer, false);
            damageNumberPopups.Add(popupObject);

            var rectTransform = popupObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(160f, 64f);
            rectTransform.anchoredPosition = ResolveDamageNumberCanvasPosition(targetRenderer, popupLayer);

            var label = popupObject.GetComponent<TextMeshProUGUI>();
            ConfigureDamageNumberLabel(label, damageAmount, DamageNumberPopupUiFontSize);
            label.raycastTarget = false;

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            StartCoroutine(DamageNumberPopupRoutine(rectTransform, label));
        }

        private void PlayDamageNumberWorldPopup(int damageAmount, SpriteRenderer targetRenderer)
        {
            var popupObject = new GameObject("DamageNumberPopup", typeof(TextMeshPro));
            popupObject.transform.SetParent(targetRenderer.transform, false);
            popupObject.transform.localPosition = targetRenderer.transform.InverseTransformPoint(ResolveDamageNumberWorldPosition(targetRenderer));
            popupObject.transform.localRotation = Quaternion.identity;
            popupObject.transform.localScale = Vector3.one;
            damageNumberPopups.Add(popupObject);

            var label = popupObject.GetComponent<TextMeshPro>();
            ConfigureDamageNumberLabel(label, damageAmount, DamageNumberPopupWorldFontSize);

            var meshRenderer = popupObject.GetComponent<MeshRenderer>();
            meshRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            meshRenderer.sortingOrder = targetRenderer.sortingOrder + DamageNumberPopupSortingOrderOffset;

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            StartCoroutine(DamageNumberPopupRoutine(popupObject.transform, label));
        }

        private static void ConfigureDamageNumberLabel(TMP_Text label, int damageAmount, float fontSize)
        {
            label.text = damageAmount.ToString();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, 0.92f, 0.55f, 1f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static Vector3 ResolveDamageNumberWorldPosition(SpriteRenderer targetRenderer)
        {
            if (targetRenderer == null)
            {
                return Vector3.up * 1.2f;
            }

            if (targetRenderer.sprite == null)
            {
                return targetRenderer.transform.position + (Vector3.up * 1.2f);
            }

            return targetRenderer.bounds.center + Vector3.up * (targetRenderer.bounds.extents.y + 0.35f);
        }

        private static Vector2 ResolveDamageNumberCanvasPosition(SpriteRenderer targetRenderer, RectTransform popupLayer)
        {
            var worldPosition = ResolveDamageNumberWorldPosition(targetRenderer);
            var canvas = popupLayer != null ? popupLayer.GetComponentInParent<Canvas>() : null;
            var worldCamera = Camera.main;
            if (worldCamera == null)
            {
                return new Vector2(worldPosition.x * 100f, worldPosition.y * 100f);
            }

            var screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera != null ? canvas.worldCamera : worldCamera
                : null;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                popupLayer,
                screenPoint,
                eventCamera,
                out var localPoint)
                ? localPoint
                : Vector2.zero;
        }

        private RectTransform ResolveDamageNumberPopupLayer()
        {
            if (damageNumberPopupLayer != null)
            {
                damageNumberPopupLayer.SetAsLastSibling();
                return damageNumberPopupLayer;
            }

            var canvas = GameObject.Find("CombatCanvas")?.GetComponent<Canvas>()
                ?? FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.transform.Find("DamageNumberPopupLayer") as RectTransform;
            if (existing != null)
            {
                damageNumberPopupLayer = existing;
                damageNumberPopupLayer.SetAsLastSibling();
                return damageNumberPopupLayer;
            }

            var layerObject = new GameObject("DamageNumberPopupLayer", typeof(RectTransform));
            layerObject.transform.SetParent(canvas.transform, false);
            damageNumberPopupLayer = layerObject.GetComponent<RectTransform>();
            damageNumberPopupLayer.anchorMin = Vector2.zero;
            damageNumberPopupLayer.anchorMax = Vector2.one;
            damageNumberPopupLayer.offsetMin = Vector2.zero;
            damageNumberPopupLayer.offsetMax = Vector2.zero;
            damageNumberPopupLayer.SetAsLastSibling();
            return damageNumberPopupLayer;
        }

        private IEnumerator DamageNumberPopupRoutine(Transform popupTransform, TMP_Text label)
        {
            var startPosition = popupTransform.localPosition;
            var startTime = Time.realtimeSinceStartup;

            while (true)
            {
                if (popupTransform == null || label == null)
                {
                    yield break;
                }

                var elapsed = Time.realtimeSinceStartup - startTime;
                var t = Mathf.Clamp01(elapsed / DamageNumberPopupDurationSeconds);
                popupTransform.localPosition = startPosition + Vector3.up * Mathf.SmoothStep(0f, DamageNumberPopupRiseDistance, t);

                var pop = t < 0.24f
                    ? Mathf.Lerp(0.92f, 1.16f, Mathf.Clamp01(t / 0.24f))
                    : Mathf.Lerp(1.16f, 1f, Mathf.Clamp01((t - 0.24f) / 0.26f));
                popupTransform.localScale = Vector3.one * pop;

                var color = label.color;
                color.a = t < 0.62f ? 1f : Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.62f) / 0.38f));
                label.color = color;

                if (t >= 1f)
                {
                    break;
                }

                yield return null;
            }

            var popupObject = popupTransform.gameObject;
            damageNumberPopups.Remove(popupObject);
            Destroy(popupObject);
        }

        private void ClearDamageNumberPopups()
        {
            foreach (var popup in damageNumberPopups)
            {
                if (popup == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(popup);
                }
                else
                {
                    DestroyImmediate(popup);
                }
            }

            damageNumberPopups.Clear();
        }

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

        private void SpawnParticleBurst(
            CombatParticleEffectBinding effect,
            Transform anchor,
            string fallbackObjectName,
            ParticleSystem fallbackPrefab,
            Color fallbackColor,
            Material fallbackMaterial,
            float fallbackLifetimeSeconds,
            int fallbackBurstCount,
            float fallbackStartSpeed,
            float fallbackStartSize,
            bool swirl)
        {
            var prefab = effect?.particlePrefab != null ? effect.particlePrefab : fallbackPrefab;
            var material = effect?.particleMaterial != null ? effect.particleMaterial : fallbackMaterial;
            var color = effect != null ? effect.ResolveColor(fallbackColor) : fallbackColor;
            if (material != null)
            {
                color = Color.white;
            }

            var objectName = effect?.ResolveObjectName(fallbackObjectName) ?? fallbackObjectName;
            var lifetimeSeconds = effect != null ? effect.EffectiveLifetimeSeconds : fallbackLifetimeSeconds;
            var burstCount = effect != null ? effect.EffectiveBurstCount : fallbackBurstCount;
            var startSpeed = effect != null ? effect.EffectiveStartSpeed : fallbackStartSpeed;
            var startSize = effect != null ? effect.EffectiveStartSize : fallbackStartSize;
            var shouldSwirl = effect != null ? effect.swirl : swirl;

            SpawnParticleBurst(
                prefab,
                anchor,
                objectName,
                color,
                material,
                lifetimeSeconds,
                burstCount,
                startSpeed,
                startSize,
                shouldSwirl);
        }

        private void SpawnParticleBurst(
            ParticleSystem prefab,
            Transform anchor,
            string objectName,
            Color color,
            Material material,
            float lifetimeSeconds,
            int burstCount,
            float startSpeed,
            float startSize,
            bool swirl)
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
            ConfigureParticleBurst(particles, color, material, lifetimeSeconds, burstCount, startSpeed, startSize, parent, swirl);
            particles.Play(true);
            if (swirl && Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(SwirlParticleTransformRoutine(particles.transform, lifetimeSeconds));
            }

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
            Material material,
            float lifetimeSeconds,
            int burstCount,
            float startSpeed,
            float startSize,
            Transform anchor,
            bool swirl)
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
            shape.radius = 0.36f;

            if (swirl)
            {
                ConfigureSwirlBurst(particles, lifetimeSeconds);
            }
            else
            {
                DisableSwirlBurst(particles);
            }

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

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

        private static void ConfigureSwirlBurst(ParticleSystem particles, float lifetimeSeconds)
        {
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.54f;
            shape.radiusThickness = 0.32f;
            shape.arc = 360f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = false;

            var rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 1.5f, Mathf.PI * 1.5f);

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(Mathf.Clamp01(0.32f / Mathf.Max(0.05f, lifetimeSeconds)), 1.18f),
                new Keyframe(1f, 0f)));
        }

        private static IEnumerator SwirlParticleTransformRoutine(Transform particleTransform, float lifetimeSeconds)
        {
            var elapsed = 0f;
            var duration = Mathf.Max(0.05f, lifetimeSeconds);
            while (elapsed < duration)
            {
                if (particleTransform == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                particleTransform.localRotation = Quaternion.Euler(0f, 0f, progress * 540f);
                yield return null;
            }
        }

        private static void DisableSwirlBurst(ParticleSystem particles)
        {
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = false;

            var rotation = particles.rotationOverLifetime;
            rotation.enabled = false;

            var size = particles.sizeOverLifetime;
            size.enabled = false;
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

        private CombatParticleEffectBinding ResolveShieldImpactParticleEffect()
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.shieldImpactEffect : null;
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

        private Material ResolveShieldImpactParticleMaterial()
        {
            return shieldImpactParticleMaterial != null
                ? shieldImpactParticleMaterial
                : runtimeShieldImpactParticleMaterial ??= CreateParticleMaterial(
                    "ShieldImpactParticleMaterial",
                    shieldImpactParticleColor);
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

        private static Material CreateParticleMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = materialName,
                renderQueue = (int)RenderQueue.Transparent,
            };
            ApplyParticleMaterialColor(material, color);
            return material;
        }

        private static void ApplyParticleMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_TintColor"))
            {
                material.SetColor("_TintColor", color);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private void DestroyRuntimeParticleMaterials()
        {
            DestroyRuntimeMaterial(ref runtimeShieldImpactParticleMaterial);
            DestroyRuntimeMaterial(ref runtimeFearDebuffParticleMaterial);
            DestroyRuntimeMaterial(ref runtimeDarknessDebuffParticleMaterial);
        }

        private static void DestroyRuntimeMaterial(ref Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }

            material = null;
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

        private void ResolveWorldVfxProfile()
        {
            if (worldVfxProfile == null)
            {
                worldVfxProfile = Resources.Load<CombatWorldVfxProfileSO>(DefaultWorldVfxProfileResourceName);
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

        private static int ResolvePlayerHpDamage(CombatSnapshot previous, CombatSnapshot next)
        {
            if (previous?.Player == null || next?.Player == null)
            {
                return 0;
            }

            return Mathf.Max(0, previous.Player.CurrentHp - next.Player.CurrentHp);
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

        private static int ResolveEnemyHpDamage(CombatSnapshot previous, CombatSnapshot next)
        {
            var previousEnemy = previous?.Enemies?.FirstOrDefault();
            var nextEnemy = next?.Enemies?.FirstOrDefault();
            if (previousEnemy == null || nextEnemy == null)
            {
                return 0;
            }

            return Mathf.Max(0, previousEnemy.CurrentHp - nextEnemy.CurrentHp);
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
