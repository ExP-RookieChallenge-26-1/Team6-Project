using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project2048.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        private readonly List<EnemyIntent> baseIntents = new();

        public EnemySO Data { get; private set; }
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int AttackPower { get; private set; }
        public int EffectiveAttackPower => Project2048.Combat.PlayerCombatController.ResolveStageModifiedStat(AttackPower, AttackModifier);
        public int BaseDefensePower { get; private set; }
        public int DefenseModifier { get; private set; }
        public int EffectiveDefensePower => Project2048.Combat.PlayerCombatController.ResolveStageModifiedStat(BaseDefensePower, DefenseModifier);
        public int Block { get; private set; }
        public int ShieldHp => Block;
        public int ThornRetaliationDamage { get; private set; }
        public int AttackModifier { get; private set; }
        public float CriticalChance { get; private set; }
        public float CriticalDamageMultiplier { get; private set; } = 1.5f;
        public bool IsDead => CurrentHp <= 0;
        public EnemyIntent CurrentIntent { get; private set; }
        public IReadOnlyList<EnemyIntent> CurrentIntents { get; private set; } = Array.Empty<EnemyIntent>();

        public event Action<int, int> OnHpChanged;
        public event Action<int> OnBlockChanged;
        public event Action<EnemyIntent> OnIntentChanged;
        public event Action<EnemyController> OnDead;

        private void OnDestroy()
        {
            UnbindDataValidation();
        }

        public void Init(EnemySO data)
        {
            UnbindDataValidation();
            Data = data ?? throw new ArgumentNullException(nameof(data));
            BindDataValidation();
            MaxHp = Mathf.Max(1, data.maxHp);
            CurrentHp = MaxHp;
            AttackPower = Mathf.Max(0, data.attackPower);
            BaseDefensePower = Mathf.Max(0, data.baseDefensePower);
            DefenseModifier = 0;
            Block = 0;
            ThornRetaliationDamage = 0;
            AttackModifier = 0;
            CriticalChance = Mathf.Clamp01(data.criticalChance);
            CriticalDamageMultiplier = Mathf.Max(1f, data.criticalDamageMultiplier);
            baseIntents.Clear();
            CurrentIntent = null;
            CurrentIntents = Array.Empty<EnemyIntent>();

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnBlockChanged?.Invoke(Block);
            OnIntentChanged?.Invoke(CurrentIntent);
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
            BaseDefensePower = Mathf.Max(0, Data.baseDefensePower);
            CriticalChance = Mathf.Clamp01(Data.criticalChance);
            CriticalDamageMultiplier = Mathf.Max(1f, Data.criticalDamageMultiplier);

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            RefreshIntentPreview();
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
            CurrentHp = Mathf.Max(0, CurrentHp - remainingDamage);

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnBlockChanged?.Invoke(Block);

            if (CurrentHp <= 0)
            {
                OnDead?.Invoke(this);
            }

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
        }

        public void ClearBlock()
        {
            if (Block == 0)
            {
                return;
            }

            Block = 0;
            ThornRetaliationDamage = 0;
            OnBlockChanged?.Invoke(Block);
        }

        public void SetIntent(EnemyIntent intent)
        {
            SetIntents(intent != null ? new[] { intent } : null);
        }

        public void SetIntents(IEnumerable<EnemyIntent> intents)
        {
            baseIntents.Clear();
            if (intents != null)
            {
                foreach (var intent in intents)
                {
                    if (intent != null)
                    {
                        baseIntents.Add(intent.Clone());
                    }
                }
            }

            RefreshIntentPreview();
        }

        public void ApplyAttackModifier(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            AttackModifier = Mathf.Clamp(AttackModifier + amount, -6, 6);
            RefreshIntentPreview();
        }

        public void ApplyDefenseModifier(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            DefenseModifier = Mathf.Clamp(DefenseModifier + amount, -6, 6);
            RefreshIntentPreview();
        }

        private void RefreshIntentPreview()
        {
            var currentIntents = new List<EnemyIntent>(baseIntents.Count);
            foreach (var baseIntent in baseIntents)
            {
                var currentIntent = baseIntent?.Clone();
                if (currentIntent == null)
                {
                    continue;
                }

                currentIntents.Add(currentIntent);
            }

            CurrentIntents = currentIntents;
            CurrentIntent = currentIntents.Count > 0 ? currentIntents[0] : null;
            OnIntentChanged?.Invoke(CurrentIntent);
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

        private void HandleDataValidated(EnemySO _)
        {
            RefreshFromData();
        }
    }
}
