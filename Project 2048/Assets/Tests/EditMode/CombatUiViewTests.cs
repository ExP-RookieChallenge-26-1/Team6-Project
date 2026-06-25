using System.Collections;
using System.Linq;
using NUnit.Framework;
using Project2048.Audio;
using Project2048.Board2048;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Prototype;
using Project2048.Rewards;
using Project2048.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Project2048.Tests
{
    public class CombatUiViewTests
    {
        private const string HpBarSpritePath = "Assets/Art/UI/WideHexHpBar.png";
        private const string TopCombatBackgroundSpriteGuid = "ac20f033bdceb3149b44f3a942308b67";
        private const string BottomCombatBackgroundSpriteGuid = "cc30ddc0c7eae2049b751203e84d9c4b";
        private const string BottomCombatBackgroundLitSpriteGuid = "515518d798eb1764c965c9a1c6f4c472";
        private const string AttackIntentIconSpriteGuid = "68f332d9bf83b46f4568e77fd4bcdc88";
        private const string DefenseIntentIconSpriteGuid = "02048f9cb8476d1bc11e6380c2f185a9";
        private const string FearIntentIconSpriteGuid = "58190a852f3a9df835c30baaa3209362";
        private static readonly Vector2 ExpectedSkillLanternSlotSize = new(156f, 156f);
        private readonly System.Collections.Generic.List<Object> ownedObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var ownedObject in ownedObjects)
            {
                if (ownedObject != null)
                {
                    Object.DestroyImmediate(ownedObject);
                }
            }

            ownedObjects.Clear();
        }

        [Test]
        public void BoardTransitionDuration_KeepsTileMovementSnappy()
        {
            Assert.That(CombatUiView.BoardTransitionDurationSeconds, Is.EqualTo(0.14f).Within(0.001f));
        }

        [Test]
        public void BoardToActionDelay_WaitsAfterMovesAreSpentBeforeShowingSkillChoices()
        {
            Assert.That(CombatUiView.BoardToActionPanelDelaySeconds, Is.GreaterThanOrEqualTo(0.35f));
        }

        [Test]
        public void HpDamageTrailDuration_IsLongEnoughToRead()
        {
            Assert.That(CombatUiView.HpDamageTrailDurationSeconds, Is.GreaterThanOrEqualTo(0.5f));
        }

        [Test]
        public void BottomPanelMergeFlashDuration_StaysBrief()
        {
            Assert.That(CombatUiView.BottomPanelMergeFlashSeconds, Is.InRange(0.1f, 0.3f));
        }

        [Test]
        public void CombatVfxDuration_KeepsTemporaryDebuffFeedbackShort()
        {
            Assert.That(CombatUiView.CombatVfxDurationSeconds, Is.InRange(0.45f, 0.9f));
        }

        [Test]
        public void BoardCellView_ShowsEdgeOnlyForNumberAndKeepsBlackCellWhiteValue()
        {
            var emptyCellColor = new Color(26f / 255f, 26f / 255f, 26f / 255f, 1f);
            var cellObject = CreateOwnedGameObject("Cell");
            var background = cellObject.AddComponent<Image>();
            var edge = CreateOwnedGameObject("Edge");
            edge.transform.SetParent(cellObject.transform, false);
            var valueObject = CreateOwnedGameObject("Value");
            valueObject.transform.SetParent(cellObject.transform, false);
            var valueText = valueObject.AddComponent<TMPro.TextMeshProUGUI>();
            var cell = cellObject.AddComponent<BoardCellView>();
            SetPrivateField(cell, "background", background);
            SetPrivateField(cell, "valueText", valueText);

            cell.SetValue(0, Color.red, Color.green, Color.blue, Color.yellow);

            Assert.That(background.color, Is.EqualTo(emptyCellColor));
            Assert.That(valueText.text, Is.Empty);
            Assert.That(valueText.color, Is.EqualTo(Color.white));
            Assert.That(edge.activeSelf, Is.False);

            cell.SetValue(128, Color.red, Color.green, Color.blue, Color.yellow);

            Assert.That(background.color, Is.EqualTo(Color.black));
            Assert.That(valueText.text, Is.EqualTo("128"));
            Assert.That(valueText.color, Is.EqualTo(Color.white));
            Assert.That(edge.activeSelf, Is.True);

            cell.SetValue(Board2048Manager.ObstacleValue, Color.red, Color.green, Color.blue, Color.yellow);

            Assert.That(background.color, Is.EqualTo(Color.yellow));
            Assert.That(valueText.text, Is.Empty);
            Assert.That(valueText.color, Is.EqualTo(Color.white));
            Assert.That(edge.activeSelf, Is.False);
        }

        [Test]
        public void SettingButton_ClickOpensRuntimeSettingPopup()
        {
            var canvasObject = CreateOwnedGameObject("Canvas");
            canvasObject.AddComponent<Canvas>();
            var view = canvasObject.AddComponent<CombatUiView>();
            var settingImage = CreateImageChild(canvasObject.transform, "SettingButton");

            Assert.That(settingImage.GetComponent<Button>(), Is.Null);

            view.Initialize(null);
            var settingButton = settingImage.GetComponent<Button>();
            Assert.That(settingButton, Is.Not.Null);

            settingButton.onClick.Invoke();

            var popup = canvasObject.GetComponentInChildren<global::SettingPopup>(true);
            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.gameObject.activeSelf, Is.True);

            var volumeSliders = popup
                .GetComponentsInChildren<Slider>(true)
                .Where(slider => slider.gameObject.name.EndsWith("VolumeSlider"))
                .Select(slider => slider.gameObject.name)
                .ToArray();
            Assert.That(volumeSliders, Is.EquivalentTo(new[]
            {
                "MasterVolumeSlider",
                "BGMVolumeSlider",
                "SFXVolumeSlider",
            }));
        }

        [Test]
        public void PauseButton_ClickOpensRuntimeConfirmPopup()
        {
            var canvasObject = CreateOwnedGameObject("Canvas");
            canvasObject.AddComponent<Canvas>();
            var view = canvasObject.AddComponent<CombatUiView>();
            var pauseImage = CreateImageChild(canvasObject.transform, "pauseButton");

            Assert.That(pauseImage.GetComponent<Button>(), Is.Null);

            view.Initialize(null);
            var pauseButton = pauseImage.GetComponent<Button>();
            Assert.That(pauseButton, Is.Not.Null);

            pauseButton.onClick.Invoke();

            var popup = canvasObject.GetComponentInChildren<global::ConfirmPopup>(true);
            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.gameObject.activeSelf, Is.True);
            Assert.That(
                popup.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Select(text => text.text),
                Does.Contain("종료하시겠습니까?"));
        }

        [Test]
        public void BoardSwipeHandler_PointerSwipe_EmitsDirectionForMobileTouch()
        {
            var go = new GameObject("Swipe Handler");
            try
            {
                var handler = go.AddComponent<BoardSwipeHandler>();
                Direction? observed = null;
                handler.OnSwipe += direction => observed = direction;

                Assert.That(handler, Is.InstanceOf<IPointerDownHandler>());
                Assert.That(handler, Is.InstanceOf<IPointerUpHandler>());

                var pointerDown = (IPointerDownHandler)handler;
                var pointerUp = (IPointerUpHandler)handler;

                pointerDown.OnPointerDown(new PointerEventData(null)
                {
                    position = new Vector2(100f, 100f),
                });
                pointerUp.OnPointerUp(new PointerEventData(null)
                {
                    position = new Vector2(180f, 108f),
                });

                Assert.That(observed, Is.EqualTo(Direction.Right));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [UnityTest]
        public IEnumerator ActionPhase_UsesFourAuthoredSkillSlotsDirectly()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var boardPanel = CreateOwnedGameObject("BoardPanel");
            var actionPanel = CreateOwnedGameObject("ActionPanel");
            var skillsView = CreateOwnedGameObject("SkillsView");
            boardPanel.transform.SetParent(viewObject.transform, false);
            actionPanel.transform.SetParent(viewObject.transform, false);
            skillsView.transform.SetParent(actionPanel.transform, false);

            var costText = CreateTextChild(actionPanel.transform, "CostText");
            var skillsHeader = CreateTextChild(skillsView.transform, "SkillsHeader");
            var endTurnButton = CreateButtonChild(skillsView.transform, "EndTurnButton");
            var skillButtons = new System.Collections.Generic.List<Button>();
            var skillLabels = new System.Collections.Generic.List<TMPro.TMP_Text>();
            for (var index = 0; index < PlayerCombatController.MaxEquippedSkillSlots; index++)
            {
                var button = CreateButtonChild(skillsView.transform, $"SkillButton{index + 1}");
                var authoredSize = new Vector2(123f + index, 111f + index);
                ((RectTransform)button.transform).sizeDelta = authoredSize;
                var label = CreateTextChild(button.transform, "Label");
                skillButtons.Add(button);
                skillLabels.Add(label);
            }

            SetPrivateField(view, "boardPanel", boardPanel);
            SetPrivateField(view, "actionPanel", actionPanel);
            SetPrivateField(view, "skillsView", skillsView);
            SetPrivateField(view, "costText", costText);
            SetPrivateField(view, "skillsHeaderText", skillsHeader);
            SetPrivateField(view, "skillTierButtons", skillButtons);
            SetPrivateField(view, "skillTierLabels", skillLabels);
            SetPrivateField(view, "skillsEndTurnButton", endTurnButton);
            var tooltipRoot = CreateStatusTooltipForTest(viewObject.transform);
            SetPrivateField(view, "statusTooltip", tooltipRoot.gameObject);
            SetPrivateField(view, "statusTooltipText", tooltipRoot.GetComponentInChildren<TMPro.TMP_Text>(true));
            var bottomArtwork = CreateImageChild(viewObject.transform, "BottomPanelArtwork");
            var bottomDefaultSprite = CreateOwnedSprite("BottomPanelDefault");
            var bottomLitSprite = CreateOwnedSprite("BottomPanelLit");
            SetPrivateField(view, "bottomPanelBackground", bottomArtwork);
            SetPrivateField(view, "bottomPanelDefaultSprite", bottomDefaultSprite);
            SetPrivateField(view, "bottomPanelMergeLitSprite", bottomLitSprite);

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var attack = CreateSkill(
                "attack",
                "빛 발사",
                SkillType.Attack,
                cost: 0,
                power: 40,
                description: "적에게 위력 40 피해를 준다.");
            var defense = CreateSkill("guard", "가시 방어", SkillType.Defense, cost: 0, power: 5);
            var flash = CreateSkill("flash", "섬광", SkillType.Debuff, cost: 0, power: 2);
            var counter = CreateSkill("counter", "카운터", SkillType.Defense, cost: 0, power: 3);
            var playerData = CreatePlayerData(20, 0, attack, defense, flash, counter);
            var enemyData = CreateEnemyData("Enemy", 10, 0);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(skillsView.activeSelf, Is.True);
            Assert.That(endTurnButton.gameObject.activeSelf, Is.True);
            Assert.That(skillsHeader.text, Is.EqualTo("기술 선택"));
            Assert.That(costText.text, Does.StartWith("보유 코스트"));

            var renderedButtons = GetPrivateField(view, "skillTierButtons") as System.Collections.Generic.List<Button>;
            Assert.That(renderedButtons, Is.Not.Null);
            Assert.That(renderedButtons.Count, Is.EqualTo(PlayerCombatController.MaxEquippedSkillSlots));
            for (var index = 0; index < PlayerCombatController.MaxEquippedSkillSlots; index++)
            {
                Assert.That(renderedButtons[index].gameObject.activeSelf, Is.True);
                var authoredSize = new Vector2(123f + index, 111f + index);
                Assert.That(((RectTransform)renderedButtons[index].transform).sizeDelta, Is.EqualTo(authoredSize));
            }

            Assert.That(skillLabels[0].text, Does.Contain("빛 발사"));
            Assert.That(skillLabels[1].text, Does.Contain("가시 방어"));
            Assert.That(skillButtons[0].GetComponent<Image>().color, Is.EqualTo(CombatUiView.ThemeSkillAttackColor));
            Assert.That(skillButtons[1].GetComponent<Image>().color, Is.EqualTo(CombatUiView.ThemeSkillDefenseColor));
            Assert.That(skillButtons[2].GetComponent<Image>().color, Is.EqualTo(CombatUiView.ThemeSkillChangeColor));

            var tooltipTarget = skillButtons[0].GetComponent<StatusEffectTooltipTarget>();
            Assert.That(tooltipTarget, Is.InstanceOf<IPointerDownHandler>());
            Assert.That(tooltipTarget, Is.Not.InstanceOf<IPointerEnterHandler>());
            yield return ShowTooltipByLongPress(tooltipTarget);
            Assert.That(tooltipRoot.gameObject.activeSelf, Is.True);
            Assert.That(tooltipRoot.GetComponentInChildren<TMPro.TMP_Text>(true).text, Does.Contain("빛 발사"));
            Assert.That(tooltipRoot.GetComponentInChildren<TMPro.TMP_Text>(true).text, Does.Contain("위력 40"));
            Assert.That(tooltipRoot.GetComponentInChildren<TMPro.TMP_Text>(true).text, Does.Contain("명중 100"));
            Assert.That(tooltipRoot.GetComponentInChildren<TMPro.TMP_Text>(true).text, Does.Contain("코스트 0"));
            Assert.That(tooltipRoot.GetComponentInChildren<TMPro.TMP_Text>(true).text, Does.Contain("적에게 위력 40 피해"));
            Assert.That(tooltipRoot.GetComponentInChildren<TMPro.TMP_Text>(true).alignment, Is.EqualTo(TMPro.TextAlignmentOptions.Left));
            ReleaseLongPressTooltip(tooltipTarget);

            var hpBeforeClick = enemy.CurrentHp;
            skillButtons[0].onClick.Invoke();
            Assert.That(enemy.CurrentHp, Is.LessThan(hpBeforeClick));
            Assert.That(bottomArtwork.sprite, Is.SameAs(bottomLitSprite));
            yield return new WaitForSeconds(CombatUiView.BottomPanelMergeFlashSeconds + 0.05f);
            Assert.That(bottomArtwork.sprite, Is.SameAs(bottomDefaultSprite));
        }

        [UnityTest]
        public IEnumerator RewardOverlay_BindsSkillChoiceTooltip()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var rewardOverlay = CreateOwnedGameObject("RewardOverlay");
            rewardOverlay.transform.SetParent(viewObject.transform, false);
            var resultOverlay = CreateOwnedGameObject("ResultOverlay");
            resultOverlay.transform.SetParent(viewObject.transform, false);
            var boardPanel = CreateOwnedGameObject("BoardPanel");
            boardPanel.transform.SetParent(viewObject.transform, false);
            var actionPanel = CreateOwnedGameObject("ActionPanel");
            actionPanel.transform.SetParent(viewObject.transform, false);
            var rewardTitle = CreateTextChild(rewardOverlay.transform, "RewardTitle");
            var rewardDescription = CreateTextChild(rewardOverlay.transform, "RewardDescription");
            var firstText = CreateTextChild(rewardOverlay.transform, "FirstText");
            var secondText = CreateTextChild(rewardOverlay.transform, "SecondText");
            var thirdText = CreateTextChild(rewardOverlay.transform, "ThirdText");
            var firstButton = CreateButtonChild(rewardOverlay.transform, "FirstButton");
            var secondButton = CreateButtonChild(rewardOverlay.transform, "SecondButton");
            var thirdButton = CreateButtonChild(rewardOverlay.transform, "ThirdButton");
            var tooltipRoot = CreateStatusTooltipForTest(viewObject.transform);

            SetPrivateField(view, "rewardOverlay", rewardOverlay);
            SetPrivateField(view, "resultOverlay", resultOverlay);
            SetPrivateField(view, "boardPanel", boardPanel);
            SetPrivateField(view, "actionPanel", actionPanel);
            SetPrivateField(view, "rewardTitleText", rewardTitle);
            SetPrivateField(view, "rewardDescriptionText", rewardDescription);
            SetPrivateField(view, "rewardChoiceButtons", new System.Collections.Generic.List<Button> { firstButton, secondButton, thirdButton });
            SetPrivateField(view, "rewardChoiceLabels", new System.Collections.Generic.List<TMPro.TMP_Text> { firstText, secondText, thirdText });
            SetPrivateField(view, "statusTooltip", tooltipRoot.gameObject);
            SetPrivateField(view, "statusTooltipText", tooltipRoot.GetComponentInChildren<TMPro.TMP_Text>(true));

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var rewardManager = CreateOwnedGameObject("RewardManager").AddComponent<RewardManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var finisher = CreateSkill("finisher", "강타", SkillType.Attack, cost: 0, power: 120);
            var learnedSkill = CreateSkill(
                "shield-bash",
                "방패 밀치기",
                SkillType.Attack,
                cost: 5,
                power: 60,
                description: "현재 보호막 수치로 계산하여 적에게 위력 60 피해를 준다.");
            var skillReward = ScriptableObject.CreateInstance<BattleRewardSO>();
            skillReward.rewardKind = RewardChoiceKind.LearnSkill;
            skillReward.skillToLearn = learnedSkill;
            ownedObjects.Add(skillReward);
            var rewardTable = ScriptableObject.CreateInstance<RewardTableSO>();
            rewardTable.rewards = new System.Collections.Generic.List<BattleRewardSO> { skillReward };
            ownedObjects.Add(rewardTable);

            rewardManager.Initialize(new RunProgress(), rewardTable);
            SetPrivateField(bootstrap, "combatManager", manager);
            SetPrivateField(bootstrap, "rewardManager", rewardManager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = CreatePlayerData(20, 999, finisher),
                enemyDataList = new System.Collections.Generic.List<EnemySO> { CreateEnemyData("Enemy", 1, 0) },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            manager.RequestUseSkill(finisher, enemy);
            rewardManager.OfferReward(new CombatResult(), player);

            var tooltipTarget = firstButton.GetComponent<StatusEffectTooltipTarget>();
            Assert.That(firstText.text, Does.Contain("방패 밀치기"));
            Assert.That(tooltipTarget, Is.InstanceOf<IPointerDownHandler>());
            Assert.That(tooltipTarget, Is.Not.InstanceOf<IPointerEnterHandler>());

            yield return ShowTooltipByLongPress(tooltipTarget);

            var tooltipText = tooltipRoot.GetComponentInChildren<TMPro.TMP_Text>(true).text;
            Assert.That(tooltipRoot.gameObject.activeSelf, Is.True);
            Assert.That(tooltipText, Does.Contain("방패 밀치기"));
            Assert.That(tooltipText, Does.Contain("위력 60"));
            Assert.That(tooltipText, Does.Contain("현재 보호막 수치"));
        }

        [Test]
        public void ActionPhase_DisablesUnaffordableAuthoredSkillButtons()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var boardPanel = CreateOwnedGameObject("BoardPanel");
            var actionPanel = CreateOwnedGameObject("ActionPanel");
            var skillsView = CreateOwnedGameObject("SkillsView");
            boardPanel.transform.SetParent(viewObject.transform, false);
            actionPanel.transform.SetParent(viewObject.transform, false);
            skillsView.transform.SetParent(actionPanel.transform, false);

            var costText = CreateTextChild(actionPanel.transform, "CostText");
            var skillsHeader = CreateTextChild(skillsView.transform, "SkillsHeader");
            var skillButtons = new System.Collections.Generic.List<Button>();
            var skillLabels = new System.Collections.Generic.List<TMPro.TMP_Text>();
            for (var index = 0; index < PlayerCombatController.MaxEquippedSkillSlots; index++)
            {
                var button = CreateButtonChild(skillsView.transform, $"SkillButton{index + 1}");
                var label = CreateTextChild(button.transform, "Label");
                skillButtons.Add(button);
                skillLabels.Add(label);
            }

            SetPrivateField(view, "boardPanel", boardPanel);
            SetPrivateField(view, "actionPanel", actionPanel);
            SetPrivateField(view, "skillsView", skillsView);
            SetPrivateField(view, "costText", costText);
            SetPrivateField(view, "skillsHeaderText", skillsHeader);
            SetPrivateField(view, "skillTierButtons", skillButtons);
            SetPrivateField(view, "skillTierLabels", skillLabels);

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var expensive = CreateSkill("expensive", "큰 기술", SkillType.Attack, cost: 99, power: 4);
            var playerData = CreatePlayerData(20, 0, expensive);
            var enemyData = CreateEnemyData("Enemy", 10, 0);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.BoardManager.SetBoardState(
                new[,]
                {
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                0);
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(skillButtons[0].interactable, Is.False);
            Assert.That(skillLabels[0].text, Does.Contain("부족"));
        }

        [Test]
        public void ActionPhase_DisablesChargeSkillButtonWhileChargeIsPending()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var boardPanel = CreateOwnedGameObject("BoardPanel");
            var actionPanel = CreateOwnedGameObject("ActionPanel");
            var skillsView = CreateOwnedGameObject("SkillsView");
            boardPanel.transform.SetParent(viewObject.transform, false);
            actionPanel.transform.SetParent(viewObject.transform, false);
            skillsView.transform.SetParent(actionPanel.transform, false);

            var costText = CreateTextChild(actionPanel.transform, "CostText");
            var skillsHeader = CreateTextChild(skillsView.transform, "SkillsHeader");
            var skillButtons = new System.Collections.Generic.List<Button>();
            var skillLabels = new System.Collections.Generic.List<TMPro.TMP_Text>();
            for (var index = 0; index < PlayerCombatController.MaxEquippedSkillSlots; index++)
            {
                var button = CreateButtonChild(skillsView.transform, $"SkillButton{index + 1}");
                var label = CreateTextChild(button.transform, "Label");
                skillButtons.Add(button);
                skillLabels.Add(label);
            }

            SetPrivateField(view, "boardPanel", boardPanel);
            SetPrivateField(view, "actionPanel", actionPanel);
            SetPrivateField(view, "skillsView", skillsView);
            SetPrivateField(view, "costText", costText);
            SetPrivateField(view, "skillsHeaderText", skillsHeader);
            SetPrivateField(view, "skillTierButtons", skillButtons);
            SetPrivateField(view, "skillTierLabels", skillLabels);

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var charge = CreateSkill("gather-light", "Gather Light", SkillType.Attack, cost: 0, power: 0);
            charge.effectKind = SkillEffectKind.ChargeAttack;
            charge.chargedPower = 40;
            var playerData = CreatePlayerData(20, 2, charge);
            var enemyData = CreateEnemyData("Enemy", 100, 0);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(skillButtons[0].interactable, Is.True);

            Assert.That(manager.RequestUseSkillById("gather-light"), Is.True);

            Assert.That(skillButtons[0].interactable, Is.False);
            Assert.That(manager.GetSnapshot().Skills[0].CanExecute, Is.False);
        }

        [UnityTest]
        public IEnumerator CostFormulaHelpIcon_ShowsBoardCostFormulaOnLongPress()
        {
            var viewObject = CreateOwnedRectTransformObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var helpIcon = CreateImageChild(viewObject.transform, "CostFormulaHelpIcon");
            var helpLabel = CreateTextChild(helpIcon.transform, "Label");
            var boardHelpIcon = CreateImageChild(viewObject.transform, "BoardCostFormulaHelpIcon");
            var boardHelpLabel = CreateTextChild(boardHelpIcon.transform, "Label");
            helpIcon.gameObject.AddComponent<StatusEffectTooltipTarget>();
            boardHelpIcon.gameObject.AddComponent<StatusEffectTooltipTarget>();
            var tooltipRoot = CreateStatusTooltipForTest(viewObject.transform);
            SetPrivateField(view, "costFormulaHelpIcon", helpIcon.gameObject);
            SetPrivateField(view, "costFormulaHelpLabel", helpLabel);
            SetPrivateField(view, "boardCostFormulaHelpIcon", boardHelpIcon.gameObject);
            SetPrivateField(view, "boardCostFormulaHelpLabel", boardHelpLabel);
            SetPrivateField(view, "statusTooltip", tooltipRoot.gameObject);
            SetPrivateField(view, "statusTooltipText", tooltipRoot.GetComponentInChildren<TMPro.TMP_Text>(true));

            InvokePrivate(view, "EnsureStatusTooltip");
            InvokePrivate(view, "ConfigureCostFormulaHelp");

            var target = helpIcon.GetComponent<StatusEffectTooltipTarget>();
            Assert.That(target, Is.Not.Null);
            Assert.That(boardHelpIcon.GetComponent<StatusEffectTooltipTarget>(), Is.Not.Null);
            Assert.That(boardHelpLabel.text, Is.EqualTo("?"));

            yield return ShowTooltipByLongPress(target);

            var tooltip = GetPrivateField(view, "statusTooltip") as GameObject;
            var tooltipText = GetPrivateField(view, "statusTooltipText") as TMPro.TMP_Text;
            Assert.That(helpLabel.text, Is.EqualTo("?"));
            Assert.That(tooltip.activeSelf, Is.True);
            Assert.That(tooltipText.text, Does.Contain("log2(전체 타일 합)"));
            Assert.That(tooltipText.text, Does.Contain("2의 거듭제곱"));

            ReleaseLongPressTooltip(target);
            Assert.That(tooltip.activeSelf, Is.False);
        }

        [Test]
        public void Initialize_WithMissingSerializedBattleReferences_WiresEnemyHpAndIntentByName()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var intentBubbleImage = CreateImageChild(viewObject.transform, "IntentBubble");
            var authoredIntentBubbleSize = new Vector2(92f, 92f);
            intentBubbleImage.rectTransform.sizeDelta = authoredIntentBubbleSize;
            var intentText = CreateTextChild(viewObject.transform.Find("IntentBubble"), "IntentBubbleText");
            var playerBattleHp = CreateImageChild(viewObject.transform, "PlayerBattleHp");
            var playerBattleHpFill = CreateImageChild(playerBattleHp.transform, "Fill");
            var playerBattleHpText = CreateTextChild(playerBattleHp.transform, "Text");
            var enemyHp = CreateImageChild(viewObject.transform, "EnemyHp");
            var enemyHpFill = CreateImageChild(enemyHp.transform, "Fill");
            var enemyHpText = CreateTextChild(enemyHp.transform, "Text");
            var boardHpFill = CreateImageChild(viewObject.transform, "HpBarFill");
            var boardHpText = CreateTextChild(viewObject.transform, "HpText");
            var actionText = CreateTextChild(viewObject.transform, "ActionDescriptionText");

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var attack = CreateSkill("attack", "검격", SkillType.Attack, cost: 0, power: 5);
            var playerData = CreatePlayerData(20, 0, attack);
            var enemyData = CreateEnemyData("슬라임", 10, 4);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            view.Initialize(bootstrap);
            Assert.That(intentBubbleImage.rectTransform.sizeDelta.x, Is.EqualTo(authoredIntentBubbleSize.x).Within(0.001f));
            Assert.That(intentBubbleImage.rectTransform.sizeDelta.y, Is.EqualTo(authoredIntentBubbleSize.y).Within(0.001f));
            Assert.That(intentText.enableAutoSizing, Is.True);
            Assert.That(intentText.alignment, Is.EqualTo(TMPro.TextAlignmentOptions.Center));
            Assert.That(intentText.text, Is.EqualTo("공격"));
            Assert.That(intentBubbleImage.color, Is.EqualTo(new Color(0.85f, 0.12f, 0.12f, 1f)));
            AssertHpFillIsRenderable(playerBattleHpFill);
            AssertHpFillIsRenderable(enemyHpFill);
            AssertHpFillIsRenderable(boardHpFill);

            enemy.SetIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Defense,
                value = 3,
            });

            Assert.That(intentText.text, Is.EqualTo("방어"));
            Assert.That(intentBubbleImage.color, Is.EqualTo(new Color(0.12f, 0.32f, 0.90f, 1f)));

            enemy.SetIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Debuff,
                debuffType = DebuffType.Darkness,
                value = 2,
            });

            Assert.That(intentText.text, Is.EqualTo("변화"));
            Assert.That(intentBubbleImage.color, Is.EqualTo(new Color(0.20f, 0.07f, 0.34f, 1f)));

            enemy.SetIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Debuff,
                debuffType = DebuffType.Fear,
                value = 2,
            });

            Assert.That(intentText.text, Is.EqualTo("변화"));
            Assert.That(intentBubbleImage.color, Is.EqualTo(new Color(0.45f, 0.03f, 0.06f, 1f)));

            manager.RequestUseSkillById("attack", 0);

            Assert.That(enemyHpFill.fillAmount, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(enemyHpText.text, Is.EqualTo("5/10"));
            Assert.That(actionText.text, Is.EqualTo("최근 행동: 플레이어: 검격"));

            enemy.SetIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Attack,
                value = 4,
            });
            manager.RequestEndPlayerTurn();

            Assert.That(playerBattleHpFill.fillAmount, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(boardHpFill.fillAmount, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(playerBattleHpFill.rectTransform.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(boardHpFill.rectTransform.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(playerBattleHpText.text, Is.EqualTo("16/20"));
            Assert.That(boardHpText.text, Is.EqualTo("16/20"));
        }

        [Test]
        public void EnemyDeath_HidesEnemyHpRoot()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var enemyHp = CreateImageChild(viewObject.transform, "EnemyHp");
            CreateImageChild(enemyHp.transform, "Fill");
            CreateTextChild(enemyHp.transform, "Text");
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var attack = CreateSkill("attack", "검격", SkillType.Attack, cost: 0, power: 99);
            var playerData = CreatePlayerData(20, 0, attack);
            var enemyData = CreateEnemyData("슬라임", 10, 0);

            SetPrivateField(bootstrap, "combatManager", manager);
            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(enemyHp.gameObject.activeSelf, Is.True);

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);

            Assert.That(enemy.IsDead, Is.True);
            Assert.That(enemyHp.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void HpBarRender_ReappliesFixedThemeFillColor()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var playerBattleHp = CreateImageChild(viewObject.transform, "PlayerBattleHp");
            var fill = CreateImageChild(playerBattleHp.transform, "Fill");
            var trail = playerBattleHp.transform.Find("DamageTrailFill")?.GetComponent<Image>();
            fill.color = Color.red;

            InvokePrivate(view, "SetHpBarValue", fill, 7, 10);

            Assert.That(fill.color, Is.EqualTo(CombatUiView.ThemeHpFillColor));
            Assert.That(trail, Is.Not.Null);
            Assert.That(trail.color, Is.EqualTo(CombatUiView.ThemeHpDamageTrailColor));
            Assert.That(trail.transform.GetSiblingIndex(), Is.LessThan(fill.transform.GetSiblingIndex()));
        }

        [Test]
        public void HpBarTheme_UsesOriginalTealPalette()
        {
            Assert.That(CombatUiView.ThemePrimaryColor, Is.EqualTo(new Color(73f / 255f, 175f / 255f, 181f / 255f, 1f)));
            Assert.That(CombatUiView.ThemeHpFillColor, Is.EqualTo(CombatUiView.ThemePrimaryColor));
            Assert.That(CombatUiView.ThemeHpFillColor.g, Is.GreaterThan(CombatUiView.ThemeHpFillColor.r));
            Assert.That(CombatUiView.ThemeHpFillColor.b, Is.GreaterThan(CombatUiView.ThemeHpFillColor.r));
            Assert.That(CombatUiView.ThemeHpBarBackgroundColor.maxColorComponent, Is.LessThan(0.06f));
            Assert.That(CombatUiView.ThemeHpDamageTrailColor.a, Is.GreaterThanOrEqualTo(0.9f));
        }

        [Test]
        public void PlayerDamage_UpdatesBoardHpWhileActionScreenIsVisible()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var playerBattleHp = CreateImageChild(viewObject.transform, "PlayerBattleHp");
            var playerBattleHpFill = CreateImageChild(playerBattleHp.transform, "Fill");
            CreateTextChild(playerBattleHp.transform, "Text");
            var enemyHp = CreateImageChild(viewObject.transform, "EnemyHp");
            CreateImageChild(enemyHp.transform, "Fill");
            CreateTextChild(enemyHp.transform, "Text");
            var boardHpRoot = CreateOwnedRectTransformObject("HpBarBg");
            boardHpRoot.transform.SetParent(viewObject.transform, false);
            var boardHpFill = CreateImageChild(boardHpRoot.transform, "HpBarFill");
            var boardHpText = CreateTextChild(viewObject.transform, "HpText");

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var playerData = CreatePlayerData(20, 0);
            var enemyData = CreateEnemyData("Enemy", 10, 0);
            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            view.Initialize(bootstrap);
            manager.ResolveBoardPhase();
            var uiState = GetPrivateField(view, "uiState") as PrototypeCombatUiState;

            player.TakeDamage(4);

            Assert.That(uiState?.ScreenMode, Is.EqualTo(PrototypeCombatScreenMode.ActionSkills));
            Assert.That(playerBattleHpFill.fillAmount, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(boardHpFill.fillAmount, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(boardHpText.text, Is.EqualTo("16/20"));
        }

        [Test]
        public void HpBarDamageTrail_HoldsPreviousHpRatioWhenDamageLands()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var playerBattleHp = CreateImageChild(viewObject.transform, "PlayerBattleHp");
            var playerBattleHpFill = CreateImageChild(playerBattleHp.transform, "Fill");
            CreateTextChild(playerBattleHp.transform, "Text");
            var enemyHp = CreateImageChild(viewObject.transform, "EnemyHp");
            CreateImageChild(enemyHp.transform, "Fill");
            CreateTextChild(enemyHp.transform, "Text");
            var boardHpFill = CreateImageChild(viewObject.transform, "HpBarFill");
            CreateTextChild(viewObject.transform, "HpText");

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var playerData = CreatePlayerData(20, 0);
            var enemyData = CreateEnemyData("슬라임", 10, 0);
            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            view.Initialize(bootstrap);
            player.TakeDamage(4);

            var battleTrail = playerBattleHp.transform.Find("DamageTrailFill")?.GetComponent<Image>();
            var boardTrail = boardHpFill.transform.Find("DamageTrailFill")?.GetComponent<Image>();
            var enemyTrail = enemyHp.transform.Find("DamageTrailFill")?.GetComponent<Image>();
            Assert.That(battleTrail, Is.Not.Null);
            Assert.That(boardTrail, Is.Not.Null);
            Assert.That(enemyTrail, Is.Not.Null);
            Assert.That(playerBattleHpFill.fillAmount, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(battleTrail.fillAmount, Is.EqualTo(1f).Within(0.001f));
            Assert.That(boardHpFill.fillAmount, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(boardTrail.fillAmount, Is.EqualTo(1f).Within(0.001f));
            Assert.That(playerBattleHpFill.rectTransform.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(boardHpFill.rectTransform.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(battleTrail.transform.GetSiblingIndex(), Is.LessThan(playerBattleHpFill.transform.GetSiblingIndex()));
            Assert.That(battleTrail.color, Is.EqualTo(CombatUiView.ThemeHpDamageTrailColor));
            Assert.That(boardTrail.color, Is.EqualTo(CombatUiView.ThemeHpDamageTrailColor));
            Assert.That(enemyTrail.color, Is.EqualTo(CombatUiView.ThemeHpDamageTrailColor));
        }

        [Test]
        public void HpBarDamageFeedback_FlashesDamagedSegmentAndUsesShortShake()
        {
            Assert.That(CombatUiView.HpHitShakeDurationSeconds, Is.InRange(0.10f, 0.13f));
            var shakeMagnitudeField = typeof(CombatUiView).GetField(
                "HpHitShakeMagnitude",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.That(shakeMagnitudeField, Is.Not.Null);
            Assert.That((float)shakeMagnitudeField.GetRawConstantValue(), Is.GreaterThanOrEqualTo(10f));

            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var playerBattleHp = CreateImageChild(viewObject.transform, "PlayerBattleHp");
            var playerBattleHpFill = CreateImageChild(playerBattleHp.transform, "Fill");
            CreateTextChild(playerBattleHp.transform, "Text");
            var enemyHp = CreateImageChild(viewObject.transform, "EnemyHp");
            CreateImageChild(enemyHp.transform, "Fill");
            CreateTextChild(enemyHp.transform, "Text");
            var boardHpFill = CreateImageChild(viewObject.transform, "HpBarFill");
            CreateTextChild(viewObject.transform, "HpText");

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var playerData = CreatePlayerData(20, 0);
            var enemyData = CreateEnemyData("Slime", 10, 0);
            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            view.Initialize(bootstrap);
            player.TakeDamage(4);

            var battleFlash = playerBattleHp.transform.Find("DamageFlashFill")?.GetComponent<Image>();
            var boardFlash = boardHpFill.transform.Find("DamageFlashFill")?.GetComponent<Image>();
            Assert.That(battleFlash, Is.Not.Null);
            Assert.That(boardFlash, Is.Not.Null);
            Assert.That(battleFlash.rectTransform.anchorMin.x, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(battleFlash.rectTransform.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(boardFlash.rectTransform.anchorMin.x, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(boardFlash.rectTransform.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(battleFlash.color.r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(battleFlash.color.g, Is.EqualTo(1f).Within(0.001f));
            Assert.That(battleFlash.color.b, Is.EqualTo(1f).Within(0.001f));
            Assert.That(boardFlash.color.r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(boardFlash.color.g, Is.EqualTo(1f).Within(0.001f));
            Assert.That(boardFlash.color.b, Is.EqualTo(1f).Within(0.001f));
            Assert.That(battleFlash.color.a, Is.GreaterThan(0.5f));
            Assert.That(boardFlash.color.a, Is.GreaterThan(0.5f));
        }

        [UnityTest]
        public IEnumerator EnemyAppear_KeepsUiRootStill()
        {
            var viewObject = CreateOwnedRectTransformObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var playerData = CreatePlayerData(20, 0);
            var enemyData = CreateEnemyData("슬라임", 10, 0);
            manager.SetCombatants(player, new[] { enemy });

            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            if (!Application.isPlaying)
            {
                yield break;
            }

            yield return null;

            Assert.That(viewObject.transform.localPosition, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void CellColor_UsesObstacleColorForObstacleValue()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();

            var obstacleColor = (Color)InvokePrivate(view, "GetCellColor", Board2048Manager.ObstacleValue);

            Assert.That(obstacleColor, Is.EqualTo(new Color(0.20f, 0.07f, 0.34f, 1f)));
        }

        [Test]
        public void Initialize_RendersThreeEnemyIntentsWhenEnemyCanActThreeTimes()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            CreateImageChild(viewObject.transform, "IntentBubble");
            var intentText = CreateTextChild(viewObject.transform.Find("IntentBubble"), "IntentBubbleText");

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var playerData = CreatePlayerData(20, 0);
            var enemyData = CreateEnemyData("Multi", 10, 4);
            enemyData.intentPattern = new System.Collections.Generic.List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Attack,
                    value = 4,
                },
                new()
                {
                    intentType = EnemyIntentType.Defense,
                    value = 3,
                },
                new()
                {
                    intentType = EnemyIntentType.Debuff,
                    debuffType = DebuffType.Fear,
                    value = 1,
                },
            };
            enemyData.aiComplexity = EnemyAiComplexity.Complex;
            SetEnemyActionsPerTurn(enemyData, 3);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            view.Initialize(bootstrap);

            Assert.That(intentText.text, Is.EqualTo(
                $"{PrototypeCombatText.FormatIntent(enemyData.intentPattern[0])}\n{PrototypeCombatText.FormatIntent(enemyData.intentPattern[1])}\n{PrototypeCombatText.FormatIntent(enemyData.intentPattern[2])}"));
        }

        [UnityTest]
        public IEnumerator Initialize_BuildsBlockAndStatusEffectUiAroundHpBars()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var playerBattleHp = CreateImageChild(viewObject.transform, "PlayerBattleHp");
            CreateImageChild(playerBattleHp.transform, "Fill");
            CreateTextChild(playerBattleHp.transform, "Text");
            var enemyHp = CreateImageChild(viewObject.transform, "EnemyHp");
            CreateImageChild(enemyHp.transform, "Fill");
            CreateTextChild(enemyHp.transform, "Text");
            var boardHp = CreateImageChild(viewObject.transform, "BoardHp");
            CreateImageChild(boardHp.transform, "HpBarFill");
            var boardHpRoot = boardHp.transform;
            CreateAuthoredStatusEffectsRootForTest(playerBattleHp.transform, "PlayerBattleStatusEffects", new Vector2(CombatUiView.HpStatusEffectXOffset, -39f));
            CreateAuthoredStatusEffectsRootForTest(boardHpRoot, "PlayerBoardStatusEffects", new Vector2(CombatUiView.HpStatusEffectXOffset, -6f));
            CreateAuthoredStatusEffectsRootForTest(enemyHp.transform, "EnemyStatusEffects", new Vector2(CombatUiView.HpStatusEffectXOffset, -6f));
            CreateStatusTooltipForTest(viewObject.transform);

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var playerData = CreatePlayerData(20, 0);
            var enemyData = CreateEnemyData("Slime", 10, 0);
            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            player.AddBlock(3);
            player.ApplyFear(2);
            enemy.AddBlock(4);
            enemy.ApplyAttackModifier(2);

            view.Initialize(bootstrap);

            var playerOutline = playerBattleHp.GetComponent<Outline>();
            Assert.That(playerOutline == null || !playerOutline.enabled, Is.True);
            Assert.That(playerBattleHp.transform.Find("BlockIcon/Text").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("3"));
            Assert.That(boardHpRoot.GetComponent<Outline>() == null || !boardHpRoot.GetComponent<Outline>().enabled, Is.True);
            Assert.That(boardHpRoot.Find("BlockIcon/Text").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("3"));

            var enemyOutline = enemyHp.GetComponent<Outline>();
            Assert.That(enemyOutline == null || !enemyOutline.enabled, Is.True);
            Assert.That(enemyHp.transform.Find("BlockIcon/Text").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("4"));
            Assert.That(playerBattleHp.transform.Find("BlockIcon").GetComponent<RectTransform>().anchoredPosition.x, Is.GreaterThanOrEqualTo(12f));
            Assert.That(boardHpRoot.Find("BlockIcon").GetComponent<RectTransform>().anchoredPosition.x, Is.GreaterThanOrEqualTo(12f));
            Assert.That(enemyHp.transform.Find("BlockIcon").GetComponent<RectTransform>().anchoredPosition.x, Is.GreaterThanOrEqualTo(12f));

            Assert.That(viewObject.transform.Find("FloatingStatusLayer"), Is.Null);

            var fearRoot = playerBattleHp.transform.Find("PlayerBattleStatusEffects");
            var boardFearRoot = boardHpRoot.Find("PlayerBoardStatusEffects");
            var attackRoot = enemyHp.transform.Find("EnemyStatusEffects");
            Assert.That(fearRoot, Is.Not.Null);
            Assert.That(boardFearRoot, Is.Not.Null);
            Assert.That(attackRoot, Is.Not.Null);

            Assert.That(fearRoot.GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(-39f).Within(0.001f));
            Assert.That(boardFearRoot.GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(-6f).Within(0.001f));
            Assert.That(attackRoot.GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(-6f).Within(0.001f));

            var fearChip = fearRoot.Find("StatusEffect_fear");
            var boardFearChip = boardFearRoot.Find("StatusEffect_fear");
            var attackChip = attackRoot.Find("StatusEffect_attack-up");
            Assert.That(fearChip, Is.Not.Null);
            Assert.That(boardFearChip, Is.Not.Null);
            Assert.That(attackChip, Is.Not.Null);
            Assert.That(fearChip.GetComponent<Image>().color, Is.EqualTo(new Color(0.45f, 0.03f, 0.06f, 0.95f)));
            Assert.That(boardFearChip.GetComponent<Image>().color, Is.EqualTo(new Color(0.45f, 0.03f, 0.06f, 0.95f)));
            Assert.That(attackChip.GetComponent<Image>().color, Is.EqualTo(new Color(0.85f, 0.12f, 0.12f, 0.95f)));
            Assert.That(fearChip.GetComponentInChildren<TMPro.TMP_Text>(true), Is.Null);
            Assert.That(boardFearChip.GetComponentInChildren<TMPro.TMP_Text>(true), Is.Null);
            Assert.That(attackChip.GetComponentInChildren<TMPro.TMP_Text>(true), Is.Null);

            var fearChipRect = fearChip.GetComponent<RectTransform>();
            Assert.That(fearChipRect.sizeDelta.x, Is.EqualTo(fearChipRect.sizeDelta.y).Within(0.001f));
            Assert.That(fearChipRect.sizeDelta.x, Is.GreaterThanOrEqualTo(28f));

            var tooltipTarget = fearChip.GetComponent<StatusEffectTooltipTarget>();
            Assert.That(tooltipTarget, Is.InstanceOf<IPointerDownHandler>());
            Assert.That(tooltipTarget, Is.Not.InstanceOf<IPointerEnterHandler>());
            yield return ShowTooltipByLongPress(tooltipTarget);

            var tooltip = viewObject.transform.Find("StatusTooltip");
            Assert.That(tooltip, Is.Not.Null);
            Assert.That(tooltip.gameObject.activeSelf, Is.True);
            Assert.That(tooltip.GetComponentInChildren<TMPro.TMP_Text>(true).text, Does.Contain("공격 랭크"));

            ReleaseLongPressTooltip(tooltipTarget);
            Assert.That(tooltip.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Initialize_PreservesSceneAuthoredPlayerBattleStatusEffectsRootPosition()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var playerBattleHp = CreateImageChild(viewObject.transform, "PlayerBattleHp");
            CreateImageChild(playerBattleHp.transform, "Fill");
            CreateTextChild(playerBattleHp.transform, "Text");
            var enemyHp = CreateImageChild(viewObject.transform, "EnemyHp");
            CreateImageChild(enemyHp.transform, "Fill");
            CreateTextChild(enemyHp.transform, "Text");
            var boardHp = CreateImageChild(viewObject.transform, "BoardHp");
            CreateImageChild(boardHp.transform, "HpBarFill");

            var authoredRoot = new GameObject("PlayerBattleStatusEffects", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            authoredRoot.transform.SetParent(playerBattleHp.transform, false);
            ownedObjects.Add(authoredRoot);
            var authoredRect = authoredRoot.GetComponent<RectTransform>();
            authoredRect.anchorMin = new Vector2(0f, 0f);
            authoredRect.anchorMax = new Vector2(0f, 0f);
            authoredRect.pivot = new Vector2(0f, 1f);
            authoredRect.anchoredPosition = new Vector2(17f, -84f);
            authoredRect.sizeDelta = new Vector2(180f, 36f);
            CreateStatusEffectTemplateForTest(authoredRoot.transform);

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var playerData = CreatePlayerData(20, 0);
            var enemyData = CreateEnemyData("Slime", 10, 0);
            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            player.ApplyFear(2);

            view.Initialize(bootstrap);

            Assert.That(authoredRect.anchoredPosition.x, Is.EqualTo(17f).Within(0.001f));
            Assert.That(authoredRect.anchoredPosition.y, Is.EqualTo(-84f).Within(0.001f));
            Assert.That(authoredRect.sizeDelta.x, Is.EqualTo(180f).Within(0.001f));
            Assert.That(authoredRoot.transform.Find("StatusEffect_fear"), Is.Not.Null);
        }

        [Test]
        public void EnemyDebuffIntent_RendersDebuffOnPlayerSideOnly()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var playerBattleHp = CreateImageChild(viewObject.transform, "PlayerBattleHp");
            CreateImageChild(playerBattleHp.transform, "Fill");
            CreateTextChild(playerBattleHp.transform, "Text");
            var enemyHp = CreateImageChild(viewObject.transform, "EnemyHp");
            CreateImageChild(enemyHp.transform, "Fill");
            CreateTextChild(enemyHp.transform, "Text");
            var boardHp = CreateImageChild(viewObject.transform, "BoardHp");
            CreateImageChild(boardHp.transform, "HpBarFill");
            CreateTextChild(viewObject.transform, "HpText");
            CreateAuthoredStatusEffectsRootForTest(playerBattleHp.transform, "PlayerBattleStatusEffects", new Vector2(CombatUiView.HpStatusEffectXOffset, -39f));
            CreateAuthoredStatusEffectsRootForTest(boardHp.transform, "PlayerBoardStatusEffects", new Vector2(CombatUiView.HpStatusEffectXOffset, -6f));
            CreateAuthoredStatusEffectsRootForTest(enemyHp.transform, "EnemyStatusEffects", new Vector2(CombatUiView.HpStatusEffectXOffset, -6f));

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var playerData = CreatePlayerData(20, 0);
            var enemyData = CreateEnemyData("Debuffer", 10, 0);
            enemyData.intentPattern = new System.Collections.Generic.List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Debuff,
                    debuffType = DebuffType.Fear,
                    value = 2,
                },
            };

            manager.SetCombatants(player, new[] { enemy });
            manager.EnemyTurnDelaySeconds = 0f;
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.BoardManager.SetBoardState(new[,]
            {
                { 64, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            }, 0);
            manager.ResolveBoardPhase();

            view.Initialize(bootstrap);
            manager.RequestEndPlayerTurn();

            var playerBattleStatusRoot = playerBattleHp.transform.Find("PlayerBattleStatusEffects");
            var playerBoardStatusRoot = boardHp.transform.Find("PlayerBoardStatusEffects");
            var enemyStatusRoot = enemyHp.transform.Find("EnemyStatusEffects");

            Assert.That(playerBattleStatusRoot, Is.Not.Null);
            Assert.That(playerBoardStatusRoot, Is.Not.Null);
            Assert.That(enemyStatusRoot, Is.Not.Null);
            Assert.That(playerBattleStatusRoot.GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(-39f).Within(0.001f));
            Assert.That(playerBattleStatusRoot.Find("StatusEffect_fear"), Is.Not.Null);
            Assert.That(playerBoardStatusRoot.Find("StatusEffect_fear"), Is.Not.Null);
            Assert.That(enemyStatusRoot.Find("StatusEffect_fear"), Is.Null);
            Assert.That(enemyStatusRoot.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Initialize_ConfiguresAudioSourceForAudibleBoardEffects()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var source = viewObject.AddComponent<AudioSource>();
            source.playOnAwake = true;
            source.spatialBlend = 1f;
            source.volume = 0.05f;
            source.mute = true;
            source.minDistance = 1f;
            source.maxDistance = 2f;
            var view = viewObject.AddComponent<CombatUiView>();

            view.Initialize(null);

            Assert.That(source.playOnAwake, Is.False);
            Assert.That(source.spatialBlend, Is.EqualTo(0f).Within(0.001f));
            Assert.That(source.volume, Is.EqualTo(1f).Within(0.001f));
            Assert.That(source.mute, Is.False);
            Assert.That(source.minDistance, Is.GreaterThanOrEqualTo(1000f));
            Assert.That(source.maxDistance, Is.GreaterThanOrEqualTo(1000f));
        }

        [Test]
        public void BindButton_ReattachesButtonClickAudioAfterReplacingRuntimeListeners()
        {
            var settings = AssetDatabase.LoadAssetAtPath<Project2048AudioSettings>(
                "Assets/Resources/Audio/Project2048AudioSettings.asset");
            var audioRoot = CreateOwnedGameObject("ButtonAudioRoot");
            var router = audioRoot.AddComponent<ButtonClickAudioRouter>();
            var view = CreateOwnedGameObject("CombatView").AddComponent<CombatUiView>();
            var buttonObject = CreateOwnedGameObject("RuntimeCombatButton");
            buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            var handlerCount = 0;
            var playCount = 0;

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.ButtonClickClip, Is.Not.Null);

            ButtonClickAudioRouter.ButtonClickPlayed += CountPlay;
            try
            {
                router.Initialize(settings);
                InvokePrivate(view, "BindButton", button, (System.Action)(() => handlerCount++));
                button.onClick.Invoke();
            }
            finally
            {
                ButtonClickAudioRouter.ButtonClickPlayed -= CountPlay;
            }

            Assert.That(playCount, Is.EqualTo(1));
            Assert.That(handlerCount, Is.EqualTo(1));

            void CountPlay()
            {
                playCount++;
            }
        }

        [Test]
        public void BattleScene_CombatUiView_HasBoardEffectProfileOnly()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
            var view = Object.FindAnyObjectByType<CombatUiView>(FindObjectsInactive.Include);

            Assert.That(view, Is.Not.Null);

            var serializedView = new SerializedObject(view);
            Assert.That(serializedView.FindProperty("audioSource").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedView.FindProperty("boardTileEffectProfile").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedView.FindProperty("playerHitClip"), Is.Null);
            Assert.That(serializedView.FindProperty("enemyHitClip"), Is.Null);
            Assert.That(serializedView.FindProperty("boardMoveClip"), Is.Null);
            Assert.That(serializedView.FindProperty("boardMergeClip"), Is.Null);
            Assert.That(serializedView.FindProperty("soundVolumeScale"), Is.Null);
        }

        [Test]
        public void BattleScene_UsesAuthoredCombatBackgroundSprites()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
            var view = Object.FindAnyObjectByType<CombatUiView>(FindObjectsInactive.Include);
            var worldView = Object.FindAnyObjectByType<CombatWorldSpriteView>(FindObjectsInactive.Include);

            Assert.That(view, Is.Not.Null);
            Assert.That(worldView, Is.Not.Null);

            var serializedView = new SerializedObject(view);
            var bottomBackground = serializedView.FindProperty("bottomPanelBackground").objectReferenceValue as Image;
            var bottomDefault = serializedView.FindProperty("bottomPanelDefaultSprite").objectReferenceValue as Sprite;
            var bottomLit = serializedView.FindProperty("bottomPanelMergeLitSprite").objectReferenceValue as Sprite;

            Assert.That(bottomBackground, Is.Not.Null);
            Assert.That(bottomBackground.name, Is.EqualTo("BottomPanelArtwork"));
            Assert.That(bottomBackground.transform.parent != null ? bottomBackground.transform.parent.name : null, Is.EqualTo("BottomPanel"));
            Assert.That(bottomBackground.sprite, Is.SameAs(bottomDefault));
            Assert.That(bottomBackground.color, Is.EqualTo(Color.white));
            Assert.That(bottomBackground.preserveAspect, Is.True);
            Assert.That(bottomBackground.raycastTarget, Is.False);
            Assert.That(AssetGuid(bottomDefault), Is.EqualTo(BottomCombatBackgroundSpriteGuid));
            Assert.That(AssetGuid(bottomLit), Is.EqualTo(BottomCombatBackgroundLitSpriteGuid));

            var bottomPanelImage = bottomBackground.transform.parent.GetComponent<Image>();
            Assert.That(bottomPanelImage, Is.Not.Null);
            Assert.That(bottomPanelImage.sprite, Is.Null);
            Assert.That(bottomPanelImage.color, Is.EqualTo(CombatUiView.ThemeBottomPanelSideFillColor));
            Assert.That(bottomPanelImage.raycastTarget, Is.False);

            AssertTransparentPanel(serializedView.FindProperty("boardPanel").objectReferenceValue as GameObject);
            AssertTransparentPanel(serializedView.FindProperty("actionPanel").objectReferenceValue as GameObject);
            AssertTransparentPanel(serializedView.FindProperty("enemyTurnPanel").objectReferenceValue as GameObject);
            Assert.That(serializedView.FindProperty("actionDescriptionText").objectReferenceValue, Is.Not.Null);
            AssertSceneSkillSlotsUseLanternSize(serializedView);

            var serializedWorldView = new SerializedObject(worldView);
            var topBackground = serializedWorldView.FindProperty("defaultBackgroundSprite").objectReferenceValue as Sprite;
            var backgroundRenderer = serializedWorldView.FindProperty("backgroundRenderer").objectReferenceValue as SpriteRenderer;

            Assert.That(AssetGuid(topBackground), Is.EqualTo(TopCombatBackgroundSpriteGuid));
            Assert.That(backgroundRenderer, Is.Not.Null);
            Assert.That(backgroundRenderer.sprite, Is.SameAs(topBackground));
            Assert.That(backgroundRenderer.transform.localPosition.y, Is.EqualTo(2.75f).Within(0.001f));
            Assert.That(backgroundRenderer.transform.localScale.x, Is.EqualTo(0.5208333f).Within(0.0001f));
            Assert.That(backgroundRenderer.transform.localScale.y, Is.EqualTo(0.5208333f).Within(0.0001f));
        }

        [Test]
        public void BattleScene_UsesAuthoredIntentIconSprites()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
            var view = Object.FindAnyObjectByType<CombatUiView>(FindObjectsInactive.Include);

            Assert.That(view, Is.Not.Null);

            var serializedView = new SerializedObject(view);
            var attackIcon = serializedView.FindProperty("attackIntentSprite").objectReferenceValue as Sprite;
            var defenseIcon = serializedView.FindProperty("defenseIntentSprite").objectReferenceValue as Sprite;
            var fearIcon = serializedView.FindProperty("fearIntentSprite").objectReferenceValue as Sprite;
            var intentBubble = serializedView.FindProperty("intentBubble").objectReferenceValue as GameObject;

            Assert.That(AssetGuid(attackIcon), Is.EqualTo(AttackIntentIconSpriteGuid));
            Assert.That(AssetGuid(defenseIcon), Is.EqualTo(DefenseIntentIconSpriteGuid));
            Assert.That(AssetGuid(fearIcon), Is.EqualTo(FearIntentIconSpriteGuid));

            Assert.That(intentBubble, Is.Not.Null);
            var intentBubbleImage = intentBubble.GetComponent<Image>();
            Assert.That(intentBubbleImage, Is.Not.Null);
            Assert.That(intentBubbleImage.sprite, Is.SameAs(attackIcon));
            Assert.That(intentBubbleImage.color, Is.EqualTo(Color.white));
            Assert.That(intentBubbleImage.preserveAspect, Is.True);
        }

        [Test]
        public void BattleScene_CombatUiView_HasSceneAuthoredStatusAndBlockObjectsOnBottomHpBars()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
            var view = Object.FindAnyObjectByType<CombatUiView>(FindObjectsInactive.Include);

            Assert.That(view, Is.Not.Null);

            var serializedView = new SerializedObject(view);
            var root = serializedView.FindProperty("playerBattleStatusEffectsRoot").objectReferenceValue as RectTransform;
            Assert.That(root, Is.Not.Null);
            Assert.That(root.name, Is.EqualTo("PlayerBattleStatusEffects"));
            Assert.That(root.parent != null ? root.parent.name : null, Is.EqualTo("PlayerBattleHp"));
            Assert.That(root.anchoredPosition.x, Is.EqualTo(CombatUiView.HpStatusEffectXOffset).Within(0.001f));
            Assert.That(root.Find("StatusEffectIconSample"), Is.Not.Null);
            Assert.That(root.parent.Find("HpBarOutline"), Is.Not.Null);

            var blockIcon = root.parent.Find("BlockIcon") as RectTransform;
            Assert.That(blockIcon, Is.Not.Null);
            Assert.That(blockIcon.gameObject.activeSelf, Is.True);
            Assert.That(blockIcon.Find("Text"), Is.Not.Null);

            var boardRoot = serializedView.FindProperty("playerBoardStatusEffectsRoot").objectReferenceValue as RectTransform;
            Assert.That(boardRoot, Is.Not.Null);
            Assert.That(boardRoot.name, Is.EqualTo("PlayerBoardStatusEffects"));
            Assert.That(boardRoot.parent != null ? boardRoot.parent.name : null, Is.EqualTo("HpBarBg"));
            Assert.That(boardRoot.anchoredPosition.x, Is.EqualTo(CombatUiView.HpStatusEffectXOffset).Within(0.001f));
            Assert.That(boardRoot.Find("StatusEffectIconSample"), Is.Not.Null);
            Assert.That(boardRoot.parent.Find("HpBarOutline"), Is.Not.Null);

            var boardBlockIcon = boardRoot.parent.Find("BlockIcon") as RectTransform;
            Assert.That(boardBlockIcon, Is.Not.Null);
            Assert.That(boardBlockIcon.gameObject.activeSelf, Is.True);
            Assert.That(boardBlockIcon.Find("Text"), Is.Not.Null);

            var enemyStatusRoot = serializedView.FindProperty("enemyStatusEffectsRoot").objectReferenceValue as RectTransform;
            Assert.That(enemyStatusRoot, Is.Not.Null);
            Assert.That(enemyStatusRoot.name, Is.EqualTo("EnemyStatusEffects"));
            Assert.That(enemyStatusRoot.parent != null ? enemyStatusRoot.parent.name : null, Is.EqualTo("EnemyHp"));
            Assert.That(enemyStatusRoot.anchoredPosition.x, Is.EqualTo(CombatUiView.HpStatusEffectXOffset).Within(0.001f));
            Assert.That(enemyStatusRoot.Find("StatusEffectIconSample"), Is.Not.Null);
            Assert.That(enemyStatusRoot.parent.Find("HpBarOutline"), Is.Not.Null);

            var enemyBlockIcon = enemyStatusRoot.parent.Find("BlockIcon") as RectTransform;
            Assert.That(enemyBlockIcon, Is.Not.Null);
            Assert.That(enemyBlockIcon.gameObject.activeSelf, Is.True);
            Assert.That(enemyBlockIcon.Find("Text"), Is.Not.Null);

            var layout = root.GetComponent<HorizontalLayoutGroup>();
            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.spacing, Is.EqualTo(4f).Within(0.001f));
            Assert.That(layout.childAlignment, Is.EqualTo(TextAnchor.MiddleLeft));
            Assert.That(layout.childForceExpandWidth, Is.False);
            Assert.That(layout.childForceExpandHeight, Is.False);

            var boardLayout = boardRoot.GetComponent<HorizontalLayoutGroup>();
            Assert.That(boardLayout, Is.Not.Null);
            Assert.That(boardLayout.spacing, Is.EqualTo(4f).Within(0.001f));
            Assert.That(boardLayout.childAlignment, Is.EqualTo(TextAnchor.MiddleLeft));
            Assert.That(boardLayout.childForceExpandWidth, Is.False);
            Assert.That(boardLayout.childForceExpandHeight, Is.False);

            var enemyLayout = enemyStatusRoot.GetComponent<HorizontalLayoutGroup>();
            Assert.That(enemyLayout, Is.Not.Null);
            Assert.That(enemyLayout.spacing, Is.EqualTo(4f).Within(0.001f));
            Assert.That(enemyLayout.childAlignment, Is.EqualTo(TextAnchor.MiddleLeft));
            Assert.That(enemyLayout.childForceExpandWidth, Is.False);
            Assert.That(enemyLayout.childForceExpandHeight, Is.False);
        }

        private GameObject CreateOwnedGameObject(string name)
        {
            var gameObject = new GameObject(name);
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private GameObject CreateOwnedRectTransformObject(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private Image CreateImageChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            ownedObjects.Add(child);
            var image = child.GetComponent<Image>();
            if (name == "Fill" || name == "HpBarFill")
            {
                ConfigureAuthoredHpFillForTest(parent, image);
            }

            return image;
        }

        private Sprite CreateOwnedSprite(string name)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            ownedObjects.Add(texture);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            sprite.name = name;
            ownedObjects.Add(sprite);
            return sprite;
        }

        private Button CreateButtonChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(parent, false);
            ownedObjects.Add(child);
            return child.GetComponent<Button>();
        }

        private void ConfigureAuthoredHpFillForTest(Transform hpRoot, Image fill)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HpBarSpritePath);
            fill.sprite = sprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.color = CombatUiView.ThemeHpFillColor;
            fill.raycastTarget = false;
            SetStretchForTest(fill.rectTransform, new Vector2(2.75f, 2.75f), new Vector2(-2.75f, -2.75f));

            CreateAuthoredHpImageForTest(hpRoot, "HpBarInterior", sprite, CombatUiView.ThemeHpBarBackgroundColor, filled: false, active: true);
            CreateAuthoredHpImageForTest(hpRoot, "DamageTrailFill", sprite, CombatUiView.ThemeHpDamageTrailColor, filled: true, active: true);
            CreateAuthoredHpImageForTest(hpRoot, "DamageFlashFill", sprite, new Color(1f, 1f, 1f, 0.95f), filled: false, active: false);
            CreateAuthoredHpImageForTest(hpRoot, "HpBarOutline", sprite, CombatUiView.ThemeHpBorderColor, filled: false, active: true, inset: false);
            CreateAuthoredBlockIconForTest(hpRoot);
        }

        private void CreateAuthoredHpImageForTest(Transform parent, string name, Sprite sprite, Color color, bool filled, bool active, bool inset = true)
        {
            if (parent.Find(name) != null)
            {
                return;
            }

            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            ownedObjects.Add(child);
            child.SetActive(active);
            var image = child.GetComponent<Image>();
            image.sprite = sprite;
            image.type = filled ? Image.Type.Filled : Image.Type.Simple;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.color = color;
            image.raycastTarget = false;
            SetStretchForTest(
                image.rectTransform,
                inset ? new Vector2(2.75f, 2.75f) : Vector2.zero,
                inset ? new Vector2(-2.75f, -2.75f) : Vector2.zero);
        }

        private void CreateAuthoredBlockIconForTest(Transform hpRoot)
        {
            if (hpRoot.Find("BlockIcon") != null)
            {
                return;
            }

            var iconObject = new GameObject("BlockIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(hpRoot, false);
            ownedObjects.Add(iconObject);
            var icon = iconObject.GetComponent<RectTransform>();
            icon.anchorMin = new Vector2(1f, 0.5f);
            icon.anchorMax = new Vector2(1f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(12f, 0f);
            icon.sizeDelta = new Vector2(34f, 24f);

            var image = iconObject.GetComponent<Image>();
            image.color = new Color(0.42f, 0.46f, 0.5f, 0.95f);
            image.raycastTarget = false;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textObject.transform.SetParent(icon, false);
            ownedObjects.Add(textObject);
            var textRect = textObject.GetComponent<RectTransform>();
            SetStretchForTest(textRect, Vector2.zero, Vector2.zero);
            var label = textObject.GetComponent<TMPro.TMP_Text>();
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.fontSize = 16f;
            label.fontStyle = TMPro.FontStyles.Bold;
            label.color = Color.white;
            label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            label.raycastTarget = false;
        }

        private RectTransform CreateAuthoredStatusEffectsRootForTest(
            Transform parent,
            string rootName,
            Vector2 anchoredPosition)
        {
            var rootObject = new GameObject(rootName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rootObject.transform.SetParent(parent, false);
            ownedObjects.Add(rootObject);

            var root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(0f, 0f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = new Vector2(160f, 32f);

            var layout = rootObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateStatusEffectTemplateForTest(root);
            return root;
        }

        private Image CreateStatusEffectTemplateForTest(Transform parent)
        {
            var template = CreateImageChild(parent, "StatusEffectIconSample");
            template.gameObject.AddComponent<StatusEffectTooltipTarget>();
            template.rectTransform.sizeDelta = new Vector2(32f, 32f);
            return template;
        }

        private RectTransform CreateStatusTooltipForTest(Transform parent)
        {
            var tooltipObject = new GameObject("StatusTooltip", typeof(RectTransform), typeof(Image));
            tooltipObject.transform.SetParent(parent, false);
            tooltipObject.SetActive(false);
            ownedObjects.Add(tooltipObject);

            var tooltipRect = tooltipObject.GetComponent<RectTransform>();
            tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.pivot = new Vector2(0.5f, 0f);
            tooltipRect.anchoredPosition = new Vector2(0f, 48f);
            tooltipRect.sizeDelta = new Vector2(320f, 56f);

            var image = tooltipObject.GetComponent<Image>();
            image.color = new Color(0.02f, 0.025f, 0.03f, 0.96f);
            image.raycastTarget = false;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textObject.transform.SetParent(tooltipRect, false);
            ownedObjects.Add(textObject);
            SetStretchForTest(textObject.GetComponent<RectTransform>(), new Vector2(10f, 6f), new Vector2(-10f, -6f));

            var label = textObject.GetComponent<TMPro.TMP_Text>();
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.fontSize = 15f;
            label.color = Color.white;
            label.textWrappingMode = TMPro.TextWrappingModes.Normal;
            label.raycastTarget = false;

            return tooltipRect;
        }

        private static IEnumerator ShowTooltipByLongPress(StatusEffectTooltipTarget target)
        {
            ((IPointerDownHandler)target).OnPointerDown(new PointerEventData(null));
            yield return new WaitForSecondsRealtime(StatusEffectTooltipTarget.LongPressDelaySeconds + 0.02f);
        }

        private static void ReleaseLongPressTooltip(StatusEffectTooltipTarget target)
        {
            ((IPointerUpHandler)target).OnPointerUp(new PointerEventData(null));
        }

        private static void SetStretchForTest(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.anchoredPosition = Vector2.zero;
        }

        private TMPro.TMP_Text CreateTextChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            child.transform.SetParent(parent, false);
            ownedObjects.Add(child);
            return child.GetComponent<TMPro.TMP_Text>();
        }

        private PlayerSO CreatePlayerData(int maxHp, int attackPower, params SkillSO[] skills)
        {
            var data = ScriptableObject.CreateInstance<PlayerSO>();
            data.maxHp = maxHp;
            data.attackPower = attackPower;
            data.startingSkills = new System.Collections.Generic.List<SkillSO>(skills);
            ownedObjects.Add(data);
            return data;
        }

        private EnemySO CreateEnemyData(string enemyName, int maxHp, int attackValue)
        {
            var data = ScriptableObject.CreateInstance<EnemySO>();
            data.enemyName = enemyName;
            data.maxHp = maxHp;
            data.attackPower = attackValue;
            data.intentPattern = new System.Collections.Generic.List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Attack,
                    value = attackValue,
                },
            };
            ownedObjects.Add(data);
            return data;
        }

        private SkillSO CreateSkill(
            string skillId,
            string skillName,
            SkillType skillType,
            int cost,
            int power,
            string description = null)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.skillId = skillId;
            skill.skillName = skillName;
            skill.skillType = skillType;
            skill.cost = cost;
            skill.power = power;
            skill.description = description;
            ownedObjects.Add(skill);
            return skill;
        }

        private static void SetEnemyActionsPerTurn(EnemySO data, int count)
        {
            var field = typeof(EnemySO).GetField("actionsPerTurn");
            Assert.That(field, Is.Not.Null, "EnemySO should expose actionsPerTurn for per-enemy multi-action tuning.");
            field.SetValue(data, count);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            return target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(target);
        }

        private static object InvokePrivate(object target, string methodName, params object[] args)
        {
            var method = target.GetType()
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(target, args);
        }

        private static string AssetGuid(Object asset)
        {
            Assert.That(asset, Is.Not.Null);
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }

        private static void AssertTransparentPanel(GameObject panel)
        {
            Assert.That(panel, Is.Not.Null);
            var image = panel.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Null);
            Assert.That(image.color, Is.EqualTo(Color.clear));
            Assert.That(image.raycastTarget, Is.False);
        }

        private static void AssertSceneSkillSlotsUseLanternSize(SerializedObject serializedView)
        {
            var buttons = serializedView.FindProperty("skillTierButtons");
            Assert.That(buttons, Is.Not.Null);
            Assert.That(buttons.arraySize, Is.GreaterThanOrEqualTo(PlayerCombatController.MaxEquippedSkillSlots));

            var positions = new[]
            {
                new Vector2(-88f, 88f),
                new Vector2(88f, 88f),
                new Vector2(-88f, -88f),
                new Vector2(88f, -88f),
            };

            for (var index = 0; index < PlayerCombatController.MaxEquippedSkillSlots; index++)
            {
                var button = buttons.GetArrayElementAtIndex(index).objectReferenceValue as Button;
                Assert.That(button, Is.Not.Null);
                var rect = button.GetComponent<RectTransform>();
                Assert.That(rect.sizeDelta.x, Is.EqualTo(ExpectedSkillLanternSlotSize.x).Within(0.001f));
                Assert.That(rect.sizeDelta.y, Is.EqualTo(ExpectedSkillLanternSlotSize.y).Within(0.001f));
                Assert.That(rect.anchoredPosition.x, Is.EqualTo(positions[index].x).Within(0.001f));
                Assert.That(rect.anchoredPosition.y, Is.EqualTo(positions[index].y).Within(0.001f));
            }
        }

        private static void AssertHpFillIsRenderable(Image fill)
        {
            Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(fill.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
            Assert.That(fill.fillOrigin, Is.EqualTo((int)Image.OriginHorizontal.Left));
            Assert.That(fill.sprite, Is.Not.Null);
            Assert.That(fill.sprite.texture.width, Is.GreaterThanOrEqualTo(1024));
            Assert.That(fill.sprite.texture.height, Is.GreaterThanOrEqualTo(256));
            Assert.That(fill.color, Is.EqualTo(CombatUiView.ThemeHpFillColor));
            if (fill.transform.parent != null && fill.transform.parent.TryGetComponent<Image>(out var background))
            {
                Assert.That(background.color, Is.EqualTo(Color.clear));

                var interior = fill.transform.parent.Find("HpBarInterior")?.GetComponent<Image>();
                Assert.That(interior, Is.Not.Null);
                Assert.That(interior.color, Is.EqualTo(CombatUiView.ThemeHpBarBackgroundColor));

                var outline = fill.transform.parent.Find("HpBarOutline")?.GetComponent<Image>();
                Assert.That(outline, Is.Not.Null);
                Assert.That(outline.color, Is.EqualTo(CombatUiView.ThemeHpBorderColor));
            }

            Assert.That(fill.rectTransform.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(fill.rectTransform.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(fill.rectTransform.offsetMin.x, Is.GreaterThan(0f));
            Assert.That(fill.rectTransform.offsetMin.y, Is.GreaterThan(0f));
            Assert.That(fill.raycastTarget, Is.False);
        }
    }
}
