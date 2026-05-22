#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
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
        private const string DataFolder = "Assets/Data";
        private const string EnemyFolder = DataFolder + "/Enemies";
        private const string SkillFolder = DataFolder + "/Skills";
        private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
        private const string PlayerSpritePath = "Assets/Art/Prototype/PrototypePlayerCutout.png";
        private static readonly Vector2 SkillSlotSize = new(340f, 170f);
        private const string EnemySpritePath = "Assets/Art/Prototype/PrototypeEnemyCutout.png";
        private const string HpBarSpritePath = "Assets/Art/UI/WideHexHpBar.png";
        private const string HpBarOutlineSpritePath = "Assets/Art/UI/WideHexHpBarOutline.png";
        private const int HpBarSpriteWidth = 1024;
        private const int HpBarSpriteHeight = 256;
        private const int HpBarSpriteSamplesPerAxis = 4;
        private const float HpBarBorderThickness = 2.75f;
        private const float HpBarPointLengthPixels = 72f;
        private const float HpBarOutlineThicknessPixels = 24f;

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

        [MenuItem("Project2048/Ensure Combat Status Effect Roots")]
        public static void EnsureCombatStatusEffectRoots()
        {
            var scene = EditorSceneManager.OpenScene(BattleScenePath);
            var view = Object.FindAnyObjectByType<CombatUiView>(FindObjectsInactive.Include);
            if (view == null)
            {
                Debug.LogWarning("CombatUiView was not found in BattleScene.");
                return;
            }

            var so = new SerializedObject(view);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
            var playerBattleHpFill = so.FindProperty("playerBattleHpBarFill")?.objectReferenceValue as Image;
            var playerBattleHpRoot = playerBattleHpFill != null
                ? playerBattleHpFill.transform.parent as RectTransform
                : GameObject.Find("PlayerBattleHp")?.GetComponent<RectTransform>();
            if (playerBattleHpRoot == null)
            {
                Debug.LogWarning("PlayerBattleHp root was not found in BattleScene.");
                return;
            }

            var playerStatusRoot = EnsureStatusEffectAuthoringRoot(
                playerBattleHpRoot,
                "PlayerBattleStatusEffects",
                new Vector2(CombatUiView.HpStatusEffectXOffset, -39f));
            EnsureHpBarAuthoring(playerBattleHpRoot, playerBattleHpFill, CombatUiView.ThemeHpFillColor, CombatUiView.ThemeHpBarBackgroundColor);
            EnsureBlockIconAuthoring(playerBattleHpRoot, font);
            EnsureHpTextAuthoring(
                FindHpText(playerBattleHpRoot) ?? so.FindProperty("playerBattleHpText")?.objectReferenceValue as TMP_Text,
                "0/0");
            SetRef(so, "playerBattleStatusEffectsRoot", playerStatusRoot);

            var playerBoardHpFill = so.FindProperty("hpBarFill")?.objectReferenceValue as Image;
            var playerBoardHpRoot = playerBoardHpFill != null
                ? playerBoardHpFill.transform.parent as RectTransform
                : GameObject.Find("HpBarBg")?.GetComponent<RectTransform>();
            if (playerBoardHpRoot == null)
            {
                Debug.LogWarning("HpBarBg root was not found in BattleScene.");
            }
            else
            {
                var playerBoardStatusRoot = EnsureStatusEffectAuthoringRoot(
                    playerBoardHpRoot,
                    "PlayerBoardStatusEffects",
                    new Vector2(CombatUiView.HpStatusEffectXOffset, -6f));
                EnsureHpBarAuthoring(playerBoardHpRoot, playerBoardHpFill, CombatUiView.ThemeHpFillColor, CombatUiView.ThemeHpBarBackgroundColor);
                EnsureBlockIconAuthoring(playerBoardHpRoot, font);
                EnsureHpTextAuthoring(so.FindProperty("hpText")?.objectReferenceValue as TMP_Text, "30/30");
                SetRef(so, "playerBoardStatusEffectsRoot", playerBoardStatusRoot);
            }

            var enemyHpFill = so.FindProperty("enemyHpBarFill")?.objectReferenceValue as Image;
            var enemyHpRoot = enemyHpFill != null
                ? enemyHpFill.transform.parent as RectTransform
                : GameObject.Find("EnemyHp")?.GetComponent<RectTransform>();
            if (enemyHpRoot == null)
            {
                Debug.LogWarning("EnemyHp root was not found in BattleScene.");
            }
            else
            {
                var enemyStatusRoot = EnsureStatusEffectAuthoringRoot(
                    enemyHpRoot,
                    "EnemyStatusEffects",
                    new Vector2(CombatUiView.HpStatusEffectXOffset, -6f));
                EnsureHpBarAuthoring(enemyHpRoot, enemyHpFill, CombatUiView.ThemeHpFillColor, CombatUiView.ThemeHpBarBackgroundColor);
                EnsureBlockIconAuthoring(enemyHpRoot, font);
                EnsureHpTextAuthoring(
                    FindHpText(enemyHpRoot) ?? so.FindProperty("enemyHpText")?.objectReferenceValue as TMP_Text,
                    "0/0");
                SetRef(so, "enemyStatusEffectsRoot", enemyStatusRoot);
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(view);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Combat status effect roots ensured.");
        }

        [MenuItem("Project2048/Ensure Pokemon Skill Panel")]
        public static void EnsurePokemonSkillPanel()
        {
            var scene = EditorSceneManager.OpenScene(BattleScenePath);
            var view = Object.FindAnyObjectByType<CombatUiView>(FindObjectsInactive.Include);
            if (view == null)
            {
                Debug.LogWarning("CombatUiView was not found in BattleScene.");
                return;
            }

            var so = new SerializedObject(view);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
            var actionPanel = so.FindProperty("actionPanel")?.objectReferenceValue as GameObject
                ?? GameObject.Find("ActionPanel");
            var boardPanel = so.FindProperty("boardPanel")?.objectReferenceValue as GameObject
                ?? GameObject.Find("BoardPanel");
            if (actionPanel == null || boardPanel == null)
            {
                Debug.LogWarning("ActionPanel or BoardPanel was not found in BattleScene.");
                return;
            }

            var skillsView = so.FindProperty("skillsView")?.objectReferenceValue as GameObject
                ?? actionPanel.transform.Find("SkillsView")?.gameObject
                ?? CreateRect("SkillsView", actionPanel.transform).gameObject;
            var skillsRect = skillsView.GetComponent<RectTransform>();
            SetAnchor(skillsRect, new Vector2(0.5f, 0.48f), new Vector2(820f, 560f), Vector2.zero);
            skillsView.SetActive(false);

            var layout = skillsView.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                Object.DestroyImmediate(layout);
            }

            var header = so.FindProperty("skillsHeaderText")?.objectReferenceValue as TMP_Text
                ?? skillsView.transform.Find("SkillsHeader")?.GetComponent<TMP_Text>()
                ?? CreateLabel(skillsView.transform, "SkillsHeader", "Skill Select", 28f, TextAlignmentOptions.Center, font);
            header.text = "Skill Select";
            header.fontStyle = FontStyles.Bold;
            SetAnchor(header.rectTransform, new Vector2(0.5f, 0.92f), new Vector2(520f, 48f), Vector2.zero);

            var buttons = new List<Button>();
            var labels = new List<TMP_Text>();
            var existingButtons = so.FindProperty("skillTierButtons");
            var existingLabels = so.FindProperty("skillTierLabels");
            var positions = new[]
            {
                new Vector2(-178f, 94f),
                new Vector2(178f, 94f),
                new Vector2(-178f, -94f),
                new Vector2(178f, -94f),
            };

            for (var index = 0; index < PlayerCombatController.MaxEquippedSkillSlots; index++)
            {
                var button = existingButtons != null && index < existingButtons.arraySize
                    ? existingButtons.GetArrayElementAtIndex(index).objectReferenceValue as Button
                    : null;
                button ??= skillsView.transform.Find($"SkillSlotButton_{index + 1}")?.GetComponent<Button>();
                button ??= CreateImage(skillsView.transform, $"SkillSlotButton_{index + 1}", new Color(0.10f, 0.36f, 0.18f, 1f)).gameObject.AddComponent<Button>();

                ConfigureSkillSlotButton(button, index, positions[index], font);
                buttons.Add(button);

                var label = existingLabels != null && index < existingLabels.arraySize
                    ? existingLabels.GetArrayElementAtIndex(index).objectReferenceValue as TMP_Text
                    : null;
                label ??= button.transform.Find("Label")?.GetComponent<TMP_Text>();
                label ??= CreateLabel(button.transform, "Label", string.Empty, 23f, TextAlignmentOptions.MidlineLeft, font);
                ConfigureSkillSlotLabel(label, font);
                labels.Add(label);
            }

            var costIcon = EnsureCostFormulaHelpIcon(
                actionPanel.transform,
                "CostFormulaHelpIcon",
                new Vector2(0.94f, 0.88f),
                font);
            var boardCostIcon = EnsureCostFormulaHelpIcon(
                boardPanel.transform,
                "BoardCostFormulaHelpIcon",
                new Vector2(0.06f, 0.91f),
                font);

            SetRef(so, "skillsView", skillsView);
            SetRef(so, "skillsHeaderText", header);
            SetListRef(so, "skillTierButtons", buttons);
            SetListRef(so, "skillTierLabels", labels);
            SetRef(so, "costFormulaHelpIcon", costIcon);
            SetRef(so, "costFormulaHelpLabel", costIcon.transform.Find("Label")?.GetComponent<TMP_Text>());
            SetRef(so, "boardCostFormulaHelpIcon", boardCostIcon);
            SetRef(so, "boardCostFormulaHelpLabel", boardCostIcon.transform.Find("Label")?.GetComponent<TMP_Text>());
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Pokemon-style skill panel and board help icon ensured.");
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
            EnsureFolder(DataFolder, "Enemies");
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
            player.maxHp = 100;
            player.attackPower = 2;
            player.initialBoardMoveCount = 12;
            player.boardMoveCountBonus = 0;
            player.startingSkills = new List<SkillSO>(skills);
            player.portrait = LoadSprite(PlayerSpritePath) ?? player.portrait;
            EditorUtility.SetDirty(player);

            var enemy = CreateOrLoadAsset<EnemySO>(EnemyFolder + "/01.asset");
            enemy.name = "그림자 늑대";
            enemy.enemyName = "그림자 늑대";
            enemy.maxHp = 32;
            enemy.attackPower = 5;
            enemy.defensePower = 3;
            enemy.debuffPower = 1;
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

            refs.EnemyNameText = CreateLabel(parent, "EnemyNameText", "그림자 늑대", 32, TextAlignmentOptions.Center, font);
            SetAnchor(refs.EnemyNameText.rectTransform, new Vector2(0.74f, 0.58f), new Vector2(360, 60), Vector2.zero);

            refs.IntentBubble = CreateImage(parent, "IntentBubble", new Color(0.65f, 0.10f, 0.10f, 1f));
            SetAnchor(
                refs.IntentBubble.rectTransform,
                new Vector2(0.74f, 0.70f),
                new Vector2(CombatUiView.IntentBubbleSquareSize, CombatUiView.IntentBubbleSquareSize),
                Vector2.zero);
            refs.IntentBubbleText = CreateLabel(refs.IntentBubble.transform, "IntentBubbleText", "공격 5", 26, TextAlignmentOptions.Center, font);
            refs.IntentBubbleText.enableAutoSizing = true;
            refs.IntentBubbleText.fontSizeMin = 14f;
            refs.IntentBubbleText.fontSizeMax = 26f;
            refs.IntentBubbleText.textWrappingMode = TextWrappingModes.Normal;
            refs.IntentBubbleText.overflowMode = TextOverflowModes.Ellipsis;
            SetStretch(refs.IntentBubbleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));

            CreateStatusBar(parent, "PlayerBattleHp", new Vector2(0.22f, 0.06f), new Vector2(300, 20), CombatUiView.ThemeHpBarBackgroundColor, CombatUiView.ThemeHpFillColor, font, out refs.PlayerBattleHpFill, out refs.PlayerBattleHpText);
            refs.PlayerBattleStatusEffectsRoot = EnsureStatusEffectAuthoringRoot(
                refs.PlayerBattleHpFill.transform.parent as RectTransform,
                "PlayerBattleStatusEffects",
                new Vector2(CombatUiView.HpStatusEffectXOffset, -39f));
            EnsureBlockIconAuthoring(refs.PlayerBattleHpFill.transform.parent as RectTransform, font);
            CreateStatusBar(parent, "EnemyHp", new Vector2(0.74f, 0.06f), new Vector2(420, 20), CombatUiView.ThemeHpBarBackgroundColor, CombatUiView.ThemeHpFillColor, font, out refs.EnemyHpFill, out refs.EnemyHpText);
            refs.EnemyStatusEffectsRoot = EnsureStatusEffectAuthoringRoot(
                refs.EnemyHpFill.transform.parent as RectTransform,
                "EnemyStatusEffects",
                new Vector2(CombatUiView.HpStatusEffectXOffset, -6f));
            EnsureBlockIconAuthoring(refs.EnemyHpFill.transform.parent as RectTransform, font);

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

            var hpBg = CreateImage(boardRect, "HpBarBg", CombatUiView.ThemeHpBorderColor);
            SetAnchor(hpBg.rectTransform, new Vector2(0.27f, 0.92f), new Vector2(420, 22), Vector2.zero);
            var hpSprite = EnsureHpBarSpriteAsset();
            var hpOutlineSprite = EnsureHpBarOutlineSpriteAsset();
            ConfigureHpBarRootImage(hpBg);
            var hpInterior = CreateImage(hpBg.transform, "HpBarInterior", CombatUiView.ThemeHpBarBackgroundColor);
            ConfigureHpBarSimpleImage(hpInterior, hpSprite, CombatUiView.ThemeHpBarBackgroundColor);
            refs.HpBarFill = CreateImage(hpBg.transform, "HpBarFill", CombatUiView.ThemeHpFillColor);
            ConfigureHpBarFilledImage(refs.HpBarFill, hpSprite, CombatUiView.ThemeHpFillColor);
            EnsureHpBarFeedbackImage(hpBg.rectTransform, "DamageTrailFill", hpSprite, CombatUiView.ThemeHpDamageTrailColor, filled: true);
            EnsureHpBarFeedbackImage(hpBg.rectTransform, "DamageFlashFill", hpSprite, new Color(1f, 1f, 1f, 0.95f), filled: false);
            EnsureHpBarOutlineImage(hpBg.rectTransform, hpOutlineSprite);
            refs.PlayerBoardStatusEffectsRoot = EnsureStatusEffectAuthoringRoot(
                hpBg.rectTransform,
                "PlayerBoardStatusEffects",
                new Vector2(CombatUiView.HpStatusEffectXOffset, -6f));
            EnsureBlockIconAuthoring(hpBg.rectTransform, font);

            refs.HpText = CreateLabel(boardRect, "HpText", "30/30", 24, TextAlignmentOptions.Left, font);
            ConfigureHpTextAuthoring(refs.HpText);
            SetAnchor(refs.HpText.rectTransform, new Vector2(0.25f, 0.96f), new Vector2(360, 42), Vector2.zero);

            refs.TurnLimitText = CreateLabel(boardRect, "TurnLimitText", "제한 턴 : 12회", 30, TextAlignmentOptions.Right, font);
            SetAnchor(refs.TurnLimitText.rectTransform, new Vector2(0.76f, 0.91f), new Vector2(360, 50), Vector2.zero);
            CreateCostFormulaHelpIcon(
                boardRect,
                "BoardCostFormulaHelpIcon",
                new Vector2(0.06f, 0.91f),
                font,
                out refs.BoardCostFormulaHelpIcon,
                out refs.BoardCostFormulaHelpLabel);

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
            CreateCostFormulaHelpIcon(
                refs.ActionPanel.transform,
                "CostFormulaHelpIcon",
                new Vector2(0.94f, 0.88f),
                font,
                out refs.CostFormulaHelpIcon,
                out refs.CostFormulaHelpLabel);

            refs.CategoryView = CreateVerticalGroup(refs.ActionPanel.transform, "CategoryView", new Vector2(0.48f, 0.48f), new Vector2(700, 500), 30);
            refs.AttackCategory = CreateLabeledButton(refs.CategoryView.transform, "AttackButton", "공격", font);
            refs.DefenseCategory = CreateLabeledButton(refs.CategoryView.transform, "DefenseButton", "방어", font);
            refs.CategoryEndTurn = CreateLabeledButton(refs.CategoryView.transform, "EndTurnButton", "턴 종료", font);
            refs.CategoryView.SetActive(false);

            refs.SkillsView = CreateVerticalGroup(refs.ActionPanel.transform, "SkillsView", new Vector2(0.5f, 0.48f), new Vector2(760, 660), 18);
            refs.SkillsView.SetActive(false);
            refs.SkillsHeaderText = CreateLabel(refs.SkillsView.transform, "SkillsHeader", "공격 스킬 선택", 32, TextAlignmentOptions.Center, font);
            refs.SkillsHeaderText.rectTransform.sizeDelta = new Vector2(0, 58);

            refs.Tier1 = CreateLabeledButton(refs.SkillsView.transform, "Tier1Button", "1단계", font, out refs.Tier1Label);
            refs.Tier2 = CreateLabeledButton(refs.SkillsView.transform, "Tier2Button", "2단계", font, out refs.Tier2Label);
            refs.Tier3 = CreateLabeledButton(refs.SkillsView.transform, "Tier3Button", "3단계", font, out refs.Tier3Label);
            refs.Tier4 = CreateLabeledButton(refs.SkillsView.transform, "SkillSlotButton_4", "4단계", font, out refs.Tier4Label);
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

        private static void CreateCostFormulaHelpIcon(
            Transform parent,
            string name,
            Vector2 anchor,
            TMP_FontAsset font,
            out GameObject iconObject,
            out TMP_Text label)
        {
            var icon = CreateImage(parent, name, new Color(0.06f, 0.07f, 0.08f, 0.94f));
            icon.raycastTarget = true;
            SetAnchor(icon.rectTransform, anchor, new Vector2(46f, 46f), Vector2.zero);

            var outline = icon.gameObject.AddComponent<Outline>();
            outline.effectColor = CombatUiView.ThemeBoardHelpOutlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            label = CreateLabel(icon.transform, "Label", "?", 28f, TextAlignmentOptions.Center, font);
            label.color = CombatUiView.ThemeBoardHelpIconColor;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            SetStretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            iconObject = icon.gameObject;
        }

        private static GameObject EnsureCostFormulaHelpIcon(
            Transform parent,
            string name,
            Vector2 anchor,
            TMP_FontAsset font)
        {
            var icon = parent.Find(name)?.gameObject;
            TMP_Text label;
            if (icon == null)
            {
                CreateCostFormulaHelpIcon(parent, name, anchor, font, out icon, out label);
                return icon;
            }

            var rect = icon.GetComponent<RectTransform>() ?? icon.AddComponent<RectTransform>();
            SetAnchor(rect, anchor, new Vector2(46f, 46f), Vector2.zero);

            var image = icon.GetComponent<Image>() ?? icon.AddComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.08f, 0.94f);
            image.raycastTarget = true;

            var outline = icon.GetComponent<Outline>() ?? icon.AddComponent<Outline>();
            outline.effectColor = CombatUiView.ThemeBoardHelpOutlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            label = icon.transform.Find("Label")?.GetComponent<TMP_Text>()
                ?? CreateLabel(icon.transform, "Label", "?", 28f, TextAlignmentOptions.Center, font);
            label.text = "?";
            label.color = CombatUiView.ThemeBoardHelpIconColor;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            SetStretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return icon;
        }

        private static void ConfigureSkillSlotButton(Button button, int index, Vector2 anchoredPosition, TMP_FontAsset font)
        {
            if (button == null)
            {
                return;
            }

            button.name = $"SkillSlotButton_{index + 1}";
            var rect = button.GetComponent<RectTransform>();
            SetAnchor(rect, new Vector2(0.5f, 0.5f), SkillSlotSize, anchoredPosition);

            var image = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
            var skillColor = ResolveDesignTimeSkillSlotColor(index);
            image.color = skillColor;
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.raycastTarget = true;

            var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.Lerp(skillColor, CombatUiView.ThemePrimaryColor, 0.28f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            var layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            var label = button.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                ConfigureSkillSlotLabel(label, font);
            }
        }

        private static Color ResolveDesignTimeSkillSlotColor(int index)
        {
            return ResolveSkillTypeColor(index switch
            {
                0 => SkillType.Attack,
                1 => SkillType.Defense,
                2 => SkillType.Debuff,
                _ => SkillType.Defense,
            });
        }

        private static Color ResolveSkillTypeColor(SkillType skillType)
        {
            return skillType switch
            {
                SkillType.Attack => CombatUiView.ThemeSkillAttackColor,
                SkillType.Defense => CombatUiView.ThemeSkillDefenseColor,
                SkillType.Debuff => CombatUiView.ThemeSkillChangeColor,
                SkillType.Heal => CombatUiView.ThemeSkillChangeColor,
                _ => CombatUiView.ThemeSkillChangeColor,
            };
        }

        private static void ConfigureSkillSlotLabel(TMP_Text label, TMP_FontAsset font)
        {
            if (label == null)
            {
                return;
            }

            label.fontSize = 23f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            if (font != null)
            {
                label.font = font;
            }

            SetStretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
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
            var hpSprite = EnsureHpBarSpriteAsset();
            var outlineSprite = EnsureHpBarOutlineSpriteAsset();
            var bg = CreateImage(parent, name, CombatUiView.ThemeHpBorderColor);
            bg.raycastTarget = false;
            SetAnchor(bg.rectTransform, anchor, size, Vector2.zero);
            ConfigureHpBarRootImage(bg);

            var interior = CreateImage(bg.transform, "HpBarInterior", bgColor);
            ConfigureHpBarSimpleImage(interior, hpSprite, bgColor);

            fill = CreateImage(bg.transform, "Fill", fillColor);
            ConfigureHpBarFilledImage(fill, hpSprite, fillColor);
            EnsureHpBarFeedbackImage(bg.rectTransform, "DamageTrailFill", hpSprite, CombatUiView.ThemeHpDamageTrailColor, filled: true);
            EnsureHpBarFeedbackImage(bg.rectTransform, "DamageFlashFill", hpSprite, new Color(1f, 1f, 1f, 0.95f), filled: false);
            EnsureHpBarOutlineImage(bg.rectTransform, outlineSprite);

            text = CreateLabel(bg.transform, "Text", "0/0", CombatUiView.HpTextMinFontSize, TextAlignmentOptions.Center, font);
            text.fontStyle = FontStyles.Bold;
            ConfigureHpTextAuthoring(text);
            SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static TMP_Text FindHpText(RectTransform hpRoot)
        {
            if (hpRoot == null)
            {
                return null;
            }

            return hpRoot.Find("Text")?.GetComponent<TMP_Text>();
        }

        private static void EnsureHpTextAuthoring(TMP_Text text, string fallbackText)
        {
            if (text == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(text.text))
            {
                text.text = fallbackText;
            }
            else if (text.text.StartsWith("체력 "))
            {
                text.text = text.text.Substring("체력 ".Length);
            }

            ConfigureHpTextAuthoring(text);
            EditorUtility.SetDirty(text);
        }

        private static void ConfigureHpTextAuthoring(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.color = Color.white;
            text.fontSize = Mathf.Max(text.fontSize, CombatUiView.HpTextMinFontSize);
            text.outlineColor = CombatUiView.ThemeHpTextOutlineColor;
            text.outlineWidth = CombatUiView.HpTextOutlineWidth;
            text.raycastTarget = false;

            var outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = CombatUiView.ThemeHpTextOutlineColor;
            outline.effectDistance = new Vector2(CombatUiView.HpTextOutlineDistance, -CombatUiView.HpTextOutlineDistance);
            outline.useGraphicAlpha = true;
            EditorUtility.SetDirty(outline);
        }

        private static void EnsureHpBarAuthoring(RectTransform hpRoot, Image fill, Color fillColor, Color backgroundColor)
        {
            if (hpRoot == null)
            {
                return;
            }

            var hpSprite = EnsureHpBarSpriteAsset();
            var outlineSprite = EnsureHpBarOutlineSpriteAsset();
            if (hpRoot.TryGetComponent<Image>(out var rootImage))
            {
                ConfigureHpBarRootImage(rootImage);
            }

            var interior = EnsureChildImage(hpRoot, "HpBarInterior");
            ConfigureHpBarSimpleImage(interior, hpSprite, backgroundColor);
            if (fill != null)
            {
                ConfigureHpBarFilledImage(fill, hpSprite, fillColor);
            }

            var trail = EnsureHpBarFeedbackImage(hpRoot, "DamageTrailFill", hpSprite, CombatUiView.ThemeHpDamageTrailColor, filled: true);
            var flash = EnsureHpBarFeedbackImage(hpRoot, "DamageFlashFill", hpSprite, new Color(1f, 1f, 1f, 0.95f), filled: false);
            var outline = EnsureHpBarOutlineImage(hpRoot, outlineSprite);
            interior.transform.SetAsFirstSibling();
            if (trail != null)
            {
                trail.transform.SetSiblingIndex(1);
            }

            if (fill != null)
            {
                fill.transform.SetSiblingIndex(2);
            }

            if (flash != null)
            {
                flash.transform.SetSiblingIndex(3);
                flash.gameObject.SetActive(false);
            }

            if (outline != null)
            {
                outline.transform.SetSiblingIndex(4);
            }

            EditorUtility.SetDirty(hpRoot);
        }

        private static Image EnsureChildImage(RectTransform parent, string name)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null)
            {
                var childObject = new GameObject(name, typeof(RectTransform), typeof(Image));
                childObject.transform.SetParent(parent, false);
                child = childObject.GetComponent<RectTransform>();
            }

            var image = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
            EditorUtility.SetDirty(child);
            EditorUtility.SetDirty(image);
            return image;
        }

        private static Image EnsureHpBarFeedbackImage(RectTransform parent, string name, Sprite sprite, Color color, bool filled)
        {
            var image = EnsureChildImage(parent, name);
            if (filled)
            {
                ConfigureHpBarFilledImage(image, sprite, color);
            }
            else
            {
                ConfigureHpBarSimpleImage(image, sprite, color);
            }

            image.raycastTarget = false;
            return image;
        }

        private static Image EnsureHpBarOutlineImage(RectTransform parent, Sprite sprite)
        {
            var image = EnsureChildImage(parent, "HpBarOutline");
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = CombatUiView.ThemeHpBorderColor;
            image.raycastTarget = false;
            SetStretch(image.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return image;
        }

        private static void ConfigureHpBarRootImage(Image image)
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Color.clear;
            image.raycastTarget = false;
        }

        private static void ConfigureHpBarSimpleImage(Image image, Sprite sprite, Color color)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            SetStretch(image.rectTransform, Vector2.zero, Vector2.one, new Vector2(HpBarBorderThickness, HpBarBorderThickness), new Vector2(-HpBarBorderThickness, -HpBarBorderThickness));
        }

        private static void ConfigureHpBarFilledImage(Image image, Sprite sprite, Color color)
        {
            image.sprite = sprite;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillClockwise = true;
            image.fillAmount = Mathf.Clamp01(image.fillAmount);
            image.color = color;
            image.raycastTarget = false;
            SetStretch(image.rectTransform, Vector2.zero, Vector2.one, new Vector2(HpBarBorderThickness, HpBarBorderThickness), new Vector2(-HpBarBorderThickness, -HpBarBorderThickness));
        }

        private static Sprite EnsureHpBarSpriteAsset()
        {
            var directory = Path.GetDirectoryName(HpBarSpritePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var texture = new Texture2D(HpBarSpriteWidth, HpBarSpriteHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[HpBarSpriteWidth * HpBarSpriteHeight];
            for (var y = 0; y < HpBarSpriteHeight; y++)
            {
                for (var x = 0; x < HpBarSpriteWidth; x++)
                {
                    pixels[y * HpBarSpriteWidth + x] = new Color32(255, 255, 255, CalculateWideHexAlpha(x, y));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            File.WriteAllBytes(HpBarSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(HpBarSpritePath, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(HpBarSpritePath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(HpBarSpritePath);
        }

        private static Sprite EnsureHpBarOutlineSpriteAsset()
        {
            var directory = Path.GetDirectoryName(HpBarOutlineSpritePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var texture = new Texture2D(HpBarSpriteWidth, HpBarSpriteHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[HpBarSpriteWidth * HpBarSpriteHeight];
            for (var y = 0; y < HpBarSpriteHeight; y++)
            {
                for (var x = 0; x < HpBarSpriteWidth; x++)
                {
                    pixels[y * HpBarSpriteWidth + x] = new Color32(255, 255, 255, CalculateWideHexOutlineAlpha(x, y));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            File.WriteAllBytes(HpBarOutlineSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(HpBarOutlineSpritePath, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(HpBarOutlineSpritePath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(HpBarOutlineSpritePath);
        }

        private static byte CalculateWideHexAlpha(int x, int y)
        {
            var insideSamples = 0;
            for (var sampleY = 0; sampleY < HpBarSpriteSamplesPerAxis; sampleY++)
            {
                for (var sampleX = 0; sampleX < HpBarSpriteSamplesPerAxis; sampleX++)
                {
                    var samplePosition = new Vector2(
                        x + (sampleX + 0.5f) / HpBarSpriteSamplesPerAxis,
                        y + (sampleY + 0.5f) / HpBarSpriteSamplesPerAxis);
                    if (IsInsideWideHexBar(samplePosition))
                    {
                        insideSamples++;
                    }
                }
            }

            var totalSamples = HpBarSpriteSamplesPerAxis * HpBarSpriteSamplesPerAxis;
            return (byte)Mathf.RoundToInt(255f * insideSamples / totalSamples);
        }

        private static byte CalculateWideHexOutlineAlpha(int x, int y)
        {
            var outlineSamples = 0;
            for (var sampleY = 0; sampleY < HpBarSpriteSamplesPerAxis; sampleY++)
            {
                for (var sampleX = 0; sampleX < HpBarSpriteSamplesPerAxis; sampleX++)
                {
                    var samplePosition = new Vector2(
                        x + (sampleX + 0.5f) / HpBarSpriteSamplesPerAxis,
                        y + (sampleY + 0.5f) / HpBarSpriteSamplesPerAxis);
                    if (IsInsideWideHexBar(samplePosition) &&
                        CalculateDistanceToWideHexEdge(samplePosition) <= HpBarOutlineThicknessPixels)
                    {
                        outlineSamples++;
                    }
                }
            }

            var totalSamples = HpBarSpriteSamplesPerAxis * HpBarSpriteSamplesPerAxis;
            return (byte)Mathf.RoundToInt(255f * outlineSamples / totalSamples);
        }

        private static bool IsInsideWideHexBar(Vector2 point)
        {
            if (point.y < 0f || point.y > HpBarSpriteHeight)
            {
                return false;
            }

            var leftInset = CalculateWideHexLeftInset(point.y);
            return point.x >= leftInset && point.x <= HpBarSpriteWidth - leftInset;
        }

        private static float CalculateDistanceToWideHexEdge(Vector2 point)
        {
            var halfHeight = HpBarSpriteHeight * 0.5f;
            var leftPoint = new Vector2(0f, halfHeight);
            var topLeft = new Vector2(HpBarPointLengthPixels, 0f);
            var topRight = new Vector2(HpBarSpriteWidth - HpBarPointLengthPixels, 0f);
            var rightPoint = new Vector2(HpBarSpriteWidth, halfHeight);
            var bottomRight = new Vector2(HpBarSpriteWidth - HpBarPointLengthPixels, HpBarSpriteHeight);
            var bottomLeft = new Vector2(HpBarPointLengthPixels, HpBarSpriteHeight);

            var distance = DistanceToSegment(point, leftPoint, topLeft);
            distance = Mathf.Min(distance, DistanceToSegment(point, topLeft, topRight));
            distance = Mathf.Min(distance, DistanceToSegment(point, topRight, rightPoint));
            distance = Mathf.Min(distance, DistanceToSegment(point, rightPoint, bottomRight));
            distance = Mathf.Min(distance, DistanceToSegment(point, bottomRight, bottomLeft));
            distance = Mathf.Min(distance, DistanceToSegment(point, bottomLeft, leftPoint));
            return distance;
        }

        private static float CalculateWideHexLeftInset(float y)
        {
            var clampedY = Mathf.Clamp(y, 0f, HpBarSpriteHeight);
            var halfHeight = HpBarSpriteHeight * 0.5f;
            var distanceFromCenter = Mathf.Abs(clampedY - halfHeight);
            return HpBarPointLengthPixels * distanceFromCenter / halfHeight;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var lengthSquared = Vector2.Dot(segment, segment);
            if (lengthSquared <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, start);
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + segment * t);
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
            SetRef(so, "playerBattleStatusEffectsRoot", refs.PlayerBattleStatusEffectsRoot);
            SetRef(so, "enemyStatusEffectsRoot", refs.EnemyStatusEffectsRoot);
            SetRef(so, "actionDescriptionText", refs.ActionDescriptionText);
            SetRef(so, "boardPanel", refs.BoardPanel);
            SetRef(so, "actionPanel", refs.ActionPanel);
            SetRef(so, "enemyTurnPanel", refs.EnemyTurnPanel);
            SetRef(so, "hpBarFill", refs.HpBarFill);
            SetRef(so, "hpText", refs.HpText);
            SetRef(so, "playerBoardStatusEffectsRoot", refs.PlayerBoardStatusEffectsRoot);
            SetRef(so, "turnLimitText", refs.TurnLimitText);
            SetListRef(so, "boardCells", refs.Cells);
            SetRef(so, "boardSwipeHandler", refs.SwipeHandler);
            SetRef(so, "boardAnimationOverlay", refs.BoardAnimationOverlay);
            SetRef(so, "costText", refs.CostText);
            SetRef(so, "costFormulaHelpIcon", refs.CostFormulaHelpIcon);
            SetRef(so, "costFormulaHelpLabel", refs.CostFormulaHelpLabel);
            SetRef(so, "boardCostFormulaHelpIcon", refs.BoardCostFormulaHelpIcon);
            SetRef(so, "boardCostFormulaHelpLabel", refs.BoardCostFormulaHelpLabel);
            SetRef(so, "categoryView", refs.CategoryView);
            SetRef(so, "attackCategoryButton", refs.AttackCategory);
            SetRef(so, "defenseCategoryButton", refs.DefenseCategory);
            SetRef(so, "categoryEndTurnButton", refs.CategoryEndTurn);
            SetRef(so, "skillsView", refs.SkillsView);
            SetRef(so, "skillsHeaderText", refs.SkillsHeaderText);
            SetListRef(so, "skillTierButtons", new List<Button> { refs.Tier1, refs.Tier2, refs.Tier3, refs.Tier4 });
            SetListRef(so, "skillTierLabels", new List<TMP_Text> { refs.Tier1Label, refs.Tier2Label, refs.Tier3Label, refs.Tier4Label });
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

        private static RectTransform EnsureStatusEffectAuthoringRoot(
            RectTransform hpRoot,
            string rootName,
            Vector2 anchoredPosition)
        {
            if (hpRoot == null)
            {
                return null;
            }

            var root = hpRoot.Find(rootName) as RectTransform;
            var createdRoot = root == null;
            if (root == null)
            {
                var rootObject = new GameObject(rootName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
                rootObject.transform.SetParent(hpRoot, false);
                root = rootObject.GetComponent<RectTransform>();
            }

            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(0f, 0f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = new Vector2(160f, 32f);

            var layout = root.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
                createdRoot = true;
            }

            if (createdRoot)
            {
                ConfigureStatusEffectAuthoringLayout(layout);
            }

            EnsureStatusEffectIconSample(root);
            root.SetAsLastSibling();
            EditorUtility.SetDirty(root);
            return root;
        }

        private static void ConfigureStatusEffectAuthoringLayout(HorizontalLayoutGroup layout)
        {
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static void EnsureStatusEffectIconSample(RectTransform root)
        {
            var sample = root.Find("StatusEffectIconSample") as RectTransform;
            if (sample == null)
            {
                var sampleObject = new GameObject("StatusEffectIconSample", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                sampleObject.transform.SetParent(root, false);
                sample = sampleObject.GetComponent<RectTransform>();
                sample.sizeDelta = new Vector2(32f, 32f);

                var image = sampleObject.GetComponent<Image>();
                image.color = new Color(0.46f, 0.16f, 0.20f, 0.95f);
                image.raycastTarget = false;

                var layoutElement = sampleObject.GetComponent<LayoutElement>();
                layoutElement.preferredWidth = 32f;
                layoutElement.preferredHeight = 32f;
                layoutElement.minWidth = 32f;
                layoutElement.minHeight = 32f;
            }

            EditorUtility.SetDirty(sample);
        }

        private static RectTransform EnsureBlockIconAuthoring(RectTransform hpRoot, TMP_FontAsset font)
        {
            if (hpRoot == null)
            {
                return null;
            }

            var icon = hpRoot.Find("BlockIcon") as RectTransform;
            if (icon == null)
            {
                var iconObject = new GameObject("BlockIcon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(hpRoot, false);
                icon = iconObject.GetComponent<RectTransform>();
            }

            icon.anchorMin = new Vector2(1f, 0.5f);
            icon.anchorMax = new Vector2(1f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(12f, 0f);
            icon.sizeDelta = new Vector2(34f, 24f);

            var image = icon.GetComponent<Image>() ?? icon.gameObject.AddComponent<Image>();
            image.color = new Color(0.42f, 0.46f, 0.5f, 0.95f);
            image.raycastTarget = false;

            var text = icon.Find("Text") as RectTransform;
            if (text == null)
            {
                var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(icon, false);
                text = textObject.GetComponent<RectTransform>();
            }

            text.anchorMin = Vector2.zero;
            text.anchorMax = Vector2.one;
            text.offsetMin = Vector2.zero;
            text.offsetMax = Vector2.zero;

            var label = text.GetComponent<TextMeshProUGUI>() ?? text.gameObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 16f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.text = "0";
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            if (font != null)
            {
                label.font = font;
            }

            icon.gameObject.SetActive(true);
            EditorUtility.SetDirty(icon);
            EditorUtility.SetDirty(text);
            return icon;
        }

        private sealed class ViewRefs
        {
            public readonly List<BoardCellView> Cells = new();
            public BoardSwipeHandler SwipeHandler;
            public GameObject ActionPanel, CategoryView, SkillsView, EnemyTurnPanel, ResultOverlay, BoardPanel;
            public GameObject CostFormulaHelpIcon, BoardCostFormulaHelpIcon;
            public TMP_Text CostText, CostFormulaHelpLabel, BoardCostFormulaHelpLabel, SkillsHeaderText, EnemyTurnText, ResultTitle, ResultDesc;
            public Button AttackCategory, DefenseCategory, CategoryEndTurn;
            public Button Tier1, Tier2, Tier3, Tier4, SkillsBack, SkillsEndTurn, Restart, ReloadScene;
            public TMP_Text Tier1Label, Tier2Label, Tier3Label, Tier4Label;
            public TMP_Text TurnCounterText, IntentHeaderText, EnemyNameText, IntentBubbleText;
            public TMP_Text HpText, TurnLimitText, PlayerBattleHpText, EnemyHpText, ActionDescriptionText;
            public Image PlayerPortrait, EnemyPortrait, IntentBubble, HpBarFill, PlayerBattleHpFill, EnemyHpFill;
            public RectTransform BoardAnimationOverlay, PlayerBattleStatusEffectsRoot, PlayerBoardStatusEffectsRoot, EnemyStatusEffectsRoot;
        }
    }
}
#endif
