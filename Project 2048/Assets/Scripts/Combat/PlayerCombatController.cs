using System;
using System.Collections.Generic;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Combat
{
    public class PlayerCombatController : MonoBehaviour
    {
        public const int FearDefenseGainPenalty = 6;
        public const int MaxEquippedSkillSlots = 4;

        [SerializeField] private List<SkillSO> skills = new();

        public PlayerSO Data { get; private set; }
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int AttackPower { get; private set; }
        public int AttackPowerModifier { get; private set; }
        public int EffectiveAttackPower => Mathf.Max(0, AttackPower + AttackPowerModifier);
        public int BaseDefensePower { get; private set; }
        public int DefensePowerModifier { get; private set; }
        public int EffectiveDefensePower => Mathf.Max(0, BaseDefensePower + DefensePowerModifier);
        public int Block { get; private set; }
        public int ShieldHp => Block;
        public int ThornRetaliationDamage { get; private set; }
        public int DefenseBonus { get; private set; }
        public int FearStacks { get; private set; }
        public int BoardMoveCountBonus { get; private set; }
        public int NextTurnBoardMoveCountModifier { get; private set; }
        public float CriticalChance { get; private set; }
        public float CriticalDamageMultiplier { get; private set; } = 1.5f;
        public int CounterPercent { get; private set; }
        public int EndureTurns { get; private set; }
        public int EchoDamageBonus { get; private set; }
        public int ExtraAttackHits { get; private set; }
        public bool HasPendingChargedAttack => pendingChargedAttackPower > 0;
        public bool IsDead => CurrentHp <= 0;
        public IReadOnlyList<SkillSO> Skills => skills;

        private string pendingChargedAttackName;
        private int pendingChargedAttackPower;

        public event Action<int, int> OnHpChanged;
        public event Action<int> OnBlockChanged;
        public event Action<int> OnDefenseBonusChanged;
        public event Action OnStatusEffectsChanged;
        public event Action OnSkillsChanged;

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
            MaxHp = data.ResolveMaxHp();
            CurrentHp = MaxHp;
            AttackPower = Mathf.Max(0, data.attackPower);
            AttackPowerModifier = 0;
            BaseDefensePower = Mathf.Max(0, data.baseDefensePower);
            BoardMoveCountBonus = Mathf.Max(0, data.boardMoveCountBonus);
            CriticalChance = Mathf.Clamp01(data.criticalChance);
            CriticalDamageMultiplier = Mathf.Max(1f, data.criticalDamageMultiplier);
            Block = 0;
            ThornRetaliationDamage = 0;
            DefenseBonus = 0;
            DefensePowerModifier = 0;
            FearStacks = 0;
            NextTurnBoardMoveCountModifier = 0;
            CounterPercent = 0;
            EndureTurns = 0;
            EchoDamageBonus = 0;
            ExtraAttackHits = 0;
            pendingChargedAttackName = null;
            pendingChargedAttackPower = 0;

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

            MaxHp = Data.ResolveMaxHp();
            CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
            AttackPower = Mathf.Max(0, Data.attackPower);
            BaseDefensePower = Mathf.Max(0, Data.baseDefensePower);
            BoardMoveCountBonus = Mathf.Max(0, Data.boardMoveCountBonus);
            CriticalChance = Mathf.Clamp01(Data.criticalChance);
            CriticalDamageMultiplier = Mathf.Max(1f, Data.criticalDamageMultiplier);
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
                OnSkillsChanged?.Invoke();
                return;
            }

            foreach (var nextSkill in nextSkills)
            {
                if (nextSkill == null)
                {
                    continue;
                }

                skills.Add(nextSkill);
                if (skills.Count >= MaxEquippedSkillSlots)
                {
                    break;
                }
            }

            OnSkillsChanged?.Invoke();
        }

        public bool TryLearnSkill(SkillSO nextSkill, int replacementSlotIndex, out SkillSO forgottenSkill)
        {
            forgottenSkill = null;
            if (nextSkill == null)
            {
                return false;
            }

            if (skills.Count < MaxEquippedSkillSlots && replacementSlotIndex < 0)
            {
                skills.Add(nextSkill);
                OnSkillsChanged?.Invoke();
                return true;
            }

            if (replacementSlotIndex < 0 || replacementSlotIndex >= MaxEquippedSkillSlots)
            {
                return false;
            }

            if (replacementSlotIndex > skills.Count)
            {
                return false;
            }

            if (replacementSlotIndex == skills.Count)
            {
                skills.Add(nextSkill);
            }
            else
            {
                forgottenSkill = skills[replacementSlotIndex];
                skills[replacementSlotIndex] = nextSkill;
            }

            OnSkillsChanged?.Invoke();
            return true;
        }

        public void ApplyPermanentStatBonuses(
            int maxHpBonus,
            int attackPowerBonus,
            int defensePowerBonus,
            float criticalChanceBonus,
            float criticalDamageMultiplierBonus)
        {
            var hpBonus = Mathf.Max(0, maxHpBonus);
            MaxHp = Mathf.Max(1, MaxHp + hpBonus);
            CurrentHp = Mathf.Clamp(CurrentHp + hpBonus, 0, MaxHp);
            AttackPower = Mathf.Max(0, AttackPower + attackPowerBonus);
            BaseDefensePower = Mathf.Max(0, BaseDefensePower + defensePowerBonus);
            CriticalChance = Mathf.Clamp01(CriticalChance + criticalChanceBonus);
            CriticalDamageMultiplier = Mathf.Max(1f, CriticalDamageMultiplier + criticalDamageMultiplierBonus);

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyTemporaryCombatBuffs(int attackPowerBonus, int defensePowerBonus)
        {
            AttackPower = Mathf.Max(0, AttackPower + attackPowerBonus);
            BaseDefensePower = Mathf.Max(0, BaseDefensePower + defensePowerBonus);
            OnStatusEffectsChanged?.Invoke();
        }

        public int TakeDamage(int damage)
        {
            damage = Mathf.Max(0, damage);

            var remainingDamage = Mathf.Max(0, damage - Block);
            Block = Mathf.Max(0, Block - damage);
            if (Block == 0)
            {
                ThornRetaliationDamage = 0;
            }

            var hpBefore = CurrentHp;
            var minimumHp = EndureTurns > 0 && CurrentHp > 0 ? 1 : 0;
            CurrentHp = Mathf.Max(minimumHp, CurrentHp - remainingDamage);

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnBlockChanged?.Invoke(Block);
            return hpBefore - CurrentHp;
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

        public void ApplyThornGuard(int shieldHp, int retaliationDamage)
        {
            if (shieldHp <= 0)
            {
                return;
            }

            Block += shieldHp;
            ThornRetaliationDamage = Mathf.Max(0, retaliationDamage);
            OnBlockChanged?.Invoke(Block);
            OnStatusEffectsChanged?.Invoke();
        }

        public int GainBlockWithBonus(int baseAmount)
        {
            var total = Mathf.Max(0, baseAmount + DefenseBonus - FearStacks);
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

        public void ApplyDefensePowerModifier(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            DefensePowerModifier += amount;
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyAttackPowerModifier(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            AttackPowerModifier += amount;
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyCounter(int percent)
        {
            percent = Mathf.Clamp(percent, 0, 100);
            if (percent <= 0)
            {
                return;
            }

            CounterPercent = Mathf.Max(CounterPercent, percent);
            OnStatusEffectsChanged?.Invoke();
        }

        public int CalculateCounterDamage(int receivedHpDamage)
        {
            receivedHpDamage = Mathf.Max(0, receivedHpDamage);
            if (receivedHpDamage == 0 || CounterPercent <= 0)
            {
                return 0;
            }

            return Mathf.CeilToInt(receivedHpDamage * CounterPercent / 100f);
        }

        public void ApplyEndure(int turns)
        {
            turns = Mathf.Max(1, turns);
            EndureTurns = Mathf.Max(EndureTurns, turns);
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyEchoDamageBonus(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            EchoDamageBonus += amount;
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyExtraAttackHits(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            ExtraAttackHits += amount;
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyCriticalChanceBonus(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            CriticalChance = Mathf.Clamp01(CriticalChance + amount);
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyNextTurnBoardMoveCountModifier(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            NextTurnBoardMoveCountModifier += amount;
            OnStatusEffectsChanged?.Invoke();
        }

        public int ConsumeNextTurnBoardMoveCountModifier()
        {
            var modifier = NextTurnBoardMoveCountModifier;
            NextTurnBoardMoveCountModifier = 0;
            if (modifier != 0)
            {
                OnStatusEffectsChanged?.Invoke();
            }

            return modifier;
        }

        public int SpendHp(int amount, bool leaveOne)
        {
            amount = Mathf.Max(0, amount);
            if (amount == 0 || CurrentHp <= 0)
            {
                return 0;
            }

            var minimumHp = leaveOne ? 1 : 0;
            var before = CurrentHp;
            CurrentHp = Mathf.Max(minimumHp, CurrentHp - amount);
            if (CurrentHp != before)
            {
                OnHpChanged?.Invoke(CurrentHp, MaxHp);
            }

            return before - CurrentHp;
        }

        public void QueueChargedAttack(string displayName, int attackPower)
        {
            attackPower = Mathf.Max(0, attackPower);
            if (attackPower <= 0)
            {
                return;
            }

            pendingChargedAttackName = displayName;
            pendingChargedAttackPower = attackPower;
            OnStatusEffectsChanged?.Invoke();
        }

        public bool TryConsumePendingChargedAttack(out string displayName, out int attackPower)
        {
            displayName = pendingChargedAttackName;
            attackPower = pendingChargedAttackPower;
            pendingChargedAttackName = null;
            pendingChargedAttackPower = 0;

            if (attackPower > 0)
            {
                OnStatusEffectsChanged?.Invoke();
                return true;
            }

            return false;
        }

        public void ClearTurnLimitedSkillEffects()
        {
            var changed = CounterPercent != 0 || EndureTurns != 0;
            CounterPercent = 0;
            EndureTurns = 0;
            if (changed)
            {
                OnStatusEffectsChanged?.Invoke();
            }
        }

        public void ApplyFear(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (FearStacks == FearDefenseGainPenalty)
            {
                return;
            }

            FearStacks = FearDefenseGainPenalty;
            OnStatusEffectsChanged?.Invoke();
        }

        public void ClearFear()
        {
            if (FearStacks == 0)
            {
                return;
            }

            FearStacks = 0;
            OnStatusEffectsChanged?.Invoke();
        }

        public void ClearBlock()
        {
            if (Block == 0 && ThornRetaliationDamage == 0)
            {
                return;
            }

            Block = 0;
            ThornRetaliationDamage = 0;
            OnBlockChanged?.Invoke(Block);
            OnStatusEffectsChanged?.Invoke();
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
