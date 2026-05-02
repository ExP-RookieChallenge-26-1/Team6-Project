using System;
using UnityEngine;

namespace Project2048.Enemy
{
    /// <summary>
    /// 고정 인텐트 패턴이 없는 적의 다음 행동을 만든다.
    /// 공격/방어 성향, 디버프 순서, 강화형 배율은 EnemySO 데이터에서 읽는다.
    /// </summary>
    public class EnemyAiBrain
    {
        private readonly System.Random random;

        public EnemyAiBrain(System.Random random = null)
        {
            this.random = random ?? new System.Random();
        }

        public EnemyIntent ChooseIntent(EnemySO data, int turnIndex)
        {
            if (data == null)
            {
                return null;
            }

            // 디버프는 일정 턴마다 끼워 넣고, 나머지 턴은 공격/방어 가중치로 고른다.
            var debuffInterval = Mathf.Max(0, data.aiDebuffInterval);
            if (debuffInterval > 0 && (turnIndex + 1) % debuffInterval == 0)
            {
                return BuildDebuffIntent(data, ((turnIndex + 1) / debuffInterval) - 1);
            }

            return ChooseAttackOrDefense(data);
        }

        private EnemyIntent ChooseAttackOrDefense(EnemySO data)
        {
            var (attackWeight, defenseWeight) = GetActionWeights(data.aiActionBias);
            var totalWeight = attackWeight + defenseWeight;
            if (totalWeight <= 0)
            {
                return BuildAttackIntent(data);
            }

            var roll = random.NextDouble() * totalWeight;
            return roll < attackWeight
                ? BuildAttackIntent(data)
                : BuildDefenseIntent(data);
        }

        private static (int AttackWeight, int DefenseWeight) GetActionWeights(EnemyAiActionBias bias)
        {
            return bias switch
            {
                // 80:20 비율은 AI 성향 확인용 기본값이다. 정식 몬스터 밸런스에서 조정한다.
                EnemyAiActionBias.AttackHeavy => (80, 20),
                EnemyAiActionBias.DefenseHeavy => (20, 80),
                _ => (50, 50),
            };
        }

        private static EnemyIntent BuildAttackIntent(EnemySO data)
        {
            return new EnemyIntent
            {
                intentType = EnemyIntentType.Attack,
                value = ScaleByStrength(data.attackPower, data.aiStrength),
            };
        }

        private static EnemyIntent BuildDefenseIntent(EnemySO data)
        {
            return new EnemyIntent
            {
                intentType = EnemyIntentType.Defense,
                value = ScaleByStrength(data.defensePower, data.aiStrength),
            };
        }

        private static EnemyIntent BuildDebuffIntent(EnemySO data, int debuffIndex)
        {
            return new EnemyIntent
            {
                intentType = EnemyIntentType.Debuff,
                debuffType = ResolveDebuffType(data.aiDebuffPattern, debuffIndex),
                value = ScaleByStrength(data.debuffPower, data.aiStrength),
            };
        }

        private static DebuffType ResolveDebuffType(EnemyDebuffPattern pattern, int debuffIndex)
        {
            var even = debuffIndex % 2 == 0;
            return pattern switch
            {
                EnemyDebuffPattern.DarknessThenFear => even ? DebuffType.Darkness : DebuffType.Fear,
                _ => even ? DebuffType.Fear : DebuffType.Darkness,
            };
        }

        private static int ScaleByStrength(int value, EnemyAiStrength strength)
        {
            var baseValue = Mathf.Max(0, value);
            // 강화형 몬스터는 같은 브레인 설정을 쓰되 수치만 1.5배로 올린다.
            return strength == EnemyAiStrength.Enhanced
                ? Mathf.CeilToInt(baseValue * 1.5f)
                : baseValue;
        }
    }
}
