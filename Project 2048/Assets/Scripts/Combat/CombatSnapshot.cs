using System;
using System.Collections.Generic;
using Project2048.Enemy;
using Project2048.Skills;

namespace Project2048.Combat
{
    /// <summary>
    /// UI와 테스트가 읽는 전투 상태 묶음이다. 이 DTO를 쓰면 외부 코드가
    /// PlayerCombatController나 EnemyController 내부 상태를 직접 건드리지 않아도 된다.
    /// </summary>
    [Serializable]
    public class CombatSnapshot
    {
        public CombatPhase Phase { get; set; }
        public int CurrentCost { get; set; }
        public int RemainingBoardMoves { get; set; }
        public string LastActionDescription { get; set; }
        public CombatVfxCue LastVfxCue { get; set; }
        public int[,] Board { get; set; }
        public PlayerCombatSnapshot Player { get; set; }
        public List<EnemyCombatSnapshot> Enemies { get; set; } = new();
        public List<SkillSnapshot> Skills { get; set; } = new();
    }

    [Serializable]
    public class CombatVfxCue
    {
        public int Sequence { get; set; }
        public DebuffType DebuffType { get; set; }
        public int Value { get; set; }
        public string SourceName { get; set; }
        public string TargetName { get; set; }

        public CombatVfxCue Clone()
        {
            return new CombatVfxCue
            {
                Sequence = Sequence,
                DebuffType = DebuffType,
                Value = Value,
                SourceName = SourceName,
                TargetName = TargetName,
            };
        }
    }

    [Serializable]
    public class PlayerCombatSnapshot
    {
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public int AttackPower { get; set; }
        public int Block { get; set; }
        public int DefenseBonus { get; set; }
        public int FearStacks { get; set; }
        public List<CombatStatusEffectSnapshot> StatusEffects { get; set; } = new();
    }

    [Serializable]
    public class EnemyCombatSnapshot
    {
        public int EnemyIndex { get; set; }
        public string DisplayName { get; set; }
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public int Block { get; set; }
        public bool IsDead { get; set; }
        public string AiProfileLabel { get; set; }
        public EnemyIntent Intent { get; set; }
        public List<CombatStatusEffectSnapshot> StatusEffects { get; set; } = new();
    }

    [Serializable]
    public class CombatStatusEffectSnapshot
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Value { get; set; }
        public bool IsBuff { get; set; }
        public string IconText { get; set; }
    }

    [Serializable]
    public class SkillSnapshot
    {
        public string SkillId { get; set; }
        public string DisplayName { get; set; }
        public SkillType SkillType { get; set; }
        public int Cost { get; set; }
        public int Power { get; set; }
    }
}
