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
        public int maxHp = 10;
        public int attackPower = 3;
        public int baseDefensePower;
        public int defensePower = 3;
        public int debuffPower = 1;
        public int difficultyScore = 1;
        [Range(0f, 1f)] public float criticalChance;
        [Min(1f)] public float criticalDamageMultiplier = 1.5f;
        public EnemyAiComplexity aiComplexity = EnemyAiComplexity.Simple;
        [Range(1, MaximumActionsPerTurn)]
        public int actionsPerTurn = 1;
        public List<SkillSO> skills = new();
        public Sprite portrait;
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
                    if (skills[index] != null && skills[index].isEnemySkill)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasMinimumSkillSlots => AssignedSkillCount >= MinEquippedSkillSlots;

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
            difficultyScore = Mathf.Max(0, difficultyScore);
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
