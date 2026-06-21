#if UNITY_EDITOR
using System.Collections.Generic;
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
        private const string WorldVfxProfilePath =
            "Assets/Art/Effects/SkillVFX/Resources/PrototypeCombatWorldVfxProfile.asset";
        private const int ColumnCount = 4;
        private const float SlotWidth = 5.85f;
        private const float SlotHeight = 3.25f;
        private const float HeaderHeight = 0.78f;
        private const float GroupGap = 0.52f;
        private static readonly Vector3 PlayerOffset = new(-1.2f, -0.48f, 0f);
        private static readonly Vector3 EnemyOffset = new(1.12f, -0.22f, 0f);
        private const float ActorScale = 0.34f;

        [MenuItem("Project2048/Generate Skill VFX Showcase")]
        public static void Generate()
        {
            var groups = AssetDatabase
                .FindAssets("t:SkillSO", new[] { SkillFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkillSO>)
                .Where(skill => skill != null && skill.vfxFamily != SkillVfxFamily.None)
                .Select(skill => new ShowcaseSkillEntry(skill, ResolveShowcaseGroup(skill)))
                .OrderBy(entry => entry.Group.SortOrder)
                .ThenBy(entry => entry.Skill.vfxFamily)
                .ThenBy(entry => entry.Skill.skillId)
                .GroupBy(entry => entry.Group.Id)
                .Select(group => new ShowcaseSkillGroupLayout(group.First().Group, group.Select(entry => entry.Skill).ToArray()))
                .ToArray();
            var skillCount = groups.Sum(group => group.Skills.Length);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("AttackEffectShowcaseRoot");

            var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
            var enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnemySpritePath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
            var worldVfxProfile = AssetDatabase.LoadAssetAtPath<CombatWorldVfxProfileSO>(WorldVfxProfilePath);
            var totalHeight = CalculateTotalHeight(groups);
            var topY = totalHeight * 0.5f - 0.75f;
            var totalWidth = ColumnCount * SlotWidth;
            var leftX = -(totalWidth - SlotWidth) * 0.5f;
            var currentY = topY;

            CreateCamera(topY - 2.9f, Mathf.Max(9f, totalHeight * 0.5f + 1.8f));
            CreateLabel(
                root.transform,
                "Title",
                $"Skill VFX Showcase - {skillCount} skills / {groups.Length} groups",
                new Vector3(0f, topY + 1.4f, 0f),
                new Vector2(18f, 0.72f),
                3.1f,
                font,
                new Color(0.96f, 0.98f, 1f, 1f),
                TextAlignmentOptions.Center);

            var slotIndex = 0;
            foreach (var group in groups)
            {
                CreateGroupHeader(root.transform, group.Group, group.Skills.Length, font, currentY, totalWidth);
                currentY -= HeaderHeight;

                for (var index = 0; index < group.Skills.Length; index++)
                {
                    var column = index % ColumnCount;
                    var row = index / ColumnCount;
                    var position = new Vector3(leftX + column * SlotWidth, currentY - row * SlotHeight, 0f);
                    CreateSlot(
                        root.transform,
                        group.Skills[index],
                        playerSprite,
                        enemySprite,
                        font,
                        worldVfxProfile,
                        position,
                        slotIndex);
                    slotIndex++;
                }

                currentY -= Mathf.CeilToInt(group.Skills.Length / (float)ColumnCount) * SlotHeight + GroupGap;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {ScenePath} with {skillCount} skill VFX slots across {groups.Length} groups.");
        }

        [MenuItem("Project2048/Generate Attack Effect Showcase")]
        public static void GenerateLegacyAttackShowcaseAlias()
        {
            Generate();
        }

        private static float CalculateTotalHeight(IEnumerable<ShowcaseSkillGroupLayout> groups)
        {
            var totalHeight = 2.8f;
            foreach (var group in groups)
            {
                totalHeight += HeaderHeight;
                totalHeight += Mathf.CeilToInt(group.Skills.Length / (float)ColumnCount) * SlotHeight;
                totalHeight += GroupGap;
            }

            return totalHeight;
        }

        private static void CreateCamera(float startY, float maxOrthographicSize)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.075f, 1f);
            camera.transform.position = new Vector3(0f, startY, -10f);
            var controller = cameraObject.AddComponent<ShowcaseCameraController>();
            var serialized = new SerializedObject(controller);
            var maxSize = serialized.FindProperty("maxOrthographicSize");
            if (maxSize != null)
            {
                maxSize.floatValue = maxOrthographicSize;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            cameraObject.tag = "MainCamera";
        }

        private static void CreateGroupHeader(
            Transform parent,
            ShowcaseGroup group,
            int skillCount,
            TMP_FontAsset font,
            float y,
            float totalWidth)
        {
            var root = new GameObject($"Group_{group.SortOrder:00}_{group.Id}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, y, 0f);

            CreateLabel(
                root.transform,
                "GroupTitle",
                $"{group.DisplayName} ({skillCount})",
                new Vector3(-(totalWidth * 0.5f) + 0.2f, 0f, 0f),
                new Vector2(7.6f, 0.46f),
                1.75f,
                font,
                group.Color,
                TextAlignmentOptions.Left);

            CreateLabel(
                root.transform,
                "GroupDetail",
                group.Detail,
                new Vector3(1.7f, -0.02f, 0f),
                new Vector2(12.2f, 0.36f),
                1.15f,
                font,
                new Color(0.68f, 0.76f, 0.82f, 1f),
                TextAlignmentOptions.Left);

            CreateSeparatorLine(
                root.transform,
                "GroupSeparator",
                new Vector3(0f, -0.34f, 0f),
                totalWidth - 0.4f,
                group.Color);
        }

        private static void CreateSlot(
            Transform parent,
            SkillSO skill,
            Sprite playerSprite,
            Sprite enemySprite,
            TMP_FontAsset font,
            CombatWorldVfxProfileSO worldVfxProfile,
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
                new Vector3(0f, 0.76f, 0f),
                new Vector2(4.25f, 0.42f),
                1.82f,
                font,
                Color.white,
                TextAlignmentOptions.Center);
            var detail = CreateLabel(
                slot.transform,
                "SkillDetail",
                $"{skill.skillId} / {skill.vfxFamily}",
                new Vector3(0f, 0.42f, 0f),
                new Vector2(4.25f, 0.3f),
                1.12f,
                font,
                new Color(0.68f, 0.76f, 0.82f, 1f),
                TextAlignmentOptions.Center);

            BindWorldView(worldView, player, enemy, worldVfxProfile);
            showcaseSlot.Configure(skill, worldView, title, detail, 3f, sortingIndex * 0.12f);
            title.text = skill.skillName;
            detail.text = $"{skill.skillId}  /  {skill.vfxFamily}";
            EditorUtility.SetDirty(title);
            EditorUtility.SetDirty(detail);
            EditorUtility.SetDirty(showcaseSlot);
        }

        private static void CreateSeparatorLine(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            float width,
            Color color)
        {
            var child = new GameObject(objectName, typeof(LineRenderer));
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;

            color.a = 0.52f;
            var line = child.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(-width * 0.5f, 0f, 0f));
            line.SetPosition(1, new Vector3(width * 0.5f, 0f, 0f));
            line.startWidth = 0.035f;
            line.endWidth = 0.035f;
            line.startColor = color;
            line.endColor = color;
            line.sharedMaterial = CreateLineMaterial(color);
            line.sortingOrder = 35;
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

        private static Material CreateLineMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = "ShowcaseSeparatorLineMaterial",
            };
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private static ShowcaseGroup ResolveShowcaseGroup(SkillSO skill)
        {
            return skill.vfxFamily switch
            {
                SkillVfxFamily.SlashArc => ShowcaseGroup.Slash,
                SkillVfxFamily.BloodFountainSlash => ShowcaseGroup.Blood,
                SkillVfxFamily.LightProjectile => ShowcaseGroup.Light,
                SkillVfxFamily.LightBeam => ShowcaseGroup.Light,
                SkillVfxFamily.SupportFire => ShowcaseGroup.Light,
                SkillVfxFamily.ShieldDome => ShowcaseGroup.Shield,
                SkillVfxFamily.ImpactBurst => ShowcaseGroup.Impact,
                SkillVfxFamily.SpikedBurst => ShowcaseGroup.Impact,
                SkillVfxFamily.BuffAura => ShowcaseGroup.Buff,
                SkillVfxFamily.CounterReady => ShowcaseGroup.Buff,
                SkillVfxFamily.DebuffWave => ShowcaseGroup.Debuff,
                SkillVfxFamily.DrainTether => ShowcaseGroup.Drain,
                SkillVfxFamily.BoardDisturb => ShowcaseGroup.Darkness,
                SkillVfxFamily.TentacleWhip => ShowcaseGroup.Tentacle,
                SkillVfxFamily.FlameBurst => ShowcaseGroup.Flame,
                SkillVfxFamily.DarkChainBurst => ShowcaseGroup.Chain,
                _ => ShowcaseGroup.Other,
            };
        }

        private static void BindWorldView(
            CombatWorldSpriteView view,
            SpriteRenderer player,
            SpriteRenderer enemy,
            CombatWorldVfxProfileSO worldVfxProfile)
        {
            var serialized = new SerializedObject(view);
            SetReference(serialized, "playerRenderer", player);
            SetReference(serialized, "enemyRenderer", enemy);
            SetReference(serialized, "worldVfxProfile", worldVfxProfile);
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

        private sealed class ShowcaseSkillEntry
        {
            public ShowcaseSkillEntry(SkillSO skill, ShowcaseGroup group)
            {
                Skill = skill;
                Group = group;
            }

            public SkillSO Skill { get; }

            public ShowcaseGroup Group { get; }
        }

        private sealed class ShowcaseSkillGroupLayout
        {
            public ShowcaseSkillGroupLayout(ShowcaseGroup group, SkillSO[] skills)
            {
                Group = group;
                Skills = skills;
            }

            public ShowcaseGroup Group { get; }

            public SkillSO[] Skills { get; }
        }

        private sealed class ShowcaseGroup
        {
            public static readonly ShowcaseGroup Slash = new(
                "slash",
                10,
                "참격/베기",
                "SlashArc",
                new Color(0.76f, 0.88f, 1f, 1f));
            public static readonly ShowcaseGroup Blood = new(
                "blood",
                20,
                "출혈 참격",
                "BloodFountainSlash",
                new Color(1f, 0.22f, 0.24f, 1f));
            public static readonly ShowcaseGroup Light = new(
                "light",
                30,
                "빛 발사/지원",
                "LightProjectile / LightBeam / SupportFire",
                new Color(1f, 0.84f, 0.28f, 1f));
            public static readonly ShowcaseGroup Shield = new(
                "shield",
                40,
                "방어막/실드",
                "ShieldDome",
                new Color(0.45f, 0.86f, 1f, 1f));
            public static readonly ShowcaseGroup Impact = new(
                "impact",
                50,
                "충격/폭발",
                "ImpactBurst / SpikedBurst",
                new Color(1f, 0.58f, 0.22f, 1f));
            public static readonly ShowcaseGroup Buff = new(
                "buff",
                60,
                "버프/회복/준비",
                "BuffAura / CounterReady",
                new Color(0.66f, 1f, 0.62f, 1f));
            public static readonly ShowcaseGroup Debuff = new(
                "debuff",
                70,
                "디버프 파동",
                "DebuffWave",
                new Color(0.72f, 0.55f, 1f, 1f));
            public static readonly ShowcaseGroup Drain = new(
                "drain",
                80,
                "흡수/생체/독",
                "DrainTether",
                new Color(0.34f, 0.9f, 0.46f, 1f));
            public static readonly ShowcaseGroup Darkness = new(
                "darkness",
                90,
                "어둠/보드 방해",
                "BoardDisturb",
                new Color(0.56f, 0.38f, 0.9f, 1f));
            public static readonly ShowcaseGroup Tentacle = new(
                "tentacle",
                100,
                "촉수 타격",
                "TentacleWhip",
                new Color(0.78f, 0.32f, 0.95f, 1f));
            public static readonly ShowcaseGroup Flame = new(
                "flame",
                110,
                "화염/연소 폭발",
                "FlameBurst",
                new Color(1f, 0.38f, 0.14f, 1f));
            public static readonly ShowcaseGroup Chain = new(
                "chain",
                120,
                "어둠 사슬/족쇄",
                "DarkChainBurst",
                new Color(0.86f, 0.18f, 0.38f, 1f));
            public static readonly ShowcaseGroup Other = new(
                "other",
                900,
                "기타",
                "Unclassified",
                new Color(0.78f, 0.82f, 0.88f, 1f));

            private ShowcaseGroup(string id, int sortOrder, string displayName, string detail, Color color)
            {
                Id = id;
                SortOrder = sortOrder;
                DisplayName = displayName;
                Detail = detail;
                Color = color;
            }

            public string Id { get; }

            public int SortOrder { get; }

            public string DisplayName { get; }

            public string Detail { get; }

            public Color Color { get; }
        }
    }
}
#endif
