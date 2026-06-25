using System;
using System.Collections.Generic;
using Project2048.Presentation;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Enemy
{
    public enum EnemyEncounterRank
    {
        Normal,
        Elite,
        Boss,
        FinalBoss,
    }

    [CreateAssetMenu(menuName = "Game/Enemy")]
    public class EnemySO : ScriptableObject
    {
        public const int MaximumActionsPerTurn = 3;
        public const int MinEquippedSkillSlots = 2;
        public const int MaxEquippedSkillSlots = 4;

        public string enemyName;
        public EnemyEncounterRank encounterRank = EnemyEncounterRank.Normal;
        public int maxHp = 160;
        public int attackPower = 3;
        public int baseDefensePower;
        public int defensePower = 3;
        public int debuffPower = 1;
        [Range(0f, 1f)] public float criticalChance;
        [Min(1f)] public float criticalDamageMultiplier = 1.5f;
        public EnemyAiComplexity aiComplexity = EnemyAiComplexity.Simple;
        [Range(1, MaximumActionsPerTurn)]
        public int actionsPerTurn = 1;
        public List<SkillSO> skills = new();
        public Sprite portrait;
        [SerializeField] private GameObject battlePrefab;

        [Header("애니메이션 (클립을 넣으면 전투 중 자동 재생, 비워두면 무시 — AnimatorController 불필요)")]
        [Tooltip("평상시 반복 재생. 임포트에서 Loop Time을 켠 클립을 권장.")]
        public AnimationClip idleAnimation;
        [Tooltip("적이 등장할 때 1회 재생 후 idle로 복귀.")]
        public AnimationClip appearAnimation;
        [Tooltip("적이 공격할 때 1회 재생 후 idle(없으면 정적 스프라이트)로 복귀.")]
        public AnimationClip attackAnimation;
        [Tooltip("적이 피격될 때 1회 재생 후 idle로 복귀.")]
        public AnimationClip hitAnimation;
        [Tooltip("적이 쓰러질 때 1회 재생.")]
        public AnimationClip deathAnimation;

        public List<CombatantActionEffectBinding> actionEffects = new();

        // 값이 있으면 AI 브레인보다 이 고정 순서를 우선한다. 보스처럼 정확한 패턴이 필요한 적에게 쓴다.
        public List<EnemyIntent> intentPattern = new();

        // intentPattern이 비어 있을 때 쓰는 몬스터 AI 설정이다.
        public EnemyAiActionBias aiActionBias = EnemyAiActionBias.Balanced;
        public EnemyDebuffPattern aiDebuffPattern = EnemyDebuffPattern.FearThenDarkness;
        public EnemyAiStrength aiStrength = EnemyAiStrength.Normal;
        public int aiDebuffInterval = 3;
        public bool canUseThornGuard;
        public int thornGuardShieldHp = 4;
        public int thornGuardRetaliationDamage = 2;
        public bool canUseBullRush;
        public int bullRushInterval = 3;
        public int bullRushBonusDamage = 3;

        public event Action<EnemySO> OnRuntimeValidated;

        public int ActionsPerTurn => Mathf.Clamp(
            Mathf.Max(actionsPerTurn, ResolveDefaultActionsPerTurn(aiComplexity)),
            1,
            MaximumActionsPerTurn);

        public int AssignedSkillCount
        {
            get
            {
                if (skills == null)
                {
                    return 0;
                }

                var count = 0;
                var limit = Mathf.Min(skills.Count, MaxEquippedSkillSlots);
                for (var index = 0; index < limit; index++)
                {
                    if (skills[index] != null && skills[index].CanEnemyUse)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasMinimumSkillSlots => AssignedSkillCount >= MinEquippedSkillSlots;
        public GameObject BattlePrefab => battlePrefab;

        public string GetAiProfileLabel()
        {
            return $"AI: {FormatComplexity(aiComplexity)} / {FormatActionBias(aiActionBias)} / {FormatDebuffPattern(aiDebuffPattern)} / {FormatStrength(aiStrength)}";
        }

        public static int ResolveDefaultActionsPerTurn(EnemyAiComplexity complexity)
        {
            return complexity switch
            {
                EnemyAiComplexity.Complex => 3,
                EnemyAiComplexity.Normal => 2,
                _ => 1,
            };
        }

        public CombatEffectBinding FindActionEffect(string actionId)
        {
            return CombatantActionEffectBinding.Find(actionEffects, actionId);
        }

        private void OnValidate()
        {
            maxHp = Mathf.Max(1, maxHp);
            attackPower = Mathf.Max(0, attackPower);
            baseDefensePower = Mathf.Max(0, baseDefensePower);
            defensePower = Mathf.Max(0, defensePower);
            debuffPower = Mathf.Max(0, debuffPower);
            criticalChance = Mathf.Clamp01(criticalChance);
            criticalDamageMultiplier = Mathf.Max(1f, criticalDamageMultiplier);
            actionsPerTurn = ActionsPerTurn;
            TrimSkillSlots();
            aiDebuffInterval = Mathf.Max(0, aiDebuffInterval);
            thornGuardShieldHp = Mathf.Max(0, thornGuardShieldHp);
            thornGuardRetaliationDamage = Mathf.Max(0, thornGuardRetaliationDamage);
            bullRushInterval = Mathf.Max(0, bullRushInterval);
            bullRushBonusDamage = Mathf.Max(0, bullRushBonusDamage);
            if (Application.isPlaying)
            {
                OnRuntimeValidated?.Invoke(this);
            }
        }

        private void TrimSkillSlots()
        {
            if (skills == null)
            {
                skills = new List<SkillSO>();
                return;
            }

            for (var index = skills.Count - 1; index >= 0; index--)
            {
                if (skills[index] == null)
                {
                    skills.RemoveAt(index);
                }
            }

            if (skills.Count > MaxEquippedSkillSlots)
            {
                skills.RemoveRange(MaxEquippedSkillSlots, skills.Count - MaxEquippedSkillSlots);
            }
        }

        private static string FormatActionBias(EnemyAiActionBias actionBias)
        {
            return actionBias switch
            {
                EnemyAiActionBias.AttackHeavy => "공격 위주",
                EnemyAiActionBias.DefenseHeavy => "방어 위주",
                _ => "균형",
            };
        }

        private static string FormatDebuffPattern(EnemyDebuffPattern debuffPattern)
        {
            return debuffPattern switch
            {
                EnemyDebuffPattern.DarknessThenFear => "암흑->공포",
                _ => "공포->암흑",
            };
        }

        private static string FormatStrength(EnemyAiStrength strength)
        {
            return strength == EnemyAiStrength.Enhanced ? "강화" : "일반";
        }

        private static string FormatComplexity(EnemyAiComplexity complexity)
        {
            return complexity switch
            {
                EnemyAiComplexity.Complex => "복잡",
                EnemyAiComplexity.Normal => "보통",
                _ => "단순",
            };
        }
    }
}
