#if UNITY_EDITOR
using System.Collections.Generic;
using Project2048.Enemy;
using Project2048.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project2048.PrototypeEditor
{
    public static class SelectedMonsterRosterBuilder
    {
        private const string EnemyFolder = "Assets/Data/Prototype/Enemies";
        private const string ExistingWolfPath = EnemyFolder + "/01.asset";

        private readonly struct EnemySeed
        {
            public EnemySeed(
                string assetNumber,
                string id,
                string displayName,
                int maxHp,
                int attackPower,
                int defensePower,
                int debuffPower,
                int difficultyScore,
                EnemyAiActionBias actionBias,
                EnemyDebuffPattern debuffPattern,
                EnemyAiStrength strength,
                int debuffInterval,
                string portraitPath)
            {
                AssetNumber = assetNumber;
                Id = id;
                DisplayName = displayName;
                MaxHp = maxHp;
                AttackPower = attackPower;
                DefensePower = defensePower;
                DebuffPower = debuffPower;
                DifficultyScore = difficultyScore;
                ActionBias = actionBias;
                DebuffPattern = debuffPattern;
                Strength = strength;
                DebuffInterval = debuffInterval;
                PortraitPath = portraitPath;
            }

            public string AssetNumber { get; }
            public string Id { get; }
            public string DisplayName { get; }
            public int MaxHp { get; }
            public int AttackPower { get; }
            public int DefensePower { get; }
            public int DebuffPower { get; }
            public int DifficultyScore { get; }
            public EnemyAiActionBias ActionBias { get; }
            public EnemyDebuffPattern DebuffPattern { get; }
            public EnemyAiStrength Strength { get; }
            public int DebuffInterval { get; }
            public string PortraitPath { get; }
        }

        [MenuItem("Project2048/Build Selected Monster Enemy Assets")]
        public static void BuildSelectedMonsterEnemyAssets()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets/Data", "Prototype");
            EnsureFolder("Assets/Data/Prototype", "Enemies");

            var enemies = new List<EnemySO>();
            var wolf = AssetDatabase.LoadAssetAtPath<EnemySO>(ExistingWolfPath);
            if (wolf != null)
            {
                ConfigureEnemy(
                    wolf,
                    "\uADF8\uB9BC\uC790 \uB291\uB300",
                    maxHp: 32,
                    attackPower: 5,
                    defensePower: 3,
                    debuffPower: 1,
                    difficultyScore: 1,
                    actionBias: EnemyAiActionBias.Balanced,
                    debuffPattern: EnemyDebuffPattern.FearThenDarkness,
                    strength: EnemyAiStrength.Normal,
                    debuffInterval: 3,
                    portrait: wolf.portrait);
                enemies.Add(wolf);
            }

            foreach (var seed in GetSelectedSeeds())
            {
                enemies.Add(CreateOrUpdateEnemy(seed));
            }

            BindSceneEnemyPool(enemies);

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log($"Built selected monster enemy roster: {enemies.Count} enemies.");
        }

        private static EnemySeed[] GetSelectedSeeds()
        {
            return new[]
            {
                new EnemySeed("02", "m05_coiled_whisperer", "\uC18D\uBC15\uB41C \uC804\uC728", 40, 5, 5, 2, 2, EnemyAiActionBias.Balanced, EnemyDebuffPattern.FearThenDarkness, EnemyAiStrength.Enhanced, 3, "Assets/Art/Monsters/Cutouts/m05_coiled_whisperer.png"),
                new EnemySeed("03", "m02_masked_wisp", "\uAC70\uC9D3\uB41C \uBBF8\uC18C", 30, 4, 4, 2, 1, EnemyAiActionBias.Balanced, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Normal, 3, "Assets/Art/Monsters/Cutouts/m02_masked_wisp.png"),
                new EnemySeed("04", "m10_blueheart_raven", "\uD478\uB978\uC2EC\uC7A5", 40, 5, 4, 2, 2, EnemyAiActionBias.Balanced, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Enhanced, 3, "Assets/Art/Monsters/Cutouts/m10_blueheart_raven.png"),
                new EnemySeed("05", "m01_blue_ear_stalker", "\uD478\uB978\uADC0 \uB4E4\uC950", 32, 6, 2, 1, 1, EnemyAiActionBias.AttackHeavy, EnemyDebuffPattern.FearThenDarkness, EnemyAiStrength.Normal, 3, "Assets/Art/Monsters/Cutouts/m01_blue_ear_stalker.png"),
                new EnemySeed("06", "m08_deep_serpent", "\uBB3C\uACB0\uCE58\uB294 \uC5ED\uBCD1", 42, 7, 3, 2, 2, EnemyAiActionBias.AttackHeavy, EnemyDebuffPattern.FearThenDarkness, EnemyAiStrength.Enhanced, 2, "Assets/Art/Monsters/Cutouts/m08_deep_serpent.png"),
                new EnemySeed("07", "m04_one_eye_tallhorn", "\uC678\uB208 \uCD94\uC801\uC790", 34, 6, 3, 2, 1, EnemyAiActionBias.AttackHeavy, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Normal, 3, "Assets/Art/Monsters/Cutouts/m04_one_eye_tallhorn.png"),
                new EnemySeed("08", "m11_cane_blackcat", "\uCE74\uB974\uBBFC\uB290", 42, 7, 3, 2, 2, EnemyAiActionBias.AttackHeavy, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Enhanced, 2, "Assets/Art/Monsters/Cutouts/m11_cane_blackcat.png"),
                new EnemySeed("09", "m07_spiral_tail_beast", "\uC18C\uC6A9\uB3CC\uC774\uCE58\uB294 \uD63C\uB3C8", 36, 5, 5, 1, 1, EnemyAiActionBias.DefenseHeavy, EnemyDebuffPattern.FearThenDarkness, EnemyAiStrength.Normal, 3, "Assets/Art/Monsters/Cutouts/m07_spiral_tail_beast.png"),
                new EnemySeed("10", "m03_spiral_horn_shepherd", "\uB098\uC120\uBFD4 \uBAA9\uB3D9", 46, 5, 6, 2, 2, EnemyAiActionBias.DefenseHeavy, EnemyDebuffPattern.FearThenDarkness, EnemyAiStrength.Enhanced, 3, "Assets/Art/Monsters/Cutouts/m03_spiral_horn_shepherd.png"),
                new EnemySeed("11", "m06_shellback_pursuer", "\uB2E4\uCE35 \uAC11\uAC01", 38, 4, 6, 1, 1, EnemyAiActionBias.DefenseHeavy, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Normal, 4, "Assets/Art/Monsters/Cutouts/m06_shellback_pursuer.png"),
                new EnemySeed("12", "m14_inverted_bat_doll", "\uB124\uD06C\uB9B4\uB77C", 44, 5, 6, 2, 2, EnemyAiActionBias.DefenseHeavy, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Enhanced, 3, "Assets/Art/Monsters/Cutouts/m14_inverted_bat_doll.png"),
            };
        }

        private static EnemySO CreateOrUpdateEnemy(EnemySeed seed)
        {
            ConfigureSpriteImporter(seed.PortraitPath);
            var enemyPath = $"{EnemyFolder}/{seed.AssetNumber}.asset";
            var enemy = AssetDatabase.LoadAssetAtPath<EnemySO>(enemyPath);
            if (enemy == null)
            {
                enemy = ScriptableObject.CreateInstance<EnemySO>();
                AssetDatabase.CreateAsset(enemy, enemyPath);
            }

            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(seed.PortraitPath);
            ConfigureEnemy(
                enemy,
                seed.DisplayName,
                seed.MaxHp,
                seed.AttackPower,
                seed.DefensePower,
                seed.DebuffPower,
                seed.DifficultyScore,
                seed.ActionBias,
                seed.DebuffPattern,
                seed.Strength,
                seed.DebuffInterval,
                portrait);
            return enemy;
        }

        private static void ConfigureEnemy(
            EnemySO enemy,
            string displayName,
            int maxHp,
            int attackPower,
            int defensePower,
            int debuffPower,
            int difficultyScore,
            EnemyAiActionBias actionBias,
            EnemyDebuffPattern debuffPattern,
            EnemyAiStrength strength,
            int debuffInterval,
            Sprite portrait)
        {
            enemy.name = displayName;
            enemy.enemyName = displayName;
            enemy.maxHp = maxHp;
            enemy.attackPower = attackPower;
            enemy.defensePower = defensePower;
            enemy.debuffPower = debuffPower;
            enemy.difficultyScore = difficultyScore;
            enemy.intentPattern = new List<EnemyIntent>();
            enemy.aiActionBias = actionBias;
            enemy.aiDebuffPattern = debuffPattern;
            enemy.aiStrength = strength;
            enemy.aiDebuffInterval = debuffInterval;
            enemy.portrait = portrait;
            EditorUtility.SetDirty(enemy);
        }

        private static void BindSceneEnemyPool(IReadOnlyList<EnemySO> enemies)
        {
            var bootstrap = Object.FindAnyObjectByType<PrototypeCombatBootstrap>(FindObjectsInactive.Include);
            if (bootstrap == null)
            {
                return;
            }

            var so = new SerializedObject(bootstrap);
            var enemyData = so.FindProperty("enemyData");
            if (enemyData != null && enemies.Count > 0)
            {
                enemyData.objectReferenceValue = enemies[0];
            }

            var randomize = so.FindProperty("randomizeEnemyOnStart");
            if (randomize != null)
            {
                randomize.boolValue = true;
            }

            var pool = so.FindProperty("enemyPool");
            if (pool != null)
            {
                pool.ClearArray();
                for (var i = 0; i < enemies.Count; i++)
                {
                    pool.InsertArrayElementAtIndex(i);
                    pool.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
        }

        private static void ConfigureSpriteImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.mipmapEnabled = false;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string parent, string folder)
        {
            var path = parent + "/" + folder;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
#endif
