using System;
using System.Collections.Generic;
using Project2048.Presentation;
using UnityEngine;

namespace Project2048.Enemy
{
    [CreateAssetMenu(menuName = "Game/Enemy")]
    public class EnemySO : ScriptableObject
    {
        public string enemyName;
        public int maxHp = 10;
        public int attackPower = 3;
        public int defensePower = 3;
        public int debuffPower = 1;
        public int difficultyScore = 1;
        public Sprite portrait;
        public List<CombatantActionEffectBinding> actionEffects = new();

        // 값이 있으면 AI 브레인보다 이 고정 순서를 우선한다. 보스처럼 정확한 패턴이 필요한 적에게 쓴다.
        public List<EnemyIntent> intentPattern = new();

        // intentPattern이 비어 있을 때 쓰는 몬스터 AI 설정이다.
        public EnemyAiActionBias aiActionBias = EnemyAiActionBias.Balanced;
        public EnemyDebuffPattern aiDebuffPattern = EnemyDebuffPattern.FearThenDarkness;
        public EnemyAiStrength aiStrength = EnemyAiStrength.Normal;
        public int aiDebuffInterval = 3;

        public event Action<EnemySO> OnRuntimeValidated;

        public string GetAiProfileLabel()
        {
            return EnemyAiProfileFormatter.Format(aiActionBias, aiDebuffPattern, aiStrength);
        }

        public CombatEffectBinding FindActionEffect(string actionId)
        {
            return CombatantActionEffectBinding.Find(actionEffects, actionId);
        }

        private void OnValidate()
        {
            maxHp = Mathf.Max(1, maxHp);
            attackPower = Mathf.Max(0, attackPower);
            defensePower = Mathf.Max(0, defensePower);
            debuffPower = Mathf.Max(0, debuffPower);
            difficultyScore = Mathf.Max(0, difficultyScore);
            aiDebuffInterval = Mathf.Max(0, aiDebuffInterval);
            if (Application.isPlaying)
            {
                OnRuntimeValidated?.Invoke(this);
            }
        }
    }
}
