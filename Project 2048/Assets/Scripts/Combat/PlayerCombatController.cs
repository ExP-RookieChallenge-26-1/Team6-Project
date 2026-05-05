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
        public int FearStacks { get; private set; }
        public int BoardMoveCountBonus { get; private set; }
        public bool IsDead => CurrentHp <= 0;
        public IReadOnlyList<SkillSO> Skills => skills;

        public event Action<int, int> OnHpChanged;
        public event Action<int> OnBlockChanged;
        public event Action<int> OnDefenseBonusChanged;
        public event Action OnStatusEffectsChanged;

        private void OnDestroy()
        {
            UnbindDataValidation();
        }

        public void Init(PlayerSO data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            UnbindDataValidation();
            Data = data;
            BindDataValidation();
            MaxHp = Mathf.Max(1, data.maxHp);
            CurrentHp = MaxHp;
            AttackPower = Mathf.Max(0, data.attackPower);
            BoardMoveCountBonus = Mathf.Max(0, data.boardMoveCountBonus);
            Block = 0;
            DefenseBonus = 0;
            FearStacks = 0;

            SetSkills(data.startingSkills);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnBlockChanged?.Invoke(Block);
            OnDefenseBonusChanged?.Invoke(DefenseBonus);
            OnStatusEffectsChanged?.Invoke();
        }

        public void RefreshFromData()
        {
            if (Data == null)
            {
                return;
            }

            MaxHp = Mathf.Max(1, Data.maxHp);
            CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
            AttackPower = Mathf.Max(0, Data.attackPower);
            BoardMoveCountBonus = Mathf.Max(0, Data.boardMoveCountBonus);
            SetSkills(Data.startingSkills);

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        public void SetCurrentHpForRun(int currentHp)
        {
            CurrentHp = Mathf.Clamp(currentHp, 0, MaxHp);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
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

        public int RestoreHp(int amount)
        {
            if (amount <= 0 || MaxHp <= 0)
            {
                return 0;
            }

            var before = CurrentHp;
            CurrentHp = Mathf.Clamp(CurrentHp + amount, 0, MaxHp);
            if (CurrentHp != before)
            {
                OnHpChanged?.Invoke(CurrentHp, MaxHp);
            }

            return CurrentHp - before;
        }

        public int RestoreHpByMaxHpPercent(float percentOfMaxHp)
        {
            var amount = Mathf.CeilToInt(MaxHp * Mathf.Clamp01(percentOfMaxHp));
            return RestoreHp(amount);
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
            if (FearStacks > 0)
            {
                total = Mathf.CeilToInt(total * 0.5f);
            }

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

        public void ApplyFear(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            FearStacks += amount;
            OnStatusEffectsChanged?.Invoke();
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

        private void BindDataValidation()
        {
            if (Data != null)
            {
                Data.OnRuntimeValidated += HandleDataValidated;
            }
        }

        private void UnbindDataValidation()
        {
            if (Data != null)
            {
                Data.OnRuntimeValidated -= HandleDataValidated;
            }
        }

        private void HandleDataValidated(PlayerSO _)
        {
            RefreshFromData();
        }
    }
}
