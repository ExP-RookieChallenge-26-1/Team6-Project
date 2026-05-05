using System;
using UnityEngine;

namespace Project2048.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        private EnemyIntent baseIntent;

        public EnemySO Data { get; private set; }
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int Block { get; private set; }
        public int AttackModifier { get; private set; }
        public bool IsDead => CurrentHp <= 0;
        public EnemyIntent CurrentIntent { get; private set; }

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
            Block = 0;
            AttackModifier = 0;
            baseIntent = null;
            CurrentIntent = null;

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

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            RefreshIntentPreview();
        }

        public void TakeDamage(int damage)
        {
            damage = Mathf.Max(0, damage);

            var remainingDamage = Mathf.Max(0, damage - Block);
            Block = Mathf.Max(0, Block - damage);
            CurrentHp = Mathf.Max(0, CurrentHp - remainingDamage);

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnBlockChanged?.Invoke(Block);

            if (CurrentHp <= 0)
            {
                OnDead?.Invoke(this);
            }
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

        public void ClearBlock()
        {
            if (Block == 0)
            {
                return;
            }

            Block = 0;
            OnBlockChanged?.Invoke(Block);
        }

        public void SetIntent(EnemyIntent intent)
        {
            baseIntent = intent?.Clone();
            RefreshIntentPreview();
        }

        public void ApplyAttackModifier(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            AttackModifier += amount;
            RefreshIntentPreview();
        }

        private void RefreshIntentPreview()
        {
            CurrentIntent = baseIntent?.Clone();
            if (CurrentIntent != null && CurrentIntent.intentType == EnemyIntentType.Attack)
            {
                CurrentIntent.value = Mathf.Max(0, CurrentIntent.value + AttackModifier);
            }

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
