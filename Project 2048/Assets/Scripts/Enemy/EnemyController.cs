using System;
using System.Collections.Generic;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        private const int MaxCriticalStage = 4;
        private const float CriticalChancePerStage = 0.2f;

        private readonly List<EnemyIntent> baseIntents = new();
        private float baseCriticalChance;

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
        public int ThornRetaliationShieldHp { get; private set; }
        public int AttackModifier { get; private set; }
        public int CriticalStage { get; private set; }
        public float CriticalChance => Mathf.Clamp01(baseCriticalChance + CriticalStage * CriticalChancePerStage);
        public float CriticalDamageMultiplier { get; private set; } = 1.5f;
        public int PoisonTurns { get; private set; }
        public float PoisonMaxHpDamagePercent { get; private set; }
        public int BleedTurns { get; private set; }
        public int BleedDamage { get; private set; }
        public int BrandDamage { get; private set; }
        public string SealedSkillId { get; private set; }
        public int SealTurns { get; private set; }
        public int TauntTurns { get; private set; }
        public int EndureTurns { get; private set; }
        public string LastUsedSkillId { get; private set; }
        public SkillType LastUsedSkillType { get; private set; }
        public SkillEffectKind LastUsedSkillEffectKind { get; private set; }
        public bool LastUsedSkillWasBasic { get; private set; }
        public bool HasPoisonOrBleed => PoisonTurns > 0 || BleedTurns > 0;
        public bool IsTaunted => TauntTurns > 0;
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
            BaseDefensePower = ResolveBaseDefensePower(data);
            DefenseModifier = 0;
            Block = 0;
            ThornRetaliationDamage = 0;
            ThornRetaliationShieldHp = 0;
            AttackModifier = 0;
            baseCriticalChance = Mathf.Clamp01(data.criticalChance);
            CriticalStage = 0;
            CriticalDamageMultiplier = Mathf.Max(1f, data.criticalDamageMultiplier);
            PoisonTurns = 0;
            PoisonMaxHpDamagePercent = 0f;
            BleedTurns = 0;
            BleedDamage = 0;
            BrandDamage = 0;
            SealedSkillId = null;
            SealTurns = 0;
            TauntTurns = 0;
            EndureTurns = 0;
            LastUsedSkillId = null;
            LastUsedSkillType = SkillType.Attack;
            LastUsedSkillEffectKind = SkillEffectKind.Default;
            LastUsedSkillWasBasic = false;
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
            BaseDefensePower = ResolveBaseDefensePower(Data);
            baseCriticalChance = Mathf.Clamp01(Data.criticalChance);
            CriticalDamageMultiplier = Mathf.Max(1f, Data.criticalDamageMultiplier);

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            RefreshIntentPreview();
        }

        private static int ResolveBaseDefensePower(EnemySO data)
        {
            if (data == null)
            {
                return 0;
            }

            return Mathf.Max(0, data.baseDefensePower > 0 ? data.baseDefensePower : data.defensePower);
        }

        public int TakeDamage(int damage, float shieldPiercePercent = 0f)
        {
            damage = Mathf.Max(0, damage);

            var piercingDamage = Mathf.CeilToInt(damage * Mathf.Clamp01(shieldPiercePercent));
            var blockableDamage = Mathf.Max(0, damage - piercingDamage);
            var remainingDamage = piercingDamage + Mathf.Max(0, blockableDamage - Block);
            var shieldDamage = Mathf.Min(Block, blockableDamage);
            Block = Mathf.Max(0, Block - blockableDamage);
            ReduceThornRetaliationShield(shieldDamage);
            var hpBefore = CurrentHp;
            var lethalBeforeEndure = CurrentHp > 0 && CurrentHp - remainingDamage <= 0;
            var minimumHp = EndureTurns > 0 && CurrentHp > 0 ? 1 : 0;
            CurrentHp = Mathf.Max(minimumHp, CurrentHp - remainingDamage);
            if (lethalBeforeEndure && CurrentHp > 0 && EndureTurns > 0)
            {
                EndureTurns = 0;
            }

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnBlockChanged?.Invoke(Block);

            if (CurrentHp <= 0)
            {
                OnDead?.Invoke(this);
            }

            return hpBefore - CurrentHp;
        }

        public int TakeStatusDamage(int damage)
        {
            damage = Mathf.Max(0, damage);
            if (damage == 0 || CurrentHp <= 0)
            {
                return 0;
            }

            var hpBefore = CurrentHp;
            var lethalBeforeEndure = CurrentHp > 0 && CurrentHp - damage <= 0;
            var minimumHp = EndureTurns > 0 && CurrentHp > 0 ? 1 : 0;
            CurrentHp = Mathf.Max(minimumHp, CurrentHp - damage);
            if (lethalBeforeEndure && CurrentHp > 0 && EndureTurns > 0)
            {
                EndureTurns = 0;
            }
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            if (CurrentHp <= 0)
            {
                OnDead?.Invoke(this);
            }

            return hpBefore - CurrentHp;
        }

        public int ForceKill()
        {
            if (CurrentHp <= 0)
            {
                return 0;
            }

            var hpBefore = CurrentHp;
            CurrentHp = 0;
            EndureTurns = 0;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnDead?.Invoke(this);
            return hpBefore;
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
            ThornRetaliationShieldHp = Mathf.Max(ThornRetaliationShieldHp, shieldHp);
            ThornRetaliationDamage = Mathf.Max(ThornRetaliationDamage, retaliationDamage);
            OnBlockChanged?.Invoke(Block);
        }

        public void ApplyEndure(int turns)
        {
            EndureTurns = Mathf.Max(EndureTurns, Mathf.Max(1, turns));
        }

        public void ApplyCriticalStageModifier(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CriticalStage = Mathf.Clamp(CriticalStage + amount, 0, MaxCriticalStage);
        }

        public void ClearBlock()
        {
            if (Block == 0 && ThornRetaliationDamage == 0 && ThornRetaliationShieldHp == 0)
            {
                return;
            }

            Block = 0;
            ThornRetaliationDamage = 0;
            ThornRetaliationShieldHp = 0;
            OnBlockChanged?.Invoke(Block);
        }

        private void ReduceThornRetaliationShield(int shieldDamage)
        {
            if (ThornRetaliationShieldHp <= 0)
            {
                if (Block == 0)
                {
                    ThornRetaliationDamage = 0;
                }

                return;
            }

            ThornRetaliationShieldHp = Mathf.Max(0, ThornRetaliationShieldHp - Mathf.Max(0, shieldDamage));
            if (Block == 0 || ThornRetaliationShieldHp == 0)
            {
                ThornRetaliationDamage = 0;
                ThornRetaliationShieldHp = 0;
            }
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

        public void ApplyPoison(int turns, float maxHpDamagePercent)
        {
            turns = Mathf.Max(1, turns);
            maxHpDamagePercent = Mathf.Clamp01(maxHpDamagePercent);
            if (maxHpDamagePercent <= 0f)
            {
                return;
            }

            PoisonTurns = Mathf.Max(PoisonTurns, turns);
            PoisonMaxHpDamagePercent = Mathf.Max(PoisonMaxHpDamagePercent, maxHpDamagePercent);
            RefreshIntentPreview();
        }

        public void ApplyBleed(int turns, int damage)
        {
            turns = Mathf.Max(1, turns);
            damage = Mathf.Max(1, damage);
            BleedTurns = Mathf.Max(BleedTurns, turns);
            BleedDamage = Mathf.Max(BleedDamage, damage);
            RefreshIntentPreview();
        }

        public void ApplyBrand(int damage)
        {
            damage = Mathf.Max(1, damage);
            BrandDamage = Mathf.Max(BrandDamage, damage);
            RefreshIntentPreview();
        }

        public void ExtendPoisonAndBleed(int turns)
        {
            turns = Mathf.Max(0, turns);
            if (turns == 0)
            {
                return;
            }

            var changed = false;
            if (PoisonTurns > 0)
            {
                PoisonTurns += turns;
                changed = true;
            }

            if (BleedTurns > 0)
            {
                BleedTurns += turns;
                changed = true;
            }

            if (changed)
            {
                RefreshIntentPreview();
            }
        }

        public int TriggerOnAttackedStatusDamage()
        {
            var total = 0;
            if (BrandDamage > 0)
            {
                var damage = BrandDamage;
                BrandDamage = 0;
                total += TakeStatusDamage(damage);
                RefreshIntentPreview();
            }

            if (BleedTurns > 0 && BleedDamage > 0)
            {
                total += TakeStatusDamage(BleedDamage);
            }

            return total;
        }

        public int ResolveEndOfTurnStatuses()
        {
            var total = 0;
            var changed = false;
            if (PoisonTurns > 0)
            {
                var poisonDamage = Mathf.Max(1, Mathf.CeilToInt(MaxHp * Mathf.Max(0f, PoisonMaxHpDamagePercent)));
                total += TakeStatusDamage(poisonDamage);
                PoisonTurns--;
                changed = true;
                if (PoisonTurns == 0)
                {
                    PoisonMaxHpDamagePercent = 0f;
                }
            }

            if (BleedTurns > 0)
            {
                BleedTurns--;
                changed = true;
                if (BleedTurns == 0)
                {
                    BleedDamage = 0;
                }
            }

            if (changed)
            {
                RefreshIntentPreview();
            }

            return total;
        }

        public void RecordUsedIntent(EnemyIntent intent)
        {
            if (intent == null)
            {
                return;
            }

            LastUsedSkillId = intent.skillId;
            LastUsedSkillType = intent.skillType;
            LastUsedSkillEffectKind = intent.skillEffectKind;
            LastUsedSkillWasBasic = IsBasicIntent(intent);
        }

        public bool ApplySealFromLastUsedSkill(int turns)
        {
            if (string.IsNullOrWhiteSpace(LastUsedSkillId) || LastUsedSkillWasBasic)
            {
                return false;
            }

            SealedSkillId = LastUsedSkillId;
            SealTurns = Mathf.Max(1, turns);
            RefreshIntentPreview();
            return true;
        }

        public bool IsSkillSealed(SkillSO skill)
        {
            return skill != null && IsSkillIdSealed(skill.skillId);
        }

        public bool IsSkillIdSealed(string skillId)
        {
            return SealTurns > 0 &&
                   !string.IsNullOrWhiteSpace(SealedSkillId) &&
                   !string.IsNullOrWhiteSpace(skillId) &&
                   skillId == SealedSkillId;
        }

        public void ApplyTaunt(int turns)
        {
            TauntTurns = Mathf.Max(TauntTurns, Mathf.Max(1, turns));
            RefreshIntentPreview();
        }

        public void ConsumeTurnRestrictions()
        {
            var changed = false;
            if (SealTurns > 0)
            {
                SealTurns--;
                if (SealTurns == 0)
                {
                    SealedSkillId = null;
                }

                changed = true;
            }

            if (TauntTurns > 0)
            {
                TauntTurns--;
                changed = true;
            }

            if (changed)
            {
                RefreshIntentPreview();
            }
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

        private static bool IsBasicIntent(EnemyIntent intent)
        {
            if (intent == null)
            {
                return true;
            }

            return intent.intentType == EnemyIntentType.Attack &&
                   (string.IsNullOrWhiteSpace(intent.skillId) ||
                    intent.skillEffectKind == SkillEffectKind.BasicAttack);
        }
    }
}
