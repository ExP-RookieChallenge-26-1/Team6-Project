using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Flow;
using Project2048.Prototype;
using Project2048.Rewards;
using Project2048.Skills;
using Project2048.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Project2048.Tests
{
    public class RewardGrowthTests
    {
        private readonly List<Object> ownedObjects = new();

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
        public void CombatSetup_WithRunProgress_AddsExtraBoardMovesForNextCombat()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var runProgress = new RunProgress();

            runProgress.AddBoardMoveCount(2);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 4,
                runProgress = runProgress,
            });

            Assert.That(manager.BoardManager.MoveCount, Is.EqualTo(6));
        }

        [Test]
        public void RewardManager_RestChoice_HealsByMaxHpPercent_AndStoresRunHp()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var reward = CreateReward(healPercentOfMaxHp: 0.3f, extraBoardMoveCount: 1);
            var table = CreateRewardTable(reward);
            var runProgress = new RunProgress();
            var rewardManager = CreateGameObject<RewardManager>("RewardManager");

            player.Init(playerData);
            player.TakeDamage(10);
            runProgress.CapturePlayer(player);

            rewardManager.Initialize(runProgress, table);
            rewardManager.OfferReward(new CombatResult(), player);
            var result = rewardManager.ChooseRest(player);

            Assert.That(result.Kind, Is.EqualTo(RewardChoiceKind.Rest));
            Assert.That(result.AppliedAmount, Is.EqualTo(6));
            Assert.That(player.CurrentHp, Is.EqualTo(16));
            Assert.That(runProgress.CurrentHp, Is.EqualTo(16));
        }

        [TestCase(RewardChoiceKind.HealOne, 60)]
        [TestCase(RewardChoiceKind.HealTwo, 120)]
        [TestCase(RewardChoiceKind.HealThree, 180)]
        public void RewardManager_HealTierChoice_HealsByTierPercentOfMaxHp(
            RewardChoiceKind rewardKind,
            int expectedHeal)
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var playerData = CreatePlayerData(maxHp: 240, attackPower: 2);
            var reward = CreateReward(healPercentOfMaxHp: 0.3f, extraBoardMoveCount: 1);
            var table = CreateRewardTable(reward);
            var runProgress = new RunProgress();
            var rewardManager = CreateGameObject<RewardManager>("RewardManager");

            reward.rewardKind = rewardKind;
            player.Init(playerData);
            player.TakeDamage(220);
            runProgress.CapturePlayer(player);

            rewardManager.Initialize(runProgress, table);
            rewardManager.OfferReward(new CombatResult(), player);
            var result = rewardManager.ChooseReward(0, player);

            Assert.That(result.Kind, Is.EqualTo(rewardKind));
            Assert.That(result.AppliedAmount, Is.EqualTo(expectedHeal));
            Assert.That(player.CurrentHp, Is.EqualTo(20 + expectedHeal));
            Assert.That(runProgress.CurrentHp, Is.EqualTo(player.CurrentHp));
        }

        [Test]
        public void RewardManager_EnhanceChoice_AddsRunMoveCount_WithoutMutatingPlayerSo()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var reward = CreateReward(healPercentOfMaxHp: 0.3f, extraBoardMoveCount: 2);
            var table = CreateRewardTable(reward);
            var runProgress = new RunProgress();
            var rewardManager = CreateGameObject<RewardManager>("RewardManager");

            player.Init(playerData);
            runProgress.CapturePlayer(player);

            rewardManager.Initialize(runProgress, table);
            rewardManager.OfferReward(new CombatResult(), player);
            var result = rewardManager.ChooseEnhance(player);

            Assert.That(result.Kind, Is.EqualTo(RewardChoiceKind.Enhance));
            Assert.That(result.AppliedAmount, Is.EqualTo(2));
            Assert.That(runProgress.ExtraBoardMoveCount, Is.EqualTo(2));
            Assert.That(playerData.boardMoveCountBonus, Is.EqualTo(0));
        }

        [Test]
        public void RewardManager_OfferReward_FillsThreeRogueliteChoices()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var reward = CreateReward(healPercentOfMaxHp: 0.3f, extraBoardMoveCount: 1);
            var table = CreateRewardTable(reward);
            var rewardManager = CreateGameObject<RewardManager>("RewardManager");

            player.Init(playerData);
            rewardManager.Initialize(new RunProgress(), table);
            rewardManager.OfferReward(new CombatResult(), player);

            Assert.That(rewardManager.PendingChoices.Count, Is.EqualTo(3));
            Assert.That(rewardManager.PendingChoices[0], Is.SameAs(reward));
        }

        [Test]
        public void RewardManager_OfferReward_AllowsTwoSkillsButRequiresSecondCategory()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var knownSkill = CreateSkill("known", SkillType.Attack, cost: 1, power: 10);
            var newSkillA = CreateSkill("new-a", SkillType.Attack, cost: 1, power: 10);
            var newSkillB = CreateSkill("new-b", SkillType.Defense, cost: 1, power: 10);
            var table = CreateRewardTable(
                CreateRewardChoice("learn-known", RewardChoiceKind.LearnSkill, knownSkill),
                CreateRewardChoice("learn-new-a", RewardChoiceKind.LearnSkill, newSkillA),
                CreateRewardChoice("learn-new-b", RewardChoiceKind.LearnSkill, newSkillB),
                CreateRewardChoice("heal", RewardChoiceKind.HealTwo));
            var rewardManager = CreateGameObject<RewardManager>("RewardManager");

            playerData.startingSkills = new List<SkillSO> { knownSkill };
            player.Init(playerData);
            rewardManager.Initialize(new RunProgress(), table);
            rewardManager.OfferReward(new CombatResult(), player);

            Assert.That(rewardManager.PendingChoices.Count, Is.EqualTo(3));
            Assert.That(rewardManager.PendingChoices.Count(reward => reward.rewardKind == RewardChoiceKind.LearnSkill), Is.EqualTo(2));
            Assert.That(rewardManager.PendingChoices.Any(reward => reward.rewardKind == RewardChoiceKind.HealTwo), Is.True);
            Assert.That(
                rewardManager.PendingChoices.Any(reward => reward.skillToLearn != null && reward.skillToLearn.skillId == "known"),
                Is.False);
        }

        [Test]
        public void RewardManager_OfferReward_AddsSecondCategoryWhenTableRollsOneCategory()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var table = CreateRewardTable(
                CreateRewardChoice("heal-a", RewardChoiceKind.HealOne),
                CreateRewardChoice("heal-b", RewardChoiceKind.HealTwo),
                CreateRewardChoice("heal-c", RewardChoiceKind.HealThree));
            var rewardManager = CreateGameObject<RewardManager>("RewardManager");

            player.Init(playerData);
            rewardManager.Initialize(new RunProgress(), table);
            rewardManager.OfferReward(new CombatResult(), player);

            Assert.That(rewardManager.PendingChoices.Count, Is.EqualTo(3));
            Assert.That(CountRewardCategories(rewardManager.PendingChoices), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void TemporaryRewardBuff_AppliesRankBonusToNextCombatOnce()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var runProgress = new RunProgress();

            runProgress.AddNextCombatRankBuff(attackStageBonus: 2, defenseStageBonus: 1, boardMoveCountBonus: 2);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 4,
                runProgress = runProgress,
            });

            Assert.That(player.AttackPower, Is.EqualTo(2));
            Assert.That(player.BaseDefensePower, Is.EqualTo(2));
            Assert.That(player.AttackStage, Is.EqualTo(2));
            Assert.That(player.DefenseStage, Is.EqualTo(1));
            Assert.That(player.EffectiveAttackPower, Is.EqualTo(4));
            Assert.That(player.EffectiveDefensePower, Is.EqualTo(3));
            Assert.That(manager.BoardManager.MoveCount, Is.EqualTo(6));
            Assert.That(runProgress.NextCombatAttackStageBonus, Is.EqualTo(0));
            Assert.That(runProgress.NextCombatDefenseStageBonus, Is.EqualTo(0));
            Assert.That(runProgress.NextCombatAttackPowerBonus, Is.EqualTo(0));
            Assert.That(runProgress.NextCombatDefensePowerBonus, Is.EqualTo(0));
            Assert.That(runProgress.NextCombatBoardMoveCountBonus, Is.EqualTo(0));
        }

        [Test]
        public void RewardManager_LearnSkill_PersistsEquippedSkillsIntoNextCombat()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var rewardManager = CreateGameObject<RewardManager>("RewardManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var lightShot = CreateSkill("light-shot", SkillType.Attack, cost: 6, power: 60);
            var lowStance = CreateSkill("low-stance", SkillType.Defense, cost: 4, power: 30);
            var flash = CreateSkill("flash", SkillType.Debuff, cost: 5, power: 0);
            var learnedSkill = CreateSkill("bleeding-cut", SkillType.Attack, cost: 6, power: 50);
            var reward = CreateReward(healPercentOfMaxHp: 0.3f, extraBoardMoveCount: 1);
            var table = CreateRewardTable(reward);
            var runProgress = new RunProgress();

            playerData.startingSkills = new List<SkillSO> { lightShot, lowStance, flash };
            reward.rewardKind = RewardChoiceKind.LearnSkill;
            reward.skillToLearn = learnedSkill;
            player.Init(playerData);
            runProgress.CapturePlayer(player);

            rewardManager.Initialize(runProgress, table);
            rewardManager.OfferReward(new CombatResult(), player);
            var result = rewardManager.ChooseReward(0, player);

            Assert.That(result.Kind, Is.EqualTo(RewardChoiceKind.LearnSkill));
            Assert.That(player.Skills.Count, Is.EqualTo(4));
            Assert.That(player.Skills[3].skillId, Is.EqualTo("bleeding-cut"));
            Assert.That(runProgress.EquippedSkills.Count, Is.EqualTo(4));

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 4,
                runProgress = runProgress,
            });

            Assert.That(player.Skills.Count, Is.EqualTo(4));
            Assert.That(player.Skills[0].skillId, Is.EqualTo("light-shot"));
            Assert.That(player.Skills[1].skillId, Is.EqualTo("low-stance"));
            Assert.That(player.Skills[2].skillId, Is.EqualTo("flash"));
            Assert.That(player.Skills[3].skillId, Is.EqualTo("bleeding-cut"));
        }

        [Test]
        public void CombatUiView_VictoryShowsRewardOverlayUntilRewardChoiceIsClaimed()
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
            var restText = CreateTextChild(rewardOverlay.transform, "RestText");
            var enhanceText = CreateTextChild(rewardOverlay.transform, "EnhanceText");
            var thirdText = CreateTextChild(rewardOverlay.transform, "ThirdText");
            var restButton = CreateButtonChild(rewardOverlay.transform, "RestButton");
            var enhanceButton = CreateButtonChild(rewardOverlay.transform, "EnhanceButton");
            var thirdButton = CreateButtonChild(rewardOverlay.transform, "ThirdButton");

            SetPrivateField(view, "rewardOverlay", rewardOverlay);
            SetPrivateField(view, "resultOverlay", resultOverlay);
            SetPrivateField(view, "boardPanel", boardPanel);
            SetPrivateField(view, "actionPanel", actionPanel);
            SetPrivateField(view, "rewardTitleText", rewardTitle);
            SetPrivateField(view, "rewardDescriptionText", rewardDescription);
            SetPrivateField(view, "rewardRestText", restText);
            SetPrivateField(view, "rewardEnhanceText", enhanceText);
            SetPrivateField(view, "rewardRestButton", restButton);
            SetPrivateField(view, "rewardEnhanceButton", enhanceButton);
            SetPrivateField(view, "rewardChoiceButtons", new List<Button> { restButton, enhanceButton, thirdButton });
            SetPrivateField(view, "rewardChoiceLabels", new List<TMP_Text> { restText, enhanceText, thirdText });

            var manager = CreateGameObject<CombatManager>("CombatManager");
            var rewardManager = CreateGameObject<RewardManager>("RewardManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var bootstrap = CreateGameObject<PrototypeCombatBootstrap>("Bootstrap");
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 99);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var reward = CreateReward(healPercentOfMaxHp: 0.3f, extraBoardMoveCount: 1);
            reward.rewardKind = RewardChoiceKind.TemporaryBoardMoveCount;
            reward.temporaryBoardMoveCountBonus = 2;
            var table = CreateRewardTable(reward);
            var runProgress = new RunProgress();

            rewardManager.Initialize(runProgress, table);
            SetPrivateField(bootstrap, "combatManager", manager);
            SetPrivateField(bootstrap, "rewardManager", rewardManager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
                runProgress = runProgress,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(rewardOverlay.activeSelf, Is.False);
            Assert.That(resultOverlay.activeSelf, Is.False);

            manager.RequestUseSkill(attack, enemy);
            rewardManager.OfferReward(new CombatResult(), player);

            Assert.That(rewardOverlay.activeSelf, Is.True);
            Assert.That(resultOverlay.activeSelf, Is.False);
            Assert.That(boardPanel.activeSelf, Is.False);
            Assert.That(actionPanel.activeSelf, Is.False);
            Assert.That(rewardManager.PendingChoices.Count, Is.EqualTo(3));
            Assert.That(rewardTitle.text, Is.EqualTo("보상 선택"));
            Assert.That(restText.text, Is.EqualTo("다음 전투 이동 횟수 +2"));
            Assert.That(enhanceText.text, Is.Not.Empty);

            restButton.onClick.Invoke();

            Assert.That(rewardOverlay.activeSelf, Is.False);
            Assert.That(resultOverlay.activeSelf, Is.True);
            Assert.That(runProgress.NextCombatBoardMoveCountBonus, Is.EqualTo(2));
        }

        [Test]
        public void CombatDefeat_WhenEnemyKillsPlayer_RaisesDefeatAndDoesNotOfferReward()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var rewardManager = CreateGameObject<RewardManager>("RewardManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 10, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 20);
            var runProgress = new RunProgress();
            var defeatRaised = false;

            rewardManager.Initialize(runProgress, CreateRewardTable(CreateReward(0.3f, 1)));
            manager.OnCombatDefeat += () => defeatRaised = true;

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
                runProgress = runProgress,
            });
            manager.BoardManager.SetBoardState(new[,]
            {
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            }, 0);

            manager.ResolveBoardPhase();
            manager.RequestEndPlayerTurn();
            rewardManager.ClearReward(player);

            Assert.That(defeatRaised, Is.True);
            Assert.That(manager.CurrentPhase, Is.EqualTo(CombatPhase.Defeat));
            Assert.That(player.CurrentHp, Is.EqualTo(0));
            Assert.That(rewardManager.HasPendingReward, Is.False);
            Assert.That(runProgress.CurrentHp, Is.EqualTo(0));
        }

        [Test]
        public void Bootstrap_WithMissingRewardSceneReferences_DoesNotCreateRuntimeManagers()
        {
            var bootstrapObject = CreateOwnedGameObject("Bootstrap");
            var bootstrap = bootstrapObject.AddComponent<PrototypeCombatBootstrap>();
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");

            SetPrivateField(bootstrap, "combatManager", manager);
            SetPrivateField(bootstrap, "playerController", player);
            SetPrivateField(bootstrap, "enemyController", enemy);

            InvokePrivateMethod(bootstrap, "EnsureRuntimeObjects");

            Assert.That(bootstrap.RewardManager, Is.Null);
            Assert.That(bootstrapObject.transform.Find("RewardManager"), Is.Null);
        }

        [Test]
        public void Bootstrap_StartPrototypeCombat_UsesPlayerInitialBoardMoveCount()
        {
            var bootstrap = CreateGameObject<PrototypeCombatBootstrap>("Bootstrap");
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);

            playerData.initialBoardMoveCount = 9;
            SetPrivateField(bootstrap, "combatManager", manager);
            SetPrivateField(bootstrap, "playerController", player);
            SetPrivateField(bootstrap, "enemyController", enemy);
            SetPrivateField(bootstrap, "playerData", playerData);
            SetPrivateField(bootstrap, "enemyData", enemyData);
            SetPrivateField(bootstrap, "randomizeEnemyOnStart", false);

            bootstrap.StartPrototypeCombat();

            Assert.That(manager.BoardManager.MoveCount, Is.EqualTo(9));
        }

        [Test]
        public void BattleScene_RewardOverlayAndManagers_AreSceneAuthored()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");

            var view = Object.FindAnyObjectByType<CombatUiView>(FindObjectsInactive.Include);
            var worldSpriteView = Object.FindAnyObjectByType<CombatWorldSpriteView>(FindObjectsInactive.Include);
            var rewardManager = Object.FindAnyObjectByType<RewardManager>(FindObjectsInactive.Include);
            var combatCanvas = GameObject.Find("CombatCanvas")?.GetComponent<Canvas>();
            var backgroundSprite = GameObject.Find("BackgroundSprite")?.GetComponent<SpriteRenderer>();
            var playerSprite = GameObject.Find("PlayerSprite")?.GetComponent<SpriteRenderer>();
            var enemySprite = GameObject.Find("EnemySprite")?.GetComponent<SpriteRenderer>();
            var battleSceneBackground = GameObject.Find("BattleScene")?.GetComponent<Image>();
            var playerPortraitImage = GameObject.Find("PlayerPortrait")?.GetComponent<Image>();
            var enemyPortraitImage = GameObject.Find("EnemyPortrait")?.GetComponent<Image>();
            var intentBubble = GameObject.Find("IntentBubble")?.GetComponent<RectTransform>();
            var intentBubbleText = GameObject.Find("IntentBubbleText")?.GetComponent<TMP_Text>();

            Assert.That(view, Is.Not.Null);
            Assert.That(worldSpriteView, Is.Not.Null);
            Assert.That(rewardManager, Is.Not.Null);
            Assert.That(combatCanvas, Is.Not.Null);
            Assert.That(combatCanvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(combatCanvas.worldCamera, Is.Null);
            Assert.That(backgroundSprite, Is.Not.Null);
            Assert.That(playerSprite, Is.Not.Null);
            Assert.That(enemySprite, Is.Not.Null);
            Assert.That(backgroundSprite.sprite, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(backgroundSprite.sprite),
                Is.EqualTo("Assets/Art/Prototype/BattleSceneGreyBackground.png"));
            Assert.That(playerSprite.sprite, Is.Not.Null);
            Assert.That(enemySprite.sprite, Is.Not.Null);
            Assert.That(battleSceneBackground, Is.Not.Null);
            Assert.That(playerPortraitImage, Is.Not.Null);
            Assert.That(enemyPortraitImage, Is.Not.Null);
            Assert.That(intentBubble, Is.Not.Null);
            Assert.That(intentBubbleText, Is.Not.Null);
            Assert.That(battleSceneBackground.enabled, Is.False);
            Assert.That(playerPortraitImage.enabled, Is.False);
            Assert.That(enemyPortraitImage.enabled, Is.False);
            Assert.That(intentBubble.sizeDelta.x, Is.EqualTo(CombatUiView.IntentBubbleSquareSize).Within(0.001f));
            Assert.That(intentBubble.sizeDelta.y, Is.EqualTo(CombatUiView.IntentBubbleSquareSize).Within(0.001f));
            Assert.That(intentBubbleText.enableAutoSizing, Is.True);
            Assert.That(intentBubbleText.fontSizeMax, Is.EqualTo(14f).Within(0.001f));
            Assert.That(intentBubbleText.raycastTarget, Is.False);
            Assert.That(intentBubbleText.rectTransform.sizeDelta, Is.EqualTo(new Vector2(-6f, -6f)));

            var serializedView = new SerializedObject(view);
            var rewardOverlay = serializedView.FindProperty("rewardOverlay").objectReferenceValue as GameObject;
            var resultOverlay = serializedView.FindProperty("resultOverlay").objectReferenceValue as GameObject;
            var serializedWorldView = new SerializedObject(worldSpriteView);

            Assert.That(serializedView.FindProperty("rewardManager").objectReferenceValue, Is.EqualTo(rewardManager));
            Assert.That(rewardOverlay, Is.Not.Null);
            Assert.That(resultOverlay, Is.Not.Null);
            Assert.That(rewardOverlay.name, Is.EqualTo("RewardOverlay"));
            Assert.That(resultOverlay.name, Is.EqualTo("ResultOverlay"));
            Assert.That(rewardOverlay.scene.path, Is.EqualTo("Assets/Scenes/BattleScene.unity"));
            Assert.That(resultOverlay.scene.path, Is.EqualTo("Assets/Scenes/BattleScene.unity"));
            Assert.That(rewardOverlay.transform.parent.name, Is.EqualTo("BottomPanel"));
            Assert.That(resultOverlay.transform.parent.name, Is.EqualTo("CombatCanvas"));
            Assert.That(rewardOverlay.activeSelf, Is.True);
            Assert.That(resultOverlay.activeSelf, Is.True);
            Assert.That(
                AssetGuid(serializedWorldView.FindProperty("upperStageBackgroundSprite").objectReferenceValue),
                Is.EqualTo("163faba4684ccae4d9db68d43ba672a9"));
            Assert.That(
                AssetGuid(serializedWorldView.FindProperty("middleStageBackgroundSprite").objectReferenceValue),
                Is.EqualTo("9e1ad007a13410142a6da69e5c00074e"));
            Assert.That(
                AssetGuid(serializedWorldView.FindProperty("lowerStageBackgroundSprite").objectReferenceValue),
                Is.EqualTo("1fb74fbbb05439b46826312757f9664a"));
            Assert.That(
                AssetGuid(serializedWorldView.FindProperty("rewardMothSprite").objectReferenceValue),
                Is.EqualTo("1b1b8bc2583af9349b6cc5fb6673e155"));
        }

        [Test]
        public void CombatWorldSpriteView_Initialize_AssignsSpritesFromCombatData()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var backgroundRenderer = CreateGameObject<SpriteRenderer>("BackgroundSprite");
            var playerRenderer = CreateGameObject<SpriteRenderer>("PlayerSprite");
            var enemyRenderer = CreateGameObject<SpriteRenderer>("EnemySprite");
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var bootstrap = CreateGameObject<PrototypeCombatBootstrap>("Bootstrap");
            var backgroundSprite = CreateSprite("Background");
            var playerSprite = CreateSprite("Player");
            var enemySprite = CreateSprite("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);

            playerData.portrait = playerSprite;
            enemyData.portrait = enemySprite;
            SetPrivateField(view, "backgroundRenderer", backgroundRenderer);
            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "defaultBackgroundSprite", backgroundSprite);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            view.Initialize(bootstrap);

            Assert.That(backgroundRenderer.sprite, Is.EqualTo(backgroundSprite));
            Assert.That(playerRenderer.sprite, Is.EqualTo(playerSprite));
            Assert.That(enemyRenderer.sprite, Is.EqualTo(enemySprite));
            Assert.That(enemyRenderer.color.a, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void CombatWorldSpriteView_SetStage_UsesStagePresentationBackground()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var backgroundRenderer = CreateGameObject<SpriteRenderer>("BackgroundSprite");
            var defaultBackgroundSprite = CreateSprite("DefaultBackground");
            var stageBackgroundSprite = CreateSprite("StageBackground");
            var stage = ScriptableObject.CreateInstance<StageSO>();

            ownedObjects.Add(stage);
            SetPrivateField(stage, "floor", StageFloor.Middle);
            SetPrivateField(stage, "presentationBackgroundSprite", stageBackgroundSprite);
            SetPrivateField(view, "backgroundRenderer", backgroundRenderer);
            SetPrivateField(view, "defaultBackgroundSprite", defaultBackgroundSprite);

            view.SetStage(stage);

            Assert.That(backgroundRenderer.sprite, Is.EqualTo(stageBackgroundSprite));
        }

        [Test]
        public void CombatWorldSpriteView_RewardChoices_ShowMothAtEnemyPosition()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateGameObject<SpriteRenderer>("EnemySprite");
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var rewardManager = CreateGameObject<RewardManager>("RewardManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var bootstrap = CreateGameObject<PrototypeCombatBootstrap>("Bootstrap");
            var enemySprite = CreateSprite("Enemy");
            var mothSprite = CreateSprite("RewardMoth");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var reward = CreateReward(healPercentOfMaxHp: 0.3f, extraBoardMoveCount: 1);
            var table = CreateRewardTable(reward);
            var enemyLocalPosition = new Vector3(1.5f, 2.25f, 0f);

            reward.rewardKind = RewardChoiceKind.TemporaryBoardMoveCount;
            reward.temporaryBoardMoveCountBonus = 2;
            enemyData.portrait = enemySprite;
            enemyRenderer.transform.localPosition = enemyLocalPosition;
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "rewardMothSprite", mothSprite);
            SetPrivateField(bootstrap, "combatManager", manager);
            SetPrivateField(bootstrap, "rewardManager", rewardManager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            rewardManager.Initialize(new RunProgress(), table);
            view.Initialize(bootstrap);
            rewardManager.OfferReward(new CombatResult(), player);

            Assert.That(enemyRenderer.sprite, Is.EqualTo(mothSprite));
            Assert.That(enemyRenderer.color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(enemyRenderer.transform.localPosition, Is.EqualTo(enemyLocalPosition));

            rewardManager.ChooseReward(0, player);

            Assert.That(enemyRenderer.sprite, Is.EqualTo(enemySprite));
            Assert.That(enemyRenderer.color.a, Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator CombatWorldSpriteView_EnemyDeath_FadesEnemyRenderer()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateGameObject<SpriteRenderer>("EnemySprite");
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var bootstrap = CreateGameObject<PrototypeCombatBootstrap>("Bootstrap");
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 99);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);

            enemyData.portrait = CreateSprite("Enemy");
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(enemyRenderer.color.a, Is.EqualTo(1f).Within(0.001f));

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);
            yield return null;

            Assert.That(enemyRenderer.color.a, Is.LessThanOrEqualTo(0.05f));
        }

        [UnityTest]
        public IEnumerator CombatWorldSpriteView_EnemyDeath_WaitsForPlayerAttackEffect()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateGameObject<SpriteRenderer>("EnemySprite");
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var bootstrap = CreateGameObject<PrototypeCombatBootstrap>("Bootstrap");
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 99);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);

            attack.vfxFamily = SkillVfxFamily.BuffAura;
            attack.vfxScale = 1f;
            attack.vfxIntensity = 1f;
            enemyData.portrait = CreateSprite("Enemy");
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);
            yield return new WaitForSecondsRealtime(0.2f);

            Assert.That(enemyRenderer.color.a, Is.EqualTo(1f).Within(0.001f));

            yield return new WaitForSecondsRealtime(CombatWorldSpriteView.EnemyDeathFadeDurationSeconds + 0.9f);

            Assert.That(enemyRenderer.color.a, Is.LessThanOrEqualTo(0.05f));
        }

        [UnityTest]
        public IEnumerator CombatUiView_EnemyDeath_FadesEnemyPortrait()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var enemyPortraitObject = CreateOwnedGameObject("EnemyPortrait");
            enemyPortraitObject.transform.SetParent(viewObject.transform, false);
            var enemyPortrait = enemyPortraitObject.AddComponent<Image>();
            enemyPortrait.color = Color.white;

            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var bootstrap = CreateGameObject<PrototypeCombatBootstrap>("Bootstrap");
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 99);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);

            SetPrivateField(view, "enemyPortrait", enemyPortrait);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(enemyPortrait.color.a, Is.EqualTo(1f).Within(0.001f));

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);
            Assert.That(enemy.IsDead, Is.True);
            Assert.That(manager.GetSnapshot().Enemies[0].IsDead, Is.True);
            yield return new WaitForSecondsRealtime(CombatUiView.EnemyDeathFadeDurationSeconds + 0.1f);
            yield return null;

            Assert.That(enemyPortrait.color.a, Is.LessThanOrEqualTo(0.05f));
        }

        private T CreateGameObject<T>(string name)
            where T : Component
        {
            var gameObject = new GameObject(name);
            ownedObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private GameObject CreateOwnedGameObject(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private TMP_Text CreateTextChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            child.transform.SetParent(parent, false);
            ownedObjects.Add(child);
            return child.GetComponent<TMP_Text>();
        }

        private Button CreateButtonChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(parent, false);
            ownedObjects.Add(child);
            return child.GetComponent<Button>();
        }

        private PlayerSO CreatePlayerData(int maxHp, int attackPower)
        {
            var data = ScriptableObject.CreateInstance<PlayerSO>();
            data.maxHp = maxHp;
            data.attackPower = attackPower;
            ownedObjects.Add(data);
            return data;
        }

        private EnemySO CreateEnemyData(int maxHp, int attackValue)
        {
            var data = ScriptableObject.CreateInstance<EnemySO>();
            data.maxHp = maxHp;
            data.attackPower = attackValue;
            data.intentPattern = new List<EnemyIntent>
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

        private BattleRewardSO CreateReward(float healPercentOfMaxHp, int extraBoardMoveCount)
        {
            var reward = ScriptableObject.CreateInstance<BattleRewardSO>();
            reward.rewardId = "moth-basic";
            reward.mothDisplayName = "나방";
            reward.healPercentOfMaxHp = healPercentOfMaxHp;
            reward.extraBoardMoveCount = extraBoardMoveCount;
            ownedObjects.Add(reward);
            return reward;
        }

        private RewardTableSO CreateRewardTable(BattleRewardSO reward)
        {
            var table = ScriptableObject.CreateInstance<RewardTableSO>();
            table.rewards = new List<BattleRewardSO> { reward };
            ownedObjects.Add(table);
            return table;
        }

        private RewardTableSO CreateRewardTable(params BattleRewardSO[] rewards)
        {
            var table = ScriptableObject.CreateInstance<RewardTableSO>();
            table.rewards = rewards?.ToList() ?? new List<BattleRewardSO>();
            ownedObjects.Add(table);
            return table;
        }

        private BattleRewardSO CreateRewardChoice(
            string rewardId,
            RewardChoiceKind rewardKind,
            SkillSO skillToLearn = null)
        {
            var reward = ScriptableObject.CreateInstance<BattleRewardSO>();
            reward.rewardId = rewardId;
            reward.rewardKind = rewardKind;
            reward.skillToLearn = skillToLearn;
            ownedObjects.Add(reward);
            return reward;
        }

        private static int CountRewardCategories(IEnumerable<BattleRewardSO> rewards)
        {
            return rewards
                .Select(ResolveRewardCategory)
                .Distinct()
                .Count(category => category != 0);
        }

        private static int ResolveRewardCategory(BattleRewardSO reward)
        {
            return reward.rewardKind switch
            {
                RewardChoiceKind.TemporaryAttackPower => 1,
                RewardChoiceKind.TemporaryDefensePower => 1,
                RewardChoiceKind.TemporaryBoardMoveCount => 1,
                RewardChoiceKind.Enhance => 1,
                RewardChoiceKind.HealOne => 2,
                RewardChoiceKind.HealTwo => 2,
                RewardChoiceKind.HealThree => 2,
                RewardChoiceKind.Rest => 2,
                RewardChoiceKind.LearnSkill => 3,
                RewardChoiceKind.PermanentMaxHp => 4,
                RewardChoiceKind.PermanentAttackPower => 4,
                RewardChoiceKind.PermanentDefensePower => 4,
                RewardChoiceKind.PermanentCriticalChance => 4,
                RewardChoiceKind.PermanentCriticalDamageMultiplier => 4,
                _ => 0,
            };
        }

        private SkillSO CreateSkill(string skillId, SkillType skillType, int cost, int power)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.skillId = skillId;
            skill.skillType = skillType;
            skill.cost = cost;
            skill.power = power;
            ownedObjects.Add(skill);
            return skill;
        }

        private Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(2, 2);
            texture.name = $"{name}Texture";
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"{name}Sprite";
            ownedObjects.Add(texture);
            ownedObjects.Add(sprite);
            return sprite;
        }

        private static string AssetGuid(Object asset)
        {
            return asset == null
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(target, null);
        }
    }
}
