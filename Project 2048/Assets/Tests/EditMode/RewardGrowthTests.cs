using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Prototype;
using Project2048.Rewards;
using Project2048.Score;
using Project2048.Skills;
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
            rewardManager.OfferReward(new CombatResult { enemyDifficultyScore = 1 }, player);
            var result = rewardManager.ChooseRest(player);

            Assert.That(result.Kind, Is.EqualTo(RewardChoiceKind.Rest));
            Assert.That(result.AppliedAmount, Is.EqualTo(6));
            Assert.That(player.CurrentHp, Is.EqualTo(16));
            Assert.That(runProgress.CurrentHp, Is.EqualTo(16));
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
            rewardManager.OfferReward(new CombatResult { enemyDifficultyScore = 1 }, player);
            var result = rewardManager.ChooseEnhance(player);

            Assert.That(result.Kind, Is.EqualTo(RewardChoiceKind.Enhance));
            Assert.That(result.AppliedAmount, Is.EqualTo(2));
            Assert.That(runProgress.ExtraBoardMoveCount, Is.EqualTo(2));
            Assert.That(playerData.boardMoveCountBonus, Is.EqualTo(0));
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
            var restButton = CreateButtonChild(rewardOverlay.transform, "RestButton");
            var enhanceButton = CreateButtonChild(rewardOverlay.transform, "EnhanceButton");

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

            var manager = CreateGameObject<CombatManager>("CombatManager");
            var rewardManager = CreateGameObject<RewardManager>("RewardManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var bootstrap = CreateGameObject<PrototypeCombatBootstrap>("Bootstrap");
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 99);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var reward = CreateReward(healPercentOfMaxHp: 0.3f, extraBoardMoveCount: 1);
            var table = CreateRewardTable(reward);
            var runProgress = new RunProgress();

            rewardManager.Initialize(runProgress, table);
            rewardManager.BindCombat(manager);
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

            Assert.That(rewardOverlay.activeSelf, Is.True);
            Assert.That(resultOverlay.activeSelf, Is.False);
            Assert.That(boardPanel.activeSelf, Is.False);
            Assert.That(actionPanel.activeSelf, Is.False);
            Assert.That(rewardTitle.text, Is.EqualTo("나방"));
            Assert.That(restText.text, Is.EqualTo("휴식 : 최대 체력의 30%를 회복합니다"));
            Assert.That(enhanceText.text, Is.EqualTo("강화 : 제한 단수가 1회 증가합니다"));

            enhanceButton.onClick.Invoke();

            Assert.That(rewardOverlay.activeSelf, Is.False);
            Assert.That(resultOverlay.activeSelf, Is.True);
            Assert.That(runProgress.ExtraBoardMoveCount, Is.EqualTo(1));
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
            rewardManager.BindCombat(manager);
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

            Assert.That(defeatRaised, Is.True);
            Assert.That(manager.CurrentPhase, Is.EqualTo(CombatPhase.Defeat));
            Assert.That(player.CurrentHp, Is.EqualTo(0));
            Assert.That(rewardManager.HasPendingReward, Is.False);
            Assert.That(runProgress.CurrentHp, Is.EqualTo(0));
        }

        [Test]
        public void Bootstrap_WithMissingRewardAndScoreSceneReferences_DoesNotCreateRuntimeManagers()
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
            Assert.That(bootstrap.ScoreManager, Is.Null);
            Assert.That(bootstrapObject.transform.Find("RewardManager"), Is.Null);
            Assert.That(bootstrapObject.transform.Find("ScoreManager"), Is.Null);
        }

        [Test]
        public void BattleScene_RewardOverlayAndManagers_AreSceneAuthored()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");

            var view = Object.FindAnyObjectByType<CombatUiView>(FindObjectsInactive.Include);
            var worldSpriteView = Object.FindAnyObjectByType<CombatWorldSpriteView>(FindObjectsInactive.Include);
            var rewardManager = Object.FindAnyObjectByType<RewardManager>(FindObjectsInactive.Include);
            var scoreManager = Object.FindAnyObjectByType<ScoreManager>(FindObjectsInactive.Include);
            var combatCanvas = GameObject.Find("CombatCanvas")?.GetComponent<Canvas>();
            var backgroundSprite = GameObject.Find("BackgroundSprite")?.GetComponent<SpriteRenderer>();
            var playerSprite = GameObject.Find("PlayerSprite")?.GetComponent<SpriteRenderer>();
            var enemySprite = GameObject.Find("EnemySprite")?.GetComponent<SpriteRenderer>();
            var battleSceneBackground = GameObject.Find("BattleScene")?.GetComponent<Image>();
            var playerPortraitImage = GameObject.Find("PlayerPortrait")?.GetComponent<Image>();
            var enemyPortraitImage = GameObject.Find("EnemyPortrait")?.GetComponent<Image>();

            Assert.That(view, Is.Not.Null);
            Assert.That(worldSpriteView, Is.Not.Null);
            Assert.That(rewardManager, Is.Not.Null);
            Assert.That(scoreManager, Is.Not.Null);
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
            Assert.That(battleSceneBackground.enabled, Is.False);
            Assert.That(playerPortraitImage.enabled, Is.False);
            Assert.That(enemyPortraitImage.enabled, Is.False);

            var serializedView = new SerializedObject(view);
            var rewardOverlay = serializedView.FindProperty("rewardOverlay").objectReferenceValue as GameObject;
            var resultOverlay = serializedView.FindProperty("resultOverlay").objectReferenceValue as GameObject;

            Assert.That(serializedView.FindProperty("rewardManager").objectReferenceValue, Is.EqualTo(rewardManager));
            Assert.That(serializedView.FindProperty("scoreManager").objectReferenceValue, Is.EqualTo(scoreManager));
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
