using System;
using System.Collections.Generic;
using Project2048.Presentation;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Combat
{
    [CreateAssetMenu(menuName = "Game/Player")]
    public class PlayerSO : ScriptableObject
    {
        [Header("Combat Stats")]
        public int maxHp = 240;
        public int attackPower = 10;
        public int baseDefensePower = 2;
        [Min(0)] public int initialBoardMoveCount = 4;
        public int boardMoveCountBonus;
        [Range(0f, 1f)] public float criticalChance;
        [Min(1f)] public float criticalDamageMultiplier = 1.5f;
        public Sprite portrait;
        public List<SkillSO> startingSkills = new();
        public List<CombatantActionEffectBinding> actionEffects = new();

        public event Action<PlayerSO> OnRuntimeValidated;

        public CombatEffectBinding FindActionEffect(string actionId)
        {
            return CombatantActionEffectBinding.Find(actionEffects, actionId);
        }

        public int ResolveInitialBoardMoveCount()
        {
            return Mathf.Max(0, initialBoardMoveCount);
        }

        public int ResolveMaxHp()
        {
            return Mathf.Max(1, maxHp);
        }

        private void OnValidate()
        {
            maxHp = Mathf.Max(1, maxHp);
            attackPower = Mathf.Max(0, attackPower);
            baseDefensePower = Mathf.Max(0, baseDefensePower);
            initialBoardMoveCount = Mathf.Max(0, initialBoardMoveCount);
            boardMoveCountBonus = Mathf.Max(0, boardMoveCountBonus);
            criticalChance = Mathf.Clamp01(criticalChance);
            criticalDamageMultiplier = Mathf.Max(1f, criticalDamageMultiplier);
            if (Application.isPlaying)
            {
                OnRuntimeValidated?.Invoke(this);
            }
        }
    }
}
