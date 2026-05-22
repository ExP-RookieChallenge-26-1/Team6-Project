using System;
using System.Collections.Generic;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Combat
{
    public class PlayerCombatController : MonoBehaviour
    {
        public const int FearDefenseGainPenalty = 1;
        public const int MaxEquippedSkillSlots = 4;
        private const int MinStatStage = -6;
        private const int MaxStatStage = 6;
        private const int MaxCriticalStage = 4;
        private const float CriticalChancePerStage = 0.2f;

        [SerializeField] private List<SkillSO> skills = new();

        public PlayerSO Data { get; private set; }
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int AttackPower { get; private set; }
        public int AttackStage { get; private set; }
        public int AttackPowerModifier => AttackStage;
        public int EffectiveAttackPower => ResolveStageModifiedStat(AttackPower, AttackStage);
        public int BaseDefensePower { get; private set; }
        public int DefenseStage { get; private set; }
        public int DefensePowerModifier => DefenseStage;
        public int EffectiveDefensePower => ResolveStageModifiedStat(BaseDefensePower, DefenseStage);
        public int Block { get; private set; }
        public int ShieldHp => Block;
        public int ThornRetaliationDamage { get; private set; }
        public int DefenseBonus { get; private set; }
        public int FearStacks { get; private set; }
        public int BoardMoveCountBonus { get; private set; }
        public int NextTurnBoardMoveCountModifier { get; private set; }
        public int CriticalStage { get; private set; }
        public float CriticalChance => Mathf.Clamp01(baseCriticalChance + CriticalStage * CriticalChancePerStage);
        public float CriticalDamageMultiplier { get; private set; } = 1.5f;
        public int CounterPercent { get; private set; }
        public int EndureTurns { get; private set; }
        public float NextAttackPowerMultiplier { get; private set; } = 1f;
        public int NextAttackHitCount { get; private set; }
        public float NextAttackHitPowerMultiplier { get; private set; } = 1f;
        public int EchoDamageBonus => NextAttackPowerMultiplier > 1f ? Mathf.RoundToInt((NextAttackPowerMultiplier - 1f) * 100f) : 0;
        public int ExtraAttackHits => Mathf.Max(0, NextAttackHitCount - 1);
        public bool HasPendingChargedAttack => pendingChargedAttackPower > 0;
        public bool IsDead => CurrentHp <= 0;
        public IReadOnlyList<SkillSO> Skills => skills;

        private string pendingChargedAttackName;
        private int pendingChargedAttackPower;
        private DamageStatSource pendingChargedAttackStatSource;
        private float baseCriticalChance;

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
            AttackStage = 0;
            BaseDefensePower = Mathf.Max(0, data.baseDefensePower);
            BoardMoveCountBonus = Mathf.Max(0, data.boardMoveCountBonus);
            baseCriticalChance = Mathf.Clamp01(data.criticalChance);
            CriticalDamageMultiplier = Mathf.Max(1f, data.criticalDamageMultiplier);
            Block = 0;
            ThornRetaliationDamage = 0;
            DefenseBonus = 0;
            DefenseStage = 0;
            FearStacks = 0;
            NextTurnBoardMoveCountModifier = 0;
            CounterPercent = 0;
            EndureTurns = 0;
            CriticalStage = 0;
            NextAttackPowerMultiplier = 1f;
            NextAttackHitCount = 0;
            NextAttackHitPowerMultiplier = 1f;
            pendingChargedAttackName = null;
            pendingChargedAttackPower = 0;
            pendingChargedAttackStatSource = DamageStatSource.AttackPower;

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
            baseCriticalChance = Mathf.Clamp01(Data.criticalChance);
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
            baseCriticalChance = Mathf.Clamp01(baseCriticalChance + criticalChanceBonus);
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

        public void ApplyDefensePowerModifier(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            DefenseStage = Mathf.Clamp(DefenseStage + amount, MinStatStage, MaxStatStage);
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyAttackPowerModifier(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            AttackStage = Mathf.Clamp(AttackStage + amount, MinStatStage, MaxStatStage);
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyCriticalStageModifier(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            CriticalStage = Mathf.Clamp(CriticalStage + amount, 0, MaxCriticalStage);
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyCounter(int percent)
        {
            percent = Mathf.Clamp(percent, 0, 400);
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

            ApplyNextAttackPowerMultiplier(1f + amount / 100f);
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyExtraAttackHits(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            ApplyNextAttackSplit(amount + 1, 1f);
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyCriticalChanceBonus(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            ApplyCriticalStageModifier(Mathf.Max(1, Mathf.RoundToInt(amount / CriticalChancePerStage)));
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyNextAttackPowerMultiplier(float multiplier)
        {
            multiplier = Mathf.Max(0f, multiplier);
            if (multiplier <= 0f)
            {
                return;
            }

            NextAttackPowerMultiplier = Mathf.Max(NextAttackPowerMultiplier, multiplier);
            OnStatusEffectsChanged?.Invoke();
        }

        public void ApplyNextAttackSplit(int hitCount, float hitPowerMultiplier)
        {
            hitCount = Mathf.Max(1, hitCount);
            hitPowerMultiplier = Mathf.Max(0f, hitPowerMultiplier);
            if (hitCount <= 1 || hitPowerMultiplier <= 0f)
            {
                return;
            }

            NextAttackHitCount = Mathf.Max(NextAttackHitCount, hitCount);
            NextAttackHitPowerMultiplier = hitPowerMultiplier;
            OnStatusEffectsChanged?.Invoke();
        }

        public bool TryConsumeNextAttackModifiers(
            out float powerMultiplier,
            out int hitCount,
            out float hitPowerMultiplier)
        {
            powerMultiplier = Mathf.Max(0f, NextAttackPowerMultiplier);
            hitCount = Mathf.Max(1, NextAttackHitCount);
            hitPowerMultiplier = Mathf.Max(0f, NextAttackHitPowerMultiplier);

            var hadModifier = !Mathf.Approximately(powerMultiplier, 1f) || hitCount > 1;
            NextAttackPowerMultiplier = 1f;
            NextAttackHitCount = 0;
            NextAttackHitPowerMultiplier = 1f;
            if (hadModifier)
            {
                OnStatusEffectsChanged?.Invoke();
            }

            return hadModifier;
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
            QueueChargedAttack(displayName, attackPower, DamageStatSource.AttackPower);
        }

        public void QueueChargedAttack(string displayName, int attackPower, DamageStatSource statSource)
        {
            attackPower = Mathf.Max(0, attackPower);
            if (attackPower <= 0)
            {
                return;
            }

            pendingChargedAttackName = displayName;
            pendingChargedAttackPower = attackPower;
            pendingChargedAttackStatSource = statSource;
            OnStatusEffectsChanged?.Invoke();
        }

        public bool TryConsumePendingChargedAttack(out string displayName, out int attackPower)
        {
            return TryConsumePendingChargedAttack(out displayName, out attackPower, out _);
        }

        public bool TryConsumePendingChargedAttack(
            out string displayName,
            out int attackPower,
            out DamageStatSource statSource)
        {
            displayName = pendingChargedAttackName;
            attackPower = pendingChargedAttackPower;
            statSource = pendingChargedAttackStatSource;
            pendingChargedAttackName = null;
            pendingChargedAttackPower = 0;
            pendingChargedAttackStatSource = DamageStatSource.AttackPower;

            if (attackPower > 0)
            {
                OnStatusEffectsChanged?.Invoke();
                return true;
            }

            return false;
        }

        public int ConsumeAllShield()
        {
            if (Block <= 0)
            {
                return 0;
            }

            var consumed = Block;
            Block = 0;
            ThornRetaliationDamage = 0;
            OnBlockChanged?.Invoke(Block);
            OnStatusEffectsChanged?.Invoke();
            return consumed;
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

            var stagePenalty = Mathf.Max(1, amount);
            if (FearStacks == stagePenalty)
            {
                return;
            }

            if (FearStacks > 0)
            {
                ApplyAttackPowerModifier(FearStacks);
            }

            FearStacks = stagePenalty;
            ApplyAttackPowerModifier(-FearStacks);
        }

        public void ClearFear()
        {
            if (FearStacks == 0)
            {
                return;
            }

            ApplyAttackPowerModifier(FearStacks);
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

        public static int ResolveStageModifiedStat(int baseStat, int stage)
        {
            baseStat = Mathf.Max(0, baseStat);
            if (baseStat == 0)
            {
                return 0;
            }

            stage = Mathf.Clamp(stage, MinStatStage, MaxStatStage);
            var multiplier = stage >= 0
                ? (2f + stage) / 2f
                : 2f / (2f - stage);
            return Mathf.Max(1, Mathf.RoundToInt(baseStat * multiplier));
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
