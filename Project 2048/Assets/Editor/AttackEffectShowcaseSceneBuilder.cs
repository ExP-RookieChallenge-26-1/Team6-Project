#if UNITY_EDITOR
using System.Linq;
using Project2048.Prototype;
using Project2048.Skills;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project2048.EditorTools
{
    public static class AttackEffectShowcaseSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/AttackEffectShowcase.unity";
        private const string SkillFolder = "Assets/Data/Skills";
        private const string PlayerSpritePath = "Assets/Art/Prototype/PrototypePlayerCutout.png";
        private const string EnemySpritePath = "Assets/Art/Prototype/PrototypeEnemyCutout.png";
        private const string KoreanFontAssetPath = "Assets/Fonts/MaruBuri-Regular SDF.asset";
        private const int ColumnCount = 3;
        private const float SlotWidth = 7.2f;
        private const float SlotHeight = 4.1f;
        private static readonly Vector3 PlayerOffset = new(-1.5f, -0.49f, 0f);
        private static readonly Vector3 EnemyOffset = new(1.35f, -0.21f, 0f);
        private const float ActorScale = 0.4f;

        [MenuItem("Project2048/Generate Attack Effect Showcase")]
        public static void Generate()
        {
            var skills = AssetDatabase
                .FindAssets("t:SkillSO", new[] { SkillFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkillSO>)
                .Where(skill => skill != null && skill.skillType == SkillType.Attack)
                .Where(skill => !string.Equals(skill.skillId, "guard-break", System.StringComparison.OrdinalIgnoreCase))
                .Where(skill => !string.Equals(skill.skillId, "feint-strike", System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(skill => skill.skillId)
                .ToArray();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("AttackEffectShowcaseRoot");

            var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
            var enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnemySpritePath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
            var rows = Mathf.Max(1, Mathf.CeilToInt(skills.Length / (float)ColumnCount));
            var totalWidth = ColumnCount * SlotWidth;
            var totalHeight = rows * SlotHeight;
            var origin = new Vector3(-(totalWidth - SlotWidth) * 0.5f, (totalHeight - SlotHeight) * 0.5f, 0f);

            CreateCamera(origin.y - 1.2f);
            CreateLabel(
                root.transform,
                "Title",
                "Attack Effect Showcase",
                new Vector3(0f, origin.y + 1.9f, 0f),
                new Vector2(12f, 0.7f),
                3.4f,
                font,
                new Color(0.96f, 0.98f, 1f, 1f),
                TextAlignmentOptions.Center);

            for (var index = 0; index < skills.Length; index++)
            {
                var column = index % ColumnCount;
                var row = index / ColumnCount;
                var position = origin + new Vector3(column * SlotWidth, -row * SlotHeight, 0f);
                CreateSlot(root.transform, skills[index], playerSprite, enemySprite, font, position, index);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {ScenePath} with {skills.Length} attack skill effect slots.");
        }

        private static void CreateCamera(float startY)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.075f, 1f);
            camera.transform.position = new Vector3(0f, startY, -10f);
            cameraObject.AddComponent<ShowcaseCameraController>();
            cameraObject.tag = "MainCamera";
        }

        private static void CreateSlot(
            Transform parent,
            SkillSO skill,
            Sprite playerSprite,
            Sprite enemySprite,
            TMP_FontAsset font,
            Vector3 position,
            int sortingIndex)
        {
            var slot = new GameObject($"Slot_{sortingIndex + 1:00}_{skill.skillId}");
            slot.transform.SetParent(parent, false);
            slot.transform.position = position;

            var player = CreateSprite(slot.transform, "VirtualAlly", playerSprite, PlayerOffset, ActorScale, 10);
            var enemy = CreateSprite(slot.transform, "VirtualEnemy", enemySprite, EnemyOffset, ActorScale, 10);
            var worldView = slot.AddComponent<CombatWorldSpriteView>();
            var showcaseSlot = slot.AddComponent<AttackEffectShowcaseSlot>();

            var title = CreateLabel(
                slot.transform,
                "SkillName",
                skill.skillName,
                new Vector3(0f, 0.88f, 0f),
                new Vector2(4.65f, 0.45f),
                2.2f,
                font,
                Color.white,
                TextAlignmentOptions.Center);
            var detail = CreateLabel(
                slot.transform,
                "SkillDetail",
                $"{skill.skillId} / {skill.vfxFamily}",
                new Vector3(0f, 0.48f, 0f),
                new Vector2(4.65f, 0.34f),
                1.45f,
                font,
                new Color(0.68f, 0.76f, 0.82f, 1f),
                TextAlignmentOptions.Center);

            BindWorldView(worldView, player, enemy);
            showcaseSlot.Configure(skill, worldView, title, detail, 3f, sortingIndex * 0.12f);
            title.text = skill.skillName;
            detail.text = $"{skill.skillId}  /  {skill.vfxFamily}";
            EditorUtility.SetDirty(title);
            EditorUtility.SetDirty(detail);
            EditorUtility.SetDirty(showcaseSlot);
        }

        private static SpriteRenderer CreateSprite(
            Transform parent,
            string objectName,
            Sprite sprite,
            Vector3 localPosition,
            float scale,
            int sortingOrder)
        {
            var child = new GameObject(objectName, typeof(SpriteRenderer));
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = Vector3.one * scale;

            var renderer = child.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;
            return renderer;
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string objectName,
            string text,
            Vector3 localPosition,
            Vector2 size,
            float fontSize,
            TMP_FontAsset font,
            Color color,
            TextAlignmentOptions alignment)
        {
            var child = new GameObject(objectName, typeof(TextMeshPro));
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;

            var label = child.GetComponent<TextMeshPro>();
            label.text = text;
            label.font = font;
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 0.75f;
            label.fontSizeMax = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.rectTransform.sizeDelta = size;
            label.sortingOrder = 40;
            return label;
        }

        private static void BindWorldView(CombatWorldSpriteView view, SpriteRenderer player, SpriteRenderer enemy)
        {
            var serialized = new SerializedObject(view);
            SetReference(serialized, "playerRenderer", player);
            SetReference(serialized, "enemyRenderer", enemy);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static void SetReference(SerializedObject serialized, string propertyName, Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
#endif
