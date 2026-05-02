#if UNITY_EDITOR
using System.Collections.Generic;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Prototype;
using Project2048.Skills;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Project2048.PrototypeEditor
{
    /// <summary>
    /// One-shot scene builder for the prototype combat screen. It creates real
    /// scene GameObjects and ScriptableObject assets, so a UI owner can replace
    /// or edit the layout without touching runtime combat code.
    /// </summary>
    public static class CombatUiBuilder
    {
        private const string KoreanFontAssetPath = "Assets/Fonts/MaruBuri-Regular SDF.asset";
        private const string DataFolder = "Assets/Data/Prototype";
        private const string SkillFolder = DataFolder + "/Skills";
        private const string PlayerSpritePath = "Assets/Art/Prototype/PrototypePlayerCutout.png";
        private const string EnemySpritePath = "Assets/Art/Prototype/PrototypeEnemyCutout.png";

        [MenuItem("Project2048/Generate Combat UI")]
        public static void GenerateCombatUi()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
            var loadout = EnsurePrototypeDataAssets();

            var canvas = EnsureCanvas();
            ClearChild(canvas.transform, "PhoneRoot");
            ClearChild(canvas.transform, "ResultOverlay");

            var view = canvas.GetComponent<CombatUiView>() ?? canvas.gameObject.AddComponent<CombatUiView>();
            var refs = new ViewRefs();

            BuildPhoneLayout(canvas.transform, refs, font);
            BuildResultOverlay(canvas.transform, refs, font);
            BindViewReferences(view, refs);
            EnsurePrototypeEntry(view, loadout.PlayerData, loadout.EnemyData);
            EnsureEventSystem();

            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Combat UI scene, prototype combat data, and bindings generated.");
        }

        private static Canvas EnsureCanvas()
        {
            var canvasObject = GameObject.Find("CombatCanvas");
            if (canvasObject == null)
            {
                canvasObject = new GameObject("CombatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main ?? Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>() ?? canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
                return;
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static PrototypeCombatLoadout EnsurePrototypeDataAssets()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets/Data", "Prototype");
            EnsureFolder(DataFolder, "Skills");

            var attack1 = CreateOrLoadAsset<SkillSO>(SkillFolder + "/Attack_1.asset");
            ConfigureSkill(attack1, "attack_1", "1단계 공격", SkillType.Attack, 5, 3, 0, 0, "기본 공격.");
            var attack2 = CreateOrLoadAsset<SkillSO>(SkillFolder + "/Attack_2.asset");
            ConfigureSkill(attack2, "attack_2", "2단계 공격", SkillType.Attack, 8, 4, -2, 0, "공격하고 적 공격력을 낮춘다.");
            var attack3 = CreateOrLoadAsset<SkillSO>(SkillFolder + "/Attack_3.asset");
            ConfigureSkill(attack3, "attack_3", "3단계 공격", SkillType.Attack, 12, 8, 0, 0, "강한 공격.");
            var defense1 = CreateOrLoadAsset<SkillSO>(SkillFolder + "/Defense_1.asset");
            ConfigureSkill(defense1, "defense_1", "1단계 방어", SkillType.Defense, 5, 3, 0, 0, "방어도 3을 얻는다.");
            var defense2 = CreateOrLoadAsset<SkillSO>(SkillFolder + "/Defense_2.asset");
            ConfigureSkill(defense2, "defense_2", "2단계 방어", SkillType.Defense, 8, 4, 0, 2, "방어도를 얻고 이후 획득 방어도를 증가시킨다.");
            var defense3 = CreateOrLoadAsset<SkillSO>(SkillFolder + "/Defense_3.asset");
            ConfigureSkill(defense3, "defense_3", "3단계 방어", SkillType.Defense, 12, 10, 0, 0, "강한 방어.");

            var skills = new List<SkillSO> { attack1, attack2, attack3, defense1, defense2, defense3 };

            var player = CreateOrLoadAsset<PlayerSO>(DataFolder + "/PrototypePlayer.asset");
            player.maxHp = 30;
            player.attackPower = 2;
            player.boardMoveCountBonus = 0;
            player.startingSkills = new List<SkillSO>(skills);
            player.portrait = LoadSprite(PlayerSpritePath) ?? player.portrait;
            EditorUtility.SetDirty(player);

            var enemy = CreateOrLoadAsset<EnemySO>(DataFolder + "/PrototypeEnemy.asset");
            enemy.enemyName = "그림자 짐승";
            enemy.maxHp = 32;
            enemy.attackPower = 5;
            enemy.defensePower = 3;
            enemy.debuffPower = 1;
            enemy.difficultyScore = 1;
            enemy.portrait = LoadSprite(EnemySpritePath) ?? enemy.portrait;
            enemy.intentPattern = new List<EnemyIntent>();
            enemy.aiActionBias = EnemyAiActionBias.Balanced;
            enemy.aiDebuffPattern = EnemyDebuffPattern.FearThenDarkness;
            enemy.aiStrength = EnemyAiStrength.Normal;
            enemy.aiDebuffInterval = 3;
            EditorUtility.SetDirty(enemy);

            return new PrototypeCombatLoadout(player, enemy, skills, ownsAssets: false);
        }

        private static Sprite LoadSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                // Single-sprite import prevents Unity from auto-slicing transparent cutouts into tiny fragments.
                importer.mipmapEnabled = false;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void ConfigureSkill(
            SkillSO skill,
            string id,
            string displayName,
            SkillType type,
            int cost,
            int power,
            int targetAttackModifier,
            int selfDefenseBonus,
            string description)
        {
            skill.skillId = id;
            skill.skillName = displayName;
            skill.skillType = type;
            skill.cost = cost;
            skill.power = power;
            skill.targetAttackModifier = targetAttackModifier;
            skill.selfDefenseBonus = selfDefenseBonus;
            skill.description = description;
            EditorUtility.SetDirty(skill);
        }

        private static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string parent, string folder)
        {
            var path = parent + "/" + folder;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void BuildPhoneLayout(Transform canvas, ViewRefs refs, TMP_FontAsset font)
        {
            var phone = CreatePanel(canvas, "PhoneRoot", new Color(0.07f, 0.07f, 0.08f, 1f));
            SetStretch(phone.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var battle = CreatePanel(phone.transform, "BattleScene", new Color(0.18f, 0.18f, 0.20f, 1f));
            SetStretch(battle.rectTransform, new Vector2(0, 0.55f), Vector2.one, Vector2.zero, Vector2.zero);
            BuildBattleScene(battle.transform, refs, font);

            var bottom = CreatePanel(phone.transform, "BottomPanel", new Color(0.04f, 0.04f, 0.05f, 1f));
            SetStretch(bottom.rectTransform, Vector2.zero, new Vector2(1, 0.55f), Vector2.zero, Vector2.zero);
            BuildBoardPanel(bottom.transform, refs, font);
            BuildActionPanel(bottom.transform, refs, font);
            BuildEnemyTurnPanel(bottom.transform, refs, font);
            BuildActionLog(phone.transform, refs, font);
        }

        private static void BuildBattleScene(Transform parent, ViewRefs refs, TMP_FontAsset font)
        {
            // Assign scene Image sprites here so the cutout placeholders are visible before entering Play Mode.
            var playerSprite = LoadSprite(PlayerSpritePath);
            var enemySprite = LoadSprite(EnemySpritePath);

            var topBar = CreateRect("TopBar", parent);
            SetStretch(topBar, new Vector2(0, 0.84f), Vector2.one, Vector2.zero, Vector2.zero);

            refs.TurnCounterText = CreateLabel(topBar, "TurnCounterText", "I", 48, TextAlignmentOptions.Center, font);
            SetAnchor(refs.TurnCounterText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(140, 86), Vector2.zero);

            var pause = CreateLabel(topBar, "PauseIcon", "Ⅱ", 44, TextAlignmentOptions.Center, font);
            SetAnchor(pause.rectTransform, new Vector2(0.07f, 0.58f), new Vector2(80, 80), Vector2.zero);

            var settings = CreateLabel(topBar, "SettingsIcon", "S", 44, TextAlignmentOptions.Center, font);
            SetAnchor(settings.rectTransform, new Vector2(0.94f, 0.58f), new Vector2(80, 80), Vector2.zero);

            refs.IntentHeaderText = CreateLabel(topBar, "IntentHeaderText", "적 턴에 할 행동", 34, TextAlignmentOptions.Center, font);
            SetAnchor(refs.IntentHeaderText.rectTransform, new Vector2(0.68f, 0.18f), new Vector2(420, 70), Vector2.zero);

            refs.PlayerPortrait = CreateImage(parent, "PlayerPortrait", Color.white);
            refs.PlayerPortrait.sprite = playerSprite;
            refs.PlayerPortrait.preserveAspect = true;
            refs.PlayerPortrait.raycastTarget = false;
            SetAnchor(refs.PlayerPortrait.rectTransform, new Vector2(0.22f, 0.18f), new Vector2(170, 260), Vector2.zero);

            refs.EnemyPortrait = CreateImage(parent, "EnemyPortrait", Color.white);
            refs.EnemyPortrait.sprite = enemySprite;
            refs.EnemyPortrait.preserveAspect = true;
            refs.EnemyPortrait.raycastTarget = false;
            SetAnchor(refs.EnemyPortrait.rectTransform, new Vector2(0.74f, 0.24f), new Vector2(390, 390), Vector2.zero);

            refs.EnemyNameText = CreateLabel(parent, "EnemyNameText", "그림자 짐승", 32, TextAlignmentOptions.Center, font);
            SetAnchor(refs.EnemyNameText.rectTransform, new Vector2(0.74f, 0.58f), new Vector2(360, 60), Vector2.zero);

            refs.IntentBubble = CreateImage(parent, "IntentBubble", new Color(0.65f, 0.10f, 0.10f, 1f));
            SetAnchor(refs.IntentBubble.rectTransform, new Vector2(0.74f, 0.70f), new Vector2(180, 78), Vector2.zero);
            refs.IntentBubbleText = CreateLabel(refs.IntentBubble.transform, "IntentBubbleText", "공격 5", 28, TextAlignmentOptions.Center, font);
            SetStretch(refs.IntentBubbleText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            CreateStatusBar(parent, "PlayerBattleHp", new Vector2(0.22f, 0.06f), new Vector2(230, 24), new Color(0.06f, 0.18f, 0.08f, 1f), new Color(0.12f, 0.86f, 0.28f, 1f), font, out refs.PlayerBattleHpFill, out refs.PlayerBattleHpText);
            CreateStatusBar(parent, "EnemyHp", new Vector2(0.74f, 0.06f), new Vector2(340, 26), new Color(0.20f, 0.04f, 0.04f, 1f), new Color(0.92f, 0.12f, 0.12f, 1f), font, out refs.EnemyHpFill, out refs.EnemyHpText);

            var strike = CreateLabel(parent, "PrototypeVfxText", "*", 76, TextAlignmentOptions.Center, font);
            strike.color = new Color(1f, 0.92f, 0.30f, 1f);
            SetAnchor(strike.rectTransform, new Vector2(0.49f, 0.30f), new Vector2(120, 120), Vector2.zero);
        }

        private static void BuildActionLog(Transform parent, ViewRefs refs, TMP_FontAsset font)
        {
            var actionBg = CreateImage(parent, "ActionDescriptionBg", new Color(0.02f, 0.02f, 0.025f, 0.88f));
            actionBg.raycastTarget = false;
            SetAnchor(actionBg.rectTransform, new Vector2(0.50f, 0.035f), new Vector2(820, 58), Vector2.zero);
            refs.ActionDescriptionText = CreateLabel(actionBg.transform, "ActionDescriptionText", "최근 행동: 2048 진행", 24, TextAlignmentOptions.Center, font);
            SetStretch(refs.ActionDescriptionText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-18, 0));
        }

        private static void BuildBoardPanel(Transform bottom, ViewRefs refs, TMP_FontAsset font)
        {
            refs.BoardPanel = CreatePanel(bottom, "BoardPanel", new Color(0.04f, 0.04f, 0.05f, 1f)).gameObject;
            var boardRect = refs.BoardPanel.GetComponent<RectTransform>();
            SetStretch(boardRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var hpBg = CreateImage(boardRect, "HpBarBg", new Color(0.20f, 0.06f, 0.06f, 1f));
            SetAnchor(hpBg.rectTransform, new Vector2(0.27f, 0.92f), new Vector2(360, 28), Vector2.zero);
            refs.HpBarFill = CreateImage(hpBg.transform, "HpBarFill", new Color(0.12f, 0.86f, 0.28f, 1f));
            refs.HpBarFill.type = Image.Type.Filled;
            refs.HpBarFill.fillMethod = Image.FillMethod.Horizontal;
            refs.HpBarFill.fillOrigin = 0;
            SetStretch(refs.HpBarFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            refs.HpText = CreateLabel(boardRect, "HpText", "체력 30/30", 24, TextAlignmentOptions.Left, font);
            SetAnchor(refs.HpText.rectTransform, new Vector2(0.25f, 0.96f), new Vector2(360, 42), Vector2.zero);

            refs.TurnLimitText = CreateLabel(boardRect, "TurnLimitText", "제한 턴 : 12회", 30, TextAlignmentOptions.Right, font);
            SetAnchor(refs.TurnLimitText.rectTransform, new Vector2(0.76f, 0.91f), new Vector2(360, 50), Vector2.zero);

            var boardTitle = CreateLabel(boardRect, "BoardPhaseTitle", "내 턴", 54, TextAlignmentOptions.Left, font);
            boardTitle.fontStyle = FontStyles.Bold;
            SetAnchor(boardTitle.rectTransform, new Vector2(0.22f, 0.82f), new Vector2(300, 74), Vector2.zero);

            var grid = CreateRect("BoardGrid", boardRect);
            SetAnchor(grid, new Vector2(0.5f, 0.40f), new Vector2(620, 620), Vector2.zero);
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
            gridLayout.cellSize = new Vector2(140, 140);
            gridLayout.spacing = new Vector2(16, 16);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            FillBoardGrid(grid, refs, font);

            refs.BoardAnimationOverlay = CreateRect("BoardAnimationOverlay", boardRect);
            SetStretch(refs.BoardAnimationOverlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var swipeArea = CreateImage(boardRect, "SwipeArea", new Color(0, 0, 0, 0));
            swipeArea.raycastTarget = true;
            SetStretch(swipeArea.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            refs.SwipeHandler = swipeArea.gameObject.AddComponent<BoardSwipeHandler>();
            swipeArea.transform.SetAsLastSibling();
        }

        private static void FillBoardGrid(RectTransform grid, ViewRefs refs, TMP_FontAsset font)
        {
            for (var i = 0; i < 16; i++)
            {
                var cellImage = CreateImage(grid, $"Cell_{i:00}", new Color(0.10f, 0.10f, 0.10f, 1f));
                cellImage.raycastTarget = false;
                var view = cellImage.gameObject.AddComponent<BoardCellView>();

                var value = CreateLabel(cellImage.transform, "Value", string.Empty, 40, TextAlignmentOptions.Center, font);
                value.fontStyle = FontStyles.Bold;
                SetStretch(value.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                var so = new SerializedObject(view);
                so.FindProperty("background").objectReferenceValue = cellImage;
                so.FindProperty("valueText").objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();

                refs.Cells.Add(view);
            }
        }

        private static void BuildActionPanel(Transform bottom, ViewRefs refs, TMP_FontAsset font)
        {
            refs.ActionPanel = CreatePanel(bottom, "ActionPanel", new Color(0.04f, 0.04f, 0.05f, 1f)).gameObject;
            SetStretch(refs.ActionPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            refs.ActionPanel.SetActive(false);

            refs.CostText = CreateLabel(refs.ActionPanel.transform, "CostText", "보유 코스트: 0", 32, TextAlignmentOptions.Right, font);
            SetAnchor(refs.CostText.rectTransform, new Vector2(0.74f, 0.88f), new Vector2(440, 58), Vector2.zero);

            refs.CategoryView = CreateVerticalGroup(refs.ActionPanel.transform, "CategoryView", new Vector2(0.48f, 0.48f), new Vector2(700, 500), 30);
            refs.AttackCategory = CreateLabeledButton(refs.CategoryView.transform, "AttackButton", "공격", font);
            refs.DefenseCategory = CreateLabeledButton(refs.CategoryView.transform, "DefenseButton", "방어", font);
            refs.CategoryEndTurn = CreateLabeledButton(refs.CategoryView.transform, "EndTurnButton", "턴 종료", font);

            refs.SkillsView = CreateVerticalGroup(refs.ActionPanel.transform, "SkillsView", new Vector2(0.5f, 0.48f), new Vector2(760, 660), 18);
            refs.SkillsView.SetActive(false);
            refs.SkillsHeaderText = CreateLabel(refs.SkillsView.transform, "SkillsHeader", "공격 스킬 선택", 32, TextAlignmentOptions.Center, font);
            refs.SkillsHeaderText.rectTransform.sizeDelta = new Vector2(0, 58);

            refs.Tier1 = CreateLabeledButton(refs.SkillsView.transform, "Tier1Button", "1단계", font, out refs.Tier1Label);
            refs.Tier2 = CreateLabeledButton(refs.SkillsView.transform, "Tier2Button", "2단계", font, out refs.Tier2Label);
            refs.Tier3 = CreateLabeledButton(refs.SkillsView.transform, "Tier3Button", "3단계", font, out refs.Tier3Label);
            refs.SkillsBack = CreateLabeledButton(refs.SkillsView.transform, "BackButton", "뒤로", font);
            refs.SkillsEndTurn = CreateLabeledButton(refs.SkillsView.transform, "EndTurnButton", "턴 종료", font);
        }

        private static void BuildEnemyTurnPanel(Transform bottom, ViewRefs refs, TMP_FontAsset font)
        {
            refs.EnemyTurnPanel = CreatePanel(bottom, "EnemyTurnPanel", new Color(0.04f, 0.04f, 0.05f, 1f)).gameObject;
            SetStretch(refs.EnemyTurnPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            refs.EnemyTurnPanel.SetActive(false);

            refs.EnemyTurnText = CreateLabel(refs.EnemyTurnPanel.transform, "EnemyTurnText", "적 턴", 104, TextAlignmentOptions.Center, font);
            refs.EnemyTurnText.fontStyle = FontStyles.Bold;
            SetStretch(refs.EnemyTurnText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void BuildResultOverlay(Transform canvas, ViewRefs refs, TMP_FontAsset font)
        {
            refs.ResultOverlay = CreatePanel(canvas, "ResultOverlay", new Color(0, 0, 0, 0.78f)).gameObject;
            SetStretch(refs.ResultOverlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            refs.ResultOverlay.SetActive(false);

            var card = CreatePanel(refs.ResultOverlay.transform, "Card", new Color(0.12f, 0.12f, 0.14f, 1f));
            SetAnchor(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(740, 620), Vector2.zero);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 48, 48);
            layout.spacing = 26;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            refs.ResultTitle = CreateLabel(card.transform, "Title", "클리어!", 74, TextAlignmentOptions.Center, font);
            refs.ResultTitle.fontStyle = FontStyles.Bold;
            refs.ResultTitle.rectTransform.sizeDelta = new Vector2(0, 108);

            refs.ResultDesc = CreateLabel(card.transform, "Description", "얻은 스코어 :", 34, TextAlignmentOptions.Center, font);
            refs.ResultDesc.rectTransform.sizeDelta = new Vector2(0, 74);

            refs.Restart = CreateLabeledButton(card.transform, "RestartButton", "이어 하기", font);
            refs.ReloadScene = CreateLabeledButton(card.transform, "ReloadSceneButton", "종료", font);
        }

        private static void EnsurePrototypeEntry(CombatUiView view, PlayerSO playerData, EnemySO enemyData)
        {
            var root = GameObject.Find("PrototypeCombatEntry");
            if (root == null)
            {
                root = new GameObject("PrototypeCombatEntry");
            }

            var bootstrap = root.GetComponent<PrototypeCombatBootstrap>() ?? root.AddComponent<PrototypeCombatBootstrap>();
            var manager = EnsureChildComponent<CombatManager>(root.transform, "CombatManager");
            var player = EnsureChildComponent<PlayerCombatController>(root.transform, "Player");
            var enemy = EnsureChildComponent<EnemyController>(root.transform, "Enemy");

            var so = new SerializedObject(bootstrap);
            SetRef(so, "combatManager", manager);
            SetRef(so, "playerController", player);
            SetRef(so, "enemyController", enemy);
            SetRef(so, "combatUiView", view);
            SetRef(so, "playerData", playerData);
            SetRef(so, "enemyData", enemyData);
            var randomizeEnemy = so.FindProperty("randomizeEnemyOnStart");
            if (randomizeEnemy != null)
            {
                randomizeEnemy.boolValue = true;
            }

            so.FindProperty("boardMoveCount").intValue = 12;
            so.FindProperty("autoStartOnPlay").boolValue = true;
            so.FindProperty("enemyTurnDelaySeconds").floatValue = 1.2f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T EnsureChildComponent<T>(Transform parent, string name) where T : Component
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            return child.GetComponent<T>() ?? child.gameObject.AddComponent<T>();
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image CreatePanel(Transform parent, string name, Color color)
        {
            return CreateImage(parent, name, color);
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (font != null)
            {
                label.font = font;
            }

            return label;
        }

        private static GameObject CreateVerticalGroup(Transform parent, string name, Vector2 anchor, Vector2 size, float spacing)
        {
            var rect = CreateRect(name, parent);
            SetAnchor(rect, anchor, size, Vector2.zero);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;
            return rect.gameObject;
        }

        private static Button CreateLabeledButton(Transform parent, string name, string label, TMP_FontAsset font)
        {
            return CreateLabeledButton(parent, name, label, font, out _);
        }

        private static Button CreateLabeledButton(Transform parent, string name, string label, TMP_FontAsset font, out TMP_Text labelText)
        {
            var image = CreateImage(parent, name, new Color(0.18f, 0.18f, 0.22f, 1f));
            var button = image.gameObject.AddComponent<Button>();
            var layout = image.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 86;
            layout.preferredHeight = 98;

            labelText = CreateLabel(image.transform, "Label", label, 30, TextAlignmentOptions.Center, font);
            labelText.fontStyle = FontStyles.Bold;
            SetStretch(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void CreateStatusBar(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 size,
            Color bgColor,
            Color fillColor,
            TMP_FontAsset font,
            out Image fill,
            out TMP_Text text)
        {
            var bg = CreateImage(parent, name, bgColor);
            bg.raycastTarget = false;
            SetAnchor(bg.rectTransform, anchor, size, Vector2.zero);
            fill = CreateImage(bg.transform, "Fill", fillColor);
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            SetStretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            text = CreateLabel(bg.transform, "Text", "체력 0/0", 18, TextAlignmentOptions.Center, font);
            text.fontStyle = FontStyles.Bold;
            SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void SetStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void SetAnchor(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void ClearChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void BindViewReferences(CombatUiView view, ViewRefs refs)
        {
            var so = new SerializedObject(view);
            SetRef(so, "turnCounterText", refs.TurnCounterText);
            SetRef(so, "intentHeaderText", refs.IntentHeaderText);
            SetRef(so, "playerPortrait", refs.PlayerPortrait);
            SetRef(so, "enemyPortrait", refs.EnemyPortrait);
            SetRef(so, "enemyNameText", refs.EnemyNameText);
            SetRef(so, "intentBubble", refs.IntentBubble != null ? refs.IntentBubble.gameObject : null);
            SetRef(so, "intentBubbleText", refs.IntentBubbleText);
            SetRef(so, "playerBattleHpBarFill", refs.PlayerBattleHpFill);
            SetRef(so, "playerBattleHpText", refs.PlayerBattleHpText);
            SetRef(so, "enemyHpBarFill", refs.EnemyHpFill);
            SetRef(so, "enemyHpText", refs.EnemyHpText);
            SetRef(so, "actionDescriptionText", refs.ActionDescriptionText);
            SetRef(so, "boardPanel", refs.BoardPanel);
            SetRef(so, "actionPanel", refs.ActionPanel);
            SetRef(so, "enemyTurnPanel", refs.EnemyTurnPanel);
            SetRef(so, "hpBarFill", refs.HpBarFill);
            SetRef(so, "hpText", refs.HpText);
            SetRef(so, "turnLimitText", refs.TurnLimitText);
            SetListRef(so, "boardCells", refs.Cells);
            SetRef(so, "boardSwipeHandler", refs.SwipeHandler);
            SetRef(so, "boardAnimationOverlay", refs.BoardAnimationOverlay);
            SetRef(so, "costText", refs.CostText);
            SetRef(so, "categoryView", refs.CategoryView);
            SetRef(so, "attackCategoryButton", refs.AttackCategory);
            SetRef(so, "defenseCategoryButton", refs.DefenseCategory);
            SetRef(so, "categoryEndTurnButton", refs.CategoryEndTurn);
            SetRef(so, "skillsView", refs.SkillsView);
            SetRef(so, "skillsHeaderText", refs.SkillsHeaderText);
            SetListRef(so, "skillTierButtons", new List<Button> { refs.Tier1, refs.Tier2, refs.Tier3 });
            SetListRef(so, "skillTierLabels", new List<TMP_Text> { refs.Tier1Label, refs.Tier2Label, refs.Tier3Label });
            SetRef(so, "skillsBackButton", refs.SkillsBack);
            SetRef(so, "skillsEndTurnButton", refs.SkillsEndTurn);
            SetRef(so, "enemyTurnText", refs.EnemyTurnText);
            SetRef(so, "resultOverlay", refs.ResultOverlay);
            SetRef(so, "resultTitleText", refs.ResultTitle);
            SetRef(so, "resultDescriptionText", refs.ResultDesc);
            SetRef(so, "restartButton", refs.Restart);
            SetRef(so, "reloadSceneButton", refs.ReloadScene);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRef(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void SetListRef<T>(SerializedObject so, string propName, IList<T> values) where T : Object
        {
            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                return;
            }

            prop.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private sealed class ViewRefs
        {
            public readonly List<BoardCellView> Cells = new();
            public BoardSwipeHandler SwipeHandler;
            public GameObject ActionPanel, CategoryView, SkillsView, EnemyTurnPanel, ResultOverlay, BoardPanel;
            public TMP_Text CostText, SkillsHeaderText, EnemyTurnText, ResultTitle, ResultDesc;
            public Button AttackCategory, DefenseCategory, CategoryEndTurn;
            public Button Tier1, Tier2, Tier3, SkillsBack, SkillsEndTurn, Restart, ReloadScene;
            public TMP_Text Tier1Label, Tier2Label, Tier3Label;
            public TMP_Text TurnCounterText, IntentHeaderText, EnemyNameText, IntentBubbleText;
            public TMP_Text HpText, TurnLimitText, PlayerBattleHpText, EnemyHpText, ActionDescriptionText;
            public Image PlayerPortrait, EnemyPortrait, IntentBubble, HpBarFill, PlayerBattleHpFill, EnemyHpFill;
            public RectTransform BoardAnimationOverlay;
        }
    }
}
#endif
