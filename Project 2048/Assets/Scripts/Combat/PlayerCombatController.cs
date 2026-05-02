using System;
using System.Collections.Generic;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Combat
{
    public class PlayerCombatController : MonoBehaviour
    {
        [SerializeField] private List<SkillSO> skills = new();

        public PlayerSO Data { get; private set; }
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int AttackPower { get; private set; }
        public int Block { get; private set; }
        public int DefenseBonus { get; private set; }
        public int BoardMoveCountBonus { get; private set; }
        public bool IsDead => CurrentHp <= 0;
        public IReadOnlyList<SkillSO> Skills => skills;

        public event Action<int, int> OnHpChanged;
        public event Action<int> OnBlockChanged;
        public event Action<int> OnDefenseBonusChanged;

        public void Init(PlayerSO data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Data = data;
            MaxHp = Mathf.Max(1, data.maxHp);
            CurrentHp = MaxHp;
            AttackPower = Mathf.Max(0, data.attackPower);
            BoardMoveCountBonus = Mathf.Max(0, data.boardMoveCountBonus);
            Block = 0;
            DefenseBonus = 0;

            SetSkills(data.startingSkills);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnBlockChanged?.Invoke(Block);
            OnDefenseBonusChanged?.Invoke(DefenseBonus);
        }

        public void SetSkills(IEnumerable<SkillSO> nextSkills)
        {
            skills.Clear();
            if (nextSkills == null)
            {
                return;
            }

            skills.AddRange(nextSkills);
        }

        public void TakeDamage(int damage)
        {
            damage = Mathf.Max(0, damage);

            var remainingDamage = Mathf.Max(0, damage - Block);
            Block = Mathf.Max(0, Block - damage);
            CurrentHp = Mathf.Max(0, CurrentHp - remainingDamage);

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnBlockChanged?.Invoke(Block);
        }

        public void AddBlock(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Block += amount;
            OnBlockChanged?.Invoke(Block);
        }

        public int GainBlockWithBonus(int baseAmount)
        {
            var total = Mathf.Max(0, baseAmount + DefenseBonus);
            if (total > 0)
            {
                Block += total;
                OnBlockChanged?.Invoke(Block);
            }

            return total;
        }

        public void ApplyDefenseBonus(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            DefenseBonus += amount;
            OnDefenseBonusChanged?.Invoke(DefenseBonus);
        }

        public void ClearBlock()
        {
            if (Block == 0)
            {
                return;
            }

            Block = 0;
            OnBlockChanged?.Invoke(Block);
        }
    }
}
