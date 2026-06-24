using System.IO;
using Project2048.Enemy;
using UnityEditor;
using UnityEngine;

namespace Project2048.EditorTools
{
    /// <summary>
    /// 베이스 적(EnemySO)을 복제해 엘리트/강화 같은 바리에이션을 빠르게 만드는 에디터 툴.
    /// 복제본은 베이스의 스킬·이펙트·AI 설정을 그대로 복사하고, HP/공격력만 자동 스케일 + 랭크만 바꾼다.
    /// 생성 후에는 베이스와 완전히 독립이므로 자유롭게 편집하면 된다.
    /// 배율은 아래 상수만 고치면 된다.
    /// </summary>
    public static class EnemyVariantTool
    {
        // 엘리트 바리에이션 배율
        private const float EliteHpMultiplier = 1.6f;
        private const float EliteAttackMultiplier = 1.3f;

        // 강화 바리에이션 배율
        private const float EnhancedHpMultiplier = 1.4f;
        private const float EnhancedAttackMultiplier = 1.15f;

        private const string EliteMenu = "Assets/Enemy Variant/Create Elite Variant";
        private const string EnhancedMenu = "Assets/Enemy Variant/Create Enhanced Variant";

        [MenuItem(EliteMenu, true)]
        [MenuItem(EnhancedMenu, true)]
        private static bool ValidateEnemySelected()
        {
            return Selection.activeObject is EnemySO;
        }

        [MenuItem(EliteMenu, false, 1000)]
        private static void CreateEliteVariant()
        {
            CreateVariant(
                fileSuffix: "Elite",
                nameSuffix: " (엘리트)",
                hpMultiplier: EliteHpMultiplier,
                attackMultiplier: EliteAttackMultiplier,
                rank: EnemyEncounterRank.Elite);
        }

        [MenuItem(EnhancedMenu, false, 1001)]
        private static void CreateEnhancedVariant()
        {
            CreateVariant(
                fileSuffix: "Enhanced",
                nameSuffix: " (강화)",
                hpMultiplier: EnhancedHpMultiplier,
                attackMultiplier: EnhancedAttackMultiplier,
                rank: null);
        }

        private static void CreateVariant(
            string fileSuffix,
            string nameSuffix,
            float hpMultiplier,
            float attackMultiplier,
            EnemyEncounterRank? rank)
        {
            if (!(Selection.activeObject is EnemySO baseEnemy))
            {
                return;
            }

            var basePath = AssetDatabase.GetAssetPath(baseEnemy);
            if (string.IsNullOrEmpty(basePath))
            {
                Debug.LogError("[EnemyVariantTool] 선택한 적의 에셋 경로를 찾을 수 없습니다.");
                return;
            }

            var directory = Path.GetDirectoryName(basePath);
            var baseFileName = Path.GetFileNameWithoutExtension(basePath);
            var variantPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{directory}/{baseFileName}_{fileSuffix}.asset");

            if (!AssetDatabase.CopyAsset(basePath, variantPath))
            {
                Debug.LogError($"[EnemyVariantTool] 복제 실패: {basePath} -> {variantPath}");
                return;
            }

            var variant = AssetDatabase.LoadAssetAtPath<EnemySO>(variantPath);
            if (variant == null)
            {
                Debug.LogError($"[EnemyVariantTool] 복제본 로드 실패: {variantPath}");
                return;
            }

            Undo.RegisterCreatedObjectUndo(variant, "Create Enemy Variant");

            variant.maxHp = Mathf.Max(1, Mathf.RoundToInt(variant.maxHp * hpMultiplier));
            variant.attackPower = Mathf.Max(0, Mathf.RoundToInt(variant.attackPower * attackMultiplier));
            if (rank.HasValue)
            {
                variant.encounterRank = rank.Value;
            }

            if (!string.IsNullOrEmpty(variant.enemyName) && !variant.enemyName.EndsWith(nameSuffix))
            {
                variant.enemyName += nameSuffix;
            }

            EditorUtility.SetDirty(variant);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = variant;
            EditorGUIUtility.PingObject(variant);
            Debug.Log(
                $"[EnemyVariantTool] '{baseEnemy.name}' → '{variant.name}' 생성 " +
                $"(HP {Mathf.RoundToInt(variant.maxHp / hpMultiplier)}→{variant.maxHp}, " +
                $"공격 {Mathf.RoundToInt(variant.attackPower / attackMultiplier)}→{variant.attackPower}" +
                (rank.HasValue ? $", 랭크 {rank.Value}" : string.Empty) +
                "). 스킬·이펙트는 복사됨 — 이후 자유 편집하세요.");
        }
    }
}
