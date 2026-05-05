using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project2048.Board2048;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Presentation;
using Project2048.Rewards;
using Project2048.Score;
using Project2048.Skills;
using TMPro;
using UnityEngine;
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
    public class CombatUiView : MonoBehaviour
    {
        // Tile slides should stay snappy; panel switching gets its own delay below.
        public const float BoardTransitionDurationSeconds = 0.14f;
        public const float BoardToActionPanelDelaySeconds = 0.45f;
        public const float CombatVfxDurationSeconds = 0.65f;
        public const float EnemyDeathFadeDurationSeconds = 0.6f;
        private const float DefaultSoundVolumeScale = 3f;
        private const float UiSfxDistance = 10000f;

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
        [SerializeField] private RectTransform playerBattleStatusEffectsRoot;
        [SerializeField] private RectTransform enemyStatusEffectsRoot;
        [SerializeField] private GameObject statusTooltip;
        [SerializeField] private TMP_Text statusTooltipText;
        [SerializeField] private TMP_Text actionDescriptionText;

        [Header("Bottom panels")]
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
        [SerializeField] private GameObject categoryView;
        [SerializeField] private Button attackCategoryButton;
        [SerializeField] private Button defenseCategoryButton;
        [SerializeField] private Button categoryEndTurnButton;
        [SerializeField] private GameObject skillsView;
        [SerializeField] private TMP_Text skillsHeaderText;
        [SerializeField] private List<Button> skillTierButtons = new();
        [SerializeField] private List<TMP_Text> skillTierLabels = new();
        [SerializeField] private Button skillsBackButton;
        [SerializeField] private Button skillsEndTurnButton;

        [Header("Enemy turn panel")]
        [SerializeField] private TMP_Text enemyTurnText;

        [Header("Result overlay")]
        [SerializeField] private GameObject resultOverlay;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultDescriptionText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button reloadSceneButton;

        [Header("Reward overlay")]
        [SerializeField] private RewardManager rewardManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private GameObject rewardOverlay;
        [SerializeField] private TMP_Text rewardTitleText;
        [SerializeField] private TMP_Text rewardDescriptionText;
        [SerializeField] private TMP_Text rewardRestText;
        [SerializeField] private TMP_Text rewardEnhanceText;
        [SerializeField] private Button rewardRestButton;
        [SerializeField] private Button rewardEnhanceButton;

        [Header("Audio")]
        // Temporary prototype SFX hookup. Final audio ownership should replace
        // these inspector clips or remove this layer without touching combat core.
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private BoardTileEffectProfileSO boardTileEffectProfile;
        [SerializeField] private AudioClip playerHitClip;
        [SerializeField] private AudioClip enemyHitClip;
        [SerializeField] private AudioClip boardMoveClip;
        [SerializeField] private AudioClip boardMergeClip;
        [SerializeField] private float soundVolumeScale = DefaultSoundVolumeScale;

        [Header("Theme")]
        [SerializeField] private Color emptyCellColor = new(0.10f, 0.10f, 0.10f, 1f);
        [SerializeField] private Color filledCellColor = new(0.20f, 0.20f, 0.22f, 1f);
        [SerializeField] private Color highlightCellColor = new(0.92f, 0.90f, 0.85f, 1f);
        [SerializeField] private Color obstacleCellColor = new(0.55f, 0.10f, 0.55f, 1f);
        [SerializeField] private Color attackIntentColor = new(0.85f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color defenseIntentColor = new(0.12f, 0.32f, 0.90f, 1f);
        [SerializeField] private Color darknessIntentColor = new(0.20f, 0.07f, 0.34f, 1f);
        [SerializeField] private Color fearIntentColor = new(0.45f, 0.03f, 0.06f, 1f);
        [SerializeField] private Color playerHpFillColor = new(0.18f, 0.86f, 0.34f, 1f);
        [SerializeField] private Color enemyHpFillColor = new(0.88f, 0.14f, 0.14f, 1f);
        [SerializeField] private Color hpBarBackgroundColor = new(0.08f, 0.09f, 0.10f, 1f);
        [SerializeField] private Color blockFrameColor = new(0.66f, 0.70f, 0.74f, 1f);
        [SerializeField] private Color blockIconColor = new(0.42f, 0.46f, 0.50f, 0.95f);
        [SerializeField] private Color buffStatusColor = new(0.20f, 0.46f, 0.30f, 0.95f);
        [SerializeField] private Color debuffStatusColor = new(0.46f, 0.16f, 0.20f, 0.95f);
        [SerializeField] private Color statusTooltipColor = new(0.06f, 0.07f, 0.08f, 0.96f);

        private PrototypeCombatBootstrap bootstrap;
        private CombatManager combatManager;
        private CombatSnapshot snapshot;
        private readonly PrototypeCombatUiState uiState = new();
        private readonly PrototypeCombatAudioRouter audioRouter = new();
        private readonly List<SkillSnapshot> visibleSkills = new();
        private BoardTransition pendingBoardTransition;
        private Coroutine boardAnimationCoroutine;
        private Coroutine combatVfxCoroutine;
        private Coroutine enemyDeathFadeCoroutine;
        private bool boardTransitionAnimating;
        private bool lastEnemyWasDead;
        private int lastPlayedCombatVfxSequence;
        private RectTransform combatVfxPulseRect;
        private Vector3 combatVfxOriginalScale = Vector3.one;

        private void Awake()
        {
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
            scoreManager = owner != null ? owner.ScoreManager : scoreManager;

            ResolveMissingReferences();
            EnsureHpBarDefaults();
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
            audioRouter.Reset(snapshot);
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
            if (combatManager == null || snapshot == null || uiState.ScreenMode != PrototypeCombatScreenMode.Board)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
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

        private void WireButtons()
        {
            if (boardSwipeHandler != null)
            {
                boardSwipeHandler.OnSwipe -= HandleSwipe;
                boardSwipeHandler.OnSwipe += HandleSwipe;
            }

            BindButton(attackCategoryButton, () => uiState.SelectCategory(SkillType.Attack));
            BindButton(defenseCategoryButton, () => uiState.SelectCategory(SkillType.Defense));
            BindButton(categoryEndTurnButton, () => combatManager?.RequestEndPlayerTurn());
            BindButton(skillsBackButton, () => uiState.ClearCategory());
            BindButton(skillsEndTurnButton, () => combatManager?.RequestEndPlayerTurn());

            for (var i = 0; i < skillTierButtons.Count; i++)
            {
                var index = i;
                BindButton(skillTierButtons[i], () => OnSkillTierClicked(index));
            }

            BindButton(restartButton, () => bootstrap?.RestartCombat());
            BindButton(reloadSceneButton, () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
            BindButton(rewardRestButton, () => rewardManager?.ChooseRest(combatManager != null ? combatManager.Player : null));
            BindButton(rewardEnhanceButton, () => rewardManager?.ChooseEnhance(combatManager != null ? combatManager.Player : null));
        }

        private void BindButton(Button button, System.Action handler)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
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

        private void OnSkillTierClicked(int tierIndex)
        {
            if (combatManager == null || tierIndex < 0 || tierIndex >= visibleSkills.Count)
            {
                return;
            }

            var skill = visibleSkills[tierIndex];
            var targetIndex = skill.SkillType == SkillType.Attack ? 0 : -1;
            combatManager.RequestUseSkillById(skill.SkillId, targetIndex);
        }

        private void HandleCombatStateChanged(CombatSnapshot nextSnapshot)
        {
            var soundCues = audioRouter.GetSnapshotCues(nextSnapshot);
            var nextEnemyDead = nextSnapshot?.Enemies?.FirstOrDefault()?.IsDead ?? false;
            var enemyJustDied = !lastEnemyWasDead && nextEnemyDead;
            snapshot = nextSnapshot;
            uiState.Sync(snapshot);
            Render();
            PlaySoundCues(soundCues);
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
            var playedProfileAudio = PlayBoardTileEffectCues(audioRouter.GetBoardTileEffectCues(transition));
            PlayFallbackBoardSoundCues(audioRouter.GetBoardTransitionCues(transition), playedProfileAudio);

            pendingBoardTransition = transition;
        }

        private void BindRewardEvents()
        {
            if (rewardManager == null)
            {
                return;
            }

            rewardManager.OnRewardOffered -= HandleRewardOffered;
            rewardManager.OnRewardClaimed -= HandleRewardClaimed;
            rewardManager.OnRewardOffered += HandleRewardOffered;
            rewardManager.OnRewardClaimed += HandleRewardClaimed;
        }

        private void UnbindRewardEvents()
        {
            if (rewardManager == null)
            {
                return;
            }

            rewardManager.OnRewardOffered -= HandleRewardOffered;
            rewardManager.OnRewardClaimed -= HandleRewardClaimed;
        }

        private void HandleRewardOffered(BattleRewardSO _)
        {
            Render();
        }

        private void HandleRewardClaimed(RewardChoiceResult _)
        {
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

            RenderTopBar();
            RenderBattleScene();
            RenderPanelVisibility();

            switch (uiState.ScreenMode)
            {
                case PrototypeCombatScreenMode.Board:
                    RenderBoardPanel();
                    break;
                case PrototypeCombatScreenMode.ActionCategory:
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
            if (enemyNameText != null)
            {
                var enemyStatusFallback = enemyHpText == null && enemy != null
                    ? PrototypeCombatText.FormatEnemyHp(enemy.CurrentHp, enemy.MaxHp, enemy.Block)
                    : null;
                enemyNameText.enableAutoSizing = true;
                enemyNameText.fontSizeMin = 18f;
                enemyNameText.fontSizeMax = 32f;
                enemyNameText.textWrappingMode = TextWrappingModes.Normal;
                enemyNameText.text = PrototypeCombatText.FormatEnemyHeader(
                    enemy?.DisplayName,
                    enemy?.AiProfileLabel,
                    enemyStatusFallback);
            }

            if (intentBubble != null)
            {
                var hasIntent = enemy?.Intent != null && !enemy.IsDead;
                intentBubble.SetActive(hasIntent);
                if (hasIntent && intentBubbleText != null)
                {
                    intentBubbleText.text = PrototypeCombatText.FormatIntent(enemy.Intent);
                }

                if (hasIntent && intentBubble.TryGetComponent<Image>(out var intentBubbleImage))
                {
                    intentBubbleImage.color = GetIntentBubbleColor(enemy.Intent);
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

            if (enemyHpBarFill != null && enemy != null && enemy.MaxHp > 0)
            {
                SetHpBarValue(enemyHpBarFill, enemy.CurrentHp, enemy.MaxHp);
            }

            if (enemyHpText != null && enemy != null)
            {
                enemyHpText.text = PrototypeCombatText.FormatEnemyHp(enemy.CurrentHp, enemy.MaxHp, enemy.Block);
            }

            SetBlockIndicator(enemyHpBarFill, enemy?.Block ?? 0);
            RenderStatusEffects(enemyStatusEffectsRoot, enemy?.StatusEffects);

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
                    uiState.ScreenMode == PrototypeCombatScreenMode.ActionCategory ||
                    uiState.ScreenMode == PrototypeCombatScreenMode.ActionSkills));
            }

            if (enemyTurnPanel != null)
            {
                enemyTurnPanel.SetActive(!rewardReplacementVisible &&
                    !deferPanelSwapForBoardAnimation &&
                    uiState.ScreenMode == PrototypeCombatScreenMode.EnemyTurn);
            }

            if (categoryView != null)
            {
                categoryView.SetActive(uiState.ScreenMode == PrototypeCombatScreenMode.ActionCategory);
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

            var player = snapshot.Player;
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
                cell.SetValue(value, emptyCellColor, filledCellColor, highlightCellColor, obstacleCellColor);
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
                return obstacleCellColor;
            }

            if (value == 0)
            {
                return emptyCellColor;
            }

            return value >= 64 ? highlightCellColor : filledCellColor;
        }

        private static Color GetCellTextColor(int value)
        {
            return value >= 64 ? Color.black : Color.white;
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
            if (uiState.SelectedCategory.HasValue && snapshot != null)
            {
                visibleSkills.AddRange(uiState.GetVisibleSkills(snapshot));
            }

            if (skillsHeaderText != null)
            {
                skillsHeaderText.text = PrototypeCombatText.FormatSkillHeader(uiState.SelectedCategory);
            }

            for (var i = 0; i < skillTierButtons.Count; i++)
            {
                var hasSkill = i < visibleSkills.Count;
                var button = skillTierButtons[i];
                if (button != null)
                {
                    button.gameObject.SetActive(hasSkill);
                    button.interactable = hasSkill && snapshot != null && visibleSkills[i].Cost <= snapshot.CurrentCost;
                }

                if (i < skillTierLabels.Count && skillTierLabels[i] != null)
                {
                    if (hasSkill)
                    {
                        var skill = visibleSkills[i];
                        skillTierLabels[i].text = PrototypeCombatText.FormatSkillLabel(i, skill);
                    }
                    else
                    {
                        skillTierLabels[i].text = string.Empty;
                    }
                }
            }
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
                EnemyIntentType.Debuff when intent.debuffType == DebuffType.Darkness => darknessIntentColor,
                EnemyIntentType.Debuff when intent.debuffType == DebuffType.Fear => fearIntentColor,
                _ => attackIntentColor,
            };
        }

        private void ResolveMissingReferences()
        {
            turnCounterText ??= FindComponentInChildrenByName<TMP_Text>("TurnCounterText");
            intentHeaderText ??= FindComponentInChildrenByName<TMP_Text>("IntentHeaderText");
            playerPortrait ??= FindComponentInChildrenByName<Image>("PlayerPortrait");
            enemyPortrait ??= FindComponentInChildrenByName<Image>("EnemyPortrait");
            enemyNameText ??= FindComponentInChildrenByName<TMP_Text>("EnemyNameText");
            intentBubble ??= FindChildByName("IntentBubble")?.gameObject;
            intentBubbleText ??= FindComponentInChildrenByName<TMP_Text>("IntentBubbleText");
            playerBattleHpBarFill ??= FindNestedComponentByName<Image>("PlayerBattleHp", "Fill");
            playerBattleHpText ??= FindNestedComponentByName<TMP_Text>("PlayerBattleHp", "Text");
            enemyHpBarFill ??= FindNestedComponentByName<Image>("EnemyHp", "Fill");
            enemyHpText ??= FindNestedComponentByName<TMP_Text>("EnemyHp", "Text");
            playerBattleStatusEffectsRoot ??= FindComponentInChildrenByName<RectTransform>("PlayerBattleStatusEffects");
            enemyStatusEffectsRoot ??= FindComponentInChildrenByName<RectTransform>("EnemyStatusEffects");
            statusTooltip ??= FindChildByName("StatusTooltip")?.gameObject;
            statusTooltipText ??= FindNestedComponentByName<TMP_Text>("StatusTooltip", "Text");
            hpBarFill ??= FindComponentInChildrenByName<Image>("HpBarFill");
            hpText ??= FindComponentInChildrenByName<TMP_Text>("HpText");
            playerBoardStatusEffectsRoot ??= FindComponentInChildrenByName<RectTransform>("PlayerBoardStatusEffects");
            actionDescriptionText ??= FindComponentInChildrenByName<TMP_Text>("ActionDescriptionText");
            enemyTurnText ??= FindComponentInChildrenByName<TMP_Text>("EnemyTurnText");
        }

        private void EnsureHpBarDefaults()
        {
            ConfigureHpBarFill(playerBattleHpBarFill, playerHpFillColor, hpBarBackgroundColor);
            ConfigureHpBarFill(enemyHpBarFill, enemyHpFillColor, hpBarBackgroundColor);
            ConfigureHpBarFill(hpBarFill, playerHpFillColor, hpBarBackgroundColor);
            playerBoardStatusEffectsRoot = EnsureStatusEffectsRoot(hpBarFill, "PlayerBoardStatusEffects") ?? playerBoardStatusEffectsRoot;
            playerBattleStatusEffectsRoot = EnsureStatusEffectsRoot(playerBattleHpBarFill, "PlayerBattleStatusEffects") ?? playerBattleStatusEffectsRoot;
            enemyStatusEffectsRoot = EnsureStatusEffectsRoot(enemyHpBarFill, "EnemyStatusEffects") ?? enemyStatusEffectsRoot;
            EnsureStatusTooltip();
        }

        private static void ConfigureHpBarFill(Image fillImage, Color fillColor, Color backgroundColor)
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
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;

            var rectTransform = fillImage.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0f, 0.5f);

            if (fillImage.transform.parent != null &&
                fillImage.transform.parent.TryGetComponent<Image>(out var backgroundImage))
            {
                backgroundImage.color = backgroundColor;
                backgroundImage.raycastTarget = false;
            }
        }

        private static void SetHpBarValue(Image fillImage, int currentHp, int maxHp)
        {
            var ratio = maxHp > 0 ? Mathf.Clamp01(currentHp / (float)maxHp) : 0f;
            fillImage.fillAmount = ratio;

            var rectTransform = fillImage.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(ratio, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void SetBlockIndicator(Image fillImage, int block)
        {
            var hpRoot = ResolveHpRoot(fillImage);
            if (hpRoot == null)
            {
                return;
            }

            var outline = hpRoot.GetComponent<Outline>();
            if (outline == null)
            {
                outline = hpRoot.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = blockFrameColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
            outline.enabled = block > 0;

            var icon = EnsureBlockIcon(hpRoot);
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
                return existing;
            }

            var iconObject = new GameObject("BlockIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(hpRoot, false);
            var icon = iconObject.GetComponent<RectTransform>();
            icon.anchorMin = new Vector2(1f, 0.5f);
            icon.anchorMax = new Vector2(1f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(8f, 0f);
            icon.sizeDelta = new Vector2(34f, 24f);

            var image = iconObject.GetComponent<Image>();
            image.color = blockIconColor;
            image.raycastTarget = false;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(icon, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 16f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            if (actionDescriptionText != null && actionDescriptionText.font != null)
            {
                label.font = actionDescriptionText.font;
            }

            return icon;
        }

        private static RectTransform ResolveHpRoot(Image fillImage)
        {
            if (fillImage == null)
            {
                return null;
            }

            return fillImage.transform.parent as RectTransform ?? fillImage.rectTransform;
        }

        private RectTransform EnsureStatusEffectsRoot(Image fillImage, string rootName)
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

            var rootObject = new GameObject(rootName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rootObject.transform.SetParent(hpRoot, false);
            var root = rootObject.GetComponent<RectTransform>();
            ConfigureStatusEffectsRoot(root);

            ConfigureStatusEffectsLayout(rootObject.GetComponent<HorizontalLayoutGroup>());
            root.SetAsLastSibling();
            return root;
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
                layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
                preserveExistingLayout = false;
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
            root.anchoredPosition = isPlayerBattleStatusRoot ? new Vector2(0f, -39f) : new Vector2(0f, -6f);
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
                DestroyAnimationObject(root.GetChild(i).gameObject);
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

            var chipObject = new GameObject($"StatusEffect_{effect.Id}", typeof(RectTransform), typeof(Image), typeof(StatusEffectTooltipTarget));
            chipObject.transform.SetParent(root, false);
            var chipRect = chipObject.GetComponent<RectTransform>();
            chipRect.sizeDelta = new Vector2(32f, 32f);

            var image = chipObject.GetComponent<Image>();
            image.color = effect.IsBuff ? buffStatusColor : debuffStatusColor;
            image.raycastTarget = true;

            chipObject.GetComponent<StatusEffectTooltipTarget>()
                .Initialize(effect.Description, ShowStatusTooltip, HideStatusTooltip);
        }

        private void EnsureStatusTooltip()
        {
            if (statusTooltip == null)
            {
                statusTooltip = new GameObject("StatusTooltip", typeof(RectTransform), typeof(Image));
                statusTooltip.transform.SetParent(transform, false);
                var rect = statusTooltip.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 48f);
                rect.sizeDelta = new Vector2(320f, 56f);
            }

            if (!statusTooltip.TryGetComponent<Image>(out var image))
            {
                image = statusTooltip.AddComponent<Image>();
            }

            image.color = statusTooltipColor;
            image.raycastTarget = false;

            if (statusTooltipText == null)
            {
                var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(statusTooltip.transform, false);
                var textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10f, 6f);
                textRect.offsetMax = new Vector2(-10f, -6f);
                statusTooltipText = textObject.GetComponent<TextMeshProUGUI>();
            }

            statusTooltipText.alignment = TextAlignmentOptions.Center;
            statusTooltipText.fontSize = 15f;
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
            if (source != null && ownerRect != null && statusTooltip.transform is RectTransform tooltipRect)
            {
                var worldPosition = source.TransformPoint(source.rect.center);
                var localPosition = ownerRect.InverseTransformPoint(worldPosition);
                tooltipRect.anchoredPosition = localPosition + new Vector3(0f, 28f, 0f);
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

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            audioSource.mute = false;
            audioSource.loop = false;
            // Keep prototype UI sounds audible regardless of camera distance.
            audioSource.maxDistance = UiSfxDistance;
            audioSource.minDistance = UiSfxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            if (soundVolumeScale <= 0f)
            {
                soundVolumeScale = DefaultSoundVolumeScale;
            }

            // Inspector clips in the sample scene are the source of truth; Resources
            // paths only keep a generated/test scene from failing silently.
            playerHitClip ??= Resources.Load<AudioClip>("Audio/Prototype/PlayerHit");
            enemyHitClip ??= Resources.Load<AudioClip>("Audio/Prototype/EnemyHit");
            boardMoveClip ??= Resources.Load<AudioClip>("Audio/Prototype/BoardMove");
            boardMergeClip ??= Resources.Load<AudioClip>("Audio/Prototype/BoardMerge");
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

        private void PlaySoundCues(IReadOnlyList<PrototypeCombatSoundCue> cues)
        {
            if (cues == null || cues.Count == 0 || audioSource == null)
            {
                return;
            }

            foreach (var cue in cues)
            {
                PlaySoundCue(cue);
            }
        }

        private void PlaySoundCue(PrototypeCombatSoundCue cue)
        {
            var clip = cue switch
            {
                PrototypeCombatSoundCue.PlayerHit => playerHitClip,
                PrototypeCombatSoundCue.EnemyHit => enemyHitClip,
                PrototypeCombatSoundCue.BoardMove => boardMoveClip,
                PrototypeCombatSoundCue.BoardMerge => boardMergeClip,
                _ => null,
            };

            if (clip != null)
            {
                audioSource.PlayOneShot(clip, soundVolumeScale);
            }
        }

        private (bool Move, bool Merge) PlayBoardTileEffectCues(IReadOnlyList<BoardTileEffectCue> cues)
        {
            if (cues == null || cues.Count == 0 || boardTileEffectProfile == null)
            {
                return (false, false);
            }

            var playedMoveAudio = false;
            var playedMergeAudio = false;
            foreach (var cue in cues)
            {
                var effect = ResolveBoardTileEffect(cue);
                if (effect == null)
                {
                    continue;
                }

                var playedAudio = PlayEffectAudio(effect);
                if (playedAudio && cue.CueType == BoardTileEffectCueType.Move)
                {
                    playedMoveAudio = true;
                }
                else if (playedAudio && cue.CueType == BoardTileEffectCueType.Merge)
                {
                    playedMergeAudio = true;
                }

                SpawnBoardEffectPrefab(effect, cue.Position);
            }

            return (playedMoveAudio, playedMergeAudio);
        }

        private void PlayFallbackBoardSoundCues(
            IReadOnlyList<PrototypeCombatSoundCue> cues,
            (bool Move, bool Merge) playedProfileAudio)
        {
            if (cues == null || cues.Count == 0)
            {
                return;
            }

            foreach (var cue in cues)
            {
                if ((cue == PrototypeCombatSoundCue.BoardMove && playedProfileAudio.Move) ||
                    (cue == PrototypeCombatSoundCue.BoardMerge && playedProfileAudio.Merge))
                {
                    continue;
                }

                PlaySoundCue(cue);
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

        private bool PlayEffectAudio(CombatEffectBinding effect)
        {
            if (effect?.sfxClip == null || audioSource == null)
            {
                return false;
            }

            return CombatEffectAudioPlayer.PlayOneShot(audioSource, effect, soundVolumeScale, transform);
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
                resultDescriptionText.text = scoreManager != null
                    ? PrototypeCombatText.FormatResultDescription(snapshot, scoreManager.TotalScore)
                    : PrototypeCombatText.FormatResultDescription(snapshot);
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
            var reward = rewardManager != null ? rewardManager.PendingReward : null;
            if (rewardTitleText != null)
            {
                rewardTitleText.text = PrototypeCombatText.FormatRewardTitle(reward);
            }

            if (rewardDescriptionText != null)
            {
                rewardDescriptionText.text = PrototypeCombatText.FormatRewardDescription(reward);
            }

            if (rewardRestText != null)
            {
                rewardRestText.text = PrototypeCombatText.FormatRestReward(reward);
            }

            if (rewardEnhanceText != null)
            {
                rewardEnhanceText.text = PrototypeCombatText.FormatEnhanceReward(reward);
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

        private static Color GetDebuffVfxColor(DebuffType debuffType)
        {
            return debuffType switch
            {
                DebuffType.Fear => new Color(0.45f, 0.02f, 0.10f, 0.42f),
                DebuffType.Darkness => new Color(0.14f, 0.02f, 0.24f, 0.48f),
                _ => new Color(0f, 0f, 0f, 0.35f),
            };
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
