using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project2048.Audio;
using Project2048.Board2048;
using Project2048.Combat;
using Project2048.Core;
using Project2048.Enemy;
using Project2048.Presentation;
using Project2048.Rewards;
using Project2048.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project2048.Prototype
{
    /// <summary>
    /// Canvas-based prototype UI and temporary feedback layer. Built once in
    /// the scene so designers can replace layout, colours, sprites, and audio
    /// clips in the editor; this component only keeps those placeholders in
    /// sync with combat state.
    /// </summary>
    public partial class CombatUiView : MonoBehaviour
    {
        // Tile slides should stay snappy; panel switching gets its own delay below.
        public const float BoardTransitionDurationSeconds = 0.14f;
        public const float BoardToActionPanelDelaySeconds = 0.45f;
        public const float CombatVfxDurationSeconds = 0.65f;
        public const float EnemyDeathFadeDurationSeconds = 0.6f;
        public const float HpDamageTrailDelaySeconds = 0.25f;
        public const float HpDamageTrailDurationSeconds = 0.55f;
        public const float HpHitShakeDurationSeconds = 0.12f;
        public const float BottomPanelMergeFlashSeconds = 0.18f;

        [Header("Top bar")]
        [SerializeField] private TMP_Text turnCounterText;
        [SerializeField] private TMP_Text intentHeaderText;

        [Header("Battle scene")]
        [SerializeField] private Image playerPortrait;
        [SerializeField] private Image enemyPortrait;
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private GameObject intentBubble;
        [SerializeField] private TMP_Text intentBubbleText;
        [SerializeField] private Image playerBattleHpBarFill;
        [SerializeField] private TMP_Text playerBattleHpText;
        [SerializeField] private Image enemyHpBarFill;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private GameObject enemyHpRoot;
        [SerializeField] private RectTransform playerBattleStatusEffectsRoot;
        [SerializeField] private RectTransform enemyStatusEffectsRoot;
        [SerializeField] private GameObject statusTooltip;
        [SerializeField] private TMP_Text statusTooltipText;
        [SerializeField] private TMP_Text actionDescriptionText;

        [Header("Bottom panels")]
        [SerializeField] private Image bottomPanelBackground;
        [SerializeField] private Sprite bottomPanelDefaultSprite;
        [SerializeField] private Sprite bottomPanelMergeLitSprite;
        [SerializeField, Min(0f)] private float bottomPanelMergeFlashSeconds = BottomPanelMergeFlashSeconds;
        [SerializeField] private GameObject boardPanel;
        [SerializeField] private GameObject actionPanel;
        [SerializeField] private GameObject enemyTurnPanel;

        [Header("Board panel")]
        [SerializeField] private Image hpBarFill;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private RectTransform playerBoardStatusEffectsRoot;
        [SerializeField] private TMP_Text turnLimitText;
        [SerializeField] private List<BoardCellView> boardCells = new();
        [SerializeField] private BoardSwipeHandler boardSwipeHandler;
        [SerializeField] private RectTransform boardAnimationOverlay;

        [Header("Action panel")]
        [SerializeField] private TMP_Text costText;
        [SerializeField] private GameObject costFormulaHelpIcon;
        [SerializeField] private TMP_Text costFormulaHelpLabel;
        [SerializeField] private GameObject boardCostFormulaHelpIcon;
        [SerializeField] private TMP_Text boardCostFormulaHelpLabel;
        [SerializeField] private GameObject skillsView;
        [SerializeField] private TMP_Text skillsHeaderText;
        [SerializeField] private List<Button> skillTierButtons = new();
        [SerializeField] private List<TMP_Text> skillTierLabels = new();
        [SerializeField] private Button skillsEndTurnButton;

        [Header("Enemy turn panel")]
        [SerializeField] private TMP_Text enemyTurnText;

        [Header("Result overlay")]
        [SerializeField] private GameObject resultOverlay;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultDescriptionText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button reloadSceneButton;

        [Header("Settings")]
        [SerializeField] private Button settingButton;
        [SerializeField] private SettingPopup settingPopup;
        [SerializeField] private ConfirmPopup confirmPopup;

        [Header("Reward overlay")]
        [SerializeField] private RewardManager rewardManager;
        [SerializeField] private GameObject rewardOverlay;
        [SerializeField] private TMP_Text rewardTitleText;
        [SerializeField] private TMP_Text rewardDescriptionText;
        [SerializeField] private TMP_Text rewardRestText;
        [SerializeField] private TMP_Text rewardEnhanceText;
        [SerializeField] private Button rewardRestButton;
        [SerializeField] private Button rewardEnhanceButton;
        [SerializeField] private List<Button> rewardChoiceButtons = new();
        [SerializeField] private List<TMP_Text> rewardChoiceLabels = new();
        [SerializeField] private List<Button> skillReplacementButtons = new();
        [SerializeField] private List<TMP_Text> skillReplacementLabels = new();
        private int pendingSkillRewardChoiceIndex = -1;

        [Header("Board effects")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private SimpleBgmDucker bgmDucker;
        [SerializeField] private BoardTileEffectProfileSO boardTileEffectProfile;

        [Header("Theme")]
        [SerializeField] private Color emptyCellColor = new(0.10f, 0.10f, 0.10f, 1f);
        [SerializeField] private Color filledCellColor = ThemeBoardCellColor;
        [SerializeField] private Color highlightCellColor = new(0.92f, 0.90f, 0.85f, 1f);
        [SerializeField] private Color obstacleCellColor = new(0.55f, 0.10f, 0.55f, 1f);
        [SerializeField] private Color attackIntentColor = new(0.85f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color defenseIntentColor = new(0.12f, 0.32f, 0.90f, 1f);
        [SerializeField] private Color darknessIntentColor = new(0.20f, 0.07f, 0.34f, 1f);
        [SerializeField] private Color fearIntentColor = new(0.45f, 0.03f, 0.06f, 1f);
        [SerializeField] private Sprite attackIntentSprite;
        [SerializeField] private Sprite defenseIntentSprite;
        [SerializeField] private Sprite fearIntentSprite;
        [SerializeField] private Color playerHpFillColor = ThemeHpFillColor;
        [SerializeField] private Color enemyHpFillColor = ThemeHpFillColor;
        [SerializeField] private Color hpBarBackgroundColor = ThemeHpBarBackgroundColor;
        [SerializeField] private Color playerHpDamageTrailColor = ThemeHpDamageTrailColor;
        [SerializeField] private Color hpDamageTrailColor = ThemeHpDamageTrailColor;
        [SerializeField] private Color hpDamageFlashColor = new(1f, 1f, 1f, 0.95f);
        [SerializeField] private Color blockFrameColor = new(0.66f, 0.70f, 0.74f, 1f);
        [SerializeField] private Color blockIconColor = ThemeBoardHelpIconColor;
        [SerializeField] private Color buffStatusColor = new(0.20f, 0.46f, 0.30f, 0.95f);
        [SerializeField] private Color debuffStatusColor = new(0.46f, 0.16f, 0.20f, 0.95f);
        [SerializeField] private Color statusTooltipColor = new(0.06f, 0.07f, 0.08f, 0.96f);

        private PrototypeCombatBootstrap bootstrap;
        private CombatManager combatManager;
        private CombatSnapshot snapshot;
        private readonly PrototypeCombatUiState uiState = new();
        private readonly PrototypeCombatAudioRouter audioRouter = new();
        private readonly List<SkillSnapshot> visibleSkills = new();
        private readonly List<Image> intentIconSlots = new();
        private RectTransform intentIconRow;
        private BoardTransition pendingBoardTransition;
        private Coroutine boardAnimationCoroutine;
        private Coroutine combatVfxCoroutine;
        private Coroutine enemyDeathFadeCoroutine;
        private Coroutine enemyHpHideCoroutine;
        private Coroutine bottomPanelMergeFlashCoroutine;
        private bool boardTransitionAnimating;
        private bool lastEnemyWasDead;
        private int lastPlayedCombatVfxSequence;
        private RectTransform combatVfxPulseRect;
        private Vector3 combatVfxOriginalScale = Vector3.one;
        private readonly Dictionary<Image, float> hpMainFillRatios = new();
        private readonly Dictionary<Image, float> hpDamageTrailRatios = new();
        private readonly Dictionary<Image, Coroutine> hpDamageTrailCoroutines = new();
        private readonly Dictionary<Image, Coroutine> hpDamageFlashCoroutines = new();
        private readonly Dictionary<Image, Coroutine> hpHitShakeCoroutines = new();
        private readonly Dictionary<RectTransform, Vector2> hpHitShakeRestPositions = new();

        private void Awake()
        {
            ResolveMissingReferences();
            WireButtons();

            if (Application.isPlaying)
            {
                HideRuntimeOverlays();
            }
        }

        public void Initialize(PrototypeCombatBootstrap owner)
        {
            bootstrap = owner;
            UnbindCombatEvents();
            combatManager = owner != null ? owner.CombatManager : null;
            rewardManager = owner != null ? owner.RewardManager : rewardManager;

            ResolveMissingReferences();
            EnsureIntentBubbleDefaults();
            EnsureHpBarDefaults();
            ConfigureCostFormulaHelp();
            EnsureAudioDefaults();
            WireButtons();
            BindRewardEvents();
            HideRuntimeOverlays();

            if (combatManager == null)
            {
                return;
            }

            combatManager.OnCombatStateChanged -= HandleCombatStateChanged;
            combatManager.OnCombatStateChanged += HandleCombatStateChanged;
            if (combatManager.BoardManager != null)
            {
                combatManager.BoardManager.OnBoardTransitioned -= HandleBoardTransitioned;
                combatManager.BoardManager.OnBoardTransitioned += HandleBoardTransitioned;
            }

            snapshot = combatManager.GetSnapshot();
            lastEnemyWasDead = snapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false;
            SetEnemyPortraitAlpha(lastEnemyWasDead ? 0f : 1f);
            uiState.Sync(snapshot);
            Render();
        }

        private void OnDestroy()
        {
            UnbindCombatEvents();

            if (boardSwipeHandler != null)
            {
                boardSwipeHandler.OnSwipe -= HandleSwipe;
            }

            ClearBoardAnimationOverlay();
            ClearCombatVfx();
            ClearEnemyDeathFade();
            ClearEnemyHpHide();
            ClearBottomPanelMergeFlash();
            ClearHpDamageTrailAnimations();
        }

        private void UnbindCombatEvents()
        {
            UnbindRewardEvents();

            if (combatManager == null)
            {
                return;
            }

            combatManager.OnCombatStateChanged -= HandleCombatStateChanged;
            if (combatManager.BoardManager != null)
            {
                combatManager.BoardManager.OnBoardTransitioned -= HandleBoardTransitioned;
            }
        }

        private void HandleSwipe(Direction direction)
        {
            if (combatManager == null || uiState.ScreenMode != PrototypeCombatScreenMode.Board)
            {
                return;
            }

            combatManager.RequestBoardMove(direction);
        }

        private void Update()
        {
            if (combatManager == null || snapshot == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (Debug.isDebugBuild &&
                keyboard.sKey.wasPressedThisFrame &&
                IsPlayerCommandScreenMode(uiState.ScreenMode))
            {
                combatManager.RequestDebugKillEnemy();
                return;
            }

            if (uiState.ScreenMode != PrototypeCombatScreenMode.Board)
            {
                return;
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                combatManager.RequestBoardMove(Direction.Left);
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                combatManager.RequestBoardMove(Direction.Right);
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                combatManager.RequestBoardMove(Direction.Up);
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                combatManager.RequestBoardMove(Direction.Down);
            }
        }

        private static bool IsPlayerCommandScreenMode(PrototypeCombatScreenMode screenMode)
        {
            return screenMode == PrototypeCombatScreenMode.Board ||
                screenMode == PrototypeCombatScreenMode.ActionSkills;
        }

        private void WireButtons()
        {
            if (boardSwipeHandler != null)
            {
                boardSwipeHandler.OnSwipe -= HandleSwipe;
                boardSwipeHandler.OnSwipe += HandleSwipe;
            }

            BindButton(skillsEndTurnButton, () => combatManager?.RequestEndPlayerTurn());

            for (var i = 0; i < skillTierButtons.Count; i++)
            {
                var index = i;
                BindButton(skillTierButtons[i], () => OnSkillSlotClicked(index));
            }

            BindButton(restartButton, () => bootstrap?.RestartCombat());
            BindButton(reloadSceneButton, () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
            BindButton(settingButton, OpenSettingPopup);
            BindButton(pauseButton, OpenPauseConfirmPopup);
            BindPauseButtonEventTrigger();
        }

        private void BindButton(Button button, System.Action handler)
        {
            if (button == null)
            {
                return;
            }

            EnsureButtonRaycastable(button);
            button.onClick.RemoveAllListeners();
            var clickEmitter = button.GetComponent<ButtonClickAudioEmitter>();
            if (clickEmitter == null)
            {
                clickEmitter = button.gameObject.AddComponent<ButtonClickAudioEmitter>();
            }

            clickEmitter.EnsureBound();
            button.onClick.AddListener(() =>
            {
                handler?.Invoke();
                if (snapshot != null)
                {
                    uiState.Sync(snapshot);
                }
                Render();
            });
        }

        private static void EnsureButtonRaycastable(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = true;
            if (button.targetGraphic == null)
            {
                button.targetGraphic = button.GetComponent<Graphic>();
            }

            if (button.targetGraphic != null)
            {
                button.targetGraphic.raycastTarget = true;
            }
        }

        private void BindPauseButtonEventTrigger()
        {
            if (pauseButton == null)
            {
                return;
            }

            var trigger = pauseButton.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = pauseButton.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.RemoveAll(entry => entry.eventID == EventTriggerType.PointerClick);
            var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener(_ => OpenPauseConfirmPopup());
            trigger.triggers.Add(clickEntry);
        }

        private void OnSkillSlotClicked(int slotIndex)
        {
            if (combatManager == null ||
                snapshot == null ||
                snapshot.Skills == null ||
                slotIndex < 0 ||
                slotIndex >= snapshot.Skills.Count)
            {
                return;
            }

            var skill = snapshot.Skills[slotIndex];
            if (skill == null)
            {
                return;
            }

            if (combatManager.RequestUseSkillById(skill.SkillId))
            {
                PlayBottomPanelMergeFlash();
            }
        }

        private void HandleCombatStateChanged(CombatSnapshot nextSnapshot)
        {
            var nextEnemyDead = nextSnapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false;
            var enemyJustDied = !lastEnemyWasDead && nextEnemyDead;
            snapshot = nextSnapshot;
            uiState.Sync(snapshot);
            Render();
            PlayCombatVfxIfNeeded(snapshot.LastVfxCue);
            PlayEnemyDeathFadeIfNeeded(enemyJustDied, nextEnemyDead);
            lastEnemyWasDead = nextEnemyDead;

            if (pendingBoardTransition != null && uiState.ScreenMode == PrototypeCombatScreenMode.Board)
            {
                // 보드 이동 이벤트가 snapshot보다 먼저 올 수 있어서 잠시 보관했다가,
                // 화면이 아직 보드 패널일 때만 타일 이동 애니메이션을 재생한다.
                PlayBoardTransition(pendingBoardTransition);
                pendingBoardTransition = null;
            }
        }

        private void HandleBoardTransitioned(BoardTransition transition)
        {
            PlayBoardTileEffectCues(audioRouter.GetBoardTileEffectCues(transition));
            pendingBoardTransition = transition;
        }

        private void BindRewardEvents()
        {
            if (rewardManager == null)
            {
                return;
            }

            rewardManager.OnRewardOffered -= HandleRewardOffered;
            rewardManager.OnRewardChoicesOffered -= HandleRewardChoicesOffered;
            rewardManager.OnRewardClaimed -= HandleRewardClaimed;
            rewardManager.OnRewardOffered += HandleRewardOffered;
            rewardManager.OnRewardChoicesOffered += HandleRewardChoicesOffered;
            rewardManager.OnRewardClaimed += HandleRewardClaimed;
        }

        private void UnbindRewardEvents()
        {
            if (rewardManager == null)
            {
                return;
            }

            rewardManager.OnRewardOffered -= HandleRewardOffered;
            rewardManager.OnRewardChoicesOffered -= HandleRewardChoicesOffered;
            rewardManager.OnRewardClaimed -= HandleRewardClaimed;
        }

        private void HandleRewardOffered(BattleRewardSO _)
        {
            pendingSkillRewardChoiceIndex = -1;
            Render();
        }

        private void HandleRewardChoicesOffered(IReadOnlyList<BattleRewardSO> _)
        {
            pendingSkillRewardChoiceIndex = -1;
            Render();
        }

        private void HandleRewardClaimed(RewardChoiceResult _)
        {
            pendingSkillRewardChoiceIndex = -1;
            if (combatManager != null)
            {
                snapshot = combatManager.GetSnapshot();
                uiState.Sync(snapshot);
            }

            Render();
        }

        private void Render()
        {
            if (snapshot == null)
            {
                return;
            }

            ConfigureCostFormulaHelp();
            RenderTopBar();
            RenderBattleScene();
            RenderBoardPlayerStatus();
            RenderPanelVisibility();

            switch (uiState.ScreenMode)
            {
                case PrototypeCombatScreenMode.Board:
                    RenderBoardPanel();
                    break;
                case PrototypeCombatScreenMode.ActionSkills:
                    RenderActionPanel();
                    break;
                case PrototypeCombatScreenMode.EnemyTurn:
                    RenderEnemyTurnPanel();
                    break;
            }

            RenderOverlay();
        }

        private void RenderTopBar()
        {
            if (turnCounterText != null)
            {
                var turnCount = combatManager != null ? combatManager.TurnController.TurnCount : 1;
                turnCounterText.text = ToRoman(Mathf.Max(1, turnCount));
            }

            if (intentHeaderText != null)
            {
                intentHeaderText.text = "적 턴에 할 행동";
            }
        }

        private void RenderBattleScene()
        {
            var enemy = snapshot?.Enemies?.FirstOrDefault();
            var player = snapshot?.Player;
            var enemyIsAlive = enemy != null && !enemy.IsDead;
            if (enemyNameText != null)
            {
                enemyNameText.enableAutoSizing = true;
                enemyNameText.fontSizeMin = 18f;
                enemyNameText.fontSizeMax = 32f;
                enemyNameText.textWrappingMode = TextWrappingModes.Normal;
                enemyNameText.text = enemy?.DisplayName ?? string.Empty;
            }

            if (intentBubble != null)
            {
                EnsureIntentBubbleDefaults();
                var visibleIntents = GetVisibleIntents(enemy);
                var hasIntent = visibleIntents.Count > 0 && enemyIsAlive;
                intentBubble.SetActive(hasIntent);
                var primaryIntent = hasIntent ? visibleIntents[0] : null;
                var useIcons = hasIntent && ResolveIntentIcon(primaryIntent) != null;
                intentBubble.TryGetComponent<Image>(out var intentBubbleImage);

                if (useIcons)
                {
                    // 인텐트가 여러 개면 아이콘을 가로로 나열해서 2번째 이후도 전부 보여준다.
                    if (intentBubbleText != null)
                    {
                        intentBubbleText.text = string.Empty;
                    }

                    RenderIntentIcons(visibleIntents);

                    if (intentBubbleImage != null)
                    {
                        // 아이콘은 row가 그리므로 버블 자체 이미지는 투명 배경으로 둔다.
                        intentBubbleImage.sprite = null;
                        intentBubbleImage.color = new Color(1f, 1f, 1f, 0f);
                    }
                }
                else
                {
                    ClearIntentIcons();

                    if (hasIntent && intentBubbleText != null)
                    {
                        intentBubbleText.text = PrototypeCombatText.FormatIntents(visibleIntents);
                    }

                    if (hasIntent && intentBubbleImage != null)
                    {
                        intentBubbleImage.sprite = null;
                        intentBubbleImage.preserveAspect = false;
                        intentBubbleImage.type = Image.Type.Simple;
                        intentBubbleImage.color = GetIntentBubbleColor(primaryIntent);
                    }
                }
            }

            // Player/enemy sprites pulled from SO if assigned (data-driven).
            if (playerPortrait != null && combatManager != null && combatManager.Player != null)
            {
                var sprite = combatManager.Player != null
                    ? FindPlayerPortraitSprite()
                    : null;
                if (sprite != null)
                {
                    playerPortrait.sprite = sprite;
                }
            }

            if (enemyPortrait != null && enemy != null)
            {
                var sprite = FindEnemyPortraitSprite(enemy.EnemyIndex);
                if (sprite != null)
                {
                    enemyPortrait.sprite = sprite;
                }

                if (!enemy.IsDead && enemyDeathFadeCoroutine == null)
                {
                    SetEnemyPortraitAlpha(1f);
                }
            }

            if (playerBattleHpBarFill != null && player != null && player.MaxHp > 0)
            {
                SetHpBarValue(playerBattleHpBarFill, player.CurrentHp, player.MaxHp);
            }

            if (playerBattleHpText != null && player != null)
            {
                playerBattleHpText.text = PrototypeCombatText.FormatPlayerHp(player.CurrentHp, player.MaxHp, player.Block);
            }

            SetBlockIndicator(playerBattleHpBarFill, player?.Block ?? 0);
            RenderStatusEffects(playerBattleStatusEffectsRoot, player?.StatusEffects);

            if (enemyIsAlive)
            {
                ShowEnemyHp();
            }
            else if (enemy != null)
            {
                HideEnemyHpAfterDamageTrail();
            }
            else
            {
                HideEnemyHpImmediately();
            }

            if (enemyHpBarFill != null && enemy != null && enemy.MaxHp > 0)
            {
                SetHpBarValue(enemyHpBarFill, enemy.CurrentHp, enemy.MaxHp);
            }

            if (enemyHpText != null && enemy != null)
            {
                enemyHpText.text = PrototypeCombatText.FormatEnemyHp(enemy.CurrentHp, enemy.MaxHp, enemy.Block);
            }

            SetBlockIndicator(enemyHpBarFill, enemyIsAlive ? enemy.Block : 0);
            RenderStatusEffects(enemyStatusEffectsRoot, enemyIsAlive ? enemy.StatusEffects : null);

            if (actionDescriptionText != null)
            {
                actionDescriptionText.text = PrototypeCombatText.FormatActionDescription(snapshot?.LastActionDescription);
            }
        }

        private Sprite FindPlayerPortraitSprite()
        {
            return combatManager?.Player?.Data != null
                ? combatManager.Player.Data.portrait
                : playerPortrait != null ? playerPortrait.sprite : null;
        }

        private Sprite FindEnemyPortraitSprite(int enemyIndex)
        {
            if (combatManager == null)
            {
                return enemyPortrait != null ? enemyPortrait.sprite : null;
            }

            if (enemyIndex < 0 || enemyIndex >= combatManager.Enemies.Count)
            {
                return enemyPortrait != null ? enemyPortrait.sprite : null;
            }

            var enemyController = combatManager.Enemies[enemyIndex];
            return enemyController?.Data != null ? enemyController.Data.portrait : null;
        }

        private void RenderPanelVisibility()
        {
            var rewardReplacementVisible = IsRewardReplacementVisible();
            if (boardPanel != null)
            {
                boardPanel.SetActive(!rewardReplacementVisible &&
                    (uiState.ScreenMode == PrototypeCombatScreenMode.Board || boardTransitionAnimating));
            }

            // 마지막 보드 이동 애니메이션이 끝나기 전에 액션 패널로 바뀌면 타일이 끊겨 보인다.
            // 그래서 애니메이션 중에는 보드 패널을 유지하고 전환을 잠깐 미룬다.
            var deferPanelSwapForBoardAnimation = boardTransitionAnimating && uiState.ScreenMode != PrototypeCombatScreenMode.Board;
            if (actionPanel != null)
            {
                actionPanel.SetActive(!rewardReplacementVisible && !deferPanelSwapForBoardAnimation && (
                    uiState.ScreenMode == PrototypeCombatScreenMode.ActionSkills));
            }

            if (enemyTurnPanel != null)
            {
                enemyTurnPanel.SetActive(!rewardReplacementVisible &&
                    !deferPanelSwapForBoardAnimation &&
                    uiState.ScreenMode == PrototypeCombatScreenMode.EnemyTurn);
            }

            if (skillsView != null)
            {
                skillsView.SetActive(uiState.ScreenMode == PrototypeCombatScreenMode.ActionSkills);
            }
        }

        private void RenderBoardPanel()
        {
            if (snapshot == null)
            {
                return;
            }

            if (turnLimitText != null)
            {
                turnLimitText.text = PrototypeCombatText.FormatRemainingMoves(snapshot.RemainingBoardMoves);
            }

            for (var i = 0; i < boardCells.Count && i < 16; i++)
            {
                var row = i / 4;
                var col = i % 4;
                var cell = boardCells[i];
                if (cell == null)
                {
                    continue;
                }

                var value = snapshot.Board[row, col];
                cell.SetValue(value, emptyCellColor, filledCellColor, highlightCellColor, GetObstacleCellColor());
            }
        }

        private void PlayBoardTransition(BoardTransition transition)
        {
            if (transition == null || transition.Movements.Count == 0)
            {
                return;
            }

            if (boardAnimationCoroutine != null)
            {
                StopCoroutine(boardAnimationCoroutine);
                boardAnimationCoroutine = null;
            }

            boardTransitionAnimating = true;
            ClearBoardAnimationOverlay(stopRoutine: false);
            if (ResolveBoardAnimationOverlay() == null)
            {
                PulseTransitionTargets(transition);
                boardTransitionAnimating = false;
                return;
            }

            boardAnimationCoroutine = StartCoroutine(PlayBoardTransitionRoutine(transition));
        }

        private IEnumerator PlayBoardTransitionRoutine(BoardTransition transition)
        {
            var overlay = ResolveBoardAnimationOverlay();
            if (overlay == null)
            {
                yield break;
            }

            // The grid cells are layout-controlled, so moving temporary overlay tiles avoids fighting GridLayoutGroup.
            var animatedTiles = new List<(RectTransform Rect, Vector2 From, Vector2 To)>();
            foreach (var movement in transition.Movements)
            {
                if (movement.Value <= 0 ||
                    !TryGetCellCenter(movement.From, out var from) ||
                    !TryGetCellCenter(movement.To, out var to))
                {
                    continue;
                }

                var template = GetCell(movement.From) ?? GetCell(movement.To);
                var tile = CreateAnimatedTile(movement.Value, template);
                if (tile == null)
                {
                    continue;
                }

                tile.anchoredPosition = from;
                animatedTiles.Add((tile, from, to));
            }

            var elapsed = 0f;
            while (elapsed < BoardTransitionDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / BoardTransitionDurationSeconds));
                foreach (var animated in animatedTiles)
                {
                    if (animated.Rect != null)
                    {
                        animated.Rect.anchoredPosition = Vector2.LerpUnclamped(animated.From, animated.To, t);
                    }
                }

                yield return null;
            }

            foreach (var animated in animatedTiles)
            {
                if (animated.Rect != null)
                {
                    DestroyAnimationObject(animated.Rect.gameObject);
                }
            }

            PulseTransitionTargets(transition);
            if (uiState.ScreenMode != PrototypeCombatScreenMode.Board && BoardToActionPanelDelaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(BoardToActionPanelDelaySeconds);
            }

            boardTransitionAnimating = false;
            boardAnimationCoroutine = null;
            // After the moving overlay disappears, render the authoritative board state from CombatManager.
            Render();
        }

        private RectTransform CreateAnimatedTile(int value, BoardCellView template)
        {
            var overlay = ResolveBoardAnimationOverlay();
            if (overlay == null)
            {
                return null;
            }

            var tileObject = new GameObject("AnimatedTile", typeof(RectTransform), typeof(Image));
            tileObject.transform.SetParent(overlay, false);
            var tileRect = tileObject.GetComponent<RectTransform>();
            tileRect.anchorMin = new Vector2(0.5f, 0.5f);
            tileRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);
            tileRect.sizeDelta = template != null && template.RectTransform != null
                ? template.RectTransform.rect.size
                : new Vector2(140f, 140f);

            var image = tileObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = GetCellColor(value);

            var labelObject = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(tileObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = value.ToString();
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontSize = template != null && template.ValueText != null ? template.ValueText.fontSize : 40f;
            label.color = GetCellTextColor(value);
            if (template != null && template.ValueText != null && template.ValueText.font != null)
            {
                label.font = template.ValueText.font;
            }

            return tileRect;
        }

        private bool TryGetCellCenter(Vector2Int boardPosition, out Vector2 center)
        {
            center = Vector2.zero;
            var overlay = ResolveBoardAnimationOverlay();
            var cell = GetCell(boardPosition);
            if (overlay == null || cell == null || cell.RectTransform == null)
            {
                return false;
            }

            var worldCenter = cell.RectTransform.TransformPoint(cell.RectTransform.rect.center);
            center = overlay.InverseTransformPoint(worldCenter);
            return true;
        }

        private BoardCellView GetCell(Vector2Int boardPosition)
        {
            if (boardPosition.x < 0 || boardPosition.x >= 4 || boardPosition.y < 0 || boardPosition.y >= 4)
            {
                return null;
            }

            var index = boardPosition.y * 4 + boardPosition.x;
            return index >= 0 && index < boardCells.Count ? boardCells[index] : null;
        }

        private RectTransform ResolveBoardAnimationOverlay()
        {
            if (boardAnimationOverlay != null)
            {
                return boardAnimationOverlay;
            }

            if (boardPanel == null)
            {
                return null;
            }

            boardAnimationOverlay = boardPanel.transform.Find("BoardAnimationOverlay") as RectTransform;
            return boardAnimationOverlay;
        }

        private void PulseTransitionTargets(BoardTransition transition)
        {
            foreach (var target in transition.Movements
                         .Where(movement => movement.IsMergeParticipant)
                         .Select(movement => movement.To)
                         .Concat(transition.Spawns.Select(spawn => spawn.Position))
                         .Distinct())
            {
                GetCell(target)?.PlayMergePulse();
            }
        }

        private Color GetCellColor(int value)
        {
            if (value < 0)
            {
                return GetObstacleCellColor();
            }

            return value == 0 ? new Color(26f / 255f, 26f / 255f, 26f / 255f, 1f) : Color.black;
        }

        private static Color GetCellTextColor(int value)
        {
            return Color.white;
        }

        private void ClearBoardAnimationOverlay(bool stopRoutine = true)
        {
            if (stopRoutine && boardAnimationCoroutine != null)
            {
                StopCoroutine(boardAnimationCoroutine);
                boardAnimationCoroutine = null;
                boardTransitionAnimating = false;
            }

            var overlay = ResolveBoardAnimationOverlay();
            if (overlay == null)
            {
                return;
            }

            for (var i = overlay.childCount - 1; i >= 0; i--)
            {
                DestroyAnimationObject(overlay.GetChild(i).gameObject);
            }
        }

        private static void DestroyAnimationObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void RenderActionPanel()
        {
            if (costText != null)
            {
                costText.text = PrototypeCombatText.FormatCost(snapshot?.CurrentCost ?? 0);
            }

            visibleSkills.Clear();
            if (snapshot != null)
            {
                visibleSkills.AddRange(uiState.GetVisibleSkills(snapshot));
            }

            if (skillsHeaderText != null)
            {
                skillsHeaderText.text = PrototypeCombatText.FormatSkillHeader();
            }

            for (var i = 0; i < skillTierButtons.Count; i++)
            {
                var isSlot = i < PlayerCombatController.MaxEquippedSkillSlots;
                var hasSkill = isSlot && i < visibleSkills.Count && visibleSkills[i] != null;
                var canAfford = hasSkill && snapshot != null && visibleSkills[i].Cost <= snapshot.CurrentCost;
                var canUseSkill = canAfford && visibleSkills[i].CanExecute && snapshot?.IsSkillPresentationLocked != true;
                var button = skillTierButtons[i];
                if (button != null)
                {
                    button.gameObject.SetActive(isSlot);
                    button.interactable = hasSkill && canUseSkill;
                    ApplySkillSlotTheme(button, hasSkill ? visibleSkills[i] : null, canUseSkill);
                    BindTooltip(button, hasSkill ? PrototypeCombatText.FormatSkillTooltip(visibleSkills[i]) : string.Empty);
                    var slotIndex = i;
                    BindButton(button, () => OnSkillSlotClicked(slotIndex));
                }

                if (i < skillTierLabels.Count && skillTierLabels[i] != null)
                {
                    if (hasSkill)
                    {
                        var skill = visibleSkills[i];
                        skillTierLabels[i].text = PrototypeCombatText.FormatSkillLabel(
                            i,
                            skill,
                            canAfford,
                            snapshot?.CurrentCost ?? 0);
                    }
                    else
                    {
                        skillTierLabels[i].text = isSlot
                            ? PrototypeCombatText.FormatEmptySkillSlotLabel(i)
                            : string.Empty;
                    }
                }
            }
        }

        private static void ApplySkillSlotTheme(Button button, SkillSnapshot skill, bool canAfford)
        {
            if (button == null)
            {
                return;
            }

            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var baseColor = skill != null ? ResolveSkillTypeColor(skill.SkillType) : ThemeSkillEmptyColor;
            var normalColor = skill != null && !canAfford ? DimSkillSlotColor(baseColor) : baseColor;
            var highlightedColor = Color.Lerp(normalColor, ThemePrimaryColor, 0.18f);
            var pressedColor = Color.Lerp(normalColor, Color.black, 0.18f);
            normalColor.a = 1f;
            highlightedColor.a = 1f;
            pressedColor.a = 1f;

            image.color = normalColor;
            image.raycastTarget = true;
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.selectedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.disabledColor = skill != null ? DimSkillSlotColor(baseColor) : ThemeSkillEmptyColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var outline = button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = Color.Lerp(baseColor, ThemePrimaryColor, 0.28f);
                outline.effectDistance = new Vector2(3f, -3f);
                outline.useGraphicAlpha = true;
            }
        }

        private static Color ResolveSkillTypeColor(SkillType skillType)
        {
            return skillType switch
            {
                SkillType.Attack => ThemeSkillAttackColor,
                SkillType.Defense => ThemeSkillDefenseColor,
                SkillType.Debuff => ThemeSkillChangeColor,
                SkillType.Heal => ThemeSkillChangeColor,
                _ => ThemeSkillChangeColor,
            };
        }

        private static Color DimSkillSlotColor(Color color)
        {
            var dimmed = Color.Lerp(ThemeHpDarkColor, color, 0.45f);
            dimmed.a = 1f;
            return dimmed;
        }

        private void RenderEnemyTurnPanel()
        {
            if (enemyTurnText != null)
            {
                enemyTurnText.text = PrototypeCombatText.FormatEnemyTurnAction(snapshot?.LastActionDescription);
            }
        }

        private Color GetIntentBubbleColor(EnemyIntent intent)
        {
            if (intent == null)
            {
                return attackIntentColor;
            }

            return intent.intentType switch
            {
                EnemyIntentType.Defense => defenseIntentColor,
                EnemyIntentType.Debuff => GetDebuffColor(intent.debuffType),
                _ => attackIntentColor,
            };
        }

        private Sprite ResolveIntentIcon(EnemyIntent intent)
        {
            if (intent == null)
            {
                return attackIntentSprite;
            }

            return intent.intentType switch
            {
                EnemyIntentType.Defense => defenseIntentSprite,
                EnemyIntentType.Debuff => fearIntentSprite,
                _ => attackIntentSprite,
            };
        }

        private Color GetStatusEffectColor(CombatStatusEffectSnapshot effect)
        {
            if (effect == null || string.IsNullOrWhiteSpace(effect.Id))
            {
                return debuffStatusColor;
            }

            if (effect.Id == "attack-up" || effect.Id == "attack-down")
            {
                return WithAlpha(attackIntentColor, 0.95f);
            }

            if (effect.Id == "fear")
            {
                return WithAlpha(GetDebuffColor(DebuffType.Fear), 0.95f);
            }

            if (effect.Id == "darkness")
            {
                return WithAlpha(GetDebuffColor(DebuffType.Darkness), 0.95f);
            }

            return effect.IsBuff ? buffStatusColor : debuffStatusColor;
        }

        private Color GetDebuffColor(DebuffType debuffType)
        {
            return debuffType switch
            {
                DebuffType.Darkness => darknessIntentColor,
                DebuffType.Fear => fearIntentColor,
                _ => debuffStatusColor,
            };
        }

        private Color GetObstacleCellColor()
        {
            return GetDebuffColor(DebuffType.Darkness);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static List<EnemyIntent> GetVisibleIntents(EnemyCombatSnapshot enemy)
        {
            var intents = new List<EnemyIntent>();
            if (enemy?.Intents != null)
            {
                foreach (var intent in enemy.Intents)
                {
                    if (intent == null)
                    {
                        continue;
                    }

                    intents.Add(intent);
                    if (intents.Count >= EnemySO.MaximumActionsPerTurn)
                    {
                        break;
                    }
                }
            }

            if (intents.Count == 0 && enemy?.Intent != null)
            {
                intents.Add(enemy.Intent);
            }

            return intents;
        }

        private void ResolveMissingReferences()
        {
            turnCounterText ??= FindComponentInChildrenByName<TMP_Text>("TurnCounterText");
            intentHeaderText ??= FindComponentInChildrenByName<TMP_Text>("IntentHeaderText");
            bottomPanelBackground ??= FindComponentInChildrenByName<Image>("BottomPanelBackground");
            boardPanel ??= FindChildByName("BoardPanel")?.gameObject;
            actionPanel ??= FindChildByName("ActionPanel")?.gameObject;
            enemyTurnPanel ??= FindChildByName("EnemyTurnPanel")?.gameObject;
            playerPortrait ??= FindComponentInChildrenByName<Image>("PlayerPortrait");
            enemyPortrait ??= FindComponentInChildrenByName<Image>("EnemyPortrait");
            enemyNameText ??= FindComponentInChildrenByName<TMP_Text>("EnemyNameText");
            intentBubble ??= FindChildByName("IntentBubble")?.gameObject;
            intentBubbleText ??= FindComponentInChildrenByName<TMP_Text>("IntentBubbleText");
            playerBattleHpBarFill ??= FindNestedComponentByName<Image>("PlayerBattleHp", "Fill");
            playerBattleHpText ??= FindNestedComponentByName<TMP_Text>("PlayerBattleHp", "Text");
            enemyHpBarFill ??= FindNestedComponentByName<Image>("EnemyHp", "Fill");
            enemyHpText ??= FindNestedComponentByName<TMP_Text>("EnemyHp", "Text");
            ResolveEnemyHpRoot();
            playerBattleStatusEffectsRoot ??= FindComponentInChildrenByName<RectTransform>("PlayerBattleStatusEffects");
            enemyStatusEffectsRoot ??= FindComponentInChildrenByName<RectTransform>("EnemyStatusEffects");
            statusTooltip ??= FindChildByName("StatusTooltip")?.gameObject;
            statusTooltipText ??= FindNestedComponentByName<TMP_Text>("StatusTooltip", "Text");
            hpBarFill ??= FindComponentInChildrenByName<Image>("HpBarFill");
            hpText ??= FindComponentInChildrenByName<TMP_Text>("HpText");
            costFormulaHelpIcon ??= FindChildByName("CostFormulaHelpIcon")?.gameObject;
            costFormulaHelpLabel ??= FindNestedComponentByName<TMP_Text>("CostFormulaHelpIcon", "Label");
            boardCostFormulaHelpIcon ??= FindChildByName("BoardCostFormulaHelpIcon")?.gameObject;
            boardCostFormulaHelpLabel ??= FindNestedComponentByName<TMP_Text>("BoardCostFormulaHelpIcon", "Label");
            playerBoardStatusEffectsRoot ??= FindComponentInChildrenByName<RectTransform>("PlayerBoardStatusEffects");
            costText ??= FindComponentInChildrenByName<TMP_Text>("CostText");
            skillsView ??= FindChildByName("SkillsView")?.gameObject;
            skillsHeaderText ??= FindComponentInChildrenByName<TMP_Text>("SkillsHeaderText");
            skillsEndTurnButton ??= FindComponentInChildrenByName<Button>("TurnEndButton");
            actionDescriptionText ??= FindComponentInChildrenByName<TMP_Text>("ActionDescriptionText");
            enemyTurnText ??= FindComponentInChildrenByName<TMP_Text>("EnemyTurnText");
            pauseButton ??= FindOrCreateButtonByName("pauseButton") ?? FindOrCreateButtonByName("PauseButton");
            settingButton ??= FindOrCreateButtonByName("SettingButton");
            settingPopup ??= FindAnyObjectByType<SettingPopup>(FindObjectsInactive.Include);
            confirmPopup ??= FindAnyObjectByType<ConfirmPopup>(FindObjectsInactive.Include);
        }

        private Button FindOrCreateButtonByName(string childName)
        {
            var child = FindChildByName(childName);
            if (child == null)
            {
                return null;
            }

            var button = child.GetComponent<Button>();
            if (button != null)
            {
                return button;
            }

            button = child.gameObject.AddComponent<Button>();
            button.targetGraphic = child.GetComponent<Graphic>();
            return button;
        }

        private void OpenSettingPopup()
        {
            if (settingPopup == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                settingPopup = SettingPopup.CreateRuntime(canvas != null ? canvas.transform : transform.root);
            }

            settingPopup.Open();
        }

        private void OpenPauseConfirmPopup()
        {
            Debug.Log("Pause button clicked. Opening pause confirm popup.");
            if (confirmPopup == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                confirmPopup = ConfirmPopup.CreateRuntime(canvas != null ? canvas.transform : transform.root);
            }

            confirmPopup.Show("종료하시겠습니까?", ReturnToMainMenu, null);
        }

        private static void ReturnToMainMenu()
        {
            if (GameManager.Instance != null && GameManager.Instance.FlowController != null)
            {
                GameManager.Instance.FlowController.RequestMainMenu();
                return;
            }

            SceneManager.LoadScene("MainMenu");
        }

        private void ResolveEnemyHpRoot()
        {
            if (enemyHpRoot != null)
            {
                return;
            }

            enemyHpRoot = FindChildByName("EnemyHp")?.gameObject
                ?? enemyHpBarFill?.transform.parent?.gameObject
                ?? enemyHpText?.transform.parent?.gameObject;
        }

        private void EnsureIntentBubbleDefaults()
        {
            if (intentBubble != null && intentBubble.TryGetComponent<RectTransform>(out var bubbleRect))
            {
                var hasAuthoredSize = bubbleRect.sizeDelta.x > 0.01f && bubbleRect.sizeDelta.y > 0.01f;
                if (!hasAuthoredSize)
                {
                    bubbleRect.sizeDelta = new Vector2(IntentBubbleSquareSize, IntentBubbleSquareSize);
                }
            }

            if (intentBubbleText == null)
            {
                return;
            }

            intentBubbleText.alignment = TextAlignmentOptions.Center;
            intentBubbleText.enableAutoSizing = true;
            if (intentBubbleText.fontSizeMin <= 0f)
            {
                intentBubbleText.fontSizeMin = IntentBubbleTextMinFontSize;
            }

            if (intentBubbleText.fontSizeMax <= 0f || intentBubbleText.fontSizeMax > 40f)
            {
                intentBubbleText.fontSizeMax = IntentBubbleTextMaxFontSize;
            }

            intentBubbleText.textWrappingMode = TextWrappingModes.Normal;
            intentBubbleText.overflowMode = TextOverflowModes.Ellipsis;
            intentBubbleText.raycastTarget = false;

            var textRect = intentBubbleText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            var hasAuthoredPadding =
                Mathf.Abs(textRect.offsetMin.x) > 0.01f ||
                Mathf.Abs(textRect.offsetMin.y) > 0.01f ||
                Mathf.Abs(textRect.offsetMax.x) > 0.01f ||
                Mathf.Abs(textRect.offsetMax.y) > 0.01f;
            if (!hasAuthoredPadding)
            {
                textRect.offsetMin = new Vector2(IntentBubbleTextPadding, IntentBubbleTextPadding);
                textRect.offsetMax = new Vector2(-IntentBubbleTextPadding, -IntentBubbleTextPadding);
            }

            textRect.anchoredPosition = Vector2.zero;
        }

        // 인텐트 아이콘을 버블 안에 가로로 깐다. 슬롯은 풀링해서 재사용한다.
        private void RenderIntentIcons(List<EnemyIntent> intents)
        {
            EnsureIntentIconRow();
            if (intentIconRow == null)
            {
                return;
            }

            intentIconRow.gameObject.SetActive(true);

            var shown = 0;
            if (intents != null)
            {
                for (var i = 0; i < intents.Count && shown < EnemySO.MaximumActionsPerTurn; i++)
                {
                    var icon = ResolveIntentIcon(intents[i]);
                    if (icon == null)
                    {
                        continue;
                    }

                    var slot = GetOrCreateIntentIconSlot(shown);
                    slot.sprite = icon;
                    slot.preserveAspect = true;
                    slot.color = Color.white;
                    slot.gameObject.SetActive(true);
                    shown++;
                }
            }

            for (var i = shown; i < intentIconSlots.Count; i++)
            {
                if (intentIconSlots[i] != null)
                {
                    intentIconSlots[i].gameObject.SetActive(false);
                }
            }
        }

        private void ClearIntentIcons()
        {
            if (intentIconRow != null)
            {
                intentIconRow.gameObject.SetActive(false);
            }

            foreach (var slot in intentIconSlots)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureIntentIconRow()
        {
            if (intentIconRow != null || intentBubble == null)
            {
                return;
            }

            if (intentBubble.transform.Find("IntentIconRow") is RectTransform existing)
            {
                intentIconRow = existing;
                return;
            }

            var rowObject = new GameObject("IntentIconRow");
            intentIconRow = rowObject.AddComponent<RectTransform>();
            intentIconRow.SetParent(intentBubble.transform, false);
            intentIconRow.anchorMin = Vector2.zero;
            intentIconRow.anchorMax = Vector2.one;
            intentIconRow.offsetMin = Vector2.zero;
            intentIconRow.offsetMax = Vector2.zero;

            var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        private Image GetOrCreateIntentIconSlot(int index)
        {
            while (intentIconSlots.Count <= index)
            {
                var iconObject = new GameObject($"IntentIcon_{intentIconSlots.Count}");
                var iconRect = iconObject.AddComponent<RectTransform>();
                iconRect.SetParent(intentIconRow, false);
                var iconImage = iconObject.AddComponent<Image>();
                iconImage.raycastTarget = false;
                intentIconSlots.Add(iconImage);
            }

            return intentIconSlots[index];
        }

        private void SetEnemyHpVisible(bool visible)
        {
            ResolveEnemyHpRoot();
            if (enemyHpRoot != null && enemyHpRoot.activeSelf != visible)
            {
                enemyHpRoot.SetActive(visible);
            }
        }

        private void ShowEnemyHp()
        {
            ClearEnemyHpHide();
            SetEnemyHpVisible(true);
        }

        private void HideEnemyHpImmediately()
        {
            ClearEnemyHpHide();
            SetEnemyHpVisible(false);
        }

        private void HideEnemyHpAfterDamageTrail()
        {
            ResolveEnemyHpRoot();
            if (enemyHpRoot == null || enemyHpHideCoroutine != null || !enemyHpRoot.activeSelf)
            {
                return;
            }

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                SetEnemyHpVisible(false);
                return;
            }

            enemyHpHideCoroutine = StartCoroutine(EnemyHpHideAfterDamageTrailRoutine());
        }

        private IEnumerator EnemyHpHideAfterDamageTrailRoutine()
        {
            var delaySeconds = HpDamageTrailDelaySeconds + HpDamageTrailDurationSeconds + 0.05f;
            if (delaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            SetEnemyHpVisible(false);
            enemyHpHideCoroutine = null;
        }

        private void ClearEnemyHpHide()
        {
            if (enemyHpHideCoroutine != null)
            {
                StopCoroutine(enemyHpHideCoroutine);
                enemyHpHideCoroutine = null;
            }
        }

        private void EnsureHpBarDefaults()
        {
            NormalizeHpBarTheme();
            ConfigureHpText(playerBattleHpText);
            ConfigureHpText(enemyHpText);
            ConfigureHpText(hpText);
            ConfigureHpBarFill(playerBattleHpBarFill, hpBarBackgroundColor);
            ConfigureHpBarFill(enemyHpBarFill, hpBarBackgroundColor);
            ConfigureHpBarFill(hpBarFill, hpBarBackgroundColor);
            EnsureDamageTrailFill(playerBattleHpBarFill);
            EnsureDamageTrailFill(enemyHpBarFill);
            EnsureDamageTrailFill(hpBarFill);
            playerBoardStatusEffectsRoot = ResolveStatusEffectsRoot(hpBarFill, "PlayerBoardStatusEffects") ?? playerBoardStatusEffectsRoot;
            playerBattleStatusEffectsRoot = ResolveStatusEffectsRoot(playerBattleHpBarFill, "PlayerBattleStatusEffects") ?? playerBattleStatusEffectsRoot;
            enemyStatusEffectsRoot = ResolveStatusEffectsRoot(enemyHpBarFill, "EnemyStatusEffects") ?? enemyStatusEffectsRoot;
            EnsureStatusTooltip();
        }

        private void NormalizeHpBarTheme()
        {
            playerHpFillColor = ThemeHpFillColor;
            enemyHpFillColor = ThemeHpFillColor;
            hpBarBackgroundColor = ThemeHpBarBackgroundColor;
            playerHpDamageTrailColor = ThemeHpDamageTrailColor;
            hpDamageTrailColor = ThemeHpDamageTrailColor;
        }

        private static void ConfigureHpBarFill(Image fillImage, Color backgroundColor)
        {
            if (fillImage == null)
            {
                return;
            }

            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillClockwise = true;
            fillImage.fillAmount = Mathf.Clamp01(fillImage.fillAmount);
            fillImage.color = ThemeHpFillColor;
            fillImage.raycastTarget = false;

            var rectTransform = fillImage.rectTransform;
            ConfigureHpBarContentRect(rectTransform);
            rectTransform.pivot = new Vector2(0f, 0.5f);

            if (fillImage.transform.parent != null &&
                fillImage.transform.parent.TryGetComponent<Image>(out var backgroundImage))
            {
                backgroundImage.type = Image.Type.Simple;
                backgroundImage.color = Color.clear;
                backgroundImage.raycastTarget = false;
            }

            var hpRoot = ResolveHpRoot(fillImage);
            var interior = hpRoot != null ? hpRoot.Find(HpBarInteriorName)?.GetComponent<Image>() : null;
            if (interior != null)
            {
                interior.sprite = fillImage.sprite;
                interior.type = Image.Type.Simple;
                interior.color = backgroundColor;
                interior.raycastTarget = false;
                ConfigureHpBarContentRect(interior.rectTransform);
            }

            SetHpBarLayerOrder(hpRoot, fillImage, null, null);
            ConfigureHpBarOutline(hpRoot);
        }

        private static void ConfigureHpBarOutline(RectTransform hpRoot)
        {
            var outline = hpRoot != null ? hpRoot.Find(HpBarOutlineName)?.GetComponent<Image>() : null;
            if (outline == null)
            {
                return;
            }

            outline.type = Image.Type.Simple;
            outline.color = ThemeHpBorderColor;
            outline.raycastTarget = false;
            var outlineRect = outline.rectTransform;
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;
            outline.transform.SetAsLastSibling();
        }

        private static void ConfigureHpText(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.color = Color.white;
            text.fontSize = Mathf.Max(text.fontSize, HpTextMinFontSize);
            text.outlineColor = ThemeHpTextOutlineColor;
            text.outlineWidth = HpTextOutlineWidth;
            var outline = text.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = ThemeHpTextOutlineColor;
                outline.effectDistance = new Vector2(HpTextOutlineDistance, -HpTextOutlineDistance);
                outline.useGraphicAlpha = true;
            }
        }

        private void RenderBoardPlayerStatus()
        {
            var player = snapshot?.Player;
            if (hpBarFill != null && player != null && player.MaxHp > 0)
            {
                SetHpBarValue(hpBarFill, player.CurrentHp, player.MaxHp);
            }

            if (hpText != null && player != null)
            {
                hpText.text = PrototypeCombatText.FormatPlayerHp(player.CurrentHp, player.MaxHp, player.Block);
            }

            SetBlockIndicator(hpBarFill, player?.Block ?? 0);
            RenderStatusEffects(playerBoardStatusEffectsRoot, player?.StatusEffects);
        }

        private static void ConfigureHpBarContentRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(HpBarBorderThickness, HpBarBorderThickness);
            rectTransform.offsetMax = new Vector2(-HpBarBorderThickness, -HpBarBorderThickness);
        }

        private void SetHpBarValue(Image fillImage, int currentHp, int maxHp)
        {
            var ratio = maxHp > 0 ? Mathf.Clamp01(currentHp / (float)maxHp) : 0f;
            var damageTrailFill = EnsureDamageTrailFill(fillImage);
            var hasPreviousMainRatio = hpMainFillRatios.TryGetValue(fillImage, out var previousMainRatio);
            if (!hasPreviousMainRatio)
            {
                previousMainRatio = ratio;
            }

            SetHpFillRatio(fillImage, ratio);

            if (damageTrailFill != null)
            {
                if (!hasPreviousMainRatio)
                {
                    SetHpDamageTrailRatio(fillImage, damageTrailFill, ratio);
                }
                else if (ratio < previousMainRatio - 0.001f)
                {
                    var startRatio = Mathf.Max(ResolveHpDamageTrailRatio(fillImage), previousMainRatio);
                    SetHpDamageTrailRatio(fillImage, damageTrailFill, startRatio);
                    PlayHpDamageFlash(fillImage, startRatio, ratio);
                    PlayHpHitShake(fillImage);
                    PlayHpDamageTrail(fillImage, damageTrailFill, startRatio, ratio);
                }
                else if (ratio > previousMainRatio + 0.001f)
                {
                    StopHpDamageTrail(fillImage);
                    StopHpDamageFlash(fillImage, hideFlash: true);
                    SetHpDamageTrailRatio(fillImage, damageTrailFill, ratio);
                }
            }

            hpMainFillRatios[fillImage] = ratio;
        }

        private static void SetHpFillRatio(Image fillImage, float ratio, bool applyFixedThemeColor = true)
        {
            if (fillImage == null)
            {
                return;
            }

            ratio = Mathf.Clamp01(ratio);
            if (applyFixedThemeColor)
            {
                fillImage.color = ThemeHpFillColor;
            }

            fillImage.fillAmount = ratio;

            var rectTransform = fillImage.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = new Vector2(HpBarBorderThickness, HpBarBorderThickness);
            rectTransform.offsetMax = new Vector2(-HpBarBorderThickness, -HpBarBorderThickness);
        }

        private Image EnsureDamageTrailFill(Image fillImage)
        {
            var hpRoot = ResolveHpRoot(fillImage);
            if (fillImage == null || hpRoot == null)
            {
                return null;
            }

            var existing = hpRoot.Find("DamageTrailFill");
            if (existing == null)
            {
                return null;
            }

            var trailImage = existing.GetComponent<Image>();
            var trailRect = existing as RectTransform;
            if (trailImage == null || trailRect == null)
            {
                return null;
            }

            trailImage.sprite = fillImage.sprite;
            trailImage.type = Image.Type.Filled;
            trailImage.fillMethod = Image.FillMethod.Horizontal;
            trailImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            trailImage.fillClockwise = true;
            trailImage.color = ResolveHpDamageTrailColor(fillImage);
            trailImage.raycastTarget = false;

            trailRect.anchorMin = Vector2.zero;
            trailRect.anchorMax = Vector2.one;
            ConfigureHpBarContentRect(trailRect);
            trailRect.pivot = new Vector2(0f, 0.5f);
            SetHpFillRatio(trailImage, ResolveHpDamageTrailRatio(fillImage), applyFixedThemeColor: false);
            SetHpBarLayerOrder(hpRoot, fillImage, trailImage, null);
            return trailImage;
        }

        private Color ResolveHpDamageTrailColor(Image fillImage)
        {
            return fillImage == playerBattleHpBarFill || fillImage == hpBarFill
                ? playerHpDamageTrailColor
                : hpDamageTrailColor;
        }

        private Image EnsureDamageFlashFill(Image fillImage)
        {
            var hpRoot = ResolveHpRoot(fillImage);
            if (fillImage == null || hpRoot == null)
            {
                return null;
            }

            var existing = hpRoot.Find("DamageFlashFill");
            if (existing == null)
            {
                return null;
            }

            var flashImage = existing.GetComponent<Image>();
            var flashRect = existing as RectTransform;
            if (flashImage == null || flashRect == null)
            {
                return null;
            }

            flashImage.sprite = fillImage.sprite;
            flashImage.type = Image.Type.Simple;
            flashImage.color = hpDamageFlashColor;
            flashImage.raycastTarget = false;
            ConfigureHpBarContentRect(flashRect);
            flashRect.pivot = new Vector2(0f, 0.5f);
            SetHpBarLayerOrder(hpRoot, fillImage, hpRoot.Find("DamageTrailFill")?.GetComponent<Image>(), flashImage);
            return flashImage;
        }

        private static void SetHpBarLayerOrder(RectTransform hpRoot, Image fillImage, Image trailImage, Image flashImage)
        {
            if (hpRoot == null)
            {
                return;
            }

            var nextIndex = 0;
            var interior = hpRoot.Find(HpBarInteriorName);
            if (interior != null)
            {
                interior.SetSiblingIndex(nextIndex++);
            }

            if (trailImage != null)
            {
                trailImage.transform.SetSiblingIndex(Mathf.Min(nextIndex++, hpRoot.childCount - 1));
            }

            if (fillImage != null)
            {
                fillImage.transform.SetSiblingIndex(Mathf.Min(nextIndex++, hpRoot.childCount - 1));
            }

            if (flashImage != null)
            {
                flashImage.transform.SetSiblingIndex(Mathf.Min(nextIndex, hpRoot.childCount - 1));
            }

            var outline = hpRoot.Find(HpBarOutlineName);
            if (outline != null)
            {
                outline.SetAsLastSibling();
            }
        }

        private float ResolveHpDamageTrailRatio(Image fillImage)
        {
            if (fillImage != null && hpDamageTrailRatios.TryGetValue(fillImage, out var ratio))
            {
                return ratio;
            }

            return fillImage != null && hpMainFillRatios.TryGetValue(fillImage, out var mainRatio)
                ? mainRatio
                : fillImage != null ? Mathf.Clamp01(fillImage.fillAmount) : 0f;
        }

        private void SetHpDamageTrailRatio(Image fillImage, Image trailImage, float ratio)
        {
            if (fillImage == null || trailImage == null)
            {
                return;
            }

            ratio = Mathf.Clamp01(ratio);
            trailImage.color = ResolveHpDamageTrailColor(fillImage);
            SetHpFillRatio(trailImage, ratio, applyFixedThemeColor: false);
            hpDamageTrailRatios[fillImage] = ratio;
        }

        private void PlayHpDamageTrail(Image fillImage, Image trailImage, float fromRatio, float toRatio)
        {
            StopHpDamageTrail(fillImage);
            if (!Application.isPlaying || !isActiveAndEnabled || fillImage == null || trailImage == null)
            {
                return;
            }

            hpDamageTrailCoroutines[fillImage] = StartCoroutine(HpDamageTrailRoutine(fillImage, trailImage, fromRatio, toRatio));
        }

        private void PlayHpDamageFlash(Image fillImage, float fromRatio, float toRatio)
        {
            var flashImage = EnsureDamageFlashFill(fillImage);
            if (fillImage == null || flashImage == null)
            {
                return;
            }

            StopHpDamageFlash(fillImage, hideFlash: false);
            SetHpDamageFlashSegment(flashImage, fromRatio, toRatio, hpDamageFlashColor.a);
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            hpDamageFlashCoroutines[fillImage] = StartCoroutine(HpDamageFlashRoutine(fillImage, flashImage, fromRatio, toRatio));
        }

        private IEnumerator HpDamageFlashRoutine(Image fillImage, Image flashImage, float fromRatio, float toRatio)
        {
            var elapsed = 0f;
            while (elapsed < HpDamageFlashDurationSeconds)
            {
                if (fillImage == null || flashImage == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                var alpha = Mathf.Lerp(hpDamageFlashColor.a, 0f, Mathf.Clamp01(elapsed / HpDamageFlashDurationSeconds));
                SetHpDamageFlashSegment(flashImage, fromRatio, toRatio, alpha);
                yield return null;
            }

            SetHpDamageFlashSegment(flashImage, fromRatio, toRatio, 0f);
            hpDamageFlashCoroutines.Remove(fillImage);
        }

        private void SetHpDamageFlashSegment(Image flashImage, float fromRatio, float toRatio, float alpha)
        {
            if (flashImage == null)
            {
                return;
            }

            fromRatio = Mathf.Clamp01(fromRatio);
            toRatio = Mathf.Clamp01(toRatio);
            var left = Mathf.Min(fromRatio, toRatio);
            var right = Mathf.Max(fromRatio, toRatio);
            flashImage.gameObject.SetActive(right > left + 0.001f && alpha > 0.001f);
            var rectTransform = flashImage.rectTransform;
            rectTransform.anchorMin = new Vector2(left, 0f);
            rectTransform.anchorMax = new Vector2(right, 1f);
            rectTransform.offsetMin = new Vector2(HpBarBorderThickness, HpBarBorderThickness);
            rectTransform.offsetMax = new Vector2(-HpBarBorderThickness, -HpBarBorderThickness);

            var color = hpDamageFlashColor;
            color.a = Mathf.Clamp01(alpha);
            flashImage.color = color;
        }

        private IEnumerator HpDamageTrailRoutine(Image fillImage, Image trailImage, float fromRatio, float toRatio)
        {
            if (HpDamageTrailDelaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(HpDamageTrailDelaySeconds);
            }

            var elapsed = 0f;
            while (elapsed < HpDamageTrailDurationSeconds)
            {
                if (fillImage == null || trailImage == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / HpDamageTrailDurationSeconds));
                SetHpDamageTrailRatio(fillImage, trailImage, Mathf.Lerp(fromRatio, toRatio, t));
                yield return null;
            }

            SetHpDamageTrailRatio(fillImage, trailImage, toRatio);
            hpDamageTrailCoroutines.Remove(fillImage);
        }

        private void StopHpDamageTrail(Image fillImage)
        {
            if (fillImage == null || !hpDamageTrailCoroutines.TryGetValue(fillImage, out var routine))
            {
                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            hpDamageTrailCoroutines.Remove(fillImage);
        }

        private void StopHpDamageFlash(Image fillImage, bool hideFlash)
        {
            if (fillImage != null && hpDamageFlashCoroutines.TryGetValue(fillImage, out var routine) && routine != null)
            {
                StopCoroutine(routine);
            }

            if (fillImage != null)
            {
                hpDamageFlashCoroutines.Remove(fillImage);
            }

            if (hideFlash)
            {
                var flash = ResolveHpRoot(fillImage)?.Find("DamageFlashFill")?.GetComponent<Image>();
                if (flash != null)
                {
                    SetHpDamageFlashSegment(flash, 0f, 0f, 0f);
                }
            }
        }

        private void PlayHpHitShake(Image fillImage)
        {
            var hpRoot = ResolveHpRoot(fillImage);
            if (fillImage == null || hpRoot == null)
            {
                return;
            }

            StopHpHitShake(fillImage, restorePosition: true);
            if (!hpHitShakeRestPositions.ContainsKey(hpRoot))
            {
                hpHitShakeRestPositions[hpRoot] = hpRoot.anchoredPosition;
            }

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            hpHitShakeCoroutines[fillImage] = StartCoroutine(HpHitShakeRoutine(fillImage, hpRoot));
        }

        private IEnumerator HpHitShakeRoutine(Image fillImage, RectTransform hpRoot)
        {
            var restPosition = hpHitShakeRestPositions.TryGetValue(hpRoot, out var storedPosition)
                ? storedPosition
                : hpRoot.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < HpHitShakeDurationSeconds)
            {
                if (fillImage == null || hpRoot == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / HpHitShakeDurationSeconds);
                var offset = Mathf.Sin(t * Mathf.PI * 4f) * HpHitShakeMagnitude * (1f - t);
                hpRoot.anchoredPosition = restPosition + new Vector2(offset, 0f);
                yield return null;
            }

            hpRoot.anchoredPosition = restPosition;
            hpHitShakeCoroutines.Remove(fillImage);
        }

        private void StopHpHitShake(Image fillImage, bool restorePosition)
        {
            if (fillImage == null)
            {
                return;
            }

            if (hpHitShakeCoroutines.TryGetValue(fillImage, out var routine) && routine != null)
            {
                StopCoroutine(routine);
            }

            hpHitShakeCoroutines.Remove(fillImage);
            if (!restorePosition)
            {
                return;
            }

            var hpRoot = ResolveHpRoot(fillImage);
            if (hpRoot != null && hpHitShakeRestPositions.TryGetValue(hpRoot, out var restPosition))
            {
                hpRoot.anchoredPosition = restPosition;
            }
        }

        private void ClearHpDamageTrailAnimations()
        {
            foreach (var routine in hpDamageTrailCoroutines.Values)
            {
                if (routine != null)
                {
                    StopCoroutine(routine);
                }
            }

            hpDamageTrailCoroutines.Clear();
            foreach (var routine in hpDamageFlashCoroutines.Values)
            {
                if (routine != null)
                {
                    StopCoroutine(routine);
                }
            }

            hpDamageFlashCoroutines.Clear();
            foreach (var routine in hpHitShakeCoroutines.Values)
            {
                if (routine != null)
                {
                    StopCoroutine(routine);
                }
            }

            hpHitShakeCoroutines.Clear();
            foreach (var entry in hpHitShakeRestPositions)
            {
                if (entry.Key != null)
                {
                    entry.Key.anchoredPosition = entry.Value;
                }
            }

            hpHitShakeRestPositions.Clear();
            hpMainFillRatios.Clear();
            hpDamageTrailRatios.Clear();
        }

        private void SetBlockIndicator(Image fillImage, int block)
        {
            var hpRoot = ResolveHpRoot(fillImage);
            if (hpRoot == null)
            {
                return;
            }

            var outline = hpRoot.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }

            var icon = EnsureBlockIcon(hpRoot);
            if (icon == null)
            {
                return;
            }

            icon.gameObject.SetActive(block > 0);
            if (block <= 0)
            {
                return;
            }

            var label = icon.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = block.ToString();
            }
        }

        private RectTransform EnsureBlockIcon(RectTransform hpRoot)
        {
            var existing = hpRoot.Find("BlockIcon") as RectTransform;
            if (existing != null)
            {
                ConfigureBlockIconRect(existing);
                return existing;
            }

            return null;
        }

        private static void ConfigureBlockIconRect(RectTransform icon)
        {
            icon.anchorMin = new Vector2(1f, 0.5f);
            icon.anchorMax = new Vector2(1f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(12f, 0f);
            icon.sizeDelta = new Vector2(34f, 24f);
        }

        private static RectTransform ResolveHpRoot(Image fillImage)
        {
            if (fillImage == null)
            {
                return null;
            }

            return fillImage.transform.parent as RectTransform ?? fillImage.rectTransform;
        }

        private RectTransform ResolveStatusEffectsRoot(Image fillImage, string rootName)
        {
            var hpRoot = ResolveHpRoot(fillImage);
            if (hpRoot == null)
            {
                return null;
            }

            var existing = hpRoot.Find(rootName) as RectTransform;
            if (existing != null)
            {
                EnsureStatusEffectsLayout(existing, preserveExistingLayout: true);
                existing.SetAsLastSibling();
                return existing;
            }

            return null;
        }

        private static void EnsureStatusEffectsLayout(RectTransform root, bool preserveExistingLayout)
        {
            if (root == null)
            {
                return;
            }

            var layout = root.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                return;
            }

            if (!preserveExistingLayout)
            {
                ConfigureStatusEffectsLayout(layout);
            }
        }

        private static void ConfigureStatusEffectsLayout(HorizontalLayoutGroup layout)
        {
            if (layout == null)
            {
                return;
            }

            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static void ConfigureStatusEffectsRoot(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            var isPlayerBattleStatusRoot = root.name == "PlayerBattleStatusEffects";
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(0f, 0f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = isPlayerBattleStatusRoot
                ? new Vector2(HpStatusEffectXOffset, -39f)
                : new Vector2(HpStatusEffectXOffset, -6f);
            root.sizeDelta = new Vector2(160f, 32f);
        }

        private void RenderStatusEffects(
            RectTransform root,
            IReadOnlyList<CombatStatusEffectSnapshot> effects)
        {
            if (root == null)
            {
                return;
            }

            EnsureStatusEffectsLayout(root, preserveExistingLayout: true);

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name == StatusEffectTemplateName)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                DestroyAnimationObject(child.gameObject);
            }

            var hasEffects = effects != null && effects.Count > 0;
            root.gameObject.SetActive(hasEffects);
            if (!hasEffects)
            {
                return;
            }

            foreach (var effect in effects)
            {
                CreateStatusEffectChip(root, effect);
            }
        }

        private void CreateStatusEffectChip(RectTransform root, CombatStatusEffectSnapshot effect)
        {
            if (effect == null || string.IsNullOrWhiteSpace(effect.Id))
            {
                return;
            }

            var template = root.Find(StatusEffectTemplateName);
            if (template == null)
            {
                return;
            }

            var chipObject = Instantiate(template.gameObject, root);
            chipObject.name = $"StatusEffect_{effect.Id}";
            chipObject.SetActive(true);
            var chipRect = chipObject.GetComponent<RectTransform>();
            var image = chipObject.GetComponent<Image>();
            if (chipRect == null || image == null)
            {
                DestroyAnimationObject(chipObject);
                return;
            }

            chipRect.sizeDelta = new Vector2(32f, 32f);
            image.color = GetStatusEffectColor(effect);
            image.raycastTarget = true;

            var tooltipTarget = chipObject.GetComponent<StatusEffectTooltipTarget>();
            tooltipTarget?.Initialize(effect.Description, ShowStatusTooltip, HideStatusTooltip);
        }

        private void ConfigureCostFormulaHelp()
        {
            var description = PrototypeCombatText.FormatPlayerStatsTooltip(snapshot);
            ConfigureCostFormulaHelpIcon(costFormulaHelpIcon, ref costFormulaHelpLabel, description);
            ConfigureCostFormulaHelpIcon(boardCostFormulaHelpIcon, ref boardCostFormulaHelpLabel, description);
        }

        private void ConfigureCostFormulaHelpIcon(GameObject icon, ref TMP_Text label, string description)
        {
            if (icon == null)
            {
                return;
            }

            if (icon.TryGetComponent<Image>(out var image))
            {
                image.raycastTarget = true;
            }

            label ??= icon.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "?";
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }

            var target = icon.GetComponent<StatusEffectTooltipTarget>();
            target ??= icon.AddComponent<StatusEffectTooltipTarget>();
            target.Initialize(description, ShowStatusTooltip, HideStatusTooltip, nextShowOnClick: true);
        }

        private void BindTooltip(Button button, string description)
        {
            if (button == null)
            {
                return;
            }

            var tooltipTarget = button.GetComponent<StatusEffectTooltipTarget>();
            if (string.IsNullOrWhiteSpace(description))
            {
                if (tooltipTarget != null)
                {
                    tooltipTarget.enabled = false;
                }

                HideStatusTooltip();
                return;
            }

            tooltipTarget ??= button.gameObject.AddComponent<StatusEffectTooltipTarget>();
            tooltipTarget.enabled = true;
            tooltipTarget.Initialize(description, ShowStatusTooltip, HideStatusTooltip);
        }

        private void EnsureStatusTooltip()
        {
            if (statusTooltip == null)
            {
                return;
            }

            if (statusTooltip.TryGetComponent<Image>(out var image))
            {
                image.color = statusTooltipColor;
                image.raycastTarget = false;
            }

            if (statusTooltipText == null)
            {
                statusTooltipText = statusTooltip.GetComponentInChildren<TMP_Text>(true);
            }

            if (statusTooltipText == null)
            {
                statusTooltip.SetActive(false);
                return;
            }

            statusTooltipText.alignment = TextAlignmentOptions.Left;
            statusTooltipText.fontSize = TooltipFontSize;
            statusTooltipText.color = Color.white;
            statusTooltipText.textWrappingMode = TextWrappingModes.Normal;
            if (actionDescriptionText != null && actionDescriptionText.font != null)
            {
                statusTooltipText.font = actionDescriptionText.font;
            }

            statusTooltip.SetActive(false);
        }

        private void ShowStatusTooltip(string description, RectTransform source)
        {
            if (statusTooltip == null || statusTooltipText == null)
            {
                return;
            }

            statusTooltipText.text = string.IsNullOrWhiteSpace(description) ? string.Empty : description;
            var ownerRect = transform as RectTransform;
            var tooltipRect = statusTooltip.transform as RectTransform;
            if (tooltipRect != null)
            {
                var lineCount = string.IsNullOrEmpty(description) ? 1 : description.Count(character => character == '\n') + 1;
                var targetWidth = lineCount > 1 ? TooltipMultiLineWidth : TooltipSingleLineWidth;
                if (ownerRect != null)
                {
                    targetWidth = Mathf.Min(targetWidth, Mathf.Max(0f, ownerRect.rect.width - 16f));
                }

                tooltipRect.sizeDelta = new Vector2(
                    targetWidth,
                    Mathf.Clamp(TooltipBaseHeight + lineCount * TooltipLineHeight, TooltipMinHeight, TooltipMaxHeight));
            }

            if (source != null && ownerRect != null && tooltipRect != null)
            {
                var worldPosition = source.TransformPoint(source.rect.center);
                var localPosition = ownerRect.InverseTransformPoint(worldPosition);
                var desiredPosition = localPosition + new Vector3(0f, 28f, 0f);
                var margin = 8f;
                var tooltipSize = tooltipRect.sizeDelta;
                var ownerBounds = ownerRect.rect;
                var minX = ownerBounds.xMin + tooltipSize.x * tooltipRect.pivot.x + margin;
                var maxX = ownerBounds.xMax - tooltipSize.x * (1f - tooltipRect.pivot.x) - margin;
                var minY = ownerBounds.yMin + tooltipSize.y * tooltipRect.pivot.y + margin;
                var maxY = ownerBounds.yMax - tooltipSize.y * (1f - tooltipRect.pivot.y) - margin;
                tooltipRect.anchoredPosition = new Vector2(
                    minX <= maxX ? Mathf.Clamp(desiredPosition.x, minX, maxX) : ownerBounds.center.x,
                    minY <= maxY ? Mathf.Clamp(desiredPosition.y, minY, maxY) : ownerBounds.center.y);
            }

            statusTooltip.SetActive(true);
        }

        private void HideStatusTooltip()
        {
            if (statusTooltip != null)
            {
                statusTooltip.SetActive(false);
            }
        }

        private void EnsureAudioDefaults()
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
            audioSource.volume = 1f;
            audioSource.mute = false;
            audioSource.loop = false;
            if (sfxMixerGroup != null)
            {
                audioSource.outputAudioMixerGroup = sfxMixerGroup;
            }
            // Keep prototype UI sounds audible regardless of camera distance.
            audioSource.maxDistance = UiSfxDistance;
            audioSource.minDistance = UiSfxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
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

        private Transform FindChildByName(string childName)
        {
            return FindChildByName(transform, childName);
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChildByName(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private T FindComponentInChildrenByName<T>(string childName)
            where T : Component
        {
            var child = FindChildByName(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private T FindNestedComponentByName<T>(string parentName, string childName)
            where T : Component
        {
            var parent = FindChildByName(parentName);
            if (parent == null)
            {
                return null;
            }

            var child = FindChildByName(parent, childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private void PlayBoardTileEffectCues(IReadOnlyList<BoardTileEffectCue> cues)
        {
            if (cues == null || cues.Count == 0)
            {
                return;
            }

            if (cues.Any(cue => cue.CueType == BoardTileEffectCueType.Merge))
            {
                PlayBottomPanelMergeFlash();
            }

            if (boardTileEffectProfile == null)
            {
                return;
            }

            foreach (var cue in cues)
            {
                var effect = ResolveBoardTileEffect(cue);
                if (effect == null)
                {
                    continue;
                }

                PlayEffectAudio(effect, cue.CueType == BoardTileEffectCueType.Merge);
                SpawnBoardEffectPrefab(effect, cue.Position);
            }
        }

        private void PlayBottomPanelMergeFlash()
        {
            if (bottomPanelBackground == null || bottomPanelMergeLitSprite == null)
            {
                return;
            }

            if (bottomPanelDefaultSprite == null)
            {
                bottomPanelDefaultSprite = bottomPanelBackground.sprite;
            }

            if (bottomPanelDefaultSprite == null)
            {
                return;
            }

            if (bottomPanelMergeFlashCoroutine != null)
            {
                StopCoroutine(bottomPanelMergeFlashCoroutine);
                bottomPanelMergeFlashCoroutine = null;
            }

            bottomPanelBackground.sprite = bottomPanelMergeLitSprite;
            bottomPanelBackground.enabled = true;

            if (!Application.isPlaying || !isActiveAndEnabled || bottomPanelMergeFlashSeconds <= 0f)
            {
                bottomPanelBackground.sprite = bottomPanelDefaultSprite;
                return;
            }

            bottomPanelMergeFlashCoroutine = StartCoroutine(BottomPanelMergeFlashRoutine());
        }

        private IEnumerator BottomPanelMergeFlashRoutine()
        {
            yield return new WaitForSecondsRealtime(bottomPanelMergeFlashSeconds);
            if (bottomPanelBackground != null && bottomPanelDefaultSprite != null)
            {
                bottomPanelBackground.sprite = bottomPanelDefaultSprite;
            }

            bottomPanelMergeFlashCoroutine = null;
        }

        private void ClearBottomPanelMergeFlash()
        {
            if (bottomPanelMergeFlashCoroutine != null)
            {
                StopCoroutine(bottomPanelMergeFlashCoroutine);
                bottomPanelMergeFlashCoroutine = null;
            }

            if (bottomPanelBackground != null && bottomPanelDefaultSprite != null)
            {
                bottomPanelBackground.sprite = bottomPanelDefaultSprite;
            }
        }

        private CombatEffectBinding ResolveBoardTileEffect(BoardTileEffectCue cue)
        {
            return cue.CueType switch
            {
                BoardTileEffectCueType.Move => boardTileEffectProfile.ResolveMoveEffect(),
                BoardTileEffectCueType.Merge => boardTileEffectProfile.ResolveMergeEffect(cue.TileValue),
                _ => null,
            };
        }

        private bool PlayEffectAudio(CombatEffectBinding effect, bool duckBgm)
        {
            if (effect?.sfxClip == null || audioSource == null)
            {
                return false;
            }

            if (duckBgm)
            {
                DuckBgmForImportantSfx();
            }

            return CombatEffectAudioPlayer.PlayOneShot(audioSource, effect, 1f, transform);
        }

        private void SpawnBoardEffectPrefab(CombatEffectBinding effect, Vector2Int boardPosition)
        {
            if (effect?.vfxPrefab == null)
            {
                return;
            }

            var overlay = ResolveBoardAnimationOverlay();
            GameObject instance;
            if (overlay != null)
            {
                instance = Instantiate(effect.vfxPrefab, overlay);
                if (TryGetCellCenter(boardPosition, out var center))
                {
                    if (instance.transform is RectTransform rect)
                    {
                        rect.anchorMin = new Vector2(0.5f, 0.5f);
                        rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchoredPosition = center + new Vector2(effect.localOffset.x, effect.localOffset.y);
                    }
                    else
                    {
                        instance.transform.localPosition = new Vector3(center.x, center.y, 0f) + effect.localOffset;
                    }
                }
            }
            else
            {
                instance = Instantiate(effect.vfxPrefab, transform);
                instance.transform.localPosition += effect.localOffset;
            }

            var lifetime = effect.EffectiveAutoDestroySeconds;
            if (lifetime > 0f)
            {
                Destroy(instance, lifetime);
            }
        }

        private void RenderOverlay()
        {
            var rewardVisible = IsRewardReplacementVisible();

            if (rewardOverlay != null)
            {
                rewardOverlay.SetActive(rewardVisible);
            }

            if (rewardVisible)
            {
                RenderRewardOverlay();
            }

            if (resultOverlay == null)
            {
                return;
            }

            var visible = snapshot != null &&
                (snapshot.Phase == CombatPhase.Victory || snapshot.Phase == CombatPhase.Defeat) &&
                !rewardVisible;
            resultOverlay.SetActive(visible);

            if (!visible)
            {
                return;
            }

            if (resultTitleText != null)
            {
                resultTitleText.text = PrototypeCombatText.FormatResultTitle(snapshot.Phase);
            }

            if (resultDescriptionText != null)
            {
                resultDescriptionText.text = PrototypeCombatText.FormatResultDescription(snapshot);
            }

            SetButtonLabel(restartButton, snapshot.Phase == CombatPhase.Victory ? "이어 하기" : "다시 하기");
            SetButtonLabel(reloadSceneButton, "종료");
        }

        private bool IsRewardReplacementVisible()
        {
            return snapshot != null &&
                snapshot.Phase == CombatPhase.Victory &&
                rewardManager != null &&
                rewardManager.HasUnclaimedReward;
        }

        private void HideRuntimeOverlays()
        {
            if (rewardOverlay != null)
            {
                rewardOverlay.SetActive(false);
            }

            if (resultOverlay != null)
            {
                resultOverlay.SetActive(false);
            }
        }

        private void RenderRewardOverlay()
        {
            var choices = rewardManager != null ? rewardManager.PendingChoices : null;
            var isReplacingSkill = pendingSkillRewardChoiceIndex >= 0;
            if (rewardTitleText != null)
            {
                rewardTitleText.text = isReplacingSkill ? "기술 교체" : "보상 선택";
            }

            if (rewardDescriptionText != null)
            {
                rewardDescriptionText.text = isReplacingSkill
                    ? "잊을 기술을 선택하세요."
                    : "보상 3개 중 하나를 선택하세요.";
            }

            if (isReplacingSkill)
            {
                RenderSkillReplacementChoices();
                return;
            }

            for (var index = 0; index < skillReplacementButtons.Count; index++)
            {
                var button = skillReplacementButtons[index];
                if (button == null)
                {
                    continue;
                }

                button.gameObject.SetActive(false);
                BindTooltip(button, string.Empty);
            }

            for (var index = 0; index < rewardChoiceButtons.Count; index++)
            {
                var button = rewardChoiceButtons[index];
                if (button == null)
                {
                    continue;
                }

                var isChoiceSlot = index < RewardManager.OfferedChoiceCount;
                var hasChoice = isChoiceSlot && choices != null && index < choices.Count && choices[index] != null;
                button.gameObject.SetActive(isChoiceSlot);
                button.interactable = hasChoice;
                if (!hasChoice)
                {
                    BindTooltip(button, string.Empty);
                    SetRewardChoiceLabel(index, string.Empty);
                    continue;
                }

                var choiceIndex = index;
                SetRewardChoiceLabel(index, PrototypeCombatText.FormatRewardChoice(choices[index]));
                BindTooltip(button, PrototypeCombatText.FormatRewardTooltip(choices[index]));
                BindButton(button, () => OnRewardChoiceClicked(choiceIndex));
            }
        }

        private void RenderSkillReplacementChoices()
        {
            var skills = snapshot?.Skills;
            for (var index = 0; index < rewardChoiceButtons.Count; index++)
            {
                var button = rewardChoiceButtons[index];
                if (button == null)
                {
                    continue;
                }

                button.gameObject.SetActive(false);
                BindTooltip(button, string.Empty);
            }

            for (var index = 0; index < skillReplacementButtons.Count; index++)
            {
                var isSlot = index < PlayerCombatController.MaxEquippedSkillSlots;
                var hasSkill = isSlot && skills != null && index < skills.Count && skills[index] != null;
                var button = skillReplacementButtons[index];
                if (button == null)
                {
                    continue;
                }

                button.gameObject.SetActive(isSlot);
                button.interactable = hasSkill;
                BindTooltip(button, hasSkill ? PrototypeCombatText.FormatSkillTooltip(skills[index]) : string.Empty);

                var replacementIndex = index;
                SetSkillReplacementLabel(
                    index,
                    hasSkill
                        ? PrototypeCombatText.FormatSkillReplacementLabel(index, skills[index])
                        : PrototypeCombatText.FormatEmptySkillSlotLabel(index));

                if (!hasSkill)
                {
                    continue;
                }

                BindButton(button, () => rewardManager?.ChooseReward(
                    pendingSkillRewardChoiceIndex,
                    combatManager != null ? combatManager.Player : null,
                    replacementIndex));
            }
        }

        private void OnRewardChoiceClicked(int choiceIndex)
        {
            var choices = rewardManager?.PendingChoices;
            if (choices == null || choiceIndex < 0 || choiceIndex >= choices.Count)
            {
                return;
            }

            var player = combatManager != null ? combatManager.Player : null;
            var reward = choices[choiceIndex];
            if (reward != null &&
                reward.IsSkillReward &&
                player != null &&
                player.Skills.Count >= PlayerCombatController.MaxEquippedSkillSlots)
            {
                pendingSkillRewardChoiceIndex = choiceIndex;
                Render();
                return;
            }

            rewardManager?.ChooseReward(choiceIndex, player);
        }

        private void SetRewardChoiceLabel(int index, string text)
        {
            var label = index >= 0 && index < rewardChoiceLabels.Count ? rewardChoiceLabels[index] : null;
            if (label == null && index >= 0 && index < rewardChoiceButtons.Count && rewardChoiceButtons[index] != null)
            {
                label = rewardChoiceButtons[index].GetComponentInChildren<TMP_Text>(true);
            }

            if (label != null)
            {
                label.text = text;
            }
        }

        private void SetSkillReplacementLabel(int index, string text)
        {
            var label = index >= 0 && index < skillReplacementLabels.Count ? skillReplacementLabels[index] : null;
            if (label == null && index >= 0 && index < skillReplacementButtons.Count && skillReplacementButtons[index] != null)
            {
                label = skillReplacementButtons[index].GetComponentInChildren<TMP_Text>(true);
            }

            if (label != null)
            {
                label.text = text;
            }
        }

        private void PlayCombatVfxIfNeeded(CombatVfxCue cue)
        {
            if (cue == null || cue.Sequence <= 0 || cue.Sequence == lastPlayedCombatVfxSequence || !isActiveAndEnabled)
            {
                return;
            }

            lastPlayedCombatVfxSequence = cue.Sequence;
            ClearCombatVfx();
            combatVfxCoroutine = StartCoroutine(PlayCombatVfxRoutine(cue));
        }

        private IEnumerator PlayCombatVfxRoutine(CombatVfxCue cue)
        {
            var root = transform as RectTransform;
            if (root == null)
            {
                yield break;
            }

            if (cue.DebuffType == DebuffType.Darkness)
            {
                PulseObstacleCells();
            }

            var vfxObject = new GameObject("TemporaryDebuffVfx", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            vfxObject.transform.SetParent(root, false);
            var rect = vfxObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var group = vfxObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var image = vfxObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = GetDebuffVfxColor(cue.DebuffType);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(vfxObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(720f, 120f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = PrototypeCombatText.FormatDebuffVfxLabel(cue);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 44f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (actionDescriptionText != null && actionDescriptionText.font != null)
            {
                label.font = actionDescriptionText.font;
            }

            var pulseRect = cue.DebuffType == DebuffType.Fear && playerPortrait != null
                ? playerPortrait.rectTransform
                : null;
            var originalScale = pulseRect != null ? pulseRect.localScale : Vector3.one;
            combatVfxPulseRect = pulseRect;
            combatVfxOriginalScale = originalScale;

            var elapsed = 0f;
            while (elapsed < CombatVfxDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / CombatVfxDurationSeconds);
                var pulse = Mathf.Sin(t * Mathf.PI);
                group.alpha = pulse;
                labelRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.06f, pulse);

                if (pulseRect != null)
                {
                    pulseRect.localScale = originalScale * Mathf.Lerp(1f, 1.06f, pulse);
                }

                yield return null;
            }

            if (pulseRect != null)
            {
                pulseRect.localScale = originalScale;
            }

            DestroyAnimationObject(vfxObject);
            combatVfxCoroutine = null;
            combatVfxPulseRect = null;
        }

        private void PulseObstacleCells()
        {
            if (snapshot?.Board == null)
            {
                return;
            }

            for (var row = 0; row < 4; row++)
            {
                for (var col = 0; col < 4; col++)
                {
                    if (Board2048Manager.IsObstacle(snapshot.Board[row, col]))
                    {
                        GetCell(new Vector2Int(col, row))?.PlayMergePulse();
                    }
                }
            }
        }

        private Color GetDebuffVfxColor(DebuffType debuffType)
        {
            var color = GetDebuffColor(debuffType);
            color.a = debuffType switch
            {
                DebuffType.Fear => 0.42f,
                DebuffType.Darkness => 0.48f,
                _ => 0.35f,
            };
            return color;
        }

        private void ClearCombatVfx()
        {
            if (combatVfxCoroutine != null)
            {
                StopCoroutine(combatVfxCoroutine);
                combatVfxCoroutine = null;
            }

            if (combatVfxPulseRect != null)
            {
                combatVfxPulseRect.localScale = combatVfxOriginalScale;
                combatVfxPulseRect = null;
            }

            var existing = transform.Find("TemporaryDebuffVfx");
            if (existing != null)
            {
                DestroyAnimationObject(existing.gameObject);
            }
        }

        private void PlayEnemyDeathFadeIfNeeded(bool enemyJustDied, bool nextEnemyDead)
        {
            if ((enemyJustDied || nextEnemyDead) &&
                enemyPortrait != null &&
                enemyDeathFadeCoroutine == null &&
                enemyPortrait.color.a > 0.001f)
            {
                PlayEnemyDeathFade();
                return;
            }

            if (!nextEnemyDead && enemyPortrait != null)
            {
                ClearEnemyDeathFade();
                SetEnemyPortraitAlpha(1f);
            }
        }

        private void PlayEnemyDeathFade()
        {
            if (enemyPortrait == null)
            {
                return;
            }

            ClearEnemyDeathFade();
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                SetEnemyPortraitAlpha(0f);
                return;
            }

            enemyDeathFadeCoroutine = StartCoroutine(EnemyDeathFadeRoutine());
        }

        private IEnumerator EnemyDeathFadeRoutine()
        {
            var fromAlpha = enemyPortrait != null ? Mathf.Clamp01(enemyPortrait.color.a) : 1f;
            var startTime = Time.realtimeSinceStartup;
            while (true)
            {
                var elapsed = Time.realtimeSinceStartup - startTime;
                var t = Mathf.Clamp01(elapsed / EnemyDeathFadeDurationSeconds);
                SetEnemyPortraitAlpha(Mathf.Lerp(fromAlpha, 0f, t));

                if (t >= 1f)
                {
                    break;
                }

                yield return null;
            }

            SetEnemyPortraitAlpha(0f);
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

        private void SetEnemyPortraitAlpha(float alpha)
        {
            if (enemyPortrait == null)
            {
                return;
            }

            var color = enemyPortrait.color;
            color.a = Mathf.Clamp01(alpha);
            enemyPortrait.color = color;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private static string ToRoman(int value)
        {
            if (value <= 0)
            {
                return "I";
            }

            var numerals = new[]
            {
                (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
                (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
                (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
            };

            var sb = new System.Text.StringBuilder();
            foreach (var (number, symbol) in numerals)
            {
                while (value >= number)
                {
                    sb.Append(symbol);
                    value -= number;
                }
            }

            return sb.ToString();
        }

    }
}
