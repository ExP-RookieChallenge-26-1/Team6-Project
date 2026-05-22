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
        [Header("HP Formula")]
        public bool usePokemonHpFormula;
        [Min(1)] public int statLevel = 50;
        [Min(1)] public int baseHp = 40;
        [Range(0, 31)] public int hpIndividualValue;
        [Range(0, 252)] public int hpEffortValue;

        [Header("Combat Stats")]
        public int maxHp = 100;
        public int attackPower = 3;
        public int baseDefensePower;
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
            if (!usePokemonHpFormula)
            {
                return Mathf.Max(1, maxHp);
            }

            return CalculatePokemonHp(baseHp, statLevel, hpIndividualValue, hpEffortValue);
        }

        public static int CalculatePokemonHp(int baseHp, int level, int individualValue, int effortValue)
        {
            baseHp = Mathf.Max(1, baseHp);
            level = Mathf.Max(1, level);
            individualValue = Mathf.Clamp(individualValue, 0, 31);
            effortValue = Mathf.Clamp(effortValue, 0, 252);

            return Mathf.Max(
                1,
                Mathf.FloorToInt(((2 * baseHp + individualValue + Mathf.FloorToInt(effortValue / 4f)) * level) / 100f) + level + 10);
        }

        private void OnValidate()
        {
            statLevel = Mathf.Max(1, statLevel);
            baseHp = Mathf.Max(1, baseHp);
            hpIndividualValue = Mathf.Clamp(hpIndividualValue, 0, 31);
            hpEffortValue = Mathf.Clamp(hpEffortValue, 0, 252);
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
