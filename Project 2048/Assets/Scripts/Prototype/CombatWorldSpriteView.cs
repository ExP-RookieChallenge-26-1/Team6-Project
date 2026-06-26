using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project2048.Audio;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Presentation;
using Project2048.Rewards;
using Project2048.Skills;
using Project2048.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace Project2048.Prototype
{
    public partial class CombatWorldSpriteView : MonoBehaviour
    {
        private enum DarkShackleChainState
        {
            Flying,
            Latched,
            Ending,
        }

        public const float ShieldImpactParticleLifetimeSeconds = 0.8f;
        public const float ShieldBashDurationSeconds = 0.58f;
        public const float ShieldBurstSkillDurationSeconds = 0.72f;
        public const float DamageNumberPopupDurationSeconds = 0.55f;
        public const float ChargedLightBeamDurationSeconds = 0.65f;
        public const float SkillDamageDelaySeconds = 0.1f;
        public const float ChargedAttackReleaseBoardDelaySeconds = CombatUiView.BoardToActionPanelDelaySeconds;
        public const float GatherLightPreviewReleaseDelaySeconds = 2f;
        public const float GatherLightVerticalBeamLifetimeSeconds = 0.95f;
        public const float TentacleStrikeDurationSeconds = 0.58f;
        public const float HeavyStrikeSpikedBurstDurationSeconds = 0.62f;
        public const float SlashBeamDurationSeconds = 0.62f;
        public const float BloodFountainSlashDurationSeconds = 0.72f;
        public const float FlameBurstDurationSeconds = 0.82f;
        public const float DarkShackleChainDurationSeconds = 0.84f;

        private const float DefinitionCueFallbackLifetimeSeconds = 0.8f;
        private const float GatherLightVerticalBeamTargetYOffset = -0.7f;
        private const int ShieldImpactParticleCount = 22;
        private const float ReusableSkillParticleMaxStartSize = 0.24f;
        private const int ShieldCircleRingSegmentCount = 72;
        private const float ShieldCircleBaseRadius = 0.78f;
        private const float ShieldCircleBaseYOffset = 0.08f;
        private const float ShieldCircleRadiusMultiplier = 1.2f;
        private const float ThornGuardShieldRadiusMultiplier = 0.82f;
        private const float AttackArtBaseRadius = 0.62f;
        private const float AttackArtDiameterMultiplier = 2.45f;
        private const float AttackArtLifetimeSeconds = 0.34f;
        private const float AttackEffectArtSizeMultiplier = 3f;
        private const float HitEffectArtSizeMultiplier = 3f;
        private const float MagicCircleArtSizeMultiplier = 2f;
        private const float PersistentShieldArtRadius = 0.86f * ShieldCircleRadiusMultiplier;
        private const float ShieldArtDiameterMultiplier = 2.62f;
        private const float ShieldArtImpactLifetimeSeconds = 0.28f;
        private const float ShieldArtLeftOffsetX = -0.12f;
        private const int ShieldArtFrontSortingOffset = 12;
        private const float ThornGuardShieldFollowSharpness = 30f;
        private const int HeavyStrikeStarSegmentCount = 28;
        private const int HeavyStrikeSpikeRayCount = 12;
        private const float HeavyStrikeImpactScaleMultiplier = 0.4f;
        private const int BloodSlashSegmentCount = 18;
        private const float SlashBeamWidthMultiplier = 1.14f;
        private const float SlashBeamHeightMultiplier = 0.46f;
        private const float SlashAttackArtScaleMultiplier = 0.5f;
        private const float SlashBeamSourceLift = 0.12f;
        private const int DarkShackleChainSegmentCount = 16;
        private const int DarkShackleMinChainLinkCount = 8;
        private const int DarkShackleMaxChainLinkCount = 24;
        private const int DarkShackleRingSegmentCount = 18;
        private const float DarkShackleChainFlySpeed = 8f;
        private const float DarkShackleMaxFlySeconds = 0.42f;
        private const float DarkShackleLatchSeconds = 0.42f;
        private const float DarkShackleFadeSeconds = 0.16f;
        private const float DarkShackleInitialExtension = 0.1f;
        private const float DarkShackleImpactDustLifetimeSeconds = 0.34f;
        private const float DarkShackleLinkSpacing = 0.18f;
        private const float DarkShackleBoundChainsRadiusMultiplier = 1.536f;
        private static readonly Vector3 DarkShackleBoundChainsLocalOffset = new(0f, 0.08f, 0f);
        private const int FlameBurstTongueCount = 5;
        private const int FlameBurstTongueSegmentCount = 12;
        private const float LanternMuzzleLocalX = 0.34f;
        private const float LanternMuzzleLocalY = 0.36f;
        // 버프류 마법진이 너무 낮게 깔려서 플레이어 옆 허리 높이로 올린다.
        private const float SelfBuffMagicCircleYOffset = 0.18f;
        private const float SelfBuffParticleLift = 0.18f;
        private const float CloseRangeAttackAnimationSpeedMultiplier = 1.55f;
        private const float PlayerAttackAnimationSpeedResetSeconds = 0.42f;
        private const string DefaultWorldVfxProfileResourceName = "PrototypeCombatWorldVfxProfile";
        private const string LayeredPlayerActorRootName = "player_all";
        private const string LayeredPlayerBodyRendererName = "Body";
        private static readonly int PlayerAttackStateHash = Animator.StringToHash("Attack");
        private static readonly int PlayerAttackedStateHash = Animator.StringToHash("Attacked");
        private static readonly Vector3 ShieldArtLeftLocalOffset = new(ShieldArtLeftOffsetX, 0f, 0f);
        private static readonly Vector3 PlayerFrontAttackArtLocalOffset = new(0.68f, 0.16f, 0f);
        private static readonly Vector3 PlayerRightMagicCircleLocalOffset = new(0.76f, 0.1f, 0f);
        private static readonly Vector3 FlameBurstExplosionLocalOffset = new(0f, 0.18f, 0f);
        private static readonly Vector3 SupportBuffHealingVisualEffectLocalOffset = new(0f, 0.24f, 0f);
        private static readonly string[] SupportBuffVisualEffectColorPropertyNames =
        {
            "_Color",
            "Color",
            "BaseColor",
            "Tint",
            "TintColor",
            "ParticleColor",
        };
        private static readonly string[] GatherLightVerticalBeamColorPropertyNames =
        {
            "_Color",
            "Color",
            "BaseColor",
            "Tint",
            "TintColor",
            "_FresnelColor",
            "FresnelColor",
        };
        private static readonly Color ShieldCircleLightTint = new(1f, 0.96f, 0.72f, 0.94f);
        private static readonly Color ThornGuardShadowTint = new(0.025f, 0.035f, 0.03f, 0.96f);
        private static readonly Color ThornGuardBloodTint = new(0.44f, 0.07f, 0.08f, 0.9f);

        [SerializeField] private PrototypeCombatBootstrap bootstrap;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private Transform playerActorRoot;
        [SerializeField] private SpriteRenderer playerRenderer;
        [SerializeField] private SpriteRenderer enemyRenderer;
        [SerializeField] private Sprite defaultBackgroundSprite;
        [SerializeField] private Sprite upperStageBackgroundSprite;
        [SerializeField] private Sprite middleStageBackgroundSprite;
        [SerializeField] private Sprite lowerStageBackgroundSprite;
        [SerializeField] private SpriteRenderer rewardMothRenderer;
        [SerializeField] private Sprite attackEffectSprite;
        [SerializeField] private Sprite hitEffectSprite;
        [SerializeField] private Sprite shieldEffectSprite;
        [SerializeField] private Sprite thornShieldEffectSprite;
        [SerializeField] private Sprite magicCircleEffectSprite;
        [SerializeField] private Sprite flameEffectSprite;
        [SerializeField] private Sprite chainAttackEffectSprite;
        [SerializeField] private Sprite boundChainsEffectSprite;
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
        [SerializeField] private CombatClawSlash2DEffect enemyClawSlashEffectPrefab;
        [SerializeField] private bool playEnemyClawSlashEffect = true;
        [SerializeField] private Vector3 enemyClawSlashLocalOffset = new(0.72f, 0.62f, 0f);
        [SerializeField] private Vector3 enemyClawSlashLocalEulerAngles = new(0f, 0f, 90f);
        [SerializeField, Min(0.01f)] private float enemyClawSlashScale = 1f;

        private CombatManager combatManager;
        private RewardManager rewardManager;
        private StageSO currentStage;
        private CombatSnapshot snapshot;
        private bool showingRewardPresenter;
        private Coroutine rewardPresenterDelayCoroutine;
        private GameObject activeEnemyBattleActor;
        private EnemySO activeEnemyBattleActorData;
        private Material runtimeShieldImpactParticleMaterial;
        private Material runtimeChargedLightBeamMaterial;
        private FollowingShieldVfx activePlayerShieldArtVfx;
        private FollowingShieldVfx activePlayerThornGuardVfx;
        private bool playerShieldStyleLockedToThornGuard;
        private Coroutine playerAttackAnimationSpeedCoroutine;
        private Coroutine gatherLightPreviewReleaseCoroutine;
        private float playerAttackAnimationSpeedRestoreValue = 1f;
        private bool hasPlayerAttackAnimationSpeedRestoreValue;
        private int lastPlayedDamagePopupSequence;
        private readonly System.Collections.Generic.Dictionary<string, Material> runtimeSkillParticleMaterials = new();

        public void Initialize(PrototypeCombatBootstrap owner)
        {
            bootstrap = owner;
            UnbindCombatEvents();
            UnbindRewardEvents();
            combatManager = owner != null ? owner.CombatManager : null;
            rewardManager = owner != null ? owner.RewardManager : null;

            ResolveMissingReferences();
            ResolveWorldVfxProfile();
            ResolveWorldShake();
            CacheEnemyRendererRestTransform();
            HideRewardPresenter();
            RenderBackground();
            playerShieldStyleLockedToThornGuard = false;
            ClearActivePlayerShieldArtVfx();
            ClearActivePlayerThornGuardVfx();
            BindRewardEvents();

            if (combatManager == null)
            {
                return;
            }

            combatManager.PendingChargedAttackReleaseDelaySeconds = ChargedAttackReleaseBoardDelaySeconds;
            combatManager.OnCombatStateChanged -= HandleCombatStateChanged;
            combatManager.OnCombatStateChanged += HandleCombatStateChanged;
            combatManager.OnPlayerSkillUsed -= HandlePlayerSkillUsed;
            combatManager.OnPlayerSkillUsed += HandlePlayerSkillUsed;
            combatManager.OnPlayerChargedAttackReleased -= HandlePlayerChargedAttackReleased;
            combatManager.OnPlayerChargedAttackReleased += HandlePlayerChargedAttackReleased;

            snapshot = combatManager.GetSnapshot();
            lastEnemyWasDead = snapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false;
            lastPlayedEnemyDebuffVfxSequence = 0;
            lastPlayedDamagePopupSequence = 0;
            delayEnemyDeathFadeUntilRealtime = 0f;
            Render(snapshot);
            UpdatePlayerShieldArtVfx(snapshot?.Player);
            PlayEnemyIdleAnimationIfNeeded();
            SetEnemyRendererAlpha(lastEnemyWasDead ? 0f : 1f);
        }

        public void SetStage(StageSO stage)
        {
            currentStage = stage;
            HideRewardPresenter();
            RenderBackground();
        }

        private void OnDestroy()
        {
            UnbindCombatEvents();
            UnbindRewardEvents();
            ClearEnemyDeathFade();
            ClearEnemyAppearIntro();
            ClearEnemyAttackLunge();
            ClearActiveEnemyBattleActor();
            ClearPlayerCloseRangeAttackLunge();
            ClearPlayerAttackAnimationSpeed();
            ClearEnemyDirectAnimation();
            ClearGatherLightPreviewRelease();
            ClearWorldShake();
            ClearDamageNumberPopups();
            ClearActivePlayerShieldArtVfx();
            ClearActivePlayerThornGuardVfx();
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
            combatManager.OnPlayerChargedAttackReleased -= HandlePlayerChargedAttackReleased;
        }

        private void BindRewardEvents()
        {
            if (rewardManager == null)
            {
                return;
            }

            rewardManager.OnRewardChoicesOffered -= HandleRewardChoicesOffered;
            rewardManager.OnRewardChoicesOffered += HandleRewardChoicesOffered;
            rewardManager.OnRewardClaimed -= HandleRewardClaimed;
            rewardManager.OnRewardClaimed += HandleRewardClaimed;
        }

        private void UnbindRewardEvents()
        {
            if (rewardManager == null)
            {
                return;
            }

            rewardManager.OnRewardChoicesOffered -= HandleRewardChoicesOffered;
            rewardManager.OnRewardClaimed -= HandleRewardClaimed;
        }

        private void HandleRewardChoicesOffered(IReadOnlyList<BattleRewardSO> choices)
        {
            if (choices == null || choices.Count == 0)
            {
                HideRewardPresenter();
                return;
            }

            ShowRewardPresenter();
        }

        private void HandleRewardClaimed(RewardChoiceResult result)
        {
            HideRewardPresenter();
            if (enemyRenderer != null && (snapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false))
            {
                enemyRenderer.sprite = ResolveEnemySprite(snapshot);
                SetEnemyRendererAlpha(0f);
                return;
            }

            Render(snapshot);
        }

        private void HandleCombatStateChanged(CombatSnapshot nextSnapshot)
        {
            var playerHpDamage = ResolvePlayerHpDamage(snapshot, nextSnapshot);
            var enemyHpDamage = ResolveEnemyHpDamage(snapshot, nextSnapshot);
            var enemyDamagePopupCue = ResolveEnemyDamagePopupCue(nextSnapshot);
            var enemyPopupDamage = enemyDamagePopupCue != null ? enemyDamagePopupCue.Amount : enemyHpDamage;
            var enemyPopupIsCritical = enemyDamagePopupCue?.IsCritical ?? false;
            var playerWasHit = PlayerWasHit(snapshot, nextSnapshot);
            var enemyWasHit = EnemyWasHit(snapshot, nextSnapshot);
            var playerShieldWasHit = PlayerShieldWasHit(snapshot, nextSnapshot);
            var enemyUsedAttack = EnemyUsedAttack(snapshot, nextSnapshot, playerWasHit || playerShieldWasHit);
            var enemyUsedDefense = EnemyUsedDefense(snapshot, nextSnapshot);
            var enemyAttackIntent = ResolveEnemyUsedIntent(nextSnapshot, EnemyIntentType.Attack);
            var enemyDefenseIntent = ResolveEnemyUsedIntent(nextSnapshot, EnemyIntentType.Defense);
            var enemyAppeared = EnemyAppeared(snapshot, nextSnapshot);
            var nextEnemyDead = nextSnapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false;
            var enemyJustDied = !lastEnemyWasDead && nextEnemyDead;

            snapshot = nextSnapshot;
            Render(snapshot);
            PlayEnemyIdleAnimationIfNeeded();
            PlayPlayerThornGuardHitPulseIfNeeded(playerShieldWasHit);
            UpdatePlayerThornGuardVfx(nextSnapshot?.Player);
            PlayEnemyAppearEffectIfNeeded(enemyAppeared);
            PlayEnemyAttackEffectIfNeeded(enemyUsedAttack, enemyAttackIntent);
            UpdatePlayerShieldArtVfx(nextSnapshot?.Player);
            PlayPlayerActionEffectIfNeeded(playerWasHit);
            PlayEnemyActionEffectIfNeeded(enemyWasHit, enemyJustDied);
            PlayDamageNumberPopupIfNeeded(playerHpDamage, playerRenderer);
            PlayDamageNumberPopupIfNeeded(enemyPopupDamage, enemyRenderer, enemyPopupIsCritical);
            PlayEnemyDebuffCastEffectIfNeeded(snapshot?.LastVfxCue);
            PlayEnemyDefenseEffectIfNeeded(enemyUsedDefense, enemyDefenseIntent);
            PlayEnemyDeathFadeIfNeeded(enemyJustDied, nextEnemyDead);
            lastEnemyWasDead = nextEnemyDead;
        }

        private void HandlePlayerSkillUsed(SkillSO skill, EnemyController target)
        {
            if (skill == null)
            {
                return;
            }

            ResolveMissingReferences();
            ResolveWorldVfxProfile();
            var lifetimeSeconds = PlaySkillPresentationEffect(skill, delayEnemyDeathFade: true);
            combatManager?.BeginSkillPresentationLock(lifetimeSeconds, SkillDamageDelaySeconds);
        }

        public void PreviewSkillEffect(SkillSO skill)
        {
            if (skill == null)
            {
                return;
            }

            ResolveMissingReferences();
            ResolveWorldVfxProfile();
            PlaySkillPresentationEffect(skill, delayEnemyDeathFade: false, previewChargeRelease: true);
        }

        private float PlaySkillPresentationEffect(SkillSO skill, bool delayEnemyDeathFade, bool previewChargeRelease = false)
        {
            var sourceAnchor = ResolvePlayerAnchor() ?? transform;
            var targetAnchor = enemyRenderer != null ? enemyRenderer.transform : sourceAnchor;
            return PlaySkillPresentationEffect(
                skill,
                delayEnemyDeathFade,
                previewChargeRelease,
                sourceAnchor,
                targetAnchor,
                isEnemyCaster: false);
        }

        private float PlayEnemySkillPresentationEffect(SkillSO skill, bool isAttack)
        {
            if (skill == null)
            {
                return 0f;
            }

            var sourceAnchor = enemyRenderer != null ? enemyRenderer.transform : transform;
            var targetAnchor = ResolvePlayerAnchor() ?? transform;
            return PlaySkillPresentationEffect(
                skill,
                delayEnemyDeathFade: false,
                previewChargeRelease: false,
                sourceAnchor,
                targetAnchor,
                isEnemyCaster: true);
        }

        private float PlaySkillPresentationEffect(
            SkillSO skill,
            bool delayEnemyDeathFade,
            bool previewChargeRelease,
            Transform sourceAnchor,
            Transform targetAnchor,
            bool isEnemyCaster)
        {
            var effect = skill.activationEffect;
            var isAttack = skill.skillType == SkillType.Attack;
            var isChargeAttack = IsChargeAttackSkill(skill);
            sourceAnchor = sourceAnchor != null ? sourceAnchor : transform;
            targetAnchor = targetAnchor != null ? targetAnchor : sourceAnchor;
            var animator = isEnemyCaster ? enemyAnimator : isAttack ? enemyAnimator : playerAnimator;
            var family = ResolveSkillVfxFamily(skill);
            if (!isEnemyCaster)
            {
                PlayPlayerAttackAnimationIfNeeded(skill, family);
            }

            if (isChargeAttack)
            {
                if (TryPlayDefinitionCues(skill, SkillVfxTrigger.ChargeStart, sourceAnchor, targetAnchor))
                {
                    PlayCombatantActionAudioEffect(effect);
                }
                else
                {
                    PlayChargeAttackStartEffect(skill, effect, sourceAnchor);
                }

                if (previewChargeRelease)
                {
                    PreviewGatherLightReleaseIfNeeded(skill, targetAnchor);
                }

                return Mathf.Max(
                    ResolveSkillEffectVisualDurationSeconds(skill, effect),
                    ResolveDefinitionCueDurationSeconds(skill, SkillVfxTrigger.ChargeStart));
            }

            if (TryPlayDefinitionCues(skill, SkillVfxTrigger.Activate, sourceAnchor, targetAnchor))
            {
                PlayCombatantActionAudioEffect(effect);
                if (delayEnemyDeathFade)
                {
                    DelayEnemyDeathFadeForSkillEffect(skill, effect);
                }

                return Mathf.Max(
                    ResolveSkillEffectVisualDurationSeconds(skill, effect),
                    ResolveDefinitionCueDurationSeconds(skill, SkillVfxTrigger.Activate));
            }

            if ((effect == null || !effect.HasAnyAsset) && family == SkillVfxFamily.None)
            {
                return 0f;
            }

            if (isAttack && TryPlayCloseRangePlayerAttackSkillEffect(
                skill,
                effect,
                sourceAnchor,
                targetAnchor,
                playAttackAnimation: !isEnemyCaster,
                out var closeRangeLifetimeSeconds))
            {
                if (delayEnemyDeathFade)
                {
                    DelayEnemyDeathFade(closeRangeLifetimeSeconds);
                }

                return closeRangeLifetimeSeconds;
            }

            if (isAttack && TryPlayDirectedSkillEffect(skill, effect, sourceAnchor, targetAnchor, out var directedLifetimeSeconds))
            {
                if (delayEnemyDeathFade)
                {
                    DelayEnemyDeathFade(directedLifetimeSeconds);
                }

                return directedLifetimeSeconds;
            }

            if (isAttack && TryPlayProjectileSkillEffect(skill, effect, sourceAnchor, targetAnchor, animator, out var projectileLifetimeSeconds))
            {
                if (delayEnemyDeathFade)
                {
                    DelayEnemyDeathFade(projectileLifetimeSeconds);
                }

                return projectileLifetimeSeconds;
            }

            if (isAttack)
            {
                PlayLanternSkillLaunchCue(skill, sourceAnchor, targetAnchor);
            }

            var anchor = isAttack && enemyRenderer != null
                ? targetAnchor
                : sourceAnchor;
            var playReusableFamilyEffect = ShouldPlayReusableFamilyEffectFromSkillSo(skill, effect);
            if (effect?.HasAnyAsset == true)
            {
                if (playReusableFamilyEffect)
                {
                    PlayCombatantActionAudioEffect(effect);
                }
                else
                {
                    PlayCombatantActionEffect(effect, anchor, animator);
                }
            }

            if (playReusableFamilyEffect || effect?.HasAuthoredVisual != true)
            {
                PlayReusableSkillParticleEffect(skill, anchor, sourceAnchor, targetAnchor);
            }

            if (delayEnemyDeathFade)
            {
                DelayEnemyDeathFadeForSkillEffect(skill, effect);
            }

            return ResolveSkillEffectVisualDurationSeconds(skill, effect);
        }

        private SkillVfxContext BuildSkillVfxContext(SkillVfxTrigger trigger)
        {
            // 플레이어 시전 컨텍스트: caster=플레이어, primaryTarget=적.
            var caster = ResolvePlayerAnchor() ?? transform;
            var primaryTarget = enemyRenderer != null ? enemyRenderer.transform : transform;
            return new SkillVfxContext(caster, primaryTarget, trigger);
        }

        private static SkillVfxContext BuildSkillVfxContext(
            SkillVfxTrigger trigger,
            Transform sourceAnchor,
            Transform targetAnchor)
        {
            return new SkillVfxContext(sourceAnchor, targetAnchor, trigger);
        }

        // 새 데이터 기반 VFX: 해당 트리거의 큐가 하나라도 재생됐으면 true(→ 기존 절차 경로 skip).
        // vfxDefinition이 비면 false → 기존 경로로 폴백(현재 화면 유지).
        private bool TryPlayDefinitionCues(SkillSO skill, SkillVfxTrigger trigger)
        {
            var ctx = BuildSkillVfxContext(trigger);
            return TryPlayDefinitionCues(skill, trigger, ctx);
        }

        private bool TryPlayDefinitionCues(
            SkillSO skill,
            SkillVfxTrigger trigger,
            Transform sourceAnchor,
            Transform targetAnchor)
        {
            var ctx = BuildSkillVfxContext(trigger, sourceAnchor, targetAnchor);
            return TryPlayDefinitionCues(skill, trigger, ctx);
        }

        private SkillVfxRunner skillVfxRunner;

        private SkillVfxRunner EnsureSkillVfxRunner()
        {
            if (skillVfxRunner == null)
            {
                skillVfxRunner = gameObject.GetComponent<SkillVfxRunner>()
                    ?? gameObject.AddComponent<SkillVfxRunner>();
            }

            return skillVfxRunner;
        }

        private bool TryPlayDefinitionCues(SkillSO skill, SkillVfxTrigger trigger, SkillVfxContext ctx)
        {
            if (skill == null)
            {
                return false;
            }

            // 큐 순회·지연·스폰·수명은 SkillVfxRunner가 전담한다. 뷰는 위임만 한다.
            return EnsureSkillVfxRunner().Play(skill.vfxDefinition, ctx, transform);
        }

        private static float ResolveDefinitionCueDurationSeconds(SkillSO skill, SkillVfxTrigger trigger)
        {
            if (skill == null || skill.vfxDefinition == null || !skill.vfxDefinition.HasAnyCue)
            {
                return 0f;
            }

            var duration = 0f;
            foreach (var cue in skill.vfxDefinition.CuesFor(trigger))
            {
                if (cue == null || !cue.HasPrefab)
                {
                    continue;
                }

                var lifetime = cue.lifetimeOverride > 0f
                    ? cue.lifetimeOverride
                    : ResolvePrefabVisualDurationSeconds(cue.prefab, DefinitionCueFallbackLifetimeSeconds);
                duration = Mathf.Max(duration, Mathf.Max(0f, cue.delaySeconds) + lifetime);
            }

            return duration;
        }

        private static float ResolvePrefabVisualDurationSeconds(GameObject prefab, float fallbackLifetimeSeconds)
        {
            if (prefab == null)
            {
                return fallbackLifetimeSeconds;
            }

            var duration = 0f;
            foreach (var particles in prefab.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particles == null)
                {
                    continue;
                }

                var main = particles.main;
                duration = Mathf.Max(duration, main.duration + main.startLifetime.constantMax);
            }

            foreach (var animator in prefab.GetComponentsInChildren<Animator>(true))
            {
                var controller = animator != null ? animator.runtimeAnimatorController : null;
                if (controller == null || controller.animationClips == null)
                {
                    continue;
                }

                foreach (var clip in controller.animationClips)
                {
                    if (clip != null)
                    {
                        duration = Mathf.Max(duration, clip.length);
                    }
                }
            }

            return duration > 0f ? duration : fallbackLifetimeSeconds;
        }

        private bool TryPlayDirectedSkillEffect(
            SkillSO skill,
            CombatEffectBinding effect,
            Transform sourceAnchor,
            Transform targetAnchor,
            out float lifetimeSeconds)
        {
            lifetimeSeconds = 0f;
            if (skill == null)
            {
                return false;
            }

            switch (ResolveSkillVfxFamily(skill))
            {
                case SkillVfxFamily.ShieldDome:
                    if (!IsShieldAttackSkill(skill))
                    {
                        return false;
                    }

                    PlayCombatantActionAudioEffect(effect);
                    PlayShieldAttackSkillEffect(skill, sourceAnchor, targetAnchor);
                    lifetimeSeconds = ResolveShieldAttackSkillDurationSeconds(skill);
                    return true;
                case SkillVfxFamily.LightBeam:
                    PlayCombatantActionAudioEffect(effect);
                    PlayChargedLightBeamEffect(targetAnchor);
                    PlayCombatantActionParticleEffect(effect, targetAnchor);
                    lifetimeSeconds = ChargedLightBeamDurationSeconds;
                    return true;
                case SkillVfxFamily.TentacleWhip:
                    if (!PlayTentacleStrikeSkillEffect(skill, sourceAnchor, targetAnchor))
                    {
                        return false;
                    }

                    PlayCombatantActionAudioEffect(effect);
                    lifetimeSeconds = TentacleStrikeDurationSeconds + HeavyStrikeSpikedBurstDurationSeconds;
                    return true;
                case SkillVfxFamily.SlashArc:
                    PlayCombatantActionAudioEffect(effect);
                    PlaySlashBeamSkillEffect(skill, sourceAnchor, targetAnchor);
                    lifetimeSeconds = SlashBeamDurationSeconds;
                    return true;
                case SkillVfxFamily.SpikedBurst:
                    PlayCombatantActionAudioEffect(effect);
                    PlaySpecializedSkillArtLayer(skill, targetAnchor, sourceAnchor);
                    PlaySpikedBurstSkillEffect(skill, targetAnchor, sourceAnchor);
                    PlayCombatantActionParticleEffect(effect, targetAnchor);
                    lifetimeSeconds = HeavyStrikeSpikedBurstDurationSeconds;
                    return true;
                case SkillVfxFamily.BloodFountainSlash:
                    PlayCombatantActionAudioEffect(effect);
                    PlaySlashBeamSkillEffect(skill, sourceAnchor, targetAnchor);
                    lifetimeSeconds = SlashBeamDurationSeconds;
                    return true;
                case SkillVfxFamily.DarkChainBurst:
                    PlayDarkShackleSkillEffect(skill, effect, sourceAnchor, targetAnchor);
                    lifetimeSeconds = DarkShackleChainDurationSeconds;
                    return true;
                default:
                    return false;
            }
        }

        private bool TryPlayProjectileSkillEffect(CombatEffectBinding effect, EnemyController target, out float lifetimeSeconds)
        {
            var sourceTransform = ResolvePlayerAnchor() ?? transform;
            var targetTransform = target != null && enemyRenderer != null ? enemyRenderer.transform : transform;
            return TryPlayProjectileSkillEffect(null, effect, sourceTransform, targetTransform, enemyAnimator, out lifetimeSeconds);
        }

        private bool TryPlayProjectileSkillEffect(
            SkillSO skill,
            CombatEffectBinding effect,
            Transform sourceTransform,
            Transform targetTransform,
            Animator animator,
            out float lifetimeSeconds)
        {
            lifetimeSeconds = 0f;
            if (effect?.vfxPrefab == null || sourceTransform == null || targetTransform == null)
            {
                return false;
            }

            var prefabProjectile = effect.vfxPrefab.GetComponentInChildren<CombatProjectileEffect>(true);
            if (prefabProjectile == null)
            {
                return false;
            }

            PlayCombatantActionAudioEffect(effect);

            var sourcePosition = ResolveLanternSkillSourcePosition(sourceTransform, targetTransform);
            var instance = Instantiate(effect.vfxPrefab, sourcePosition, Quaternion.identity, transform);
            var projectile = instance.GetComponentInChildren<CombatProjectileEffect>(true);
            projectile?.LaunchFromWorldPosition(sourcePosition, targetTransform, effect.localOffset);

            if (effect.animationClip != null && animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Play(effect.animationClip.name, 0, 0f);
            }

            var lifetime = projectile != null
                ? Mathf.Max(effect.EffectiveAutoDestroySeconds, projectile.EstimatedLifetimeSeconds + 0.2f)
                : effect.EffectiveAutoDestroySeconds;
            if (TryScheduleFlameBurstImpactExplosion(skill, targetTransform, projectile, out var explosionLifetimeSeconds))
            {
                lifetime = Mathf.Max(lifetime, explosionLifetimeSeconds);
            }

            lifetimeSeconds = lifetime;
            if (lifetime > 0f && Application.isPlaying)
            {
                Destroy(instance, lifetime);
            }

            return true;
        }

        private bool TryScheduleFlameBurstImpactExplosion(
            SkillSO skill,
            Transform targetTransform,
            CombatProjectileEffect projectile,
            out float lifetimeSeconds)
        {
            lifetimeSeconds = 0f;
            if (ResolveSkillVfxFamily(skill) != SkillVfxFamily.FlameBurst || targetTransform == null)
            {
                return false;
            }

            var explosionPrefab = ResolveFlameBurstExplosionPrefab(skill);
            if (explosionPrefab == null)
            {
                return false;
            }

            var explosionLifetimeSeconds = ResolveFlameBurstExplosionLifetimeSeconds(explosionPrefab);
            if (explosionLifetimeSeconds <= 0f)
            {
                return false;
            }

            var impactDelaySeconds = projectile != null ? projectile.TravelSeconds : 0f;
            lifetimeSeconds = impactDelaySeconds + explosionLifetimeSeconds;
            if (Application.isPlaying && impactDelaySeconds > 0f)
            {
                StartCoroutine(PlayFlameBurstImpactExplosionAfterDelay(explosionPrefab, targetTransform, impactDelaySeconds));
            }
            else
            {
                PlayFlameBurstImpactExplosion(explosionPrefab, targetTransform);
            }

            return true;
        }

        private bool TryPlayImmediateFlameBurstImpactExplosion(SkillSO skill, Transform targetTransform, out float lifetimeSeconds)
        {
            lifetimeSeconds = 0f;
            if (ResolveSkillVfxFamily(skill) != SkillVfxFamily.FlameBurst || targetTransform == null)
            {
                return false;
            }

            var explosionPrefab = ResolveFlameBurstExplosionPrefab(skill);
            if (explosionPrefab == null)
            {
                return false;
            }

            lifetimeSeconds = ResolveFlameBurstExplosionLifetimeSeconds(explosionPrefab);
            if (lifetimeSeconds <= 0f)
            {
                return false;
            }

            PlayFlameBurstImpactExplosion(explosionPrefab, targetTransform);
            return true;
        }

        private static GameObject ResolveFlameBurstExplosionPrefab(SkillSO skill)
        {
            var tuning = ResolveSkillVfxTuning(skill);
            if (tuning != null && tuning.secondaryPrefab != null)
            {
                return tuning.secondaryPrefab;
            }

            var package = ResolveSkillVfxPackage(skill);
            return package != null ? package.secondaryPrefab : null;
        }

        private static float ResolveFlameBurstExplosionLifetimeSeconds(GameObject explosionPrefab)
        {
            var prefabExplosion = explosionPrefab != null
                ? explosionPrefab.GetComponentInChildren<LayeredExplosionEffect>(true)
                : null;
            return prefabExplosion != null ? prefabExplosion.EstimatedLifetimeSeconds + 0.25f : 0f;
        }

        private IEnumerator PlayFlameBurstImpactExplosionAfterDelay(
            GameObject explosionPrefab,
            Transform targetTransform,
            float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            PlayFlameBurstImpactExplosion(explosionPrefab, targetTransform);
        }

        private void PlayFlameBurstImpactExplosion(GameObject explosionPrefab, Transform targetTransform)
        {
            if (explosionPrefab == null || targetTransform == null)
            {
                return;
            }

            var worldPosition = targetTransform.position + targetTransform.TransformVector(FlameBurstExplosionLocalOffset);
            var instance = Instantiate(explosionPrefab, worldPosition, Quaternion.identity, targetTransform);
            var explosion = instance.GetComponentInChildren<LayeredExplosionEffect>(true);
            if (explosion == null)
            {
                return;
            }

            var targetRenderer = targetTransform.GetComponent<SpriteRenderer>();
            explosion.ApplySorting(targetRenderer, 16);
            explosion.PlayAt(worldPosition);

            if (Application.isPlaying)
            {
                Destroy(instance, explosion.EstimatedLifetimeSeconds + 0.25f);
            }
        }

        private GameObject SpawnSkillPrefabVisualAtAnchor(
            GameObject prefab,
            Transform anchor,
            string objectName,
            Color tint,
            float scale,
            float lifetimeSeconds,
            Vector3 localOffset,
            int sortingOffset)
        {
            if (prefab == null || anchor == null)
            {
                return null;
            }

            var worldPosition = ResolveAnchorVisualCenterWorldPosition(anchor) + anchor.TransformVector(localOffset);
            var instance = Instantiate(prefab, worldPosition, Quaternion.identity, transform);
            instance.name = objectName;
            if (scale > 0f && !Mathf.Approximately(scale, 1f))
            {
                instance.transform.localScale *= scale;
            }

            SkillVfxPlayer.ApplyTint(instance, tint);

            var layeredExplosions = instance.GetComponentsInChildren<LayeredExplosionEffect>(true);
            if (layeredExplosions.Length > 0)
            {
                var sortingReference = anchor.GetComponent<SpriteRenderer>();
                foreach (var explosion in layeredExplosions)
                {
                    if (explosion == null)
                    {
                        continue;
                    }

                    explosion.ApplySorting(sortingReference, sortingOffset);
                    explosion.PlayAt(worldPosition);
                    lifetimeSeconds = Mathf.Max(lifetimeSeconds, explosion.EstimatedLifetimeSeconds + 0.25f);
                }
            }
            else
            {
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    ApplyAnchorSorting(renderer, anchor, sortingOffset);
                }

                foreach (var particles in instance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    particles.Play(true);
                }

                foreach (var visualEffect in instance.GetComponentsInChildren<VisualEffect>(true))
                {
                    if (Application.isPlaying)
                    {
                        visualEffect.Reinit();
                    }

                    visualEffect.Play();
                }
            }

            if (Application.isPlaying && lifetimeSeconds > 0f)
            {
                Destroy(instance, lifetimeSeconds + 0.15f);
            }

            return instance;
        }

        private void DelayEnemyDeathFadeForSkillEffect(SkillSO skill, CombatEffectBinding effect)
        {
            if (skill == null || skill.skillType != SkillType.Attack)
            {
                return;
            }

            DelayEnemyDeathFade(ResolveSkillEffectVisualDurationSeconds(skill, effect));
        }

        private void DelayEnemyDeathFade(float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                return;
            }

            delayEnemyDeathFadeUntilRealtime = Mathf.Max(
                delayEnemyDeathFadeUntilRealtime,
                Time.realtimeSinceStartup + durationSeconds);
        }

        private void HandlePlayerChargedAttackReleased(string skillName, int chargedPower, int attackCount, EnemyController target)
        {
            ResolveMissingReferences();
            var targetTransform = target != null && enemyRenderer != null ? enemyRenderer.transform : transform;
            var chargedSkill = ResolvePlayerChargedLightSkill(skillName);
            var releaseCount = Mathf.Max(1, attackCount);
            var releaseIntervalSeconds = ResolveChargedAttackReleaseEffectDurationSeconds(chargedSkill);
            if (Application.isPlaying && isActiveAndEnabled && releaseCount > 1)
            {
                StartCoroutine(PlayChargedAttackReleaseRepeatsRoutine(chargedSkill, targetTransform, releaseCount, releaseIntervalSeconds));
            }
            else
            {
                for (var index = 0; index < releaseCount; index++)
                {
                    PlayChargedAttackReleaseEffect(chargedSkill, targetTransform);
                }
            }

            var totalDurationSeconds = releaseIntervalSeconds * releaseCount;
            DelayEnemyDeathFade(totalDurationSeconds);
            combatManager?.BeginSkillPresentationLock(totalDurationSeconds);
        }

        private static float ResolveChargedAttackReleaseEffectDurationSeconds(SkillSO chargedSkill)
        {
            var fallbackDurationSeconds = Mathf.Max(ChargedLightBeamDurationSeconds, GatherLightVerticalBeamLifetimeSeconds);
            if (IsGatherLightSkill(chargedSkill))
            {
                return Mathf.Max(
                    fallbackDurationSeconds,
                    ResolveGatherLightProjectileTravelSeconds(chargedSkill) + GatherLightVerticalBeamLifetimeSeconds);
            }

            return Mathf.Max(
                fallbackDurationSeconds,
                Mathf.Max(
                    ResolveDefinitionCueDurationSeconds(chargedSkill, SkillVfxTrigger.ChargeRelease),
                    ResolveSkillEffectVisualDurationSeconds(chargedSkill, chargedSkill != null ? chargedSkill.activationEffect : null)));
        }

        private IEnumerator PlayChargedAttackReleaseRepeatsRoutine(
            SkillSO chargedSkill,
            Transform targetTransform,
            int releaseCount,
            float releaseIntervalSeconds)
        {
            for (var index = 0; index < releaseCount; index++)
            {
                PlayChargedAttackReleaseEffect(chargedSkill, targetTransform);
                if (index < releaseCount - 1)
                {
                    yield return new WaitForSeconds(Mathf.Max(0f, releaseIntervalSeconds));
                }
            }
        }

        private void PlayChargedAttackReleaseEffect(SkillSO chargedSkill, Transform targetTransform)
        {
            if (IsGatherLightSkill(chargedSkill))
            {
                PlayGatherLightReleasedAttackEffect(chargedSkill, targetTransform, playAttackAnimation: true);
            }
            else if (!TryPlayDefinitionCues(chargedSkill, SkillVfxTrigger.ChargeRelease))
            {
                PlayGatherLightReleasedAttackEffect(chargedSkill, targetTransform, playAttackAnimation: true);
            }
        }

        private void Render(CombatSnapshot currentSnapshot)
        {
            RenderBackground();

            if (ShouldAssignPlayerRendererSprite())
            {
                playerRenderer.sprite = combatManager?.Player?.Data?.portrait;
            }

            if (enemyRenderer == null)
            {
                return;
            }

            if (TryRenderRewardPresenter())
            {
                return;
            }

            if (!TryRenderEnemyBattleActor(currentSnapshot))
            {
                enemyRenderer.enabled = true;
                enemyRenderer.sprite = ResolveEnemySprite(currentSnapshot);
            }

            AlignActiveEnemyFeetToGround();

            var enemyIsAlive = !(currentSnapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false);
            if (enemyIsAlive && enemyDeathFadeCoroutine == null && enemyAppearIntroCoroutine == null)
            {
                SetEnemyRendererAlpha(1f);
            }
        }

        private void ShowRewardPresenter()
        {
            ResolveMissingReferences();
            if (rewardMothRenderer == null)
            {
                return;
            }

            if (ShouldDelayRewardPresenterUntilEnemyHidden())
            {
                if (rewardPresenterDelayCoroutine != null)
                {
                    StopCoroutine(rewardPresenterDelayCoroutine);
                }

                rewardPresenterDelayCoroutine = StartCoroutine(ShowRewardPresenterAfterEnemyHiddenRoutine());
                return;
            }

            ShowRewardPresenterNow();
        }

        private bool ShouldDelayRewardPresenterUntilEnemyHidden()
        {
            return Application.isPlaying &&
                   isActiveAndEnabled &&
                   enemyRenderer != null &&
                   (snapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false) &&
                   enemyRenderer.color.a > 0.001f;
        }

        private IEnumerator ShowRewardPresenterAfterEnemyHiddenRoutine()
        {
            while (enemyDeathFadeDelayCoroutine != null || enemyDeathFadeCoroutine != null)
            {
                yield return null;
            }

            rewardPresenterDelayCoroutine = null;
            if (rewardMothRenderer == null ||
                rewardManager?.HasUnclaimedReward != true ||
                !(snapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false))
            {
                yield break;
            }

            ShowRewardPresenterNow();
        }

        private void ShowRewardPresenterNow()
        {
            showingRewardPresenter = true;
            if (enemyRenderer == null)
            {
                return;
            }

            CacheEnemyRendererRestTransform();
            ClearEnemyDeathFade();
            ClearEnemyAppearIntro(restoreTransform: false);
            ClearEnemyAttackLunge(restoreTransform: false);
            ClearEnemyDirectAnimation(restoreCurrentSprite: false);
            delayEnemyDeathFadeUntilRealtime = 0f;
            ClearActiveEnemyBattleActor();
            if (enemyRenderer != null)
            {
                SetEnemyRendererAlpha(0f);
            }

            TryRenderRewardPresenter();
        }

        private void HideRewardPresenter()
        {
            if (rewardPresenterDelayCoroutine != null)
            {
                StopCoroutine(rewardPresenterDelayCoroutine);
                rewardPresenterDelayCoroutine = null;
            }

            showingRewardPresenter = false;
            SetRewardMothVisible(false);
            RestoreEnemyRendererTransform();
        }

        private bool TryRenderRewardPresenter()
        {
            if (!showingRewardPresenter || rewardMothRenderer == null)
            {
                return false;
            }

            SetRewardMothVisible(true);
            return true;
        }

        private void SetRewardMothVisible(bool visible)
        {
            if (rewardMothRenderer == null)
            {
                return;
            }

            rewardMothRenderer.gameObject.SetActive(visible);
            rewardMothRenderer.enabled = visible;
            if (visible)
            {
                rewardMothRenderer.color = Color.white;
            }
        }

        private bool TryRenderEnemyBattleActor(CombatSnapshot currentSnapshot)
        {
            var enemyData = ResolveEnemyData(currentSnapshot);
            if (enemyData == null || enemyData.BattlePrefab == null)
            {
                ClearActiveEnemyBattleActor();
                return false;
            }

            if (activeEnemyBattleActorData != enemyData || activeEnemyBattleActor == null)
            {
                ClearActiveEnemyBattleActor();
                activeEnemyBattleActor = Instantiate(enemyData.BattlePrefab, enemyRenderer.transform);
                activeEnemyBattleActor.name = enemyData.BattlePrefab.name;
                activeEnemyBattleActor.transform.localPosition = Vector3.zero;
                activeEnemyBattleActor.transform.localRotation = Quaternion.identity;
                activeEnemyBattleActor.transform.localScale = Vector3.one;
                activeEnemyBattleActorData = enemyData;
            }

            activeEnemyBattleActor.SetActive(true);
            enemyRenderer.sprite = null;
            enemyRenderer.enabled = false;
            enemyAnimator = activeEnemyBattleActor.GetComponentInChildren<Animator>(includeInactive: true);
            if (enemyAnimator == null)
            {
                enemyAnimator = activeEnemyBattleActor.AddComponent<Animator>();
            }

            return true;
        }

        private void ClearActiveEnemyBattleActor()
        {
            if (activeEnemyBattleActor != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(activeEnemyBattleActor);
                }
                else
                {
                    DestroyImmediate(activeEnemyBattleActor);
                }
            }

            activeEnemyBattleActor = null;
            activeEnemyBattleActorData = null;
            if (enemyRenderer != null)
            {
                enemyRenderer.enabled = true;
                enemyAnimator = enemyRenderer.GetComponent<Animator>();
            }
        }

        private void PlayPlayerActionEffectIfNeeded(bool playerWasHit)
        {
            if (!playerWasHit)
            {
                return;
            }

            PlayPlayerAttackedAnimation();
            PlayCombatantActionEffect(
                combatManager?.Player?.Data?.FindActionEffect(CombatActionIds.Hit),
                ResolvePlayerAnchor() ?? transform,
                playerAnimator);
        }

        private void PlayPlayerAttackedAnimation()
        {
            if (playerAnimator == null)
            {
                return;
            }

            if (playerAnimator.runtimeAnimatorController == null ||
                !playerAnimator.HasState(0, PlayerAttackedStateHash))
            {
                return;
            }

            playerAnimator.Play(PlayerAttackedStateHash, 0, 0f);
        }

        private void PlayEnemyAttackEffectIfNeeded(bool enemyUsedAttack, EnemyIntent enemyAttackIntent)
        {
            if (!enemyUsedAttack)
            {
                return;
            }

            var enemyData = ResolveCurrentEnemyData();
            PlayEnemyOneShotAnimation(enemyData?.attackAnimation, returnToIdle: true);
            var skill = ResolveEnemySkillForIntent(enemyAttackIntent);
            if (skill != null && PlayEnemySkillPresentationEffect(skill, isAttack: true) > 0f)
            {
                return;
            }

            PlayEnemyAttackLunge(enemyData?.FindActionEffect(CombatActionIds.Attack));
        }

        private void PlayEnemyAppearEffectIfNeeded(bool enemyAppeared)
        {
            if (!enemyAppeared)
            {
                return;
            }

            var enemyData = ResolveCurrentEnemyData();
            PlayEnemyOneShotAnimation(enemyData?.appearAnimation, returnToIdle: true);
            PlayEnemyAppearIntro(enemyData?.FindActionEffect(CombatActionIds.Appear));
        }

        private void PlayEnemyActionEffectIfNeeded(bool enemyWasHit, bool enemyJustDied)
        {
            var enemyData = ResolveCurrentEnemyData();
            if (enemyJustDied)
            {
                var deathAnimationDuration = PlayEnemyOneShotAnimation(enemyData?.deathAnimation, returnToIdle: false);
                DelayEnemyDeathFade(deathAnimationDuration);
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

            PlayEnemyOneShotAnimation(enemyData?.hitAnimation, returnToIdle: true);
            PlayCombatantActionEffect(
                enemyData?.FindActionEffect(CombatActionIds.Hit),
                enemyRenderer != null ? enemyRenderer.transform : transform,
                enemyAnimator,
                delayAudioUntilAuthoredVisualEnds: true);
        }

        private void PlayEnemyDefenseEffectIfNeeded(bool enemyUsedDefense, EnemyIntent enemyDefenseIntent)
        {
            if (!enemyUsedDefense)
            {
                return;
            }

            var skill = ResolveEnemySkillForIntent(enemyDefenseIntent);
            if (skill != null && PlayEnemySkillPresentationEffect(skill, isAttack: false) > 0f)
            {
                return;
            }

            PlayCombatantActionEffect(
                ResolveCurrentEnemyData()?.FindActionEffect(CombatActionIds.Defend),
                enemyRenderer != null ? enemyRenderer.transform : transform,
                enemyAnimator);
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
            PlayShieldImpactArtPulse(anchor);
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

        private void PlayCombatantActionEffect(
            CombatEffectBinding effect,
            Transform anchor,
            Animator animator,
            bool delayAudioUntilAuthoredVisualEnds = false)
        {
            if (effect == null || !effect.HasAnyAsset)
            {
                return;
            }

            var audioDelay = delayAudioUntilAuthoredVisualEnds
                ? ResolveAuthoredVisualDurationSeconds(effect)
                : 0f;
            if (audioDelay <= 0f)
            {
                PlayCombatantActionAudioEffect(effect);
            }

            if (effect.vfxPrefab != null)
            {
                var parent = anchor != null ? anchor : transform;
                var instance = Instantiate(effect.vfxPrefab, parent.position, Quaternion.identity, parent);
                instance.transform.localPosition += effect.localOffset;
                var lifetime = effect.EffectiveAutoDestroySeconds;
                if (lifetime > 0f && Application.isPlaying)
                {
                    Destroy(instance, lifetime);
                }
            }

            if (effect.particleEffect?.HasParticleVisual == true)
            {
                PlayCombatantActionParticleEffect(effect, anchor);
            }

            if (effect.animationClip != null && animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Play(effect.animationClip.name, 0, 0f);
            }

            if (audioDelay > 0f)
            {
                PlayCombatantActionAudioEffect(effect, audioDelay);
            }
        }

        private void PlayCombatantActionParticleEffect(CombatEffectBinding effect, Transform anchor, Vector3 localOffset = default)
        {
            if (effect?.particleEffect?.HasParticleVisual != true)
            {
                return;
            }

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
                swirl: false,
                localOffset: localOffset);
        }

        private void PlayChargeAttackStartEffect(SkillSO skill, CombatEffectBinding effect, Transform sourceAnchor)
        {
            PlayCombatantActionAudioEffect(effect);
            if (IsGatherLightSkill(skill))
            {
                PlayGatherLightChargeBuffEffect(skill, effect, sourceAnchor);
                return;
            }

            // 충전 버프 이펙트는 캐릭터 발이 아니라 몸(스프라이트 시각적 중앙)에서 빛이 모이게 한다.
            var bodyLocalOffset = ResolveVisualCenterLocalOffset(sourceAnchor, Vector3.zero);
            if (effect?.particleEffect?.HasParticleVisual == true)
            {
                PlayCombatantActionParticleEffect(effect, sourceAnchor, bodyLocalOffset);
                return;
            }

            SpawnParticleBurst(
                null,
                sourceAnchor,
                $"{(skill != null && !string.IsNullOrWhiteSpace(skill.skillId) ? skill.skillId : "ChargeAttack")}ChargeParticles",
                new Color(0.86f, 0.96f, 1f, 0.78f),
                null,
                0.65f,
                28,
                0.28f,
                0.14f,
                swirl: true,
                bodyLocalOffset);
        }

        private void PlayGatherLightChargeBuffEffect(SkillSO skill, CombatEffectBinding effect, Transform sourceAnchor)
        {
            var anchor = sourceAnchor != null ? sourceAnchor : transform;
            var localOffset = ResolveVisualCenterLocalOffset(anchor, new Vector3(0f, SelfBuffParticleLift, 0f));
            SpawnParticleBurst(
                effect?.particleEffect,
                anchor,
                "GatherLightBuffParticles",
                null,
                skill != null ? ResolveReusableSkillParticleColor(skill) : new Color(0.86f, 0.96f, 1f, 0.78f),
                null,
                0.65f,
                42,
                0.47f,
                0.18f,
                swirl: true,
                localOffset);
        }

        private static bool ShouldPlayReusableFamilyEffectFromSkillSo(SkillSO skill, CombatEffectBinding effect)
        {
            return skill != null &&
                ResolveSkillVfxFamily(skill) != SkillVfxFamily.None &&
                effect?.particleEffect?.HasParticleVisual == true &&
                effect.vfxPrefab == null &&
                effect.animationClip == null;
        }

        private static float ResolveAuthoredVisualDurationSeconds(CombatEffectBinding effect)
        {
            if (effect == null || !effect.HasAuthoredVisual)
            {
                return 0f;
            }

            var duration = 0f;
            if (effect.vfxPrefab != null)
            {
                duration = Mathf.Max(duration, effect.EffectiveAutoDestroySeconds);
            }

            if (effect.particleEffect?.HasParticleVisual == true)
            {
                duration = Mathf.Max(duration, effect.particleEffect.EffectiveLifetimeSeconds);
            }

            if (effect.animationClip != null)
            {
                duration = Mathf.Max(duration, effect.animationClip.length);
            }

            return duration;
        }

        private static float ResolveSkillEffectVisualDurationSeconds(SkillSO skill, CombatEffectBinding effect)
        {
            var duration = effect?.HasAuthoredVisual == true
                ? ResolveAuthoredVisualDurationSeconds(effect)
                : 0f;
            var family = ResolveSkillVfxFamily(skill);
            if (family != SkillVfxFamily.None)
            {
                ResolveReusableSkillParticleDefaults(
                    family,
                    out var lifetimeSeconds,
                    out _,
                    out _,
                    out _,
                    out _);
                duration = Mathf.Max(duration, lifetimeSeconds);
            }

            if (family == SkillVfxFamily.FlameBurst)
            {
                duration = Mathf.Max(
                    duration,
                    ResolveFlameBurstExplosionLifetimeSeconds(ResolveFlameBurstExplosionPrefab(skill)));
            }

            return duration;
        }

        private void PlayReusableSkillParticleEffect(
            SkillSO skill,
            Transform anchor,
            Transform sourceAnchor = null,
            Transform facingTargetAnchor = null)
        {
            var family = ResolveSkillVfxFamily(skill);
            if (family == SkillVfxFamily.None)
            {
                return;
            }

            ResolveReusableSkillParticleDefaults(
                family,
                out var lifetimeSeconds,
                out var burstCount,
                out var startSpeed,
                out var startSize,
                out var swirl);

            var scale = Mathf.Max(0.01f, skill.vfxScale);
            var intensity = Mathf.Max(0.1f, skill.vfxIntensity);
            var repeatCount = Mathf.Max(1, skill.vfxRepeatCount);
            var scaledStartSize = Mathf.Min(startSize * scale, ReusableSkillParticleMaxStartSize);
            var resolvedSourceAnchor = sourceAnchor != null ? sourceAnchor : ResolvePlayerAnchor() ?? transform;
            var resolvedFacingTargetAnchor = facingTargetAnchor != null ? facingTargetAnchor : anchor != null ? anchor : resolvedSourceAnchor;
            if (family == SkillVfxFamily.LightBeam)
            {
                PlayChargedLightBeamEffect(skill, anchor);
                return;
            }

            if (family == SkillVfxFamily.TentacleWhip)
            {
                PlayTentacleStrikeSkillEffect(
                    skill,
                    resolvedSourceAnchor,
                    anchor);
                return;
            }

            if (family == SkillVfxFamily.SpikedBurst)
            {
                PlaySpikedBurstSkillEffect(skill, anchor, resolvedSourceAnchor);
                return;
            }

            if (family == SkillVfxFamily.BloodFountainSlash)
            {
                PlayBloodFountainSlashSkillEffect(
                    skill,
                    resolvedSourceAnchor,
                    anchor);
                return;
            }

            if (family == SkillVfxFamily.FlameBurst)
            {
                PlayFlameBurstSkillParticleEffect(
                    skill,
                    anchor,
                    lifetimeSeconds,
                    burstCount,
                    startSpeed,
                    scaledStartSize,
                    scale,
                    intensity,
                    repeatCount);
                return;
            }

            if (family == SkillVfxFamily.DarkChainBurst)
            {
                PlayDarkShackleSkillEffect(
                    skill,
                    null,
                    resolvedSourceAnchor,
                    anchor);
                return;
            }

            if (family == SkillVfxFamily.SupportFire)
            {
                PlaySupportFireSkillParticleEffect(
                    skill,
                    anchor,
                    lifetimeSeconds,
                    burstCount,
                    startSpeed,
                    scaledStartSize,
                    scale,
                    intensity,
                    repeatCount,
                    resolvedSourceAnchor,
                    resolvedFacingTargetAnchor);
                return;
            }

            if (family == SkillVfxFamily.ShieldDome && IsShieldGeneratingSkill(skill))
            {
                PlayShieldCircleSkillParticleEffect(
                    skill,
                    anchor,
                    lifetimeSeconds,
                    burstCount,
                    startSpeed,
                    scaledStartSize,
                    scale,
                    intensity,
                    repeatCount);
                return;
            }

            PlaySupportBuffHealingVisualEffect(skill, resolvedSourceAnchor, lifetimeSeconds);
            if (UsesSupportBuffHealingVisualEffect(skill))
            {
                return;
            }

            PlayMagicCircleArtForReusableSkill(skill, resolvedSourceAnchor, resolvedFacingTargetAnchor, lifetimeSeconds);
            PlayAttackArtForReusableSkill(skill, anchor, resolvedSourceAnchor);
            if (UsesSpriteOnlyReusableSkillEffect(family))
            {
                return;
            }

            var particleLocalOffset = IsSelfBuffPresentationSkill(skill)
                ? ResolveVisualCenterLocalOffset(anchor, new Vector3(0f, SelfBuffParticleLift, 0f))
                : Vector3.zero;
            SpawnParticleBurst(
                skill.activationEffect?.particleEffect,
                anchor,
                $"{family}SkillParticles",
                null,
                ResolveReusableSkillParticleColor(skill),
                null,
                lifetimeSeconds,
                Mathf.RoundToInt(burstCount * intensity * repeatCount),
                startSpeed * Mathf.Sqrt(scale),
                scaledStartSize,
                swirl,
                particleLocalOffset);
        }

        private void PlayShieldCircleSkillParticleEffect(
            SkillSO skill,
            Transform anchor,
            float lifetimeSeconds,
            int burstCount,
            float startSpeed,
            float startSize,
            float scale,
            float intensity,
            int repeatCount)
        {
            if (IsThornGuardSkill(skill))
            {
                PlayThornGuardCircleSkillParticleEffect(
                    skill,
                    anchor,
                    lifetimeSeconds,
                    burstCount,
                    startSpeed,
                    startSize,
                    scale,
                    intensity,
                    repeatCount);
                return;
            }

            var primary = ResolveShieldCircleLightColor(ResolveReusableSkillParticleColor(skill));
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);
            var radius = ShieldCircleBaseRadius *
                ShieldCircleRadiusMultiplier *
                ResolveDesignTimeRadiusMultiplier(tuning, package, designTimeBinding, 1f) *
                Mathf.Clamp(Mathf.Sqrt(scale), 0.72f, 1.35f);
            var lifetime = ResolveDesignTimeLifetime(tuning, package, designTimeBinding, Mathf.Max(0.45f, lifetimeSeconds));
            var localOffset = ResolveVisualCenterLocalOffset(
                anchor,
                ResolveDesignTimeLocalOffset(tuning, package, designTimeBinding, Vector3.zero) +
                new Vector3(0f, ShieldCircleBaseYOffset, 0f));

            SpawnShieldArtSpriteLayer(
                anchor,
                "ShieldGuardArt",
                ResolveDesignTimeArtColor(primary, tuning, package, designTimeBinding, 0.12f, primary.a),
                radius,
                lifetime,
                localOffset,
                sortingOffset: ResolveDesignTimeSortingOffset(tuning, package, designTimeBinding, 5),
                spriteOverride: ResolveDesignTimeSprite(tuning, package, designTimeBinding, shieldEffectSprite, ResolveShieldEffectSprite()),
                prefabOverride: ResolveDesignTimePrefab(tuning, package, designTimeBinding, ResolveShieldEffectPrefab()));
        }

        private void PlayThornGuardCircleSkillParticleEffect(
            SkillSO skill,
            Transform anchor,
            float lifetimeSeconds,
            int burstCount,
            float startSpeed,
            float startSize,
            float scale,
            float intensity,
            int repeatCount)
        {
            playerShieldStyleLockedToThornGuard = true;
            ClearActivePlayerShieldArtVfx();
            var primary = ResolveThornGuardDarkColor(ResolveReusableSkillParticleColor(skill), ThornGuardShadowTint, 0.68f);
            var radius = ShieldCircleBaseRadius *
                ThornGuardShieldRadiusMultiplier *
                Mathf.Clamp(Mathf.Sqrt(scale), 0.78f, 1.42f);
            var lifetime = Mathf.Max(0.5f, lifetimeSeconds * 1.08f);
            var shieldRoot = CreateThornGuardShieldVfxRoot(anchor, skill != null ? skill.power : 1);
            SpawnThornGuardShieldArt(shieldRoot, anchor, skill, radius, lifetime);

            activePlayerThornGuardVfx = shieldRoot;
            shieldRoot.SetShieldValue(Mathf.Max(1, skill != null ? skill.power : 1));
        }

        private void PlayShieldAttackSkillEffect(SkillSO skill, Transform sourceAnchor, Transform targetAnchor)
        {
            if (IsShieldBurstSkill(skill))
            {
                PlayShieldBurstSkillEffect(skill, sourceAnchor, targetAnchor);
                return;
            }

            PlayShieldBashSkillEffect(skill, sourceAnchor, targetAnchor);
        }

        private void PlayShieldBashSkillEffect(SkillSO skill, Transform sourceAnchor, Transform targetAnchor)
        {
            var primary = ResolveShieldCircleLightColor(ResolveReusableSkillParticleColor(skill));
            var scale = Mathf.Max(0.01f, skill != null ? skill.vfxScale : 1f);
            var radius = ShieldCircleBaseRadius * Mathf.Clamp(Mathf.Sqrt(scale), 0.72f, 1.35f);
            var lifetime = ShieldBashDurationSeconds;
            var offset = new Vector3(0f, ShieldCircleBaseYOffset, 0f);
            var startPosition = ResolveShieldWorldPosition(ResolveAnchorWorldPosition(sourceAnchor, offset));
            var endPosition = ResolveShieldWorldPosition(ResolveAnchorWorldPosition(targetAnchor, offset));
            var artColor = ResolveShieldArtColor(primary, 0.88f);
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);

            var shieldArt = SpawnShieldArtSpriteLayer(
                transform,
                "ShieldBashGuardArt",
                artColor,
                radius,
                lifetime,
                Vector3.zero,
                sortingOffset: 9,
                sortingAnchor: targetAnchor,
                spriteOverride: ResolveDesignTimeSprite(tuning, package, designTimeBinding, shieldEffectSprite, ResolveShieldEffectSprite()),
                prefabOverride: ResolveDesignTimePrefab(tuning, package, designTimeBinding, ResolveShieldEffectPrefab()),
                animatePulse: false);
            if (shieldArt != null)
            {
                shieldArt.transform.position = startPosition;
                if (Application.isPlaying && isActiveAndEnabled)
                {
                    StartCoroutine(AnimateShieldBashArtRoutine(
                        shieldArt,
                        startPosition,
                        endPosition,
                        shieldArt.transform.localScale,
                        artColor,
                        lifetime));
                }
            }

            PlayShieldAttackExplosion(skill, targetAnchor, "ShieldBashEasyExplosion", primary, scale, 0.28f);
        }

        private void PlayShieldBurstSkillEffect(SkillSO skill, Transform sourceAnchor, Transform targetAnchor)
        {
            var primary = ResolveShieldCircleLightColor(ResolveReusableSkillParticleColor(skill));
            var scale = Mathf.Max(0.01f, skill != null ? skill.vfxScale : 1f);
            var radius = ShieldCircleBaseRadius * Mathf.Clamp(Mathf.Sqrt(scale), 0.88f, 1.55f);
            var lifetime = ShieldBurstSkillDurationSeconds;
            var offset = new Vector3(0f, ShieldCircleBaseYOffset, 0f);
            var sourcePosition = ResolveShieldWorldPosition(ResolveAnchorWorldPosition(sourceAnchor, offset));
            var endPosition = ResolveShieldWorldPosition(ResolveAnchorWorldPosition(targetAnchor, offset));
            var artColor = ResolveShieldArtColor(primary, 0.9f);
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);

            var shieldArt = SpawnShieldArtSpriteLayer(
                transform,
                "ShieldBurstGuardArt",
                artColor,
                radius * 1.08f,
                lifetime,
                Vector3.zero,
                sortingOffset: 9,
                sortingAnchor: targetAnchor,
                spriteOverride: ResolveDesignTimeSprite(tuning, package, designTimeBinding, shieldEffectSprite, ResolveShieldEffectSprite()),
                prefabOverride: ResolveDesignTimePrefab(tuning, package, designTimeBinding, ResolveShieldEffectPrefab()),
                animatePulse: false);
            if (shieldArt != null)
            {
                shieldArt.transform.position = sourcePosition;
                if (Application.isPlaying && isActiveAndEnabled)
                {
                    StartCoroutine(AnimateShieldBashArtRoutine(
                        shieldArt,
                        sourcePosition,
                        endPosition,
                        shieldArt.transform.localScale,
                        artColor,
                        lifetime));
                }
            }

            PlayShieldAttackExplosion(skill, targetAnchor, "ShieldBurstEasyExplosion", primary, scale, 0.24f);
        }

        private void PlayShieldAttackExplosion(
            SkillSO skill,
            Transform targetAnchor,
            string objectName,
            Color tint,
            float scale,
            float delaySeconds)
        {
            var explosionPrefab = ResolveShieldAttackExplosionPrefab(skill);
            if (explosionPrefab == null || targetAnchor == null)
            {
                return;
            }

            if (Application.isPlaying && isActiveAndEnabled && delaySeconds > 0f)
            {
                StartCoroutine(PlayShieldAttackExplosionAfterDelayRoutine(
                    explosionPrefab,
                    targetAnchor,
                    objectName,
                    tint,
                    scale,
                    delaySeconds));
                return;
            }

            SpawnSkillPrefabVisualAtAnchor(
                explosionPrefab,
                targetAnchor,
                objectName,
                tint,
                Mathf.Clamp(scale, 0.8f, 1.55f),
                ResolvePrefabVisualDurationSeconds(explosionPrefab, 0.9f),
                new Vector3(0f, ShieldCircleBaseYOffset + 0.1f, 0f),
                sortingOffset: 16);
        }

        private IEnumerator PlayShieldAttackExplosionAfterDelayRoutine(
            GameObject explosionPrefab,
            Transform targetAnchor,
            string objectName,
            Color tint,
            float scale,
            float delaySeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delaySeconds));
            SpawnSkillPrefabVisualAtAnchor(
                explosionPrefab,
                targetAnchor,
                objectName,
                tint,
                Mathf.Clamp(scale, 0.8f, 1.55f),
                ResolvePrefabVisualDurationSeconds(explosionPrefab, 0.9f),
                new Vector3(0f, ShieldCircleBaseYOffset + 0.1f, 0f),
                sortingOffset: 16);
        }

        private static GameObject ResolveShieldAttackExplosionPrefab(SkillSO skill)
        {
            var tuning = ResolveSkillVfxTuning(skill);
            if (tuning != null && tuning.secondaryPrefab != null)
            {
                return tuning.secondaryPrefab;
            }

            var package = ResolveSkillVfxPackage(skill);
            return package != null ? package.secondaryPrefab : null;
        }

        private static float ResolveShieldAttackSkillDurationSeconds(SkillSO skill)
        {
            var baseDuration = IsShieldBurstSkill(skill)
                ? ShieldBurstSkillDurationSeconds
                : ShieldBashDurationSeconds;
            var explosionPrefab = ResolveShieldAttackExplosionPrefab(skill);
            if (explosionPrefab == null)
            {
                return baseDuration;
            }

            return Mathf.Max(baseDuration, 0.28f + ResolvePrefabVisualDurationSeconds(explosionPrefab, 0.9f));
        }

        private void PlayShieldImpactArtPulse(Transform anchor)
        {
            SpawnShieldArtSpriteLayer(
                anchor,
                "ShieldImpactArt",
                ResolveShieldArtColor(shieldImpactParticleColor, 0.58f),
                ShieldCircleBaseRadius * 0.86f,
                ShieldArtImpactLifetimeSeconds,
                ResolveVisualCenterLocalOffset(anchor, new Vector3(0f, ShieldCircleBaseYOffset, 0f)),
                sortingOffset: 8);
        }

        private FollowingShieldVfx CreatePlayerShieldArtVfxRoot(Transform anchor, int shieldHp)
        {
            if (ResolveShieldEffectSprite() == null)
            {
                return null;
            }

            ClearActivePlayerShieldArtVfx();

            var root = new GameObject("PlayerShieldArtVfx");
            root.transform.SetParent(transform, true);

            var follower = root.AddComponent<FollowingShieldVfx>();
            follower.Bind(
                anchor != null ? anchor : transform,
                new Vector3(0f, ShieldCircleBaseYOffset, 0f),
                ThornGuardShieldFollowSharpness,
                Mathf.Max(1, shieldHp));

            SpawnShieldArtSpriteLayer(
                follower.transform,
                "PlayerPersistentShieldArt",
                new Color(0.82f, 0.94f, 1f, 0.62f),
                PersistentShieldArtRadius,
                0f,
                Vector3.zero,
                sortingOffset: 4,
                sortingAnchor: anchor,
                autoDestroy: false,
                persistentPulse: true);

            return follower;
        }

        private FollowingShieldVfx CreateThornGuardShieldVfxRoot(Transform anchor, int shieldHp)
        {
            ClearActivePlayerThornGuardVfx();

            var root = new GameObject("ThornGuardShieldVfx");
            root.transform.SetParent(transform, true);

            var follower = root.AddComponent<FollowingShieldVfx>();
            follower.Bind(
                anchor != null ? anchor : transform,
                new Vector3(0f, ShieldCircleBaseYOffset, 0f),
                ThornGuardShieldFollowSharpness,
                Mathf.Max(1, shieldHp));

            return follower;
        }

        private void SpawnThornGuardShieldArt(
            FollowingShieldVfx shieldRoot,
            Transform anchor,
            SkillSO skill,
            float radius,
            float lifetime)
        {
            if (shieldRoot == null)
            {
                return;
            }

            var secondary = ResolveThornGuardDarkColor(
                skill != null ? ResolveReusableSkillSecondaryParticleColor(skill) : ThornGuardBloodTint,
                ThornGuardBloodTint,
                0.74f);
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var thornArtColor = Color.Lerp(secondary, Color.white, 0.18f);
            thornArtColor.a = 0.72f;
            SpawnShieldArtSpriteLayer(
                shieldRoot.transform,
                "ThornGuardShieldArt",
                thornArtColor,
                radius,
                Mathf.Max(0.5f, lifetime),
                Vector3.zero,
                sortingOffset: 3,
                sortingAnchor: anchor,
                autoDestroy: false,
                persistentPulse: true,
                spriteOverride: ResolveDesignTimeSecondarySprite(
                    tuning,
                    package,
                    thornShieldEffectSprite,
                    ResolveThornShieldEffectSprite()),
                prefabOverride: ResolveDesignTimeSecondaryPrefab(
                    tuning,
                    package,
                    ResolveThornShieldEffectPrefab()));
        }

        private void PlayFlameBurstSkillParticleEffect(
            SkillSO skill,
            Transform anchor,
            float lifetimeSeconds,
            int burstCount,
            float startSpeed,
            float startSize,
            float scale,
            float intensity,
            int repeatCount)
        {
            var parent = anchor != null ? anchor : transform;
            var lifetime = Mathf.Max(FlameBurstDurationSeconds, lifetimeSeconds);
            var resolvedScale = Mathf.Clamp(scale, 0.72f, 1.65f);
            // 절차적 화염도 청록 기반(청록 폴백)으로 통일.
            var primary = ResolveSkillTintedColor(
                ResolveReusableSkillParticleColor(skill),
                new Color(0.12f, 0.85f, 0.78f, 0.96f),
                0.26f,
                0.9f);
            var secondary = ResolveSkillTintedColor(
                ResolveReusableSkillSecondaryParticleColor(skill),
                new Color(0.02f, 0.42f, 0.45f, 0.86f),
                0.38f,
                0.82f);
            var smoke = Color.Lerp(secondary, new Color(0.04f, 0.09f, 0.1f, 0.54f), 0.68f);
            smoke.a = 0.48f;

            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);
            var footLocalOffset = ResolveFootEffectLocalOffset(
                parent,
                ResolveDesignTimeLocalOffset(tuning, package, designTimeBinding, new Vector3(0f, -0.42f, 0f)));
            var flameArtLocalOffset = footLocalOffset + new Vector3(0f, 0.16f, 0f);

            var root = new GameObject("FlameBurstFlameTongues");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = footLocalOffset;

            SpawnAttackArtSpriteLayer(
                parent,
                "FlameBurstArt",
                ResolveDesignTimeArtColor(primary, tuning, package, designTimeBinding, 0.16f, 0.84f),
                AttackArtBaseRadius *
                    ResolveDesignTimeRadiusMultiplier(tuning, package, designTimeBinding, 1.1f) *
                    Mathf.Clamp(Mathf.Sqrt(resolvedScale), 0.85f, 1.45f),
                ResolveDesignTimeLifetime(tuning, package, designTimeBinding, lifetime * 0.82f),
                flameArtLocalOffset,
                sortingOffset: ResolveDesignTimeSortingOffset(tuning, package, designTimeBinding, 11),
                spriteOverride: ResolveDesignTimeSprite(tuning, package, designTimeBinding, flameEffectSprite, ResolveFlameEffectSprite()),
                prefabOverride: ResolveDesignTimePrefab(tuning, package, designTimeBinding, ResolveFlameEffectPrefab()));

            for (var i = 0; i < FlameBurstTongueCount; i++)
            {
                var line = CreateLocalSkillLine(
                    root.transform,
                    $"FlameBurstFlameTongue{i + 1}",
                    i % 2 == 0 ? primary : secondary,
                    Mathf.Clamp(0.062f * resolvedScale, 0.028f, 0.078f),
                    Mathf.Clamp(0.01f * resolvedScale, 0.005f, 0.02f),
                    FlameBurstTongueSegmentCount + 1,
                    parent,
                    11 + i);
                var xOffset = Mathf.Lerp(-0.32f, 0.32f, FlameBurstTongueCount == 1 ? 0.5f : i / (float)(FlameBurstTongueCount - 1));
                var height = Mathf.Lerp(0.58f, 0.96f, i % 3 / 2f) * resolvedScale;
                var sway = (i % 2 == 0 ? 1f : -1f) * Mathf.Lerp(0.08f, 0.16f, i / (float)FlameBurstTongueCount);
                SetFlameTongueGeometry(line, xOffset * resolvedScale, height, sway * resolvedScale);
            }

            var particleCount = Mathf.RoundToInt(burstCount * intensity * repeatCount);
            SpawnParticleBurst(
                null,
                parent,
                "FlameBurstFlameParticles",
                primary,
                null,
                lifetime,
                Mathf.Max(34, particleCount),
                Mathf.Max(0.08f, startSpeed * 0.28f),
                Mathf.Min(startSize * 0.68f, ReusableSkillParticleMaxStartSize * 0.64f),
                false,
                footLocalOffset + new Vector3(0f, 0.1f, 0f),
                particles => ConfigureFlameBurstParticles(particles, resolvedScale, lifetime));

            SpawnParticleBurst(
                null,
                parent,
                "FlameBurstEmbers",
                secondary,
                null,
                lifetime * 0.86f,
                Mathf.Max(16, Mathf.RoundToInt(particleCount * 0.55f)),
                Mathf.Max(0.16f, startSpeed * 0.48f),
                Mathf.Min(startSize * 0.42f, ReusableSkillParticleMaxStartSize * 0.36f),
                false,
                footLocalOffset + new Vector3(0f, 0.2f, 0f),
                particles => ConfigureFlameEmberParticles(particles, resolvedScale, lifetime * 0.86f));

            SpawnParticleBurst(
                null,
                parent,
                "FlameBurstSmoke",
                smoke,
                null,
                lifetime * 1.12f,
                Mathf.Max(10, Mathf.RoundToInt(particleCount * 0.28f)),
                0.08f,
                Mathf.Min(startSize * 0.86f, ReusableSkillParticleMaxStartSize * 0.72f),
                false,
                footLocalOffset + new Vector3(0f, 0.38f, 0f),
                particles => ConfigureFlameSmokeParticles(particles, resolvedScale, lifetime * 1.12f));

            if (TryPlayImmediateFlameBurstImpactExplosion(skill, parent, out var explosionLifetimeSeconds))
            {
                lifetime = Mathf.Max(lifetime, explosionLifetimeSeconds);
            }

            if (Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(FadeSkillLineRootRoutine(root, lifetime, root.GetComponentsInChildren<LineRenderer>()));
            }
        }

        private void PlaySupportFireSkillParticleEffect(
            SkillSO skill,
            Transform anchor,
            float lifetimeSeconds,
            int burstCount,
            float startSpeed,
            float startSize,
            float scale,
            float intensity,
            int repeatCount,
            Transform sourceAnchor,
            Transform facingTargetAnchor)
        {
            if (IsLightEchoSkill(skill))
            {
                PlayLightEchoBuffOnlyEffect(skill, anchor, lifetimeSeconds, burstCount, startSpeed, startSize, scale, intensity);
                return;
            }

            var primary = ResolveReusableSkillParticleColor(skill);
            var secondary = ResolveReusableSkillSecondaryParticleColor(skill);
            var shotCount = Mathf.Clamp(repeatCount, 2, 4);
            var perShotBurstCount = Mathf.Max(6, Mathf.RoundToInt(burstCount * intensity / shotCount));
            var offsets = new[]
            {
                new Vector3(-0.42f, 0.44f, 0f),
                new Vector3(0.42f, 0.32f, 0f),
                new Vector3(0f, 0.72f, 0f),
                new Vector3(0.18f, 0.54f, 0f),
            };

            PlaySupportBuffMagicCircleArt(
                skill,
                sourceAnchor != null ? sourceAnchor : transform,
                facingTargetAnchor,
                lifetimeSeconds);
            PlaySpecializedSkillArtLayer(
                skill,
                anchor,
                sourceAnchor != null ? sourceAnchor : transform);

            for (var i = 0; i < shotCount; i++)
            {
                SpawnParticleBurst(
                    null,
                    anchor,
                    "LightEchoSupportFireParticles",
                    i % 2 == 0 ? primary : secondary,
                    null,
                    lifetimeSeconds,
                    perShotBurstCount,
                    startSpeed * Mathf.Sqrt(scale),
                    startSize,
                    false,
                    offsets[i]);
            }
        }

        private void PlayLightEchoBuffOnlyEffect(
            SkillSO skill,
            Transform anchor,
            float lifetimeSeconds,
            int burstCount,
            float startSpeed,
            float startSize,
            float scale,
            float intensity)
        {
            var parent = anchor != null ? anchor : transform;
            var localOffset = ResolveVisualCenterLocalOffset(parent, new Vector3(0f, SelfBuffParticleLift, 0f));
            SpawnParticleBurst(
                skill != null ? skill.activationEffect?.particleEffect : null,
                parent,
                "LightEchoBuffParticles",
                null,
                skill != null ? ResolveReusableSkillParticleColor(skill) : new Color(0.94f, 0.76f, 0.34f, 1f),
                null,
                lifetimeSeconds,
                Mathf.RoundToInt(burstCount * intensity),
                startSpeed * Mathf.Sqrt(Mathf.Max(0.01f, scale)),
                startSize,
                swirl: true,
                localOffset);
        }

        private void PlaySupportBuffMagicCircleArt(
            SkillSO skill,
            Transform anchor,
            Transform facingTargetAnchor,
            float lifetimeSeconds)
        {
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var sprite = ResolveDesignTimeSecondarySprite(
                tuning,
                package,
                magicCircleEffectSprite,
                ResolveMagicCircleEffectSprite());
            if (sprite == null)
            {
                return;
            }

            var color = ResolveSkillTintedColor(
                ResolveReusableSkillParticleColor(skill),
                Color.white,
                0.32f,
                0.72f);
            var facingSign = ResolveAttackFacingSign(anchor, facingTargetAnchor);
            var magicCircleLocalOffset = MirrorLocalOffsetXByFacing(
                ResolvePlayerRightLocalOffset(new Vector3(0f, 0.08f, 0f)),
                facingSign);
            magicCircleLocalOffset.y += SelfBuffMagicCircleYOffset;

            var art = SpawnAttackArtSpriteLayer(
                anchor,
                "MagicCircleEffectArt",
                color,
                AttackArtBaseRadius *
                    MagicCircleArtSizeMultiplier *
                    Mathf.Clamp(Mathf.Sqrt(Mathf.Max(0.01f, skill.vfxScale)), 0.84f, 1.5f),
                Mathf.Clamp(lifetimeSeconds, 0.38f, 0.9f),
                magicCircleLocalOffset,
                sortingOffset: 6,
                spriteOverride: sprite,
                prefabOverride: ResolveDesignTimeSecondaryPrefab(tuning, package, ResolveMagicCircleEffectPrefab()));
            if (art != null)
            {
                art.transform.localRotation = ResolveFacingMirroredLocalRotation(-8f, facingSign);
            }
        }

        private SpriteRenderer PlaySpecializedSkillArtLayer(
            SkillSO skill,
            Transform anchor,
            Transform sourceAnchor)
        {
            if (skill == null)
            {
                return null;
            }

            var scale = Mathf.Max(0.01f, skill.vfxScale);
            var family = ResolveSkillVfxFamily(skill);
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);
            var color = ResolveDesignTimeArtColor(
                ResolveReusableSkillParticleColor(skill),
                tuning,
                package,
                designTimeBinding,
                0.2f,
                0.86f);
            var objectName = "SpecializedSkillArt";
            var sprite = ResolveDesignTimeSprite(
                tuning,
                package,
                designTimeBinding,
                package == null ? attackEffectSprite : null,
                ResolveAttackEffectSprite());
            var prefab = ResolveDesignTimePrefab(tuning, package, designTimeBinding, ResolveAttackEffectPrefab());
            var radiusMultiplier = ResolveDesignTimeRadiusMultiplier(tuning, package, designTimeBinding, 1f);
            var lifetime = AttackArtLifetimeSeconds;
            var localOffset = new Vector3(0f, 0.14f, 0f);
            var sortingOffset = 13;
            var rotationDegrees = -8f;

            switch (family)
            {
                case SkillVfxFamily.TentacleWhip:
                    objectName = "TentacleStrikeImpactArt";
                    radiusMultiplier = ResolveDesignTimeRadiusMultiplier(tuning, package, designTimeBinding, 0.92f);
                    lifetime = TentacleStrikeDurationSeconds;
                    color = ResolveDesignTimeArtColor(
                        ResolveReusableSkillSecondaryParticleColor(skill),
                        tuning,
                        package,
                        designTimeBinding,
                        0.18f,
                        0.82f);
                    break;
                case SkillVfxFamily.SpikedBurst:
                    objectName = "HeavyStrikeSpikedBurstArt";
                    sprite = ResolveDesignTimeSprite(tuning, package, designTimeBinding, hitEffectSprite, ResolveHitEffectSprite());
                    prefab = ResolveDesignTimePrefab(tuning, package, designTimeBinding, ResolveHitEffectPrefab());
                    radiusMultiplier = ResolveDesignTimeRadiusMultiplier(
                        tuning,
                        package,
                        designTimeBinding,
                        1.12f * HitEffectArtSizeMultiplier) *
                        HeavyStrikeImpactScaleMultiplier;
                    lifetime = HeavyStrikeSpikedBurstDurationSeconds;
                    sortingOffset = 14;
                    rotationDegrees = 0f;
                    break;
                case SkillVfxFamily.BloodFountainSlash:
                    objectName = "BloodFountainSlashArt";
                    radiusMultiplier = ResolveDesignTimeRadiusMultiplier(
                        tuning,
                        package,
                        designTimeBinding,
                        1.08f * AttackEffectArtSizeMultiplier);
                    lifetime = BloodFountainSlashDurationSeconds;
                    color = ResolveDesignTimeArtColor(
                        ResolveReusableSkillParticleColor(skill),
                        tuning,
                        package,
                        designTimeBinding,
                        0.12f,
                        0.84f);
                    rotationDegrees = -16f;
                    break;
                case SkillVfxFamily.SupportFire:
                    objectName = "SupportFireImpactArt";
                    radiusMultiplier = ResolveDesignTimeRadiusMultiplier(
                        tuning,
                        package,
                        designTimeBinding,
                        0.82f * AttackEffectArtSizeMultiplier);
                    lifetime = Mathf.Max(0.38f, ResolveReusableSkillParticleDuration(skill));
                    localOffset = new Vector3(0f, 0.48f, 0f);
                    sortingOffset = 12;
                    rotationDegrees = -6f;
                    break;
                default:
                    return null;
            }

            var resolvedLocalOffset = ResolveDesignTimeLocalOffset(tuning, package, designTimeBinding, localOffset);
            if (family == SkillVfxFamily.SpikedBurst || family == SkillVfxFamily.BloodFountainSlash)
            {
                resolvedLocalOffset = ResolveCloseRangeImpactLocalOffset(
                    anchor != null ? anchor : transform,
                    sourceAnchor,
                    resolvedLocalOffset);
            }

            var art = SpawnAttackArtSpriteLayer(
                anchor,
                objectName,
                color,
                AttackArtBaseRadius * radiusMultiplier * Mathf.Clamp(Mathf.Sqrt(scale), 0.74f, 1.48f),
                ResolveDesignTimeLifetime(tuning, package, designTimeBinding, lifetime),
                resolvedLocalOffset,
                ResolveDesignTimeSortingOffset(tuning, package, designTimeBinding, sortingOffset),
                spriteOverride: sprite,
                prefabOverride: prefab);
            if (art == null)
            {
                return null;
            }

            var source = sourceAnchor != null
                ? sourceAnchor
                : playerRenderer != null
                    ? playerRenderer.transform
                    : transform;
            var facingSign = ResolveAttackFacingSign(source, anchor != null ? anchor : transform);
            art.transform.localRotation = Quaternion.Euler(
                0f,
                facingSign >= 0f ? 0f : 180f,
                ResolveDesignTimeRotationDegrees(tuning, package, designTimeBinding, rotationDegrees) * facingSign);
            return art;
        }

        private static float ResolveReusableSkillParticleDuration(SkillSO skill)
        {
            var family = ResolveSkillVfxFamily(skill);
            if (skill == null || family == SkillVfxFamily.None)
            {
                return AttackArtLifetimeSeconds;
            }

            ResolveReusableSkillParticleDefaults(
                family,
                out var lifetimeSeconds,
                out _,
                out _,
                out _,
                out _);
            return lifetimeSeconds;
        }

        private static SkillVfxFamily ResolveSkillVfxFamily(SkillSO skill)
        {
            return skill != null ? skill.ResolveVfxFamily() : SkillVfxFamily.None;
        }

        private static bool UsesSpriteOnlyReusableSkillEffect(SkillVfxFamily family)
        {
            return family == SkillVfxFamily.SlashArc ||
                family == SkillVfxFamily.LightProjectile ||
                family == SkillVfxFamily.ImpactBurst;
        }

        private static bool UsesPlayerFrontAttackArt(SkillVfxFamily family)
        {
            return family == SkillVfxFamily.LightProjectile;
        }

        private static bool UsesPlayerRightMagicCircle(SkillVfxFamily family)
        {
            return family == SkillVfxFamily.BuffAura ||
                family == SkillVfxFamily.CounterReady;
        }

        private static bool UsesSupportBuffHealingVisualEffect(SkillVfxFamily family)
        {
            return family == SkillVfxFamily.BuffAura ||
                family == SkillVfxFamily.CounterReady ||
                family == SkillVfxFamily.BoardDisturb;
        }

        private bool UsesSupportBuffHealingVisualEffect(SkillSO skill)
        {
            if (skill == null || !UsesSupportBuffHealingVisualEffect(ResolveSkillVfxFamily(skill)))
            {
                return false;
            }

            return ResolveSupportBuffVisualEffectPrefab(skill) != null;
        }

        private static SkillVfxPackageSO ResolveSkillVfxPackage(SkillSO skill)
        {
            return skill != null ? skill.vfxPackage : null;
        }

        private static SkillVfxTuning ResolveSkillVfxTuning(SkillSO skill)
        {
            return skill != null ? skill.ResolveVfxTuning() : null;
        }

        private SkillVfxDesignTimeBinding ResolveSkillVfxDesignTimeBinding(SkillSO skill)
        {
            return ResolveSkillVfxDesignTimeBinding(ResolveSkillVfxFamily(skill));
        }

        private SkillVfxDesignTimeBinding ResolveSkillVfxDesignTimeBinding(SkillVfxFamily family)
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.ResolveDesignTimeBinding(family) : null;
        }

        private static Sprite ResolveDesignTimeSprite(
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            SkillVfxDesignTimeBinding binding,
            Sprite explicitOverride,
            Sprite fallback)
        {
            if (explicitOverride != null)
            {
                return explicitOverride;
            }

            if (tuning != null && tuning.primarySprite != null)
            {
                return tuning.primarySprite;
            }

            if (package != null && package.primarySprite != null)
            {
                return package.primarySprite;
            }

            return binding != null && binding.sprite != null ? binding.sprite : fallback;
        }

        private static Sprite ResolveDesignTimeSprite(
            SkillVfxDesignTimeBinding binding,
            Sprite explicitOverride,
            Sprite fallback)
        {
            if (explicitOverride != null)
            {
                return explicitOverride;
            }

            return binding != null && binding.sprite != null ? binding.sprite : fallback;
        }

        private static Sprite ResolveDesignTimeSecondarySprite(
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            Sprite explicitOverride,
            Sprite fallback)
        {
            if (explicitOverride != null)
            {
                return explicitOverride;
            }

            if (tuning != null && tuning.secondarySprite != null)
            {
                return tuning.secondarySprite;
            }

            if (package != null && package.secondarySprite != null)
            {
                return package.secondarySprite;
            }

            return fallback;
        }

        private static GameObject ResolveDesignTimePrefab(
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            SkillVfxDesignTimeBinding binding,
            GameObject fallback)
        {
            if (tuning != null && tuning.primaryPrefab != null)
            {
                return tuning.primaryPrefab;
            }

            if (package != null && package.primaryPrefab != null)
            {
                return package.primaryPrefab;
            }

            return binding != null && binding.prefab != null ? binding.prefab : fallback;
        }

        private static GameObject ResolveDesignTimeSecondaryPrefab(
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            GameObject fallback)
        {
            if (tuning != null && tuning.secondaryPrefab != null)
            {
                return tuning.secondaryPrefab;
            }

            if (package != null && package.secondaryPrefab != null)
            {
                return package.secondaryPrefab;
            }

            return fallback;
        }

        private static GameObject ResolveDesignTimePrefab(
            SkillVfxDesignTimeBinding binding,
            GameObject fallback)
        {
            return binding != null && binding.prefab != null ? binding.prefab : fallback;
        }

        private static Vector3 ResolveDesignTimeLocalOffset(
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            SkillVfxDesignTimeBinding binding,
            Vector3 fallback)
        {
            if (tuning != null)
            {
                return tuning.localOffset;
            }

            return package != null
                ? package.localOffset
                : binding != null
                    ? binding.localOffset
                    : fallback;
        }

        private static Vector3 ResolveDesignTimeLocalOffset(
            SkillVfxDesignTimeBinding binding,
            Vector3 fallback)
        {
            return binding != null ? binding.localOffset : fallback;
        }

        private static Vector3 ResolvePlayerFrontLocalOffset(Vector3 offset)
        {
            if (Mathf.Abs(offset.x) <= 0.001f)
            {
                offset.x = PlayerFrontAttackArtLocalOffset.x;
            }

            return offset;
        }

        private static Vector3 ResolvePlayerRightLocalOffset(Vector3 offset)
        {
            if (Mathf.Abs(offset.x) <= 0.001f)
            {
                offset.x = PlayerRightMagicCircleLocalOffset.x;
            }

            return offset;
        }

        private static Vector3 MirrorLocalOffsetXByFacing(Vector3 offset, float facingSign)
        {
            offset.x = Mathf.Abs(offset.x) * NormalizeFacingSign(facingSign);
            return offset;
        }

        private static Quaternion ResolveFacingMirroredLocalRotation(float rotationDegrees, float facingSign)
        {
            var normalizedSign = NormalizeFacingSign(facingSign);
            return Quaternion.Euler(
                0f,
                normalizedSign >= 0f ? 0f : 180f,
                rotationDegrees * normalizedSign);
        }

        private static float NormalizeFacingSign(float facingSign)
        {
            return facingSign >= 0f ? 1f : -1f;
        }

        private static float ResolveDesignTimeRadiusMultiplier(
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            SkillVfxDesignTimeBinding binding,
            float fallback)
        {
            if (tuning != null && tuning.radiusMultiplier > 0f)
            {
                return tuning.radiusMultiplier;
            }

            if (package != null && package.radiusMultiplier > 0f)
            {
                return package.radiusMultiplier;
            }

            return binding != null && binding.radiusMultiplier > 0f ? binding.radiusMultiplier : fallback;
        }

        private static float ResolveDesignTimeRadiusMultiplier(
            SkillVfxDesignTimeBinding binding,
            float fallback)
        {
            return binding != null && binding.radiusMultiplier > 0f ? binding.radiusMultiplier : fallback;
        }

        private static float ResolveDesignTimeLifetime(
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            SkillVfxDesignTimeBinding binding,
            float fallback)
        {
            if (tuning != null && tuning.lifetimeSeconds > 0f)
            {
                return tuning.lifetimeSeconds;
            }

            if (package != null && package.lifetimeSeconds > 0f)
            {
                return package.lifetimeSeconds;
            }

            return binding != null && binding.lifetimeSeconds > 0f ? binding.lifetimeSeconds : fallback;
        }

        private static float ResolveDesignTimeLifetime(
            SkillVfxDesignTimeBinding binding,
            float fallback)
        {
            return binding != null && binding.lifetimeSeconds > 0f ? binding.lifetimeSeconds : fallback;
        }

        private static int ResolveDesignTimeSortingOffset(
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            SkillVfxDesignTimeBinding binding,
            int fallback)
        {
            if (tuning != null)
            {
                return tuning.sortingOffset;
            }

            return package != null ? package.sortingOffset : binding != null ? binding.sortingOffset : fallback;
        }

        private static int ResolveDesignTimeSortingOffset(
            SkillVfxDesignTimeBinding binding,
            int fallback)
        {
            return binding != null ? binding.sortingOffset : fallback;
        }

        private static float ResolveDesignTimeRotationDegrees(
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            SkillVfxDesignTimeBinding binding,
            float fallback)
        {
            if (tuning != null)
            {
                return tuning.rotationDegrees;
            }

            return package != null ? package.rotationDegrees : binding != null ? binding.rotationDegrees : fallback;
        }

        private static float ResolveDesignTimeRotationDegrees(
            SkillVfxDesignTimeBinding binding,
            float fallback)
        {
            return binding != null ? binding.rotationDegrees : fallback;
        }

        private static Color ResolveDesignTimeArtColor(
            Color source,
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            SkillVfxDesignTimeBinding binding,
            float fallbackTintWhiteBlend,
            float fallbackAlpha)
        {
            var tintWhiteBlend = tuning != null ? tuning.tintWhiteBlend : package != null ? package.tintWhiteBlend : binding != null ? binding.tintWhiteBlend : fallbackTintWhiteBlend;
            var alpha = tuning != null ? tuning.alpha : package != null ? package.alpha : binding != null ? binding.alpha : fallbackAlpha;
            var color = ResolveSkillTintedColor(source, Color.white, tintWhiteBlend, alpha);
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static Color ResolveDesignTimeArtColor(
            Color source,
            SkillVfxDesignTimeBinding binding,
            float fallbackTintWhiteBlend,
            float fallbackAlpha)
        {
            var tintWhiteBlend = binding != null ? binding.tintWhiteBlend : fallbackTintWhiteBlend;
            var alpha = binding != null ? binding.alpha : fallbackAlpha;
            var color = ResolveSkillTintedColor(source, Color.white, tintWhiteBlend, alpha);
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private void PlaySlashBeamSkillEffect(SkillSO skill, Transform sourceAnchor, Transform targetAnchor)
        {
            var source = ResolveSlashSkillSourcePosition(skill, sourceAnchor != null ? sourceAnchor : transform, targetAnchor);
            var target = targetAnchor != null ? ResolveSkillImpactWorldPosition(targetAnchor) : source + Vector3.right;
            if ((target - source).sqrMagnitude <= 0.0001f)
            {
                target = source + Vector3.right;
            }

            if (ResolveSkillVfxFamily(skill) == SkillVfxFamily.SlashArc)
            {
                PlaySlashArcAttackAndHitArt(skill, sourceAnchor, targetAnchor, source, target);
                return;
            }

            SpawnSlashBeamArt(skill, sourceAnchor, targetAnchor, source, target);
            PlaySlashHeavyImpactArt(skill, sourceAnchor, targetAnchor);
            PlaySpikedBurstSkillEffect(skill, targetAnchor, sourceAnchor);
        }

        private void PlaySlashArcAttackAndHitArt(
            SkillSO skill,
            Transform sourceAnchor,
            Transform targetAnchor,
            Vector3 sourceWorldPosition,
            Vector3 targetWorldPosition)
        {
            PlaySlashArcAttackArt(skill, sourceAnchor, targetAnchor, sourceWorldPosition, targetWorldPosition);
            PlaySlashArcHitImpactArt(skill, targetAnchor);
        }

        private void PlaySlashArcAttackArt(
            SkillSO skill,
            Transform sourceAnchor,
            Transform targetAnchor,
            Vector3 sourceWorldPosition,
            Vector3 targetWorldPosition)
        {
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);
            var sprite = ResolveDesignTimeSprite(
                tuning,
                package,
                designTimeBinding,
                attackEffectSprite,
                ResolveAttackEffectSprite());
            if (sprite == null)
            {
                return;
            }

            var direction = targetWorldPosition - sourceWorldPosition;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.right;
            }

            var center = Vector3.Lerp(sourceWorldPosition, targetWorldPosition, 0.5f);
            var scale = Mathf.Max(0.01f, skill != null ? skill.vfxScale : 1f);
            var art = SpawnAttackArtSpriteLayer(
                transform,
                "SlashArcAttackBeamArt",
                Color.white,
                AttackArtBaseRadius *
                    Mathf.Clamp(Mathf.Sqrt(scale), 0.7f, 1.42f) *
                    ResolveDesignTimeRadiusMultiplier(tuning, package, designTimeBinding, 1f) *
                    SlashAttackArtScaleMultiplier,
                ResolveDesignTimeLifetime(tuning, package, designTimeBinding, SlashBeamDurationSeconds),
                transform.InverseTransformPoint(center),
                sortingOffset: ResolveDesignTimeSortingOffset(tuning, package, designTimeBinding, 14),
                spriteOverride: sprite,
                prefabOverride: ResolveDesignTimePrefab(tuning, package, designTimeBinding, ResolveAttackEffectPrefab()),
                animatePulse: false);
            if (art == null)
            {
                return;
            }

            art.transform.rotation =
                Quaternion.FromToRotation(Vector3.right, direction.normalized) *
                Quaternion.Euler(0f, 0f, ResolveDesignTimeRotationDegrees(tuning, package, designTimeBinding, 0f));
            CenterSpriteRendererBoundsOnWorldPosition(art, center);
            ApplyAnchorSorting(art, targetAnchor != null ? targetAnchor : sourceAnchor, 14);
        }

        private void PlaySlashArcHitImpactArt(SkillSO skill, Transform targetAnchor)
        {
            var parent = targetAnchor != null ? targetAnchor : transform;
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var scale = Mathf.Max(0.01f, skill != null ? skill.vfxScale : 1f);
            var localOffset = ResolveDesignTimeLocalOffset(
                tuning,
                package,
                null,
                new Vector3(0f, 0.18f, 0f));
            localOffset.x = 0f;
            var hitSprite = ResolveDesignTimeSecondarySprite(
                tuning,
                package,
                hitEffectSprite,
                ResolveHitEffectSprite());
            var art = SpawnAttackArtSpriteLayer(
                parent,
                "HitImpactEffectArt",
                Color.white,
                AttackArtBaseRadius *
                    Mathf.Clamp(Mathf.Sqrt(scale), 0.78f, 1.5f) *
                    1.08f *
                    HitEffectArtSizeMultiplier,
                HeavyStrikeSpikedBurstDurationSeconds,
                localOffset,
                sortingOffset: 15,
                spriteOverride: hitSprite,
                prefabOverride: ResolveHitEffectPrefab(),
                animatePulse: false);
            if (art != null)
            {
                CenterSpriteRendererBoundsOnWorldPosition(art, parent.TransformPoint(localOffset));
            }
        }

        private static void CenterSpriteRendererBoundsOnWorldPosition(SpriteRenderer renderer, Vector3 worldPosition)
        {
            if (renderer == null)
            {
                return;
            }

            var delta = worldPosition - renderer.bounds.center;
            delta.z = 0f;
            renderer.transform.position += delta;
        }

        private void PlaySlashHeavyImpactArt(SkillSO skill, Transform sourceAnchor, Transform targetAnchor)
        {
            var parent = targetAnchor != null ? targetAnchor : transform;
            var scale = Mathf.Max(0.01f, skill != null ? skill.vfxScale : 1f);
            var color = ResolveSkillTintedColor(
                skill != null ? ResolveReusableSkillSecondaryParticleColor(skill) : Color.clear,
                new Color(1f, 0.72f, 0.08f, 0.94f),
                0.24f,
                0.86f);
            var localOffset = ResolveCloseRangeImpactLocalOffset(parent, sourceAnchor, new Vector3(0f, 0.18f, 0f));

            SpawnAttackArtSpriteLayer(
                parent,
                "HeavyStrikeSpikedBurstArt",
                color,
                AttackArtBaseRadius *
                    Mathf.Clamp(Mathf.Sqrt(scale), 0.78f, 1.5f) *
                    1.12f *
                    HitEffectArtSizeMultiplier *
                    HeavyStrikeImpactScaleMultiplier,
                HeavyStrikeSpikedBurstDurationSeconds,
                localOffset,
                sortingOffset: 14,
                spriteOverride: hitEffectSprite != null ? hitEffectSprite : ResolveHitEffectSprite(),
                prefabOverride: ResolveHitEffectPrefab());
        }

        private void SpawnSlashBeamArt(
            SkillSO skill,
            Transform sourceAnchor,
            Transform targetAnchor,
            Vector3 sourceWorldPosition,
            Vector3 targetWorldPosition)
        {
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);
            var sprite = ResolveDesignTimeSprite(
                tuning,
                package,
                designTimeBinding,
                attackEffectSprite,
                ResolveAttackEffectSprite());
            if (sprite == null)
            {
                return;
            }

            var direction = targetWorldPosition - sourceWorldPosition;
            direction.z = 0f;
            var distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return;
            }

            var family = ResolveSkillVfxFamily(skill);
            var scale = Mathf.Clamp(Mathf.Max(0.01f, skill != null ? skill.vfxScale : 1f), 0.72f, 1.65f);
            var color = ResolveDesignTimeArtColor(
                skill != null ? ResolveReusableSkillParticleColor(skill) : Color.white,
                tuning,
                package,
                designTimeBinding,
                0.08f,
                0.88f);
            var objectName = family == SkillVfxFamily.BloodFountainSlash
                ? "BloodFountainSlashAttackBeamArt"
                : "SlashArcAttackBeamArt";
            var beamObject = new GameObject(objectName, typeof(SpriteRenderer));
            beamObject.transform.SetParent(transform, false);
            beamObject.transform.position = sourceWorldPosition + new Vector3(0f, SlashBeamSourceLift * scale, 0f);
            beamObject.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction.normalized);

            var spriteSize = sprite.bounds.size;
            var width = Mathf.Max(0.01f, spriteSize.x);
            var height = Mathf.Max(0.01f, spriteSize.y);
            var baseScale = new Vector3(
                distance / width * SlashBeamWidthMultiplier * SlashAttackArtScaleMultiplier,
                SlashBeamHeightMultiplier * scale / height * SlashAttackArtScaleMultiplier,
                1f);
            beamObject.transform.localScale = baseScale;

            var renderer = beamObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            ApplyAnchorSorting(renderer, targetAnchor != null ? targetAnchor : sourceAnchor, 14);

            if (Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(AnimateAttackArtPulseRoutine(renderer, SlashBeamDurationSeconds, baseScale, color));
                Destroy(beamObject, SlashBeamDurationSeconds + 0.15f);
            }
        }

        private void PlaySpikedBurstSkillEffect(SkillSO skill, Transform targetAnchor, Transform sourceAnchor = null)
        {
            var parent = targetAnchor != null ? targetAnchor : transform;
            var impactLocalOffset = ResolveCloseRangeImpactLocalOffset(parent, sourceAnchor, new Vector3(0f, 0.18f, 0f));
            var scale = Mathf.Clamp(Mathf.Max(0.01f, skill != null ? skill.vfxScale : 1f), 0.78f, 1.85f);
            var visualScale = scale * HeavyStrikeImpactScaleMultiplier;
            var intensity = Mathf.Max(0.1f, skill != null ? skill.vfxIntensity : 1f);
            var primary = ResolveSkillTintedColor(
                skill != null ? ResolveReusableSkillParticleColor(skill) : Color.clear,
                new Color(1f, 0.72f, 0.08f, 0.96f),
                0.28f,
                0.92f);
            var secondary = ResolveSkillTintedColor(
                skill != null ? ResolveReusableSkillSecondaryParticleColor(skill) : Color.clear,
                new Color(0.74f, 0.035f, 0.015f, 0.9f),
                0.34f,
                0.84f);

            var root = new GameObject("HeavyStrikeSpikedBurst");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = impactLocalOffset;

            var star = CreateLocalSkillLine(
                root.transform,
                "HeavyStrikeSpikedBurstStar",
                primary,
                Mathf.Clamp(0.064f * visualScale, 0.026f, 0.12f),
                Mathf.Clamp(0.036f * visualScale, 0.014f, 0.078f),
                HeavyStrikeStarSegmentCount + 1,
                parent,
                11);
            SetSpikedBurstStarGeometry(star, 0.22f * visualScale, 0.62f * visualScale, 0.08f);

            var shock = CreateLocalSkillLine(
                root.transform,
                "HeavyStrikeSpikedShockRing",
                secondary,
                Mathf.Clamp(0.034f * visualScale, 0.014f, 0.07f),
                Mathf.Clamp(0.02f * visualScale, 0.008f, 0.05f),
                HeavyStrikeStarSegmentCount + 1,
                parent,
                10);
            SetSpikedBurstStarGeometry(shock, 0.36f * visualScale, 0.78f * visualScale, 0.17f);

            for (var i = 0; i < HeavyStrikeSpikeRayCount; i++)
            {
                var color = i % 2 == 0 ? primary : secondary;
                var ray = CreateLocalSkillLine(
                    root.transform,
                    $"HeavyStrikeSpikeRay{i + 1}",
                    color,
                    Mathf.Clamp(0.055f * visualScale, 0.02f, 0.095f),
                    Mathf.Clamp(0.012f * visualScale, 0.005f, 0.032f),
                    2,
                    parent,
                    12);
                var angle = Mathf.PI * 2f * i / HeavyStrikeSpikeRayCount + (i % 3) * 0.07f;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                ray.SetPosition(0, direction * (0.12f * visualScale));
                ray.SetPosition(1, direction * Mathf.Lerp(0.72f, 0.98f, i % 4 / 3f) * visualScale);
            }

            SpawnParticleBurst(
                null,
                parent,
                "HeavyStrikeSpikedExplosionParticles",
                primary,
                null,
                HeavyStrikeSpikedBurstDurationSeconds,
                Mathf.RoundToInt(40 * intensity),
                1.25f * Mathf.Sqrt(visualScale),
                Mathf.Clamp(0.16f * visualScale, 0.052f, 0.24f),
                false,
                impactLocalOffset,
                particles => ConfigureSpikedBurstParticles(particles, visualScale, HeavyStrikeSpikedBurstDurationSeconds));

            SpawnParticleBurst(
                null,
                parent,
                "HeavyStrikeSpikeShards",
                secondary,
                null,
                0.44f,
                Mathf.RoundToInt(24 * intensity),
                1.7f * Mathf.Sqrt(visualScale),
                Mathf.Clamp(0.09f * visualScale, 0.03f, 0.15f),
                false,
                impactLocalOffset,
                particles => ConfigureSpikedBurstParticles(particles, visualScale * 0.82f, 0.44f));

            if (Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(FadeSkillLineRootRoutine(
                    root,
                    HeavyStrikeSpikedBurstDurationSeconds,
                    root.GetComponentsInChildren<LineRenderer>()));
            }
        }

        private void PlayBloodFountainSlashSkillEffect(SkillSO skill, Transform sourceAnchor, Transform targetAnchor)
        {
            PlaySlashBeamSkillEffect(skill, sourceAnchor, targetAnchor);
        }

        private void PlayDarkShackleSkillEffect(
            SkillSO skill,
            CombatEffectBinding effect,
            Transform sourceAnchor,
            Transform targetAnchor)
        {
            var parent = targetAnchor != null ? targetAnchor : transform;
            var scale = Mathf.Clamp(Mathf.Max(0.01f, skill != null ? skill.vfxScale : 1f), 0.78f, 1.7f);
            var source = ResolveLanternSkillSourcePosition(sourceAnchor != null ? sourceAnchor : transform, targetAnchor);
            var impact = targetAnchor != null ? ResolveSkillImpactWorldPosition(targetAnchor) : source + Vector3.right;
            if ((impact - source).sqrMagnitude <= 0.0001f)
            {
                impact = source + Vector3.right;
            }

            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);
            var root = new GameObject("DarkShackleChainLaunch");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var chainAttackProjectile = SpawnDarkShackleChainProjectileVfx(
                root.transform,
                source,
                impact,
                scale,
                parent,
                tuning,
                package,
                designTimeBinding);

            if (Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(AnimateDarkShackleVfxChainRoutine(
                    root,
                    chainAttackProjectile,
                    sourceAnchor != null ? sourceAnchor : transform,
                    targetAnchor,
                    source,
                    impact,
                    scale,
                    effect,
                    tuning,
                    package));
                return;
            }

            UpdateDarkShackleChainProjectileVfx(
                chainAttackProjectile,
                source,
                impact,
                1f,
                scale);
            SpawnDarkShackleImpactDust(parent, impact, scale);
            SpawnDarkShackleImpactEffects(parent, impact, scale, tuning, package);
        }

        private GameObject SpawnDarkShackleChainProjectileVfx(
            Transform parent,
            Vector3 source,
            Vector3 impact,
            float scale,
            Transform sortingAnchor,
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            SkillVfxDesignTimeBinding designTimeBinding)
        {
            var prefab = ResolveDesignTimePrefab(tuning, package, designTimeBinding, ResolveChainAttackEffectPrefab());
            var projectile = prefab != null
                ? Instantiate(prefab, parent != null ? parent : transform)
                : CreateDarkShackleChainProjectileSprite(parent, tuning, package, designTimeBinding);
            if (projectile == null)
            {
                return null;
            }

            projectile.name = "DarkShackleChainProjectileVfx";
            projectile.transform.SetParent(parent != null ? parent : transform, true);
            projectile.transform.localPosition = Vector3.zero;
            projectile.transform.localRotation = Quaternion.identity;
            projectile.transform.localScale = Vector3.one;

            foreach (var renderer in projectile.GetComponentsInChildren<Renderer>(true))
            {
                ApplyAnchorSorting(renderer, sortingAnchor, ResolveDesignTimeSortingOffset(tuning, package, designTimeBinding, 15));
            }

            var sprite = ResolveDesignTimeSprite(tuning, package, designTimeBinding, chainAttackEffectSprite, ResolveChainAttackEffectSprite());
            foreach (var renderer in projectile.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sprite != null)
                {
                    renderer.sprite = sprite;
                }

                renderer.color = Color.white;
            }

            var visualEffect = projectile.GetComponentInChildren<VisualEffect>(true);
            if (visualEffect != null && Application.isPlaying)
            {
                visualEffect.Reinit();
                visualEffect.Play();
            }

            UpdateDarkShackleChainProjectileVfx(projectile, source, impact, 0f, scale);
            return projectile;
        }

        private GameObject CreateDarkShackleChainProjectileSprite(
            Transform parent,
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            SkillVfxDesignTimeBinding designTimeBinding)
        {
            var sprite = ResolveDesignTimeSprite(tuning, package, designTimeBinding, chainAttackEffectSprite, ResolveChainAttackEffectSprite());
            if (sprite == null)
            {
                return null;
            }

            var spriteObject = new GameObject("DarkShackleChainProjectileVfx", typeof(SpriteRenderer));
            spriteObject.transform.SetParent(parent != null ? parent : transform, false);
            var renderer = spriteObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            return spriteObject;
        }

        private IEnumerator AnimateDarkShackleVfxChainRoutine(
            GameObject root,
            GameObject chainProjectile,
            Transform sourceAnchor,
            Transform targetAnchor,
            Vector3 fallbackSource,
            Vector3 fallbackImpact,
            float scale,
            CombatEffectBinding effect,
            SkillVfxTuning tuning,
            SkillVfxPackageSO package)
        {
            var elapsed = 0f;
            var source = fallbackSource;
            var target = fallbackImpact;
            while (root != null && elapsed < DarkShackleMaxFlySeconds)
            {
                var deltaTime = Time.unscaledDeltaTime;
                elapsed += deltaTime;
                source = ResolveDarkShackleSourcePosition(sourceAnchor, targetAnchor, fallbackSource);
                target = ResolveDarkShackleTargetPosition(targetAnchor, fallbackImpact);
                var progress = Mathf.Clamp01(elapsed / DarkShackleMaxFlySeconds);
                UpdateDarkShackleChainProjectileVfx(chainProjectile, source, target, progress, scale);
                yield return null;
            }

            if (root == null)
            {
                yield break;
            }

            source = ResolveDarkShackleSourcePosition(sourceAnchor, targetAnchor, fallbackSource);
            target = ResolveDarkShackleTargetPosition(targetAnchor, fallbackImpact);
            UpdateDarkShackleChainProjectileVfx(chainProjectile, source, target, 1f, scale);
            PlayCombatantActionAudioEffect(effect);
            SpawnDarkShackleImpactDust(targetAnchor != null ? targetAnchor : transform, target, scale);
            var boundChains = SpawnDarkShackleImpactEffects(targetAnchor != null ? targetAnchor : transform, target, scale, tuning, package);

            var holdSeconds = DarkShackleLatchSeconds + DarkShackleFadeSeconds;
            while (root != null && holdSeconds > 0f)
            {
                holdSeconds -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (boundChains != null)
            {
                Destroy(boundChains.gameObject);
            }

            if (root != null)
            {
                Destroy(root);
            }
        }

        private static void UpdateDarkShackleChainProjectileVfx(
            GameObject projectile,
            Vector3 source,
            Vector3 impact,
            float progress,
            float scale)
        {
            if (projectile == null)
            {
                return;
            }

            var direction = impact - source;
            direction.z = 0f;
            var distance = direction.magnitude;
            if (distance <= 0.0001f)
            {
                direction = Vector3.right;
                distance = 1f;
            }

            var axis = direction / distance;
            var extension = Mathf.Lerp(
                DarkShackleInitialExtension,
                1f,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress)));
            var visibleLength = Mathf.Max(0.01f, distance * extension);
            projectile.transform.position = source + axis * (visibleLength * 0.5f);
            projectile.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg);

            var height = Mathf.Clamp(0.46f * Mathf.Max(0.01f, scale), 0.32f, 0.72f);
            if (projectile.GetComponentInChildren<VisualEffect>(true) != null)
            {
                projectile.transform.localScale = new Vector3(Mathf.Max(0.01f, distance), height, 1f);
                return;
            }

            projectile.transform.localScale = Vector3.one;
            foreach (var renderer in projectile.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var spriteSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector3.one;
                var width = Mathf.Max(0.01f, spriteSize.x);
                var spriteHeight = Mathf.Max(0.01f, spriteSize.y);
                renderer.transform.localScale = new Vector3(visibleLength / width, height / spriteHeight, 1f);
            }
        }

        private void SpawnDarkShackleImpactDust(Transform targetAnchor, Vector3 impact, float scale)
        {
            var parent = targetAnchor != null ? targetAnchor : transform;
            SpawnParticleBurst(
                null,
                parent,
                "DarkShackleImpactDust",
                Color.white,
                null,
                DarkShackleImpactDustLifetimeSeconds,
                Mathf.RoundToInt(18 * Mathf.Clamp(scale, 0.75f, 1.8f)),
                Mathf.Clamp(0.45f * scale, 0.28f, 0.72f),
                Mathf.Clamp(0.055f * scale, 0.035f, 0.085f),
                swirl: false,
                localOffset: parent.InverseTransformPoint(impact),
                sortingAnchor: parent);
        }

        private SpriteRenderer SpawnDarkShackleChainAttackProjectile(
            Transform parent,
            Vector3 source,
            Vector3 impact,
            Color color,
            float scale,
            Transform sortingAnchor,
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            SkillVfxDesignTimeBinding designTimeBinding)
        {
            var art = SpawnAttackArtSpriteLayer(
                parent != null ? parent : transform,
                "DarkShackleChainAttackArt",
                color,
                AttackArtBaseRadius *
                    ResolveDesignTimeRadiusMultiplier(tuning, package, designTimeBinding, 1f) *
                    Mathf.Clamp(Mathf.Sqrt(scale), 0.8f, 1.38f),
                0f,
                Vector3.zero,
                sortingOffset: ResolveDesignTimeSortingOffset(tuning, package, designTimeBinding, 15),
                spriteOverride: ResolveDesignTimeSprite(
                    tuning,
                    package,
                    designTimeBinding,
                    chainAttackEffectSprite,
                    ResolveChainAttackEffectSprite()),
                prefabOverride: ResolveDesignTimePrefab(tuning, package, designTimeBinding, ResolveChainAttackEffectPrefab()),
                autoDestroy: false,
                animatePulse: false);
            if (art == null)
            {
                return null;
            }

            ApplyAnchorSorting(art, sortingAnchor, ResolveDesignTimeSortingOffset(tuning, package, designTimeBinding, 15));
            UpdateDarkShackleChainAttackProjectile(
                art,
                source,
                impact,
                source,
                color,
                1f,
                ResolveDesignTimeRotationDegrees(tuning, package, designTimeBinding, -4f));
            return art;
        }

        private GameObject CreateDarkShackleLaunchRoot(SkillVfxTuning tuning, SkillVfxPackageSO package)
        {
            var prefab = ResolveDarkChainLaunchPrefab(tuning, package);
            var root = prefab != null
                ? Instantiate(prefab, transform)
                : new GameObject("DarkShackleChainLaunch");
            root.name = "DarkShackleChainLaunch";
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root;
        }

        private LineRenderer ResolveAuthoredWorldSkillLine(
            GameObject root,
            string objectName,
            Color color,
            float startWidth,
            float endWidth,
            int positionCount,
            Transform sortingAnchor,
            int sortingOffset)
        {
            var line = root != null
                ? root.transform.Find(objectName)?.GetComponent<LineRenderer>()
                : null;
            if (line == null)
            {
                return CreateWorldSkillLine(
                    root != null ? root.transform : transform,
                    objectName,
                    color,
                    startWidth,
                    endWidth,
                    positionCount,
                    sortingAnchor,
                    sortingOffset);
            }

            line.gameObject.SetActive(true);
            ConfigureWorldSkillLine(line, color, startWidth, endWidth, positionCount, sortingAnchor, sortingOffset);
            return line;
        }

        private LineRenderer[] ResolveAuthoredDarkShackleLinks(
            GameObject root,
            int linkCount,
            Color color,
            float width,
            Transform sortingAnchor,
            int sortingOffset)
        {
            var existingLinks = root != null
                ? root.GetComponentsInChildren<LineRenderer>(true)
                    .Where(line => line != null && line.name.StartsWith("DarkShackleChainLink", System.StringComparison.Ordinal))
                    .OrderBy(line => line.name)
                    .ToArray()
                : System.Array.Empty<LineRenderer>();
            var links = new LineRenderer[Mathf.Max(0, linkCount)];
            for (var i = 0; i < links.Length; i++)
            {
                var line = i < existingLinks.Length ? existingLinks[i] : null;
                if (line == null)
                {
                    var linkObject = new GameObject($"DarkShackleChainLink{i + 1:00}", typeof(LineRenderer));
                    linkObject.transform.SetParent(root != null ? root.transform : transform, false);
                    line = linkObject.GetComponent<LineRenderer>();
                }

                line.gameObject.SetActive(true);
                ConfigureDarkShackleLinkLine(line, color, width, sortingAnchor, sortingOffset);
                links[i] = line;
            }

            for (var i = links.Length; i < existingLinks.Length; i++)
            {
                if (existingLinks[i] != null)
                {
                    existingLinks[i].gameObject.SetActive(false);
                }
            }

            return links;
        }

        private LineRenderer CreateWorldSkillLine(
            Transform parent,
            string objectName,
            Color color,
            float startWidth,
            float endWidth,
            int positionCount,
            Transform sortingAnchor,
            int sortingOffset)
        {
            var lineObject = new GameObject(objectName, typeof(LineRenderer));
            lineObject.transform.SetParent(parent != null ? parent : transform, false);

            var line = lineObject.GetComponent<LineRenderer>();
            ConfigureWorldSkillLine(line, color, startWidth, endWidth, positionCount, sortingAnchor, sortingOffset);
            return line;
        }

        private void ConfigureWorldSkillLine(
            LineRenderer line,
            Color color,
            float startWidth,
            float endWidth,
            int positionCount,
            Transform sortingAnchor,
            int sortingOffset)
        {
            if (line == null)
            {
                return;
            }

            line.useWorldSpace = true;
            line.positionCount = Mathf.Max(2, positionCount);
            line.numCapVertices = 6;
            line.numCornerVertices = 5;
            line.startWidth = Mathf.Max(0.004f, startWidth);
            line.endWidth = Mathf.Max(0.004f, endWidth);
            line.sharedMaterial = ResolveRuntimeSkillParticleMaterial(line.gameObject.name, color);
            line.startColor = color;
            line.endColor = color;
            ApplyAnchorSorting(line, sortingAnchor, sortingOffset);
        }

        private void ConfigureDarkShackleLinkLine(
            LineRenderer line,
            Color color,
            float width,
            Transform sortingAnchor,
            int sortingOffset)
        {
            if (line == null)
            {
                return;
            }

            line.useWorldSpace = true;
            line.positionCount = DarkShackleRingSegmentCount + 1;
            line.numCapVertices = 3;
            line.numCornerVertices = 3;
            line.startWidth = Mathf.Max(0.004f, width);
            line.endWidth = Mathf.Max(0.004f, width);
            line.sharedMaterial = ResolveRuntimeSkillParticleMaterial(line.gameObject.name, color);
            line.startColor = color;
            line.endColor = color;
            ApplyAnchorSorting(line, sortingAnchor, sortingOffset);
        }

        private static int ResolveDarkShackleLinkCount(Vector3 source, Vector3 impact, float scale)
        {
            var distance = Vector3.Distance(source, impact);
            var spacing = Mathf.Max(0.01f, DarkShackleLinkSpacing * Mathf.Max(0.1f, scale));
            return Mathf.Clamp(
                Mathf.CeilToInt(distance / spacing),
                DarkShackleMinChainLinkCount,
                DarkShackleMaxChainLinkCount);
        }

        private static Vector3 ResolveDarkShackleSourcePosition(
            Transform sourceAnchor,
            Transform targetAnchor,
            Vector3 fallbackSource)
        {
            return sourceAnchor != null
                ? ResolveLanternSkillSourcePosition(sourceAnchor, targetAnchor)
                : fallbackSource;
        }

        private static Vector3 ResolveDarkShackleTargetPosition(Transform targetAnchor, Vector3 fallbackImpact)
        {
            return targetAnchor != null ? ResolveSkillImpactWorldPosition(targetAnchor) : fallbackImpact;
        }

        private SpriteRenderer SpawnDarkShackleImpactEffects(
            Transform targetAnchor,
            Vector3 impact,
            Color primary,
            float scale,
            SkillVfxTuning tuning,
            SkillVfxPackageSO package)
        {
            return SpawnDarkShackleImpactEffects(targetAnchor, impact, scale, tuning, package);
        }

        private SpriteRenderer SpawnDarkShackleImpactEffects(
            Transform targetAnchor,
            Vector3 impact,
            float scale,
            SkillVfxTuning tuning,
            SkillVfxPackageSO package)
        {
            var parent = targetAnchor != null ? targetAnchor : transform;
            var localImpact = parent.InverseTransformPoint(impact);
            DestroyChildrenNamed(parent, "DarkShackleBoundChainsArt");

            var boundArtColor = Color.white;
            var sizeScale = Mathf.Clamp(Mathf.Sqrt(Mathf.Max(0.01f, scale)), 0.88f, 1.55f);
            return SpawnAttackArtSpriteLayer(
                parent,
                "DarkShackleBoundChainsArt",
                boundArtColor,
                AttackArtBaseRadius * DarkShackleBoundChainsRadiusMultiplier * sizeScale,
                DarkShackleLatchSeconds + DarkShackleFadeSeconds + 0.18f,
                localImpact + DarkShackleBoundChainsLocalOffset,
                sortingOffset: 16,
                spriteOverride: ResolveDesignTimeSecondarySprite(
                    tuning,
                    package,
                    boundChainsEffectSprite,
                    ResolveBoundChainsEffectSprite()),
                prefabOverride: ResolveDesignTimeSecondaryPrefab(
                    tuning,
                    package,
                    ResolveBoundChainsEffectPrefab()),
                autoDestroy: false,
                animatePulse: false);
        }

        private IEnumerator AnimateDarkShackleChainRoutine(
            GameObject root,
            LineRenderer chain,
            LineRenderer head,
            LineRenderer[] links,
            SpriteRenderer chainAttackProjectile,
            Transform sourceAnchor,
            Transform targetAnchor,
            Vector3 fallbackSource,
            Vector3 fallbackImpact,
            float scale,
            float intensity,
            Color primary,
            Color secondary,
            Color chainAttackArtColor,
            SkillVfxTuning tuning,
            SkillVfxPackageSO package,
            float chainAttackRotationDegrees)
        {
            var state = DarkShackleChainState.Flying;
            var flyElapsed = 0f;
            var latchRemaining = DarkShackleLatchSeconds;
            var fadeRemaining = DarkShackleFadeSeconds;
            var motionTime = 0f;
            var source = ResolveDarkShackleSourcePosition(sourceAnchor, targetAnchor, fallbackSource);
            var target = ResolveDarkShackleTargetPosition(targetAnchor, fallbackImpact);
            var headPosition = source;
            var impactSpawned = false;

            while (root != null && chain != null)
            {
                var deltaTime = Time.unscaledDeltaTime;
                motionTime += deltaTime;
                source = ResolveDarkShackleSourcePosition(sourceAnchor, targetAnchor, fallbackSource);
                target = ResolveDarkShackleTargetPosition(targetAnchor, fallbackImpact);

                switch (state)
                {
                    case DarkShackleChainState.Flying:
                        flyElapsed += deltaTime;
                        headPosition = Vector3.MoveTowards(
                            headPosition,
                            target,
                            DarkShackleChainFlySpeed * deltaTime);
                        if ((headPosition - target).sqrMagnitude <= 0.0025f ||
                            flyElapsed >= DarkShackleMaxFlySeconds)
                        {
                            headPosition = target;
                            state = DarkShackleChainState.Latched;
                            SpawnDarkShackleImpactEffects(
                                targetAnchor != null ? targetAnchor : transform,
                                target,
                                primary,
                                scale,
                                tuning,
                                package);
                            if (chainAttackProjectile != null)
                            {
                                chainAttackProjectile.enabled = false;
                            }

                            impactSpawned = true;
                        }

                        break;
                    case DarkShackleChainState.Latched:
                        headPosition = target;
                        latchRemaining -= deltaTime;
                        if (latchRemaining <= 0f)
                        {
                            state = DarkShackleChainState.Ending;
                        }

                        break;
                    case DarkShackleChainState.Ending:
                        headPosition = target;
                        fadeRemaining -= deltaTime;
                        if (fadeRemaining <= 0f)
                        {
                            Destroy(root);
                            yield break;
                        }

                        break;
                }

                var fade = state == DarkShackleChainState.Ending
                    ? Mathf.Clamp01(fadeRemaining / DarkShackleFadeSeconds)
                    : 1f;
                var latchWeight = state == DarkShackleChainState.Latched ? 1f : 0.35f;
                SetDarkShackleChainGeometry(chain, source, headPosition, scale, latchWeight, motionTime);
                SetDarkShackleLinkGeometry(links, source, headPosition, scale, latchWeight, motionTime);
                SetDarkShackleHeadGeometry(head, source, headPosition, scale);
                if (state == DarkShackleChainState.Flying)
                {
                    UpdateDarkShackleChainAttackProjectile(
                        chainAttackProjectile,
                        source,
                        target,
                        headPosition,
                        chainAttackArtColor,
                        fade,
                        chainAttackRotationDegrees);
                }

                SetLineAlpha(chain, primary, fade);
                SetLineAlpha(head, secondary, fade);
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        SetLineAlpha(link, secondary, fade * 0.9f);
                    }
                }

                yield return null;
            }

            if (!impactSpawned && root != null)
            {
                SpawnDarkShackleImpactEffects(
                    targetAnchor != null ? targetAnchor : transform,
                    target,
                    primary,
                    scale,
                    tuning,
                    package);
            }

            if (root != null)
            {
                Destroy(root);
            }
        }

        private static void UpdateDarkShackleChainAttackProjectile(
            SpriteRenderer projectile,
            Vector3 source,
            Vector3 impact,
            Vector3 position,
            Color color,
            float alphaMultiplier,
            float rotationDegrees)
        {
            if (projectile == null)
            {
                return;
            }

            projectile.enabled = true;
            projectile.transform.position = position;
            var direction = impact - source;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.right;
            }

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationDegrees);
            color.a *= Mathf.Clamp01(alphaMultiplier);
            projectile.color = color;
        }

        private static void SetDarkShackleChainGeometry(
            LineRenderer chain,
            Vector3 source,
            Vector3 end,
            float scale,
            float latchWeight,
            float motionProgress)
        {
            if (chain == null)
            {
                return;
            }

            var direction = end - source;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.right;
            }

            var normal = new Vector3(-direction.y, direction.x, 0f).normalized;
            for (var i = 0; i <= DarkShackleChainSegmentCount; i++)
            {
                var t = i / (float)DarkShackleChainSegmentCount;
                var ripple = Mathf.Sin((t * 4.2f + motionProgress * 2.8f) * Mathf.PI) *
                    0.055f * scale * Mathf.Clamp01(latchWeight) * (1f - t * 0.35f);
                chain.SetPosition(i, Vector3.Lerp(source, end, t) + normal * ripple);
            }
        }

        private static void SetDarkShackleLinkGeometry(
            LineRenderer[] links,
            Vector3 source,
            Vector3 end,
            float scale,
            float latchWeight,
            float motionProgress)
        {
            if (links == null)
            {
                return;
            }

            var direction = end - source;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.right;
            }

            var distance = direction.magnitude;
            var visibleCount = Mathf.Clamp(
                Mathf.CeilToInt(distance / Mathf.Max(0.01f, DarkShackleLinkSpacing * scale)),
                1,
                links.Length);
            var axis = direction.normalized;
            var normal = new Vector3(-axis.y, axis.x, 0f);
            for (var i = 0; i < links.Length; i++)
            {
                if (i >= visibleCount)
                {
                    links[i].enabled = false;
                    SetDarkShackleLinkRing(links[i], Vector3.zero, axis, normal, 0f, 0f, 0f);
                    continue;
                }

                links[i].enabled = true;
                var t = visibleCount == 1 ? 0.5f : (i + 1f) / (visibleCount + 1f);
                var center = Vector3.Lerp(source, end, t);
                var phase = (i % 2 == 0 ? 1f : -1f) * 0.5f + motionProgress;
                var wobble = Mathf.Sin((t * 3.5f + phase) * Mathf.PI) * 0.035f * scale;
                center += normal * wobble * Mathf.Clamp01(latchWeight);
                SetDarkShackleLinkRing(
                    links[i],
                    center,
                    axis,
                    normal,
                    0.062f * scale,
                    0.028f * scale,
                    i % 2 == 0 ? 0f : Mathf.PI * 0.5f);
            }
        }

        private static void SetDarkShackleHeadGeometry(
            LineRenderer head,
            Vector3 source,
            Vector3 headPosition,
            float scale)
        {
            if (head == null)
            {
                return;
            }

            var direction = headPosition - source;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.right;
            }

            var axis = direction.normalized;
            var normal = new Vector3(-axis.y, axis.x, 0f);
            var tip = headPosition;
            var neck = tip - axis * (0.09f * scale);
            var basePoint = tip - axis * (0.2f * scale);
            var halfWidth = 0.062f * scale;
            var hookWidth = 0.036f * scale;

            head.SetPosition(0, tip);
            head.SetPosition(1, neck + normal * halfWidth);
            head.SetPosition(2, basePoint + normal * hookWidth);
            head.SetPosition(3, neck);
            head.SetPosition(4, basePoint - normal * hookWidth);
            head.SetPosition(5, neck - normal * halfWidth);
            head.SetPosition(6, tip);
        }

        private static void SetDarkShackleLinkRing(
            LineRenderer link,
            Vector3 center,
            Vector3 axis,
            Vector3 normal,
            float halfLength,
            float halfWidth,
            float rotationOffset)
        {
            if (link == null)
            {
                return;
            }

            for (var i = 0; i <= DarkShackleRingSegmentCount; i++)
            {
                var angle = rotationOffset + Mathf.PI * 2f * i / DarkShackleRingSegmentCount;
                var point = center +
                    axis * (Mathf.Cos(angle) * halfLength) +
                    normal * (Mathf.Sin(angle) * halfWidth);
                link.SetPosition(i, point);
            }
        }

        private LineRenderer CreateLocalSkillLine(
            Transform parent,
            string objectName,
            Color color,
            float startWidth,
            float endWidth,
            int positionCount,
            Transform sortingAnchor,
            int sortingOffset)
        {
            var lineObject = new GameObject(objectName, typeof(LineRenderer));
            lineObject.transform.SetParent(parent != null ? parent : transform, false);
            lineObject.transform.localPosition = Vector3.zero;

            var line = lineObject.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = Mathf.Max(2, positionCount);
            line.numCapVertices = 8;
            line.numCornerVertices = 6;
            line.startWidth = Mathf.Max(0.004f, startWidth);
            line.endWidth = Mathf.Max(0.004f, endWidth);
            line.sharedMaterial = ResolveRuntimeSkillParticleMaterial(objectName, color);
            line.startColor = color;
            line.endColor = color;
            ApplyAnchorSorting(line, sortingAnchor != null ? sortingAnchor : parent, sortingOffset);
            return line;
        }

        private static void SetSpikedBurstStarGeometry(LineRenderer line, float innerRadius, float outerRadius, float rotationRadians)
        {
            if (line == null)
            {
                return;
            }

            for (var i = 0; i <= HeavyStrikeStarSegmentCount; i++)
            {
                var pointIndex = i % HeavyStrikeStarSegmentCount;
                var angle = rotationRadians + Mathf.PI * 2f * pointIndex / HeavyStrikeStarSegmentCount;
                var radius = pointIndex % 2 == 0 ? outerRadius : innerRadius;
                if (pointIndex % 7 == 0)
                {
                    radius = outerRadius * 1.18f;
                }

                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private static void SetBloodSlashGeometry(LineRenderer line, float scale, float facingSign, float verticalOffset)
        {
            if (line == null)
            {
                return;
            }

            for (var i = 0; i <= BloodSlashSegmentCount; i++)
            {
                var t = i / (float)BloodSlashSegmentCount;
                var x = Mathf.Lerp(-0.52f, 0.52f, t) * Mathf.Sign(facingSign == 0f ? 1f : facingSign) * scale;
                var y = (Mathf.Lerp(-0.22f, 0.34f, t) + Mathf.Sin(t * Mathf.PI) * 0.22f + verticalOffset) * scale;
                line.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        private static void SetFlameTongueGeometry(LineRenderer line, float xOffset, float height, float sway)
        {
            if (line == null)
            {
                return;
            }

            for (var i = 0; i <= FlameBurstTongueSegmentCount; i++)
            {
                var t = i / (float)FlameBurstTongueSegmentCount;
                var taper = 1f - t;
                var curl = Mathf.Sin(t * Mathf.PI * 1.35f) * sway * taper;
                var lick = Mathf.Sin((t * 2.6f + xOffset) * Mathf.PI) * 0.035f * taper;
                var x = xOffset * taper + curl + lick;
                var y = t * height + Mathf.Sin(t * Mathf.PI) * 0.08f;
                line.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        private static float ResolveAttackFacingSign(Transform sourceAnchor, Transform targetAnchor)
        {
            if (sourceAnchor == null || targetAnchor == null)
            {
                return 1f;
            }

            return sourceAnchor.position.x <= targetAnchor.position.x ? 1f : -1f;
        }

        private static Vector3 ResolveLanternSkillSourcePosition(Transform sourceAnchor, Transform targetAnchor)
        {
            if (sourceAnchor == null)
            {
                return Vector3.zero;
            }

            var facingSign = ResolveAttackFacingSign(sourceAnchor, targetAnchor);
            return ResolveAnchorVisualCenterWorldPosition(sourceAnchor) +
                new Vector3(LanternMuzzleLocalX * facingSign, LanternMuzzleLocalY, 0f);
        }

        // Slash arcs spawn from the authored player-front offset and mirror toward the target.
        private Vector3 ResolveSlashSkillSourcePosition(SkillSO skill, Transform sourceAnchor, Transform targetAnchor)
        {
            if (sourceAnchor == null)
            {
                return Vector3.zero;
            }

            var facingSign = ResolveAttackFacingSign(sourceAnchor, targetAnchor);
            var localOffset = ResolvePlayerFrontLocalOffset(ResolveDesignTimeLocalOffset(
                ResolveSkillVfxTuning(skill),
                ResolveSkillVfxPackage(skill),
                ResolveSkillVfxDesignTimeBinding(skill),
                PlayerFrontAttackArtLocalOffset));
            localOffset.x = Mathf.Abs(localOffset.x) * facingSign;
            return ResolveAnchorVisualCenterWorldPosition(sourceAnchor) + localOffset;
        }

        private static Vector3 ResolveLanternSkillLocalOffset(Transform sourceAnchor, Transform targetAnchor)
        {
            if (sourceAnchor == null)
            {
                return Vector3.zero;
            }

            return sourceAnchor.InverseTransformPoint(ResolveLanternSkillSourcePosition(sourceAnchor, targetAnchor));
        }

        private static Vector3 ResolveSkillImpactWorldPosition(Transform targetAnchor)
        {
            return targetAnchor != null
                ? ResolveAnchorVisualCenterWorldPosition(targetAnchor) + new Vector3(0f, 0.18f, 0f)
                : Vector3.zero;
        }

        private void PlayLanternSkillLaunchCue(SkillSO skill, Transform sourceAnchor, Transform targetAnchor)
        {
            if (skill == null || sourceAnchor == null || targetAnchor == null)
            {
                return;
            }

            var sourcePosition = ResolveLanternSkillSourcePosition(sourceAnchor, targetAnchor);
            var targetPosition = ResolveSkillImpactWorldPosition(targetAnchor);
            if ((targetPosition - sourcePosition).sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var primary = ResolveSkillTintedColor(
                ResolveReusableSkillParticleColor(skill),
                new Color(1f, 0.78f, 0.22f, 0.86f),
                0.28f,
                0.78f);
            var secondary = ResolveSkillTintedColor(
                ResolveReusableSkillSecondaryParticleColor(skill),
                new Color(1f, 1f, 0.72f, 0.72f),
                0.18f,
                0.58f);

            var trailObject = new GameObject($"{ResolveSkillVfxFamily(skill)}LanternLaunchTrail", typeof(LineRenderer));
            trailObject.transform.SetParent(transform, false);
            var line = trailObject.GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, sourcePosition);
            line.SetPosition(1, targetPosition);
            line.startWidth = Mathf.Clamp(0.038f * Mathf.Max(0.7f, skill.vfxScale), 0.026f, 0.07f);
            line.endWidth = Mathf.Clamp(0.082f * Mathf.Max(0.7f, skill.vfxScale), 0.05f, 0.13f);
            line.numCapVertices = 8;
            line.numCornerVertices = 3;
            line.sharedMaterial = ResolveRuntimeSkillParticleMaterial(trailObject.name, primary);
            line.startColor = primary;
            line.endColor = secondary;
            ApplyAnchorSorting(line, targetAnchor, 8);

            SpawnParticleBurst(
                null,
                sourceAnchor,
                "LanternSkillMuzzleParticles",
                primary,
                null,
                0.28f,
                Mathf.RoundToInt(12 * Mathf.Max(0.8f, skill.vfxIntensity)),
                0.22f,
                Mathf.Clamp(0.07f * Mathf.Max(0.8f, skill.vfxScale), 0.045f, 0.1f),
                swirl: true,
                ResolveLanternSkillLocalOffset(sourceAnchor, targetAnchor));

            if (Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(AnimateLanternLaunchCueRoutine(
                    trailObject,
                    line,
                    sourcePosition,
                    targetPosition,
                    0.32f));
            }
        }

        private static IEnumerator AnimateLanternLaunchCueRoutine(
            GameObject root,
            LineRenderer line,
            Vector3 sourcePosition,
            Vector3 targetPosition,
            float durationSeconds)
        {
            var duration = Mathf.Max(0.05f, durationSeconds);
            var elapsed = 0f;
            var startColor = line != null ? line.startColor : Color.clear;
            var endColor = line != null ? line.endColor : Color.clear;
            while (elapsed < duration)
            {
                if (root == null || line == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var reveal = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress / 0.42f), 3f);
                var alpha = 1f - Mathf.Clamp01((progress - 0.32f) / 0.68f);
                line.SetPosition(0, sourcePosition);
                line.SetPosition(1, Vector3.Lerp(sourcePosition, targetPosition, reveal));
                SetLineAlpha(line, startColor, endColor, alpha);
                yield return null;
            }

            if (root != null)
            {
                Destroy(root);
            }
        }

        private void PreviewGatherLightReleaseIfNeeded(SkillSO skill, Transform targetAnchor)
        {
            if (!IsGatherLightSkill(skill) || targetAnchor == null)
            {
                return;
            }

            ClearGatherLightPreviewRelease();
            if (Application.isPlaying && isActiveAndEnabled)
            {
                gatherLightPreviewReleaseCoroutine = StartCoroutine(PlayGatherLightPreviewReleaseRoutine(skill, targetAnchor));
                return;
            }

            PlayGatherLightReleasedAttackEffect(skill, targetAnchor, playAttackAnimation: true);
        }

        private IEnumerator PlayGatherLightPreviewReleaseRoutine(SkillSO skill, Transform targetAnchor)
        {
            yield return new WaitForSeconds(GatherLightPreviewReleaseDelaySeconds);
            gatherLightPreviewReleaseCoroutine = null;
            if (skill == null || targetAnchor == null)
            {
                yield break;
            }

            PlayGatherLightReleasedAttackEffect(skill, targetAnchor, playAttackAnimation: true);
        }

        private void ClearGatherLightPreviewRelease()
        {
            if (gatherLightPreviewReleaseCoroutine == null)
            {
                return;
            }

            StopCoroutine(gatherLightPreviewReleaseCoroutine);
            gatherLightPreviewReleaseCoroutine = null;
        }

        private void PlayGatherLightReleasedAttackEffect(SkillSO skill, Transform targetAnchor, bool playAttackAnimation)
        {
            if (targetAnchor == null)
            {
                return;
            }

            if (playAttackAnimation)
            {
                PlayPlayerAttackAnimation();
            }

            var sourceAnchor = ResolvePlayerAnchor() ?? transform;
            // 빛 모으기 릴리즈: 홀리 파이어볼 투사체를 발사하고, 투사체가 적에게 닿는 시점에
            // 적의 발쪽에서 버티컬 빔이 솟아오른다. 수평 충전 빔/랜턴 발사 트레일은 쓰지 않는다.
            var firedProjectile = TryPlayProjectileSkillEffect(
                skill,
                skill != null ? skill.activationEffect : null,
                sourceAnchor,
                targetAnchor,
                enemyAnimator,
                out _);
            var beamDelaySeconds = firedProjectile ? ResolveGatherLightProjectileTravelSeconds(skill) : 0f;
            PlayGatherLightVerticalBeamAfterDelay(skill, targetAnchor, beamDelaySeconds);
        }

        private static float ResolveGatherLightProjectileTravelSeconds(SkillSO skill)
        {
            var prefab = skill != null && skill.activationEffect != null ? skill.activationEffect.vfxPrefab : null;
            var projectile = prefab != null ? prefab.GetComponentInChildren<CombatProjectileEffect>(true) : null;
            return projectile != null ? projectile.TravelSeconds : 0f;
        }

        private void PlayGatherLightVerticalBeamAfterDelay(SkillSO skill, Transform targetAnchor, float delaySeconds)
        {
            if (delaySeconds > 0f && Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(PlayGatherLightVerticalBeamAfterDelayRoutine(skill, targetAnchor, delaySeconds));
                return;
            }

            PlayGatherLightVerticalBeamEffect(skill, targetAnchor);
        }

        private IEnumerator PlayGatherLightVerticalBeamAfterDelayRoutine(SkillSO skill, Transform targetAnchor, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            if (targetAnchor == null)
            {
                yield break;
            }

            PlayGatherLightVerticalBeamEffect(skill, targetAnchor);
        }

        private SkillSO ResolvePlayerChargedLightSkill(string skillName)
        {
            var skills = combatManager?.Player?.Skills;
            if (skills == null)
            {
                return null;
            }

            var gatherLight = skills.FirstOrDefault(IsGatherLightSkill);
            if (gatherLight != null)
            {
                return gatherLight;
            }

            return skills.FirstOrDefault(skill =>
                skill != null &&
                skill.ResolveEffectKind() == SkillEffectKind.ChargeAttack &&
                ResolveSkillVfxFamily(skill) == SkillVfxFamily.LightBeam &&
                (string.IsNullOrWhiteSpace(skillName) ||
                    string.Equals(skill.skillName, skillName, System.StringComparison.Ordinal)));
        }

        private void PlayGatherLightVerticalBeamEffect(SkillSO skill, Transform targetAnchor)
        {
            var prefab = ResolveGatherLightVerticalBeamPrefab(skill);
            if (prefab == null || targetAnchor == null)
            {
                return;
            }

            // Keep the follow-up beam anchored near the enemy's feet without depending on HP bar layout.
            var position = ResolveGatherLightVerticalBeamWorldPosition(targetAnchor);
            var instance = Instantiate(prefab, position, Quaternion.identity, transform);
            instance.name = "GatherLightVerticalBeam";
            instance.transform.localScale = Vector3.one * Mathf.Clamp(
                Mathf.Sqrt(Mathf.Max(0.01f, skill != null ? skill.vfxScale : 1f)),
                0.9f,
                1.55f);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                ApplyAnchorSorting(renderer, targetAnchor, 18);
            }

            var color = ResolveGatherLightVerticalBeamColor(skill);
            foreach (var visualEffect in instance.GetComponentsInChildren<VisualEffect>(true))
            {
                ApplyGatherLightVerticalBeamColor(visualEffect, color);
                if (Application.isPlaying)
                {
                    visualEffect.Reinit();
                    visualEffect.Play();
                }
            }

            if (Application.isPlaying)
            {
                Destroy(instance, GatherLightVerticalBeamLifetimeSeconds + 0.2f);
            }
        }

        private Vector3 ResolveGatherLightVerticalBeamWorldPosition(Transform targetAnchor)
        {
            var position = targetAnchor.position;
            position.y += GatherLightVerticalBeamTargetYOffset;
            return position;
        }

        private static GameObject ResolveGatherLightVerticalBeamPrefab(SkillSO skill)
        {
            var tuning = ResolveSkillVfxTuning(skill);
            if (tuning != null && tuning.secondaryPrefab != null)
            {
                return tuning.secondaryPrefab;
            }

            var package = ResolveSkillVfxPackage(skill);
            return package != null ? package.secondaryPrefab : null;
        }

        private static Color ResolveGatherLightVerticalBeamColor(SkillSO skill)
        {
            if (skill == null)
            {
                return new Color(0.72f, 0.94f, 1f, 0.96f);
            }

            var primary = ResolveReusableSkillParticleColor(skill);
            var secondary = ResolveReusableSkillSecondaryParticleColor(skill);
            var color = Color.Lerp(primary, secondary, 0.22f);
            color = Color.Lerp(color, new Color(0.72f, 0.94f, 1f, 1f), 0.48f);
            color.a = Mathf.Max(0.92f, color.a);
            return color;
        }

        private static void ApplyGatherLightVerticalBeamColor(VisualEffect visualEffect, Color color)
        {
            if (visualEffect == null)
            {
                return;
            }

            var vectorColor = new Vector4(color.r, color.g, color.b, color.a);
            var vectorRgb = new Vector3(color.r, color.g, color.b);
            foreach (var propertyName in GatherLightVerticalBeamColorPropertyNames)
            {
                if (visualEffect.HasVector4(propertyName))
                {
                    visualEffect.SetVector4(propertyName, vectorColor);
                }

                if (visualEffect.HasVector3(propertyName))
                {
                    visualEffect.SetVector3(propertyName, vectorRgb);
                }
            }
        }

        private void PlayChargedLightBeamEffect(EnemyController target)
        {
            var targetTransform = target != null && enemyRenderer != null ? enemyRenderer.transform : transform;
            PlayChargedLightBeamEffect(null, targetTransform);
        }

        private void PlayChargedLightBeamEffect(Transform targetTransform)
        {
            PlayChargedLightBeamEffect(null, targetTransform);
        }

        private void PlayChargedLightBeamEffect(SkillSO skill, Transform targetTransform)
        {
            var sourceTransform = ResolvePlayerAnchor() ?? transform;
            var sourcePosition = ResolveLanternSkillSourcePosition(sourceTransform, targetTransform);
            var targetPosition = ResolveSkillImpactWorldPosition(targetTransform);

            var glowLine = SpawnChargedLightBeamLine(
                "ChargedLightBeamGlow",
                sourcePosition,
                targetPosition,
                targetTransform,
                new Color(0.72f, 0.92f, 1f, 0.34f),
                new Color(1f, 1f, 1f, 0.22f),
                0.28f,
                0.42f,
                7);
            var line = SpawnChargedLightBeamLine(
                "ChargedLightBeam",
                sourcePosition,
                targetPosition,
                targetTransform,
                new Color(0.86f, 0.97f, 1f, 0.95f),
                new Color(1f, 1f, 1f, 0.78f),
                0.08f,
                0.16f,
                9);

            SpawnParticleBurst(
                null,
                sourceTransform,
                "ChargedLightBeamMuzzleParticles",
                new Color(0.82f, 0.95f, 1f, 0.8f),
                null,
                0.36f,
                14,
                0.18f,
                0.11f,
                swirl: true,
                ResolveLanternSkillLocalOffset(sourceTransform, targetTransform));

            SpawnParticleBurst(
                null,
                targetTransform,
                "ChargedLightBeamImpactParticles",
                new Color(0.9f, 0.98f, 1f, 0.86f),
                null,
                0.45f,
                18,
                0.3f,
                0.14f,
                swirl: true);

            SpawnChargedLightAttackArt(skill, sourceTransform);

            if (Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(AnimateChargedLightBeamRoutine(
                    line,
                    glowLine,
                    sourcePosition,
                    targetPosition,
                    ChargedLightBeamDurationSeconds));
            }
        }

        private void SpawnChargedLightAttackArt(SkillSO skill, Transform sourceTransform)
        {
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxFamily(skill) == SkillVfxFamily.LightBeam
                ? ResolveSkillVfxDesignTimeBinding(skill)
                : ResolveSkillVfxDesignTimeBinding(SkillVfxFamily.LightBeam);
            var art = SpawnAttackArtSpriteLayer(
                sourceTransform,
                "ChargedLightAttackArt",
                ResolveDesignTimeArtColor(Color.white, tuning, package, designTimeBinding, 0.18f, 0.92f),
                AttackArtBaseRadius * ResolveDesignTimeRadiusMultiplier(
                    tuning,
                    package,
                    designTimeBinding,
                    1.18f * AttackEffectArtSizeMultiplier),
                ResolveDesignTimeLifetime(tuning, package, designTimeBinding, ChargedLightBeamDurationSeconds),
                ResolvePlayerFrontLocalOffset(ResolveDesignTimeLocalOffset(
                    tuning,
                    package,
                    designTimeBinding,
                    PlayerFrontAttackArtLocalOffset)),
                sortingOffset: ResolveDesignTimeSortingOffset(tuning, package, designTimeBinding, 13),
                spriteOverride: ResolveDesignTimeSprite(tuning, package, designTimeBinding, attackEffectSprite, ResolveAttackEffectSprite()),
                prefabOverride: ResolveDesignTimePrefab(tuning, package, designTimeBinding, ResolveAttackEffectPrefab()));
            if (art == null)
            {
                return;
            }

            art.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                ResolveDesignTimeRotationDegrees(tuning, package, designTimeBinding, -10f));
        }

        private LineRenderer SpawnChargedLightBeamLine(
            string objectName,
            Vector3 sourcePosition,
            Vector3 targetPosition,
            Transform targetTransform,
            Color startColor,
            Color endColor,
            float startWidth,
            float endWidth,
            int sortingOffset)
        {
            var beamObject = new GameObject(objectName, typeof(LineRenderer));
            beamObject.transform.SetParent(transform, false);

            var line = beamObject.GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, sourcePosition);
            line.SetPosition(1, targetPosition);
            line.startWidth = Mathf.Max(0.01f, startWidth);
            line.endWidth = Mathf.Max(0.01f, endWidth);
            line.numCapVertices = 10;
            line.numCornerVertices = 4;
            line.sharedMaterial = ResolveChargedLightBeamMaterial();
            line.startColor = startColor;
            line.endColor = endColor;

            ApplyAnchorSorting(line, targetTransform, sortingOffset);
            return line;
        }

        private static IEnumerator AnimateChargedLightBeamRoutine(
            LineRenderer line,
            LineRenderer glowLine,
            Vector3 sourcePosition,
            Vector3 targetPosition,
            float durationSeconds)
        {
            var duration = Mathf.Max(0.05f, durationSeconds);
            var startColor = line != null ? line.startColor : Color.white;
            var endColor = line != null ? line.endColor : Color.white;
            var glowStartColor = glowLine != null ? glowLine.startColor : Color.clear;
            var glowEndColor = glowLine != null ? glowLine.endColor : Color.clear;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (line == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var travelProgress = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress / 0.34f), 3f);
                var currentEnd = Vector3.Lerp(sourcePosition, targetPosition, travelProgress);
                line.SetPosition(0, sourcePosition);
                line.SetPosition(1, currentEnd);
                if (glowLine != null)
                {
                    glowLine.SetPosition(0, sourcePosition);
                    glowLine.SetPosition(1, currentEnd);
                }

                var alpha = 1f - Mathf.Clamp01((progress - 0.22f) / 0.78f);
                var nextStart = startColor;
                var nextEnd = endColor;
                nextStart.a *= alpha;
                nextEnd.a *= alpha;
                line.startColor = nextStart;
                line.endColor = nextEnd;
                if (glowLine != null)
                {
                    var nextGlowStart = glowStartColor;
                    var nextGlowEnd = glowEndColor;
                    nextGlowStart.a *= alpha;
                    nextGlowEnd.a *= alpha;
                    glowLine.startColor = nextGlowStart;
                    glowLine.endColor = nextGlowEnd;
                }

                yield return null;
            }

            if (line != null)
            {
                Destroy(line.gameObject);
            }

            if (glowLine != null)
            {
                Destroy(glowLine.gameObject);
            }
        }

        private bool PlayTentacleStrikeSkillEffect(SkillSO skill, Transform sourceAnchor, Transform targetAnchor)
        {
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);
            var prefab = ResolveDesignTimePrefab(tuning, package, designTimeBinding, null);
            if (prefab == null || prefab.GetComponentInChildren<Animator>(true) == null)
            {
                return false;
            }

            var source = ResolveAnchorVisualCenterWorldPosition(sourceAnchor != null ? sourceAnchor : transform);
            var localOffset = ResolveDesignTimeLocalOffset(tuning, package, designTimeBinding, Vector3.zero);
            var target = targetAnchor != null ? ResolveSkillImpactWorldPosition(targetAnchor) : source + Vector3.right;
            if ((target - source).sqrMagnitude <= 0.0001f)
            {
                target = source + Vector3.right;
            }

            var spawnPosition = source + (sourceAnchor != null
                ? sourceAnchor.TransformVector(localOffset)
                : localOffset);
            var root = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
            root.name = "TentacleStrikeWhip";
            root.transform.localRotation = Quaternion.identity;

            var renderer = root.GetComponentInChildren<SpriteRenderer>(true);
            var animator = root.GetComponentInChildren<Animator>(true);
            if (renderer == null || animator == null)
            {
                DestroySpawnedObject(root);
                return false;
            }

            var scale = Mathf.Clamp(Mathf.Max(0.01f, skill != null ? skill.vfxScale : 1f), 0.01f, 1.8f);
            var facingSign = target.x >= source.x ? 1f : -1f;
            var authoredScale = root.transform.localScale;
            root.transform.localScale = new Vector3(
                Mathf.Abs(authoredScale.x) * scale * facingSign,
                Mathf.Abs(authoredScale.y) * scale,
                authoredScale.z);

            var sortingAnchor = sourceAnchor != null ? sourceAnchor : targetAnchor;
            ApplyAnchorSorting(renderer, sortingAnchor, 12);
            ApplyAuthoredChildRendererSorting(root, renderer, sortingAnchor, 13);

            animator.enabled = true;
            if (animator.runtimeAnimatorController != null)
            {
                animator.Play("Tentacle Attack", 0, 0f);
                if (!Application.isPlaying)
                {
                    animator.Update(0f);
                }
            }

            var animationDuration = ResolvePrefabVisualDurationSeconds(prefab, TentacleStrikeDurationSeconds);
            if (Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(PlayTentacleStrikeImpactAfterDelay(
                    skill,
                    targetAnchor,
                    sourceAnchor,
                    root,
                    animationDuration));
            }
            else
            {
                PlayTentacleStrikeImpact(skill, targetAnchor, sourceAnchor);
            }

            return true;
        }

        private IEnumerator PlayTentacleStrikeImpactAfterDelay(
            SkillSO skill,
            Transform targetAnchor,
            Transform sourceAnchor,
            GameObject tentacleRoot,
            float delaySeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delaySeconds));
            PlayTentacleStrikeImpact(skill, targetAnchor, sourceAnchor);

            if (tentacleRoot != null)
            {
                Destroy(tentacleRoot, 0.05f);
            }
        }

        private void PlayTentacleStrikeImpact(SkillSO skill, Transform targetAnchor, Transform sourceAnchor)
        {
            PlaySpikedBurstSkillEffect(skill, targetAnchor, sourceAnchor);
        }

        private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            var inverse = 1f - Mathf.Clamp01(t);
            var clamped = Mathf.Clamp01(t);
            return inverse * inverse * a + 2f * inverse * clamped * b + clamped * clamped * c;
        }

        private static void SetLineAlpha(LineRenderer line, Color color, float alpha)
        {
            if (line == null)
            {
                return;
            }

            color.a *= Mathf.Clamp01(alpha);
            line.startColor = color;
            line.endColor = color;
        }

        private static void SetLineAlpha(LineRenderer line, Color startColor, Color endColor, float alpha)
        {
            if (line == null)
            {
                return;
            }

            startColor.a *= Mathf.Clamp01(alpha);
            endColor.a *= Mathf.Clamp01(alpha);
            line.startColor = startColor;
            line.endColor = endColor;
        }

        private static Color ResolveSkillTintedColor(Color color, Color tint, float tintWeight, float minimumAlpha)
        {
            if (color.a <= 0f)
            {
                color = tint;
            }

            var resolved = Color.Lerp(color, tint, Mathf.Clamp01(tintWeight));
            resolved.a = Mathf.Max(Mathf.Clamp01(minimumAlpha), Mathf.Min(1f, color.a));
            return resolved;
        }

        private static void ResolveReusableSkillParticleDefaults(
            SkillVfxFamily family,
            out float lifetimeSeconds,
            out int burstCount,
            out float startSpeed,
            out float startSize,
            out bool swirl)
        {
            switch (family)
            {
                case SkillVfxFamily.SlashArc:
                    lifetimeSeconds = 0.55f;
                    burstCount = 22;
                    startSpeed = 0.8f;
                    startSize = 0.16f;
                    swirl = false;
                    break;
                case SkillVfxFamily.LightProjectile:
                    lifetimeSeconds = 0.75f;
                    burstCount = 28;
                    startSpeed = 0.75f;
                    startSize = 0.16f;
                    swirl = false;
                    break;
                case SkillVfxFamily.ShieldDome:
                    lifetimeSeconds = 0.8f;
                    burstCount = 32;
                    startSpeed = 0.35f;
                    startSize = 0.18f;
                    swirl = true;
                    break;
                case SkillVfxFamily.ImpactBurst:
                    lifetimeSeconds = 0.55f;
                    burstCount = 34;
                    startSpeed = 1f;
                    startSize = 0.2f;
                    swirl = false;
                    break;
                case SkillVfxFamily.BuffAura:
                    lifetimeSeconds = 0.85f;
                    burstCount = 28;
                    startSpeed = 0.3f;
                    startSize = 0.16f;
                    swirl = true;
                    break;
                case SkillVfxFamily.DebuffWave:
                    lifetimeSeconds = 0.7f;
                    burstCount = 26;
                    startSpeed = 0.55f;
                    startSize = 0.16f;
                    swirl = true;
                    break;
                case SkillVfxFamily.DrainTether:
                    lifetimeSeconds = 0.8f;
                    burstCount = 30;
                    startSpeed = 0.45f;
                    startSize = 0.17f;
                    swirl = true;
                    break;
                case SkillVfxFamily.CounterReady:
                    lifetimeSeconds = 0.75f;
                    burstCount = 28;
                    startSpeed = 0.35f;
                    startSize = 0.16f;
                    swirl = true;
                    break;
                case SkillVfxFamily.BoardDisturb:
                    lifetimeSeconds = 0.85f;
                    burstCount = 34;
                    startSpeed = 0.42f;
                    startSize = 0.18f;
                    swirl = true;
                    break;
                case SkillVfxFamily.SupportFire:
                    lifetimeSeconds = 0.55f;
                    burstCount = 36;
                    startSpeed = 1.05f;
                    startSize = 0.14f;
                    swirl = false;
                    break;
                case SkillVfxFamily.LightBeam:
                    lifetimeSeconds = ChargedLightBeamDurationSeconds;
                    burstCount = 30;
                    startSpeed = 0.35f;
                    startSize = 0.14f;
                    swirl = true;
                    break;
                case SkillVfxFamily.TentacleWhip:
                    lifetimeSeconds = TentacleStrikeDurationSeconds;
                    burstCount = 24;
                    startSpeed = 0.42f;
                    startSize = 0.15f;
                    swirl = true;
                    break;
                case SkillVfxFamily.SpikedBurst:
                    lifetimeSeconds = HeavyStrikeSpikedBurstDurationSeconds;
                    burstCount = 40;
                    startSpeed = 1.25f;
                    startSize = 0.16f;
                    swirl = false;
                    break;
                case SkillVfxFamily.BloodFountainSlash:
                    lifetimeSeconds = BloodFountainSlashDurationSeconds;
                    burstCount = 42;
                    startSpeed = 0.12f;
                    startSize = 0.13f;
                    swirl = false;
                    break;
                case SkillVfxFamily.FlameBurst:
                    lifetimeSeconds = FlameBurstDurationSeconds;
                    burstCount = 38;
                    startSpeed = 0.5f;
                    startSize = 0.13f;
                    swirl = false;
                    break;
                case SkillVfxFamily.DarkChainBurst:
                    lifetimeSeconds = DarkShackleChainDurationSeconds;
                    burstCount = 34;
                    startSpeed = 0.78f;
                    startSize = 0.13f;
                    swirl = false;
                    break;
                default:
                    lifetimeSeconds = 0.65f;
                    burstCount = 24;
                    startSpeed = 0.6f;
                    startSize = 0.16f;
                    swirl = false;
                    break;
            }
        }

        private static Color ResolveReusableSkillParticleColor(SkillSO skill)
        {
            var primary = skill.vfxPrimaryColor;
            if (primary.a <= 0f)
            {
                primary.a = 1f;
            }

            return primary;
        }

        private static Color ResolveReusableSkillSecondaryParticleColor(SkillSO skill)
        {
            var secondary = skill.vfxSecondaryColor;
            if (secondary.a <= 0f)
            {
                secondary.a = 1f;
            }

            return secondary;
        }


        private void RenderBackground()
        {
            var backgroundSprite = ResolveStageBackgroundSprite(currentStage) ?? defaultBackgroundSprite;
            if (backgroundRenderer != null && backgroundSprite != null)
            {
                backgroundRenderer.sprite = backgroundSprite;
            }
        }

        private Sprite ResolveStageBackgroundSprite(StageSO stage)
        {
            if (stage == null)
            {
                return null;
            }

            if (stage.PresentationBackgroundSprite != null)
            {
                return stage.PresentationBackgroundSprite;
            }

            return stage.Floor switch
            {
                StageFloor.Upper => upperStageBackgroundSprite,
                StageFloor.Middle => middleStageBackgroundSprite,
                StageFloor.Lower => lowerStageBackgroundSprite,
                _ => null,
            };
        }

        private Sprite ResolveEnemySprite(CombatSnapshot currentSnapshot)
        {
            return ResolveEnemyData(currentSnapshot)?.portrait;
        }

        private EnemySO ResolveEnemyData(CombatSnapshot currentSnapshot)
        {
            var enemyIndex = currentSnapshot?.Enemies?.FirstOrDefault()?.EnemyIndex ?? 0;
            var enemies = combatManager?.Enemies;
            if (enemies == null || enemyIndex < 0 || enemyIndex >= enemies.Count)
            {
                return null;
            }

            return enemies[enemyIndex]?.Data;
        }





        private ParticleSystem SpawnParticleBurst(
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
            bool swirl,
            Vector3 localOffset = default)
        {
            var prefab = effect?.particlePrefab != null ? effect.particlePrefab : fallbackPrefab;
            var material = effect?.particleMaterial != null ? effect.particleMaterial : fallbackMaterial;
            var color = effect != null ? effect.ResolveColor(fallbackColor) : fallbackColor;

            var objectName = effect?.ResolveObjectName(fallbackObjectName) ?? fallbackObjectName;
            var lifetimeSeconds = effect != null ? effect.EffectiveLifetimeSeconds : fallbackLifetimeSeconds;
            var burstCount = effect != null ? effect.EffectiveBurstCount : fallbackBurstCount;
            var startSpeed = effect != null ? effect.EffectiveStartSpeed : fallbackStartSpeed;
            var startSize = effect != null ? effect.EffectiveStartSize : fallbackStartSize;
            var shouldSwirl = effect != null ? effect.swirl : swirl;

            return SpawnParticleBurst(
                prefab,
                anchor,
                objectName,
                color,
                material,
                lifetimeSeconds,
                burstCount,
                startSpeed,
                startSize,
                shouldSwirl,
                localOffset);
        }

        private ParticleSystem SpawnParticleBurst(
            ParticleSystem prefab,
            Transform anchor,
            string objectName,
            Color color,
            Material material,
            float lifetimeSeconds,
            int burstCount,
            float startSpeed,
            float startSize,
            bool swirl,
            Vector3 localOffset = default,
            System.Action<ParticleSystem> configureOverride = null,
            Transform sortingAnchor = null)
        {
            var parent = anchor != null ? anchor : transform;
            var resolvedPrefab = prefab != null
                ? prefab
                : ResolveDesignTimeParticlePrefab(objectName, swirl);
            var particles = resolvedPrefab != null
                ? Instantiate(resolvedPrefab, parent.position, Quaternion.identity, parent)
                : CreateFallbackParticleSystem(parent, objectName);
            if (particles == null)
            {
                return null;
            }

            particles.gameObject.name = objectName;
            particles.transform.localPosition = localOffset;
            var resolvedMaterial = material ?? ResolveRuntimeSkillParticleMaterial(objectName, color);
            ConfigureParticleBurst(
                particles,
                color,
                resolvedMaterial,
                lifetimeSeconds,
                burstCount,
                startSpeed,
                startSize,
                sortingAnchor != null ? sortingAnchor : parent,
                swirl);
            configureOverride?.Invoke(particles);
            particles.Play(true);
            if (swirl && Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(SwirlParticleTransformRoutine(particles.transform, lifetimeSeconds));
            }

            if (lifetimeSeconds > 0f && Application.isPlaying)
            {
                Destroy(particles.gameObject, lifetimeSeconds + 0.2f);
            }

            return particles;
        }

        private ParticleSystem ResolveDesignTimeParticlePrefab(string objectName, bool swirl)
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.ResolveParticlePrefab(objectName, swirl) : null;
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
            var authoring = particles.GetComponentInParent<SkillVfxParticleBurstPrefab>();
            var burstMultiplier = authoring != null ? authoring.EffectiveBurstMultiplier : 1f;
            var lifetimeMultiplier = authoring != null ? authoring.EffectiveLifetimeMultiplier : 1f;
            var speedMultiplier = authoring != null ? authoring.EffectiveSpeedMultiplier : 1f;
            var sizeMultiplier = authoring != null ? authoring.EffectiveSizeMultiplier : 1f;
            var radiusMultiplier = authoring != null ? authoring.EffectiveRadiusMultiplier : 1f;
            var resolvedLifetimeSeconds = Mathf.Max(0.05f, lifetimeSeconds);

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Clear(true);

            var main = particles.main;
            main.duration = Mathf.Max(0.05f, resolvedLifetimeSeconds * 0.35f * lifetimeMultiplier);
            main.loop = false;
            main.startLifetime = Mathf.Max(0.05f, resolvedLifetimeSeconds * lifetimeMultiplier);
            main.startSpeed = Mathf.Max(0f, startSpeed * speedMultiplier);
            main.startSize = Mathf.Max(0.01f, startSize * sizeMultiplier);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.Clamp(Mathf.RoundToInt(burstCount * burstMultiplier), 1, short.MaxValue)),
            });

            var shape = particles.shape;
            shape.enabled = true;
            if (authoring == null || !authoring.preserveAuthoredShape)
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
            }

            shape.radius = 0.36f * radiusMultiplier;

            if (swirl)
            {
                ConfigureSwirlBurst(
                    particles,
                    resolvedLifetimeSeconds,
                    radiusMultiplier,
                    authoring != null && authoring.preserveAuthoredShape);
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
            renderer.sortingOrder = anchorRenderer.sortingOrder + (authoring != null ? authoring.sortingOffset : 2);
        }

        private static void ConfigureSwirlBurst(
            ParticleSystem particles,
            float lifetimeSeconds,
            float radiusMultiplier,
            bool preserveAuthoredShape)
        {
            if (!preserveAuthoredShape)
            {
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.54f * Mathf.Max(0.01f, radiusMultiplier);
                shape.radiusThickness = 0.32f;
                shape.arc = 360f;
            }

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

        private LineRenderer SpawnShieldCircleLine(
            Transform anchor,
            string objectName,
            Color color,
            float radius,
            float width,
            float lifetimeSeconds,
            Vector3 localOffset,
            bool spiked,
            int sortingOffset,
            Transform sortingAnchor = null,
            bool autoDestroy = true)
        {
            var parent = anchor != null ? anchor : transform;
            var lineObject = new GameObject(objectName, typeof(LineRenderer));
            lineObject.transform.SetParent(parent, false);
            lineObject.transform.localPosition = localOffset;

            var line = lineObject.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = ShieldCircleRingSegmentCount + 1;
            line.startWidth = Mathf.Max(0.01f, width);
            line.endWidth = Mathf.Max(0.01f, width);
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.sharedMaterial = ResolveRuntimeSkillParticleMaterial(objectName, color);
            line.startColor = color;
            line.endColor = color;

            for (var i = 0; i <= ShieldCircleRingSegmentCount; i++)
            {
                var angle = Mathf.PI * 2f * i / ShieldCircleRingSegmentCount;
                var spikeMultiplier = 1f;
                if (spiked)
                {
                    spikeMultiplier = i % 2 == 0 ? 1.16f : 0.94f;
                    if (i % 6 == 0)
                    {
                        spikeMultiplier = 1.26f;
                    }
                }

                var resolvedRadius = Mathf.Max(0.01f, radius * spikeMultiplier);
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * resolvedRadius, Mathf.Sin(angle) * resolvedRadius, 0f));
            }

            ApplyAnchorSorting(line, sortingAnchor != null ? sortingAnchor : parent, sortingOffset);
            if (autoDestroy && lifetimeSeconds > 0f && Application.isPlaying)
            {
                if (isActiveAndEnabled)
                {
                    StartCoroutine(FadeLineRendererRoutine(line, lifetimeSeconds));
                }

                Destroy(lineObject, lifetimeSeconds + 0.2f);
            }

            return line;
        }

        private void PlayAttackArtForReusableSkill(SkillSO skill, Transform targetAnchor, Transform sourceAnchor)
        {
            var family = ResolveSkillVfxFamily(skill);
            if (skill == null ||
                (family != SkillVfxFamily.SlashArc &&
                    family != SkillVfxFamily.LightProjectile &&
                    family != SkillVfxFamily.ImpactBurst))
            {
                return;
            }

            var scale = Mathf.Max(0.01f, skill.vfxScale);
            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);
            var color = ResolveDesignTimeArtColor(
                ResolveReusableSkillParticleColor(skill),
                tuning,
                package,
                designTimeBinding,
                0.18f,
                0.9f);
            var usesHitImpactArt = family == SkillVfxFamily.ImpactBurst ||
                family == SkillVfxFamily.SlashArc;
            var fallbackSprite = usesHitImpactArt
                ? ResolveHitEffectSprite()
                : ResolveAttackEffectSprite();
            var explicitSprite = usesHitImpactArt ? hitEffectSprite : attackEffectSprite;
            var fallbackPrefab = usesHitImpactArt
                ? ResolveHitEffectPrefab()
                : ResolveAttackEffectPrefab();
            var anchor = UsesPlayerFrontAttackArt(family) ? sourceAnchor : targetAnchor;
            var localOffset = ResolveDesignTimeLocalOffset(
                tuning,
                package,
                designTimeBinding,
                UsesPlayerFrontAttackArt(family) ? PlayerFrontAttackArtLocalOffset : new Vector3(0f, 0.16f, 0f));
            if (UsesPlayerFrontAttackArt(family))
            {
                localOffset = ResolvePlayerFrontLocalOffset(localOffset);
            }

            var art = SpawnAttackArtSpriteLayer(
                anchor,
                family switch
                {
                    SkillVfxFamily.ImpactBurst => "HitImpactEffectArt",
                    SkillVfxFamily.SlashArc => "HitImpactEffectArt",
                    SkillVfxFamily.LightProjectile => "LightProjectileEffectArt",
                    _ => "AttackEffectArt",
                },
                color,
                AttackArtBaseRadius *
                    Mathf.Clamp(Mathf.Sqrt(scale), 0.7f, 1.42f) *
                    ResolveDesignTimeRadiusMultiplier(
                        tuning,
                        package,
                        designTimeBinding,
                        usesHitImpactArt
                            ? 1.08f * HitEffectArtSizeMultiplier
                            : AttackEffectArtSizeMultiplier),
                ResolveDesignTimeLifetime(tuning, package, designTimeBinding, AttackArtLifetimeSeconds),
                localOffset,
                sortingOffset: ResolveDesignTimeSortingOffset(tuning, package, designTimeBinding, 12),
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

            var facingSign = UsesPlayerFrontAttackArt(family)
                ? 1f
                : ResolveAttackFacingSign(sourceAnchor, anchor);
            art.transform.localRotation = Quaternion.Euler(
                0f,
                facingSign >= 0f ? 0f : 180f,
                ResolveDesignTimeRotationDegrees(tuning, package, designTimeBinding, -12f) * facingSign);
        }

        private SpriteRenderer SpawnAttackArtSpriteLayer(
            Transform anchor,
            string objectName,
            Color color,
            float radius,
            float lifetimeSeconds,
            Vector3 localOffset,
            int sortingOffset,
            Sprite spriteOverride = null,
            GameObject prefabOverride = null,
            bool autoDestroy = true,
            bool animatePulse = true)
        {
            var sprite = spriteOverride != null ? spriteOverride : ResolveAttackEffectSprite();
            if (prefabOverride != null)
            {
                var authoredRenderer = TrySpawnAuthoredSpriteEffectLayer(
                    prefabOverride,
                    anchor,
                    objectName,
                    color,
                    radius,
                    AttackArtDiameterMultiplier,
                    localOffset,
                    sortingOffset,
                    anchor,
                    sprite,
                    out var authoredRoot);
                if (authoredRenderer != null)
                {
                    var authoredBaseScale = authoredRenderer.transform.localScale;
                    if (Application.isPlaying && isActiveAndEnabled && animatePulse)
                    {
                        StartCoroutine(AnimateAttackArtPulseRoutine(
                            authoredRenderer,
                            Mathf.Max(0.05f, lifetimeSeconds),
                            authoredBaseScale,
                            color));
                    }

                    if (autoDestroy && lifetimeSeconds > 0f && Application.isPlaying)
                    {
                        Destroy(authoredRoot, lifetimeSeconds + 0.15f);
                    }

                    return authoredRenderer;
                }
            }

            if (sprite == null)
            {
                return null;
            }

            var parent = anchor != null ? anchor : transform;
            var spriteObject = new GameObject(objectName, typeof(SpriteRenderer));
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = localOffset;

            var renderer = spriteObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            var baseScale = Vector3.one * ResolveEffectArtScale(sprite, radius, AttackArtDiameterMultiplier);
            spriteObject.transform.localScale = baseScale;
            ApplyAnchorSorting(renderer, parent, sortingOffset);

            if (Application.isPlaying && isActiveAndEnabled && animatePulse)
            {
                StartCoroutine(AnimateAttackArtPulseRoutine(
                    renderer,
                    Mathf.Max(0.05f, lifetimeSeconds),
                    baseScale,
                    color));
            }

            if (autoDestroy && lifetimeSeconds > 0f && Application.isPlaying)
            {
                Destroy(spriteObject, lifetimeSeconds + 0.15f);
            }

            return renderer;
        }

        private SpriteRenderer TrySpawnAuthoredSpriteEffectLayer(
            GameObject prefab,
            Transform anchor,
            string objectName,
            Color color,
            float radius,
            float diameterMultiplier,
            Vector3 localOffset,
            int sortingOffset,
            Transform sortingAnchor,
            Sprite spriteOverride,
            out GameObject rootObject)
        {
            rootObject = null;
            if (prefab == null)
            {
                return null;
            }

            var parent = anchor != null ? anchor : transform;
            rootObject = Instantiate(prefab, parent);
            rootObject.name = objectName;
            rootObject.transform.localPosition = localOffset;
            rootObject.transform.localRotation = Quaternion.identity;

            var renderer = rootObject.GetComponent<SpriteRenderer>()
                ?? rootObject.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer == null)
            {
                DestroySpawnedObject(rootObject);
                rootObject = null;
                return null;
            }

            if (spriteOverride != null)
            {
                renderer.sprite = spriteOverride;
            }

            var sprite = renderer.sprite;
            if (sprite == null)
            {
                DestroySpawnedObject(rootObject);
                rootObject = null;
                return null;
            }

            renderer.color = color;
            var authoredScale = renderer.transform.localScale;
            renderer.transform.localScale = Vector3.Scale(
                authoredScale,
                Vector3.one * ResolveEffectArtScale(sprite, radius, diameterMultiplier));
            var resolvedSortingAnchor = sortingAnchor != null ? sortingAnchor : parent;
            ApplyAnchorSorting(renderer, resolvedSortingAnchor, sortingOffset);
            ApplyAuthoredChildRendererSorting(rootObject, renderer, resolvedSortingAnchor, sortingOffset + 1);
            return renderer;
        }

        private static void ApplyAuthoredChildRendererSorting(
            GameObject rootObject,
            Renderer primaryRenderer,
            Transform sortingAnchor,
            int sortingOffset)
        {
            if (rootObject == null)
            {
                return;
            }

            foreach (var childRenderer in rootObject.GetComponentsInChildren<Renderer>(true))
            {
                if (childRenderer == null || childRenderer == primaryRenderer)
                {
                    continue;
                }

                ApplyAnchorSorting(childRenderer, sortingAnchor, sortingOffset);
            }
        }

        private static void DestroySpawnedObject(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }

        private static void DestroyChildrenNamed(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null && child.name == childName)
                {
                    DestroySpawnedObject(child.gameObject);
                }
            }
        }

        private void PlayMagicCircleArtForReusableSkill(
            SkillSO skill,
            Transform anchor,
            Transform facingTargetAnchor,
            float lifetimeSeconds)
        {
            if (skill == null)
            {
                return;
            }

            var family = ResolveSkillVfxFamily(skill);
            if (!UsesPlayerRightMagicCircle(family))
            {
                return;
            }

            var tuning = ResolveSkillVfxTuning(skill);
            var package = ResolveSkillVfxPackage(skill);
            var designTimeBinding = ResolveSkillVfxDesignTimeBinding(skill);
            var sprite = ResolveDesignTimeSprite(
                tuning,
                package,
                designTimeBinding,
                null,
                magicCircleEffectSprite != null ? magicCircleEffectSprite : ResolveMagicCircleEffectSprite());
            if (sprite == null)
            {
                return;
            }

            var scale = Mathf.Max(0.01f, skill.vfxScale);
            var color = ResolveDesignTimeArtColor(
                ResolveReusableSkillParticleColor(skill),
                tuning,
                package,
                designTimeBinding,
                0.32f,
                0.72f);
            var facingSign = ResolveAttackFacingSign(anchor, facingTargetAnchor);
            var magicCircleLocalOffset = MirrorLocalOffsetXByFacing(
                ResolvePlayerRightLocalOffset(ResolveDesignTimeLocalOffset(
                    tuning,
                    package,
                    designTimeBinding,
                    PlayerRightMagicCircleLocalOffset)),
                facingSign);
            if (IsSelfBuffPresentationSkill(skill))
            {
                magicCircleLocalOffset.y += SelfBuffMagicCircleYOffset;
            }

            var objectName = family switch
            {
                SkillVfxFamily.BuffAura => "BuffAuraEffectArt",
                SkillVfxFamily.DebuffWave => "DebuffWaveEffectArt",
                SkillVfxFamily.CounterReady => "CounterReadyEffectArt",
                SkillVfxFamily.BoardDisturb => "BoardDisturbEffectArt",
                SkillVfxFamily.DrainTether => "DrainTetherEffectArt",
                _ => "ReusableSkillEffectArt",
            };
            var art = SpawnAttackArtSpriteLayer(
                anchor,
                objectName,
                color,
                AttackArtBaseRadius *
                    ResolveDesignTimeRadiusMultiplier(
                        tuning,
                        package,
                        designTimeBinding,
                        MagicCircleArtSizeMultiplier) *
                    Mathf.Clamp(Mathf.Sqrt(scale), 0.84f, 1.5f),
                ResolveDesignTimeLifetime(tuning, package, designTimeBinding, Mathf.Clamp(lifetimeSeconds, 0.38f, 0.9f)),
                magicCircleLocalOffset,
                sortingOffset: ResolveDesignTimeSortingOffset(tuning, package, designTimeBinding, 6),
                spriteOverride: sprite,
                prefabOverride: ResolveDesignTimePrefab(tuning, package, designTimeBinding, ResolveMagicCircleEffectPrefab()));
            if (art != null)
            {
                art.transform.localRotation = ResolveFacingMirroredLocalRotation(
                    ResolveDesignTimeRotationDegrees(tuning, package, designTimeBinding, -8f),
                    facingSign);
            }
        }

        private void PlaySupportBuffHealingVisualEffect(SkillSO skill, Transform anchor, float lifetimeSeconds)
        {
            if (!UsesSupportBuffHealingVisualEffect(skill))
            {
                return;
            }

            var family = ResolveSkillVfxFamily(skill);
            var prefab = ResolveSupportBuffVisualEffectPrefab(skill);
            if (prefab == null)
            {
                return;
            }

            var parent = anchor != null ? anchor : transform;
            var instance = Instantiate(prefab, parent);
            instance.name = $"{family}HealingVisualEffect";
            instance.transform.localPosition = SupportBuffHealingVisualEffectLocalOffset;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * Mathf.Clamp(
                Mathf.Max(0.01f, skill != null ? skill.vfxScale : 1f),
                0.72f,
                1.28f);

            var color = ResolveSupportBuffHealingVisualEffectColor(skill);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                ApplyAnchorSorting(renderer, parent, 11);
            }

            foreach (var particles in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                ApplySupportBuffHealingParticleColor(particles, color);
                if (Application.isPlaying)
                {
                    particles.Play(true);
                }
            }

            foreach (var visualEffect in instance.GetComponentsInChildren<VisualEffect>(true))
            {
                ApplySupportBuffVisualEffectColor(visualEffect, color);
                if (Application.isPlaying)
                {
                    visualEffect.Reinit();
                    visualEffect.Play();
                }
            }

            if (lifetimeSeconds > 0f && Application.isPlaying)
            {
                Destroy(instance, Mathf.Max(0.45f, lifetimeSeconds) + 0.35f);
            }
        }

        private static Color ResolveSupportBuffHealingVisualEffectColor(SkillSO skill)
        {
            var primary = ResolveReusableSkillParticleColor(skill);
            var secondary = ResolveReusableSkillSecondaryParticleColor(skill);
            var color = Color.Lerp(primary, secondary, 0.32f);
            if (color.maxColorComponent < 0.42f)
            {
                color = Color.Lerp(color, Color.white, 0.36f);
            }

            color.a = Mathf.Max(0.86f, color.a);
            return color;
        }

        private static void ApplySupportBuffHealingParticleColor(ParticleSystem particles, Color color)
        {
            if (particles == null)
            {
                return;
            }

            var main = particles.main;
            main.startColor = color;
        }

        private static void ApplySupportBuffVisualEffectColor(VisualEffect visualEffect, Color color)
        {
            if (visualEffect == null)
            {
                return;
            }

            var vectorColor = new Vector4(color.r, color.g, color.b, color.a);
            foreach (var propertyName in SupportBuffVisualEffectColorPropertyNames)
            {
                if (visualEffect.HasVector4(propertyName))
                {
                    visualEffect.SetVector4(propertyName, vectorColor);
                }
            }
        }

        private Sprite ResolveAttackEffectSprite()
        {
            if (attackEffectSprite != null)
            {
                return attackEffectSprite;
            }

            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.attackEffectSprite : null;
        }

        private GameObject ResolveAttackEffectPrefab()
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.attackEffectPrefab : null;
        }

        private Sprite ResolveHitEffectSprite()
        {
            if (hitEffectSprite != null)
            {
                return hitEffectSprite;
            }

            ResolveWorldVfxProfile();
            return worldVfxProfile != null && worldVfxProfile.hitEffectSprite != null
                ? worldVfxProfile.hitEffectSprite
                : ResolveAttackEffectSprite();
        }

        private GameObject ResolveHitEffectPrefab()
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null && worldVfxProfile.hitEffectPrefab != null
                ? worldVfxProfile.hitEffectPrefab
                : ResolveAttackEffectPrefab();
        }

        private Sprite ResolveMagicCircleEffectSprite()
        {
            if (magicCircleEffectSprite != null)
            {
                return magicCircleEffectSprite;
            }

            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.magicCircleEffectSprite : null;
        }

        private GameObject ResolveMagicCircleEffectPrefab()
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.magicCircleEffectPrefab : null;
        }

        private static GameObject ResolveSupportBuffVisualEffectPrefab(SkillSO skill)
        {
            var tuning = ResolveSkillVfxTuning(skill);
            if (tuning != null && tuning.secondaryPrefab != null)
            {
                return tuning.secondaryPrefab;
            }

            var package = ResolveSkillVfxPackage(skill);
            return package != null ? package.secondaryPrefab : null;
        }

        private Sprite ResolveFlameEffectSprite()
        {
            if (flameEffectSprite != null)
            {
                return flameEffectSprite;
            }

            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.flameEffectSprite : null;
        }

        private GameObject ResolveFlameEffectPrefab()
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.flameEffectPrefab : null;
        }

        private GameObject ResolveDarkChainLaunchPrefab(SkillVfxTuning tuning, SkillVfxPackageSO package)
        {
            if (tuning != null && tuning.projectilePrefab != null)
            {
                return tuning.projectilePrefab;
            }

            if (package != null && package.projectilePrefab != null)
            {
                return package.projectilePrefab;
            }

            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.darkChainLaunchPrefab : null;
        }

        private Sprite ResolveChainAttackEffectSprite()
        {
            if (chainAttackEffectSprite != null)
            {
                return chainAttackEffectSprite;
            }

            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.chainAttackEffectSprite : null;
        }

        private GameObject ResolveChainAttackEffectPrefab()
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.chainAttackEffectPrefab : null;
        }

        private Sprite ResolveBoundChainsEffectSprite()
        {
            if (boundChainsEffectSprite != null)
            {
                return boundChainsEffectSprite;
            }

            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.boundChainsEffectSprite : null;
        }

        private GameObject ResolveBoundChainsEffectPrefab()
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.boundChainsEffectPrefab : null;
        }

        private SpriteRenderer SpawnShieldArtSpriteLayer(
            Transform anchor,
            string objectName,
            Color color,
            float radius,
            float lifetimeSeconds,
            Vector3 localOffset,
            int sortingOffset,
            Transform sortingAnchor = null,
            bool autoDestroy = true,
            bool persistentPulse = false,
            bool animatePulse = true,
            Sprite spriteOverride = null,
            GameObject prefabOverride = null)
        {
            var resolvedLocalOffset = ResolveShieldArtLocalOffset(localOffset);
            var resolvedSortingOffset = ResolveShieldArtSortingOffset(sortingOffset);
            var sprite = spriteOverride != null ? spriteOverride : ResolveShieldEffectSprite();
            prefabOverride ??= spriteOverride != null && spriteOverride == ResolveThornShieldEffectSprite()
                ? ResolveThornShieldEffectPrefab()
                : ResolveShieldEffectPrefab();
            if (prefabOverride != null)
            {
                var authoredRenderer = TrySpawnAuthoredSpriteEffectLayer(
                    prefabOverride,
                    anchor,
                    objectName,
                    color,
                    radius,
                    ShieldArtDiameterMultiplier,
                    resolvedLocalOffset,
                    resolvedSortingOffset,
                    sortingAnchor != null ? sortingAnchor : anchor,
                    sprite,
                    out var authoredRoot);
                if (authoredRenderer != null)
                {
                    var authoredBaseScale = authoredRenderer.transform.localScale;
                    if (Application.isPlaying && isActiveAndEnabled && animatePulse)
                    {
                        StartCoroutine(persistentPulse
                            ? AnimatePersistentShieldArtRoutine(authoredRenderer, authoredBaseScale, color)
                            : AnimateShieldArtPulseRoutine(authoredRenderer, Mathf.Max(0.05f, lifetimeSeconds), authoredBaseScale, color));
                    }

                    if (autoDestroy && lifetimeSeconds > 0f && Application.isPlaying)
                    {
                        Destroy(authoredRoot, lifetimeSeconds + 0.2f);
                    }

                    return authoredRenderer;
                }
            }

            if (sprite == null)
            {
                return null;
            }

            var parent = anchor != null ? anchor : transform;
            var spriteObject = new GameObject(objectName, typeof(SpriteRenderer));
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = resolvedLocalOffset;

            var renderer = spriteObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            var baseScale = Vector3.one * ResolveEffectArtScale(sprite, radius, ShieldArtDiameterMultiplier);
            spriteObject.transform.localScale = baseScale;
            ApplyAnchorSorting(renderer, sortingAnchor != null ? sortingAnchor : parent, resolvedSortingOffset);

            if (Application.isPlaying && isActiveAndEnabled && animatePulse)
            {
                StartCoroutine(persistentPulse
                    ? AnimatePersistentShieldArtRoutine(renderer, baseScale, color)
                    : AnimateShieldArtPulseRoutine(renderer, Mathf.Max(0.05f, lifetimeSeconds), baseScale, color));
            }

            if (autoDestroy && lifetimeSeconds > 0f && Application.isPlaying)
            {
                Destroy(spriteObject, lifetimeSeconds + 0.2f);
            }

            return renderer;
        }

        private static Vector3 ResolveShieldArtLocalOffset(Vector3 localOffset)
        {
            return localOffset + ShieldArtLeftLocalOffset;
        }

        private static Vector3 ResolveShieldWorldPosition(Vector3 worldPosition)
        {
            return worldPosition + ShieldArtLeftLocalOffset;
        }

        private static int ResolveShieldArtSortingOffset(int sortingOffset)
        {
            return Mathf.Max(sortingOffset, ShieldArtFrontSortingOffset);
        }

        private Sprite ResolveShieldEffectSprite()
        {
            if (shieldEffectSprite != null)
            {
                return shieldEffectSprite;
            }

            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.shieldEffectSprite : null;
        }

        private GameObject ResolveShieldEffectPrefab()
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.shieldEffectPrefab : null;
        }

        private Sprite ResolveThornShieldEffectSprite()
        {
            if (thornShieldEffectSprite != null)
            {
                return thornShieldEffectSprite;
            }

            ResolveWorldVfxProfile();
            return worldVfxProfile != null && worldVfxProfile.thornShieldEffectSprite != null
                ? worldVfxProfile.thornShieldEffectSprite
                : ResolveShieldEffectSprite();
        }

        private GameObject ResolveThornShieldEffectPrefab()
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null && worldVfxProfile.thornShieldEffectPrefab != null
                ? worldVfxProfile.thornShieldEffectPrefab
                : ResolveShieldEffectPrefab();
        }

        private static float ResolveEffectArtScale(Sprite sprite, float radius, float diameterMultiplier)
        {
            if (sprite == null)
            {
                return 1f;
            }

            var spriteSize = sprite.bounds.size;
            var maxSpriteSize = Mathf.Max(spriteSize.x, spriteSize.y);
            if (maxSpriteSize <= 0.001f)
            {
                return 1f;
            }

            return Mathf.Max(0.01f, radius * diameterMultiplier / maxSpriteSize);
        }

        private static Vector3 ResolveAnchorWorldPosition(Transform anchor, Vector3 localOffset)
        {
            return anchor != null
                ? ResolveAnchorVisualCenterWorldPosition(anchor) + anchor.TransformVector(localOffset)
                : localOffset;
        }

        private static Vector3 ResolveFootEffectLocalOffset(Transform anchor, Vector3 fallback)
        {
            var renderer = anchor != null ? anchor.GetComponent<SpriteRenderer>() : null;
            var sprite = renderer != null ? renderer.sprite : null;
            if (sprite == null)
            {
                return fallback;
            }

            return new Vector3(fallback.x, sprite.bounds.min.y + 0.08f, fallback.z);
        }

        private static Color ResolveShieldArtColor(Color color, float alpha)
        {
            if (color.a <= 0f)
            {
                color = ShieldCircleLightTint;
            }

            var resolved = Color.Lerp(color, Color.white, 0.24f);
            resolved.a = Mathf.Clamp01(alpha);
            return resolved;
        }

        private static IEnumerator AnimateShieldArtPulseRoutine(
            SpriteRenderer renderer,
            float lifetimeSeconds,
            Vector3 baseScale,
            Color baseColor)
        {
            var elapsed = 0f;
            var duration = Mathf.Max(0.05f, lifetimeSeconds);
            while (elapsed < duration)
            {
                if (renderer == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.18f));
                var fadeOut = progress > 0.68f
                    ? Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((progress - 0.68f) / 0.32f))
                    : 1f;
                var pulse = 0.9f + Mathf.Sin(progress * Mathf.PI) * 0.18f;
                renderer.transform.localScale = baseScale * pulse;

                var color = baseColor;
                color.a = baseColor.a * fadeIn * fadeOut;
                renderer.color = color;
                yield return null;
            }

            if (renderer != null)
            {
                var color = baseColor;
                color.a = 0f;
                renderer.color = color;
            }
        }

        private static IEnumerator AnimateAttackArtPulseRoutine(
            SpriteRenderer renderer,
            float lifetimeSeconds,
            Vector3 baseScale,
            Color baseColor)
        {
            var elapsed = 0f;
            var duration = Mathf.Max(0.05f, lifetimeSeconds);
            while (elapsed < duration)
            {
                if (renderer == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var pop = Mathf.Sin(progress * Mathf.PI);
                renderer.transform.localScale = baseScale * (0.82f + pop * 0.36f);

                var color = baseColor;
                var fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.12f));
                var fadeOut = progress > 0.42f
                    ? Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((progress - 0.42f) / 0.58f))
                    : 1f;
                color.a = baseColor.a * fadeIn * fadeOut;
                renderer.color = color;
                yield return null;
            }
        }

        private static IEnumerator AnimatePersistentShieldArtRoutine(
            SpriteRenderer renderer,
            Vector3 baseScale,
            Color baseColor)
        {
            while (renderer != null)
            {
                var wave = Mathf.Sin(Time.time * 4.2f);
                renderer.transform.localScale = baseScale * (1f + wave * 0.035f);
                var color = baseColor;
                color.a = baseColor.a * (0.86f + wave * 0.08f);
                renderer.color = color;
                yield return null;
            }
        }

        private static IEnumerator AnimateShieldBashArtRoutine(
            SpriteRenderer renderer,
            Vector3 startPosition,
            Vector3 endPosition,
            Vector3 baseScale,
            Color baseColor,
            float lifetimeSeconds)
        {
            var elapsed = 0f;
            var duration = Mathf.Max(0.05f, lifetimeSeconds);
            var control = Vector3.Lerp(startPosition, endPosition, 0.44f) + Vector3.up * 0.34f;
            while (elapsed < duration)
            {
                if (renderer == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var moveProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.72f));
                renderer.transform.position = QuadraticBezier(startPosition, control, endPosition, moveProgress);
                renderer.transform.localScale = baseScale * (1f + Mathf.Sin(progress * Mathf.PI) * 0.16f);
                renderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-14f, 18f, moveProgress));

                var color = baseColor;
                color.a = baseColor.a * (progress > 0.74f
                    ? Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((progress - 0.74f) / 0.26f))
                    : 1f);
                renderer.color = color;
                yield return null;
            }
        }

        private static IEnumerator AnimateShieldBurstArtRoutine(
            SpriteRenderer renderer,
            Vector3 baseScale,
            Color baseColor,
            float lifetimeSeconds)
        {
            var elapsed = 0f;
            var duration = Mathf.Max(0.05f, lifetimeSeconds);
            while (elapsed < duration)
            {
                if (renderer == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var expand = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.42f));
                renderer.transform.localScale = baseScale * Mathf.Lerp(0.92f, 1.62f, expand);

                var color = baseColor;
                var fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.12f));
                var fadeOut = progress > 0.36f
                    ? Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((progress - 0.36f) / 0.42f))
                    : 1f;
                color.a = baseColor.a * fadeIn * fadeOut;
                renderer.color = color;
                yield return null;
            }
        }

        private static void ConfigureSpikedBurstParticles(ParticleSystem particles, float scale, float lifetimeSeconds)
        {
            if (particles == null)
            {
                return;
            }

            var main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeSeconds * 0.45f, lifetimeSeconds);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.72f * scale, 1.78f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f * scale, 0.18f * scale);
            main.gravityModifier = 0f;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f * scale;
            shape.radiusThickness = 0.18f;
            shape.arc = 360f;

            var rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 3f, Mathf.PI * 3f);

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.16f, 1.38f),
                new Keyframe(0.58f, 0.82f),
                new Keyframe(1f, 0f)));
        }

        private static void ConfigureBloodFountainParticles(ParticleSystem particles, float scale, float lifetimeSeconds)
        {
            if (particles == null)
            {
                return;
            }

            var main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeSeconds * 0.36f, lifetimeSeconds * 0.82f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f * scale, 0.72f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f * scale, 0.076f * scale);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(1.08f, 1.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 96;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.055f * scale;
            shape.radiusThickness = 0.85f;
            shape.arc = 360f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.72f * scale, 0.72f * scale);
            velocity.y = new ParticleSystem.MinMaxCurve(0.9f * scale, 2.55f * scale);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 3.5f, Mathf.PI * 3.5f);

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.82f),
                new Keyframe(0.16f, 1.08f),
                new Keyframe(0.58f, 0.86f),
                new Keyframe(1f, 0f)));
        }

        private static void ConfigureBloodMistParticles(ParticleSystem particles, float scale, float facingSign)
        {
            if (particles == null)
            {
                return;
            }

            var main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.26f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.42f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f * scale, 0.12f * scale);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.5f);

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.22f * scale;
            shape.radiusThickness = 0.7f;

            var direction = Mathf.Sign(facingSign == 0f ? 1f : facingSign);
            var horizontalMin = direction > 0f ? 0.08f * scale : -0.56f * scale;
            var horizontalMax = direction > 0f ? 0.56f * scale : -0.08f * scale;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(horizontalMin, horizontalMax);
            velocity.y = new ParticleSystem.MinMaxCurve(0.28f * scale, 0.86f * scale);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.38f),
                new Keyframe(0.24f, 1f),
                new Keyframe(1f, 0f)));
        }

        private static void ConfigureFlameBurstParticles(ParticleSystem particles, float scale, float lifetimeSeconds)
        {
            if (particles == null)
            {
                return;
            }

            var main = particles.main;
            main.duration = Mathf.Max(0.2f, lifetimeSeconds);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeSeconds * 0.28f, lifetimeSeconds * 0.72f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.16f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f * scale, 0.105f * scale);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.08f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = particles.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(34f * scale);
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.Clamp(Mathf.RoundToInt(14 * scale), 1, short.MaxValue)),
                new ParticleSystem.Burst(0.18f, (short)Mathf.Clamp(Mathf.RoundToInt(10 * scale), 1, short.MaxValue)),
            });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.28f * scale;
            shape.radiusThickness = 0.82f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.22f * scale, 0.22f * scale);
            velocity.y = new ParticleSystem.MinMaxCurve(0.92f * scale, 1.55f * scale);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 2.6f, Mathf.PI * 2.6f);

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.28f),
                new Keyframe(0.2f, 1.16f),
                new Keyframe(0.58f, 0.82f),
                new Keyframe(1f, 0f)));
        }

        private static void ConfigureFlameEmberParticles(ParticleSystem particles, float scale, float lifetimeSeconds)
        {
            if (particles == null)
            {
                return;
            }

            var main = particles.main;
            main.duration = Mathf.Max(0.2f, lifetimeSeconds);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeSeconds * 0.42f, lifetimeSeconds);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.26f * scale, 0.86f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.026f * scale, 0.058f * scale);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.16f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = particles.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(14f * scale);

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 32f;
            shape.radius = 0.22f * scale;
            shape.radiusThickness = 0.5f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.34f * scale, 0.34f * scale);
            velocity.y = new ParticleSystem.MinMaxCurve(0.58f * scale, 1.18f * scale);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.18f, 1f),
                new Keyframe(1f, 0f)));
        }

        private static void ConfigureFlameSmokeParticles(ParticleSystem particles, float scale, float lifetimeSeconds)
        {
            if (particles == null)
            {
                return;
            }

            var main = particles.main;
            main.duration = Mathf.Max(0.2f, lifetimeSeconds);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeSeconds * 0.52f, lifetimeSeconds);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.18f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f * scale, 0.18f * scale);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.04f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = particles.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(8f * scale);

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.24f * scale;
            shape.radiusThickness = 0.75f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.12f * scale, 0.12f * scale);
            velocity.y = new ParticleSystem.MinMaxCurve(0.34f * scale, 0.74f * scale);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.42f),
                new Keyframe(0.4f, 1f),
                new Keyframe(1f, 0f)));
        }

        private static void ConfigureShieldCircleParticles(
            ParticleSystem particles,
            float radius,
            float radiusThickness,
            float lifetimeSeconds)
        {
            if (particles == null)
            {
                return;
            }

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Mathf.Max(0.05f, radius);
            shape.radiusThickness = Mathf.Clamp01(radiusThickness);
            shape.arc = 360f;

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(Mathf.Clamp01(0.2f / Mathf.Max(0.05f, lifetimeSeconds)), 1.1f),
                new Keyframe(1f, 0f)));
        }

        private IEnumerator FadeLineRendererRoutine(LineRenderer line, float lifetimeSeconds)
        {
            var duration = Mathf.Max(0.05f, lifetimeSeconds);
            var elapsed = 0f;
            var startColor = line != null ? line.startColor : Color.clear;
            var endColor = line != null ? line.endColor : Color.clear;
            while (elapsed < duration)
            {
                if (line == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var alpha = 1f - Mathf.Clamp01(elapsed / duration);
                var nextStart = startColor;
                var nextEnd = endColor;
                nextStart.a *= alpha;
                nextEnd.a *= alpha;
                line.startColor = nextStart;
                line.endColor = nextEnd;
                yield return null;
            }
        }

        private static IEnumerator FadeSkillLineRootRoutine(GameObject root, float lifetimeSeconds, LineRenderer[] lines)
        {
            var duration = Mathf.Max(0.05f, lifetimeSeconds);
            var elapsed = 0f;
            lines ??= System.Array.Empty<LineRenderer>();
            var startColors = new Color[lines.Length];
            var endColors = new Color[lines.Length];
            for (var i = 0; i < lines.Length; i++)
            {
                startColors[i] = lines[i] != null ? lines[i].startColor : Color.clear;
                endColors[i] = lines[i] != null ? lines[i].endColor : Color.clear;
            }

            while (elapsed < duration)
            {
                if (root == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var alpha = 1f - Mathf.Clamp01(elapsed / duration);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line == null)
                    {
                        continue;
                    }

                    var nextStart = startColors[i];
                    var nextEnd = endColors[i];
                    nextStart.a *= alpha;
                    nextEnd.a *= alpha;
                    line.startColor = nextStart;
                    line.endColor = nextEnd;
                }

                yield return null;
            }

            if (root != null)
            {
                Destroy(root);
            }
        }

        private static void ApplyAnchorSorting(Renderer renderer, Transform anchor, int sortingOffset)
        {
            if (renderer == null || anchor == null)
            {
                return;
            }

            var anchorRenderer = ResolveAnchorSortingRenderer(anchor);
            if (anchorRenderer == null)
            {
                return;
            }

            renderer.sortingLayerID = anchorRenderer.sortingLayerID;
            renderer.sortingOrder = anchorRenderer.sortingOrder + sortingOffset;
        }

        private static SpriteRenderer ResolveAnchorSortingRenderer(Transform anchor)
        {
            if (anchor == null)
            {
                return null;
            }

            var anchorRenderer = anchor.GetComponent<SpriteRenderer>();
            if (anchorRenderer != null)
            {
                return anchorRenderer;
            }

            if (IsLayeredPlayerActorRoot(anchor))
            {
                return ResolveLayeredPlayerPrimaryRenderer(anchor);
            }

            return anchor.GetComponentsInChildren<SpriteRenderer>(includeInactive: true)
                .FirstOrDefault(childRenderer => childRenderer != null);
        }

        private static bool IsShieldGeneratingSkill(SkillSO skill)
        {
            if (skill == null)
            {
                return false;
            }

            var effectKind = skill.ResolveEffectKind();
            return effectKind == SkillEffectKind.ThornGuard ||
                (effectKind == SkillEffectKind.BasicDefense && skill.power > 0);
        }

        private static bool IsChargeAttackSkill(SkillSO skill)
        {
            return skill != null && skill.ResolveEffectKind() == SkillEffectKind.ChargeAttack;
        }

        private static bool IsGatherLightSkill(SkillSO skill)
        {
            return skill != null &&
                string.Equals(skill.skillId, "gather-light", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLightEchoSkill(SkillSO skill)
        {
            return skill != null &&
                string.Equals(skill.skillId, "light-echo", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSelfBuffPresentationSkill(SkillSO skill)
        {
            if (skill == null || (skill.skillType == SkillType.Attack && skill.RequiresEnemyTarget))
            {
                return false;
            }

            var family = ResolveSkillVfxFamily(skill);
            return family == SkillVfxFamily.BuffAura ||
                family == SkillVfxFamily.CounterReady ||
                family == SkillVfxFamily.BoardDisturb ||
                IsGatherLightSkill(skill) ||
                IsLightEchoSkill(skill);
        }

        private static bool IsShieldAttackSkill(SkillSO skill)
        {
            if (skill == null)
            {
                return false;
            }

            var effectKind = skill.ResolveEffectKind();
            return effectKind == SkillEffectKind.ShieldScalingAttack ||
                effectKind == SkillEffectKind.ShieldBurstAttack ||
                string.Equals(skill.skillId, "shield-bash", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(skill.skillId, "shield-burst", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsShieldBurstSkill(SkillSO skill)
        {
            if (skill == null)
            {
                return false;
            }

            return skill.ResolveEffectKind() == SkillEffectKind.ShieldBurstAttack ||
                skill.consumesAllShield ||
                string.Equals(skill.skillId, "shield-burst", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsThornGuardSkill(SkillSO skill)
        {
            return skill != null &&
                (skill.ResolveEffectKind() == SkillEffectKind.ThornGuard ||
                    string.Equals(skill.skillId, "thorn-guard", System.StringComparison.OrdinalIgnoreCase));
        }

        private static Color ResolveShieldCircleLightColor(Color color)
        {
            if (color.a <= 0f)
            {
                color = ShieldCircleLightTint;
            }

            var resolved = Color.Lerp(color, ShieldCircleLightTint, 0.42f);
            resolved.a = Mathf.Max(0.88f, Mathf.Min(0.96f, color.a));
            return resolved;
        }

        private static Color ResolveThornGuardDarkColor(Color color, Color tint, float tintWeight)
        {
            if (color.a <= 0f)
            {
                color = tint;
            }

            var resolved = Color.Lerp(color, tint, Mathf.Clamp01(tintWeight));
            resolved.a = Mathf.Max(0.86f, Mathf.Min(0.96f, color.a));
            return resolved;
        }


        private CombatParticleEffectBinding ResolveShieldImpactParticleEffect()
        {
            ResolveWorldVfxProfile();
            return worldVfxProfile != null ? worldVfxProfile.shieldImpactEffect : null;
        }



        private Material ResolveShieldImpactParticleMaterial()
        {
            return shieldImpactParticleMaterial != null
                ? shieldImpactParticleMaterial
                : runtimeShieldImpactParticleMaterial ??= CreateParticleMaterial(
                    "ShieldImpactParticleMaterial",
                    shieldImpactParticleColor);
        }


        private Material ResolveRuntimeSkillParticleMaterial(string objectName, Color color)
        {
            var materialKey = $"{objectName}:{ColorUtility.ToHtmlStringRGBA(color)}";
            if (runtimeSkillParticleMaterials.TryGetValue(materialKey, out var material) && material != null)
            {
                return material;
            }

            material = CreateParticleMaterial($"{objectName}Material", color);
            if (material != null)
            {
                runtimeSkillParticleMaterials[materialKey] = material;
            }

            return material;
        }

        private Material ResolveChargedLightBeamMaterial()
        {
            return runtimeChargedLightBeamMaterial ??= CreateParticleMaterial(
                "ChargedLightBeamMaterial",
                new Color(0.86f, 0.97f, 1f, 0.92f));
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
            DestroyRuntimeMaterial(ref runtimeChargedLightBeamMaterial);
            foreach (var material in runtimeSkillParticleMaterials.Values)
            {
                DestroyRuntimeMaterialInstance(material);
            }

            runtimeSkillParticleMaterials.Clear();
        }

        private static void DestroyRuntimeMaterial(ref Material material)
        {
            if (material == null)
            {
                return;
            }

            DestroyRuntimeMaterialInstance(material);
            material = null;
        }

        private static void DestroyRuntimeMaterialInstance(Material material)
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
        }



        private void ResolveMissingReferences()
        {
            if (backgroundRenderer == null)
            {
                backgroundRenderer = FindRendererByName(transform, "BackgroundSprite");
            }

            if (playerActorRoot == null)
            {
                playerActorRoot = FindTransformByName(transform, LayeredPlayerActorRootName);
            }

            if (playerActorRoot != null && IsLayeredPlayerActorRoot(playerActorRoot))
            {
                var layeredRenderer = ResolveLayeredPlayerPrimaryRenderer(playerActorRoot);
                if (layeredRenderer != null)
                {
                    playerRenderer = layeredRenderer;
                }
            }
            else if (playerRenderer == null)
            {
                playerRenderer = FindRendererByName(transform, "PlayerSprite");
            }

            if (playerActorRoot == null && playerRenderer != null)
            {
                playerActorRoot = playerRenderer.transform;
            }

            if (enemyRenderer == null)
            {
                enemyRenderer = FindRendererByName(transform, "EnemySprite");
            }

            if (rewardMothRenderer == null)
            {
                rewardMothRenderer = FindRendererInChildrenByName(transform, "RewardMothSprite");
            }

            if (playerAnimator == null && playerActorRoot != null)
            {
                playerAnimator = playerActorRoot.GetComponentInChildren<Animator>(includeInactive: true);
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


        private static SpriteRenderer FindRendererByName(Transform root, string objectName)
        {
            var localRenderer = FindRendererInChildrenByName(root, objectName);
            if (localRenderer != null)
            {
                return localRenderer;
            }

            var target = GameObject.Find(objectName);
            return target != null ? target.GetComponent<SpriteRenderer>() : null;
        }

        private static SpriteRenderer FindRendererInChildrenByName(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            return root
                .GetComponentsInChildren<SpriteRenderer>(includeInactive: true)
                .FirstOrDefault(renderer => string.Equals(
                    renderer.name,
                    objectName,
                    System.StringComparison.Ordinal));
        }

        private static Transform FindTransformByName(Transform root, string objectName)
        {
            if (root != null && !string.IsNullOrWhiteSpace(objectName))
            {
                var localTransform = root
                    .GetComponentsInChildren<Transform>(includeInactive: true)
                    .FirstOrDefault(candidate => string.Equals(
                        candidate.name,
                        objectName,
                        System.StringComparison.Ordinal));
                if (localTransform != null)
                {
                    return localTransform;
                }
            }

            var target = GameObject.Find(objectName);
            return target != null ? target.transform : null;
        }

        private static SpriteRenderer ResolveLayeredPlayerPrimaryRenderer(Transform actorRoot)
        {
            if (actorRoot == null)
            {
                return null;
            }

            var renderers = actorRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            return renderers.FirstOrDefault(renderer =>
                    string.Equals(renderer.name, LayeredPlayerBodyRendererName, System.StringComparison.Ordinal)) ??
                renderers.FirstOrDefault();
        }

        private Transform ResolvePlayerAnchor()
        {
            return playerActorRoot != null ? playerActorRoot : playerRenderer != null ? playerRenderer.transform : null;
        }

        private static Vector3 ResolveAnchorVisualCenterWorldPosition(Transform anchor)
        {
            if (anchor == null)
            {
                return Vector3.zero;
            }

            var renderer = anchor.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                return renderer.bounds.center;
            }

            if (!string.Equals(anchor.name, LayeredPlayerActorRootName, System.StringComparison.Ordinal))
            {
                return anchor.position;
            }

            var childRenderers = anchor.GetComponentsInChildren<SpriteRenderer>(includeInactive: true)
                .Where(childRenderer => childRenderer != null && childRenderer.sprite != null)
                .ToArray();
            if (childRenderers.Length == 0)
            {
                return anchor.position;
            }

            var bounds = childRenderers[0].bounds;
            for (var i = 1; i < childRenderers.Length; i++)
            {
                bounds.Encapsulate(childRenderers[i].bounds);
            }

            return bounds.center;
        }

        private static Vector3 ResolveVisualCenterLocalOffset(Transform anchor, Vector3 worldOffset)
        {
            if (anchor == null)
            {
                return worldOffset;
            }

            return anchor.InverseTransformPoint(ResolveAnchorVisualCenterWorldPosition(anchor) + worldOffset);
        }

        private static Vector3 ResolveAnchorVisualBottomWorldPosition(Transform anchor)
        {
            if (anchor == null)
            {
                return Vector3.zero;
            }

            if (TryResolveAnchorRendererBounds(anchor, out var bounds))
            {
                return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }

            return anchor.position;
        }

        private static bool TryResolveAnchorRendererBounds(Transform anchor, out Bounds bounds)
        {
            bounds = default;
            if (anchor == null)
            {
                return false;
            }

            var renderer = anchor.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                bounds = renderer.bounds;
                return true;
            }

            var hasBounds = false;
            foreach (var childRenderer in anchor.GetComponentsInChildren<SpriteRenderer>(includeInactive: true))
            {
                if (childRenderer == null || childRenderer.sprite == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = childRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(childRenderer.bounds);
                }
            }

            return hasBounds;
        }

        private bool ShouldAssignPlayerRendererSprite()
        {
            if (playerRenderer == null)
            {
                return false;
            }

            return playerActorRoot == null || !IsLayeredPlayerActorRoot(playerActorRoot);
        }

        private static bool IsLayeredPlayerActorRoot(Transform actorRoot)
        {
            return actorRoot != null &&
                string.Equals(actorRoot.name, LayeredPlayerActorRootName, System.StringComparison.Ordinal) &&
                actorRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true).Length > 1;
        }

        private void PlayPlayerAttackAnimationIfNeeded(SkillSO skill, SkillVfxFamily family)
        {
            if (skill == null)
            {
                return;
            }

            if (skill.skillType == SkillType.Attack && UsesCloseRangePlayerLunge(family))
            {
                return;
            }

            PlayPlayerAttackAnimation(ResolvePlayerAttackAnimationSpeedMultiplier(skill));
        }

        private void PlayPlayerAttackAnimation(float speedMultiplier = 1f)
        {
            if (playerAnimator == null)
            {
                return;
            }

            if (playerAnimator.runtimeAnimatorController == null ||
                !playerAnimator.HasState(0, PlayerAttackStateHash))
            {
                return;
            }

            ApplyPlayerAttackAnimationSpeed(speedMultiplier);
            playerAnimator.Play(PlayerAttackStateHash, 0, 0f);
        }

        private void ApplyPlayerAttackAnimationSpeed(float speedMultiplier)
        {
            if (playerAnimator == null ||
                !Application.isPlaying ||
                !isActiveAndEnabled)
            {
                return;
            }

            speedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            if (playerAttackAnimationSpeedCoroutine != null)
            {
                StopCoroutine(playerAttackAnimationSpeedCoroutine);
                playerAttackAnimationSpeedCoroutine = null;
            }

            if (!hasPlayerAttackAnimationSpeedRestoreValue)
            {
                playerAttackAnimationSpeedRestoreValue = playerAnimator.speed;
                hasPlayerAttackAnimationSpeedRestoreValue = true;
            }

            playerAnimator.speed = playerAttackAnimationSpeedRestoreValue * speedMultiplier;
            playerAttackAnimationSpeedCoroutine = StartCoroutine(
                RestorePlayerAttackAnimationSpeedRoutine(PlayerAttackAnimationSpeedResetSeconds / speedMultiplier));
        }

        private IEnumerator RestorePlayerAttackAnimationSpeedRoutine(float delaySeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, delaySeconds));
            playerAttackAnimationSpeedCoroutine = null;
            RestorePlayerAttackAnimationSpeed();
        }

        private void ClearPlayerAttackAnimationSpeed()
        {
            if (playerAttackAnimationSpeedCoroutine != null)
            {
                StopCoroutine(playerAttackAnimationSpeedCoroutine);
                playerAttackAnimationSpeedCoroutine = null;
            }

            RestorePlayerAttackAnimationSpeed();
        }

        private void RestorePlayerAttackAnimationSpeed()
        {
            if (hasPlayerAttackAnimationSpeedRestoreValue && playerAnimator != null)
            {
                playerAnimator.speed = playerAttackAnimationSpeedRestoreValue;
            }

            hasPlayerAttackAnimationSpeedRestoreValue = false;
            playerAttackAnimationSpeedRestoreValue = 1f;
        }

        private static float ResolvePlayerAttackAnimationSpeedMultiplier(SkillSO skill)
        {
            return skill != null &&
                skill.skillType == SkillType.Attack &&
                UsesCloseRangePlayerLunge(ResolveSkillVfxFamily(skill))
                    ? CloseRangeAttackAnimationSpeedMultiplier
                    : 1f;
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

        private void UpdatePlayerShieldArtVfx(PlayerCombatSnapshot playerSnapshot)
        {
            var shieldHp = playerSnapshot != null ? playerSnapshot.ShieldHp : 0;
            if (shieldHp <= 0)
            {
                playerShieldStyleLockedToThornGuard = false;
                ClearActivePlayerShieldArtVfx();
                ClearActivePlayerThornGuardVfx();
                return;
            }

            if (PlayerSnapshotHasThornGuard(playerSnapshot))
            {
                playerShieldStyleLockedToThornGuard = true;
            }

            if (playerShieldStyleLockedToThornGuard)
            {
                ClearActivePlayerShieldArtVfx();
                if (activePlayerThornGuardVfx == null)
                {
                    activePlayerThornGuardVfx = CreateThornGuardShieldVfxRoot(
                        ResolvePlayerAnchor() ?? transform,
                        shieldHp);
                    SpawnThornGuardShieldArt(
                        activePlayerThornGuardVfx,
                        ResolvePlayerAnchor() ?? transform,
                        null,
                        ShieldCircleBaseRadius * ThornGuardShieldRadiusMultiplier,
                        0.5f);
                }

                activePlayerThornGuardVfx?.SetShieldValue(shieldHp);
                return;
            }

            ClearActivePlayerThornGuardVfx();
            if (activePlayerShieldArtVfx == null)
            {
                activePlayerShieldArtVfx = CreatePlayerShieldArtVfxRoot(
                    ResolvePlayerAnchor() ?? transform,
                    shieldHp);
            }

            activePlayerShieldArtVfx?.SetShieldValue(shieldHp);
        }

        private static bool PlayerSnapshotHasThornGuard(PlayerCombatSnapshot playerSnapshot)
        {
            return playerSnapshot?.StatusEffects != null &&
                playerSnapshot.StatusEffects.Any(effect =>
                    effect != null &&
                    effect.Value > 0 &&
                    string.Equals(effect.Id, "thorn-guard", System.StringComparison.OrdinalIgnoreCase));
        }

        private void UpdatePlayerThornGuardVfx(PlayerCombatSnapshot playerSnapshot)
        {
            if (activePlayerThornGuardVfx == null)
            {
                return;
            }

            var shieldHp = playerSnapshot != null ? playerSnapshot.ShieldHp : 0;
            if (shieldHp <= 0)
            {
                activePlayerThornGuardVfx.StopAndDestroy();
                activePlayerThornGuardVfx = null;
                return;
            }

            activePlayerThornGuardVfx.SetShieldValue(shieldHp);
        }

        private void PlayPlayerThornGuardHitPulseIfNeeded(bool shieldWasHit)
        {
            if (!shieldWasHit || activePlayerThornGuardVfx == null)
            {
                return;
            }

            var hitWorldPosition = enemyRenderer != null
                ? enemyRenderer.transform.position
                : activePlayerThornGuardVfx.transform.position + Vector3.right;
            activePlayerThornGuardVfx.PlayHitPulse(hitWorldPosition);
        }

        private void ClearActivePlayerShieldArtVfx()
        {
            if (activePlayerShieldArtVfx == null)
            {
                return;
            }

            activePlayerShieldArtVfx.StopAndDestroy();
            activePlayerShieldArtVfx = null;
        }

        private void ClearActivePlayerThornGuardVfx()
        {
            if (activePlayerThornGuardVfx == null)
            {
                return;
            }

            activePlayerThornGuardVfx.StopAndDestroy();
            activePlayerThornGuardVfx = null;
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

        private CombatDamagePopupCue ResolveEnemyDamagePopupCue(CombatSnapshot next)
        {
            var cue = next?.LastDamagePopupCue;
            var enemy = next?.Enemies?.FirstOrDefault();
            if (cue == null ||
                enemy == null ||
                cue.Amount <= 0 ||
                cue.Sequence <= lastPlayedDamagePopupSequence ||
                cue.TargetEnemyIndex != enemy.EnemyIndex)
            {
                return null;
            }

            lastPlayedDamagePopupSequence = cue.Sequence;
            return cue;
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

        private static bool EnemyUsedAttack(CombatSnapshot previous, CombatSnapshot next, bool attackConnected)
        {
            if (!attackConnected || next?.Phase != CombatPhase.EnemyTurn)
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
                (nextEnemy.Block > previousEnemy.Block || EnemyHasDefenseSkillIntent(nextEnemy));
        }

        private static EnemyIntent ResolveEnemyUsedIntent(CombatSnapshot snapshot, EnemyIntentType intentType)
        {
            var enemy = snapshot?.Enemies?.FirstOrDefault();
            if (enemy?.Intents != null)
            {
                var skillIntent = enemy.Intents.FirstOrDefault(intent =>
                    intent?.intentType == intentType &&
                    !string.IsNullOrWhiteSpace(intent.skillId));
                if (skillIntent != null)
                {
                    return skillIntent;
                }

                var matchingIntent = enemy.Intents.FirstOrDefault(intent => intent?.intentType == intentType);
                if (matchingIntent != null)
                {
                    return matchingIntent;
                }
            }

            return enemy?.Intent?.intentType == intentType ? enemy.Intent : null;
        }

        private SkillSO ResolveEnemySkillForIntent(EnemyIntent intent)
        {
            if (intent == null || string.IsNullOrWhiteSpace(intent.skillId))
            {
                return null;
            }

            var skills = ResolveCurrentEnemyData()?.skills;
            return skills?.FirstOrDefault(skill =>
                skill != null &&
                string.Equals(skill.skillId, intent.skillId, System.StringComparison.OrdinalIgnoreCase));
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

        private static bool EnemyHasDefenseSkillIntent(EnemyCombatSnapshot enemy)
        {
            if (enemy?.Intents != null && enemy.Intents.Any(intent =>
                intent?.intentType == EnemyIntentType.Defense &&
                !string.IsNullOrWhiteSpace(intent.skillId)))
            {
                return true;
            }

            return enemy?.Intent?.intentType == EnemyIntentType.Defense &&
                !string.IsNullOrWhiteSpace(enemy.Intent.skillId);
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

        private sealed class FollowingShieldVfx : MonoBehaviour
        {
            private Transform target;
            private Vector3 worldOffset;
            private float followSharpness;
            private bool isStopping;

            public void Bind(Transform newTarget, Vector3 offset, float sharpness, int shieldValue)
            {
                target = newTarget;
                worldOffset = offset;
                followSharpness = Mathf.Max(0.01f, sharpness);
                if (target != null)
                {
                    transform.position = ResolveAnchorVisualCenterWorldPosition(target) + worldOffset;
                }
            }

            public void SetShieldValue(int shieldValue)
            {
            }

            public void PlayHitPulse(Vector3 hitWorldPosition)
            {
            }

            public void StopAndDestroy()
            {
                if (isStopping)
                {
                    return;
                }

                isStopping = true;
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }

            private void LateUpdate()
            {
                if (target == null || isStopping)
                {
                    return;
                }

                var desiredPosition = ResolveAnchorVisualCenterWorldPosition(target) + worldOffset;
                var followT = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, desiredPosition, followT);
                transform.rotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }
        }
    }
}
