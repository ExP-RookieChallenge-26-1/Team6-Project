using System.Collections.Generic;
using Project2048.Combat;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Enemy
{
    public class EnemyAiBrain
    {
        private const int MovePowerScale = 10;
        private const float DuplicateActionPenalty = 35f;

        private readonly System.Random random;

        public EnemyAiBrain(System.Random random = null)
        {
            this.random = random ?? new System.Random();
        }

        public EnemyIntent ChooseIntent(EnemySO data, int turnIndex)
        {
            var plan = ChooseIntents(data, count: 1, turnIndex);
            return plan.Count > 0 ? plan[0] : null;
        }

        public List<EnemyIntent> ChooseIntents(EnemySO data, int count, int turnIndex)
        {
            return ChooseIntents(data, null, null, count, turnIndex);
        }

        public List<EnemyIntent> ChooseIntents(
            EnemyController enemy,
            PlayerCombatController player,
            int count,
            int turnIndex)
        {
            return ChooseIntents(enemy?.Data, enemy, player, count, turnIndex);
        }

        private List<EnemyIntent> ChooseIntents(
            EnemySO data,
            EnemyController enemy,
            PlayerCombatController player,
            int count,
            int turnIndex)
        {
            var plan = new List<EnemyIntent>();
            if (data == null)
            {
                return plan;
            }

            var intentCount = Mathf.Clamp(count, 1, EnemySO.MaximumActionsPerTurn);
            var usedSignatures = new Dictionary<string, int>();
            var plannedEnemyBlock = enemy?.Block ?? 0;
            var plannedThornGuard = enemy?.ThornRetaliationDamage > 0;

            for (var slotIndex = 0; slotIndex < intentCount; slotIndex++)
            {
                var actionIndex = turnIndex + slotIndex;
                var candidates = BuildCandidates(data, actionIndex);
                if (candidates.Count == 0)
                {
                    candidates.Add(BuildAttackIntent(data));
                }

                var selected = SelectBestCandidate(
                    candidates,
                    data,
                    enemy,
                    player,
                    actionIndex,
                    usedSignatures,
                    plannedEnemyBlock,
                    plannedThornGuard);

                if (selected == null)
                {
                    continue;
                }

                plan.Add(selected.Clone());
                var signature = GetIntentSignature(selected);
                usedSignatures.TryGetValue(signature, out var usedCount);
                usedSignatures[signature] = usedCount + 1;

                if (selected.intentType == EnemyIntentType.Defense)
                {
                    plannedEnemyBlock += Mathf.Max(0, selected.value);
                    plannedThornGuard |= selected.isThornGuard;
                }
            }

            return plan;
        }

        private List<EnemyIntent> BuildCandidates(EnemySO data, int actionIndex)
        {
            var candidates = new List<EnemyIntent>();

            if (CanUseSpecialActions(data) &&
                data.canUseBullRush &&
                data.bullRushInterval > 0 &&
                (actionIndex + 1) % data.bullRushInterval == 0)
            {
                candidates.Add(BuildBullRushIntent(data));
            }

            AddSkillCandidates(data, candidates);
            candidates.Add(BuildAttackIntent(data));
            candidates.Add(BuildDefenseIntent(data));

            if (IsDebuffDue(data, actionIndex) || HasNoDebuffSkillCandidate(candidates))
            {
                candidates.Add(BuildDebuffIntent(data, ResolveDebuffIndex(data, actionIndex)));
            }

            return candidates;
        }

        private static void AddSkillCandidates(EnemySO data, List<EnemyIntent> candidates)
        {
            if (data.skills == null || data.skills.Count == 0)
            {
                return;
            }

            for (var index = 0; index < data.skills.Count && index < EnemySO.MaxEquippedSkillSlots; index++)
            {
                var skill = data.skills[index];
                if (skill == null || !skill.CanEnemyUse)
                {
                    continue;
                }

                candidates.Add(BuildSkillIntent(data, skill));
            }
        }

        private EnemyIntent SelectBestCandidate(
            List<EnemyIntent> candidates,
            EnemySO data,
            EnemyController enemy,
            PlayerCombatController player,
            int actionIndex,
            Dictionary<string, int> usedSignatures,
            int plannedEnemyBlock,
            bool plannedThornGuard)
        {
            EnemyIntent bestIntent = null;
            var bestScore = float.MinValue;

            foreach (var candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                var score = ScoreCandidate(
                    candidate,
                    data,
                    enemy,
                    player,
                    actionIndex,
                    usedSignatures,
                    plannedEnemyBlock,
                    plannedThornGuard);

                score += (float)random.NextDouble() * 0.01f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIntent = candidate;
                }
            }

            return bestIntent;
        }

        private static float ScoreCandidate(
            EnemyIntent intent,
            EnemySO data,
            EnemyController enemy,
            PlayerCombatController player,
            int actionIndex,
            Dictionary<string, int> usedSignatures,
            int plannedEnemyBlock,
            bool plannedThornGuard)
        {
            var score = intent.intentType switch
            {
                EnemyIntentType.Attack => ScoreAttack(intent, data, enemy, player),
                EnemyIntentType.Defense => ScoreDefense(intent, data, enemy, player, plannedEnemyBlock, plannedThornGuard),
                EnemyIntentType.Debuff => ScoreDebuff(intent, data, player, actionIndex),
                _ => 0f,
            };

            var signature = GetIntentSignature(intent);
            if (usedSignatures.TryGetValue(signature, out var usedCount) && usedCount > 0)
            {
                var canRepeatForLethal = intent.intentType == EnemyIntentType.Attack &&
                    CanLikelyKill(intent, data, enemy, player);
                score -= canRepeatForLethal
                    ? usedCount * 5f
                    : usedCount * DuplicateActionPenalty;
            }

            return score;
        }

        private static float ScoreAttack(
            EnemyIntent intent,
            EnemySO data,
            EnemyController enemy,
            PlayerCombatController player)
        {
            var expectedDamage = EstimateDamage(intent, data, enemy, player);
            var playerShield = player?.ShieldHp ?? 0;
            var expectedHpDamage = player != null
                ? Mathf.Max(0, expectedDamage - playerShield)
                : expectedDamage;

            var score = 30f + expectedDamage * 1.25f + expectedHpDamage * 3f;

            if (player != null && expectedHpDamage >= player.CurrentHp)
            {
                score += data.aiActionBias == EnemyAiActionBias.AttackHeavy ? 1200f : 1000f;
            }

            if (playerShield > 0 && expectedHpDamage <= 0)
            {
                score -= Mathf.Min(35f, playerShield * 1.5f);
            }

            var enemyMissingRatio = ResolveMissingHpRatio(enemy, data);
            if (intent.lifeStealPercent > 0f)
            {
                score += 20f + enemyMissingRatio * 90f;
            }

            if (intent.damageStatSource == DamageStatSource.ShieldHp)
            {
                var shield = enemy?.ShieldHp ?? 0;
                score += shield > 0 ? shield * 3f : -45f;
            }

            if (intent.damageStatSource == DamageStatSource.DefensePower)
            {
                score += (enemy?.EffectiveDefensePower ?? data.defensePower) * 2f;
            }

            if (intent.nextBoardMoveCountModifier < 0 ||
                intent.nextCostGainModifier < 0 ||
                intent.nextCostGainMultiplier < 1f)
            {
                score += 35f;
            }

            return data.aiActionBias switch
            {
                EnemyAiActionBias.AttackHeavy => score * 1.35f,
                EnemyAiActionBias.DefenseHeavy => score * 0.78f,
                _ => score,
            };
        }

        private static float ScoreDefense(
            EnemyIntent intent,
            EnemySO data,
            EnemyController enemy,
            PlayerCombatController player,
            int plannedEnemyBlock,
            bool plannedThornGuard)
        {
            var missingHpRatio = ResolveMissingHpRatio(enemy, data);
            var playerAttackPressure = player?.EffectiveAttackPower ?? 0;
            var score = 25f + missingHpRatio * 95f + playerAttackPressure * 2.5f;

            if (plannedEnemyBlock > data.maxHp / 2)
            {
                score -= 45f;
            }

            if (intent.value <= 0 && intent.selfDefensePowerModifier == 0)
            {
                score -= 25f;
            }

            if (intent.selfDefensePowerModifier > 0)
            {
                score += 20f + intent.selfDefensePowerModifier * 12f;
            }

            if (intent.isThornGuard)
            {
                score += 25f + playerAttackPressure * 2f;
                if (plannedThornGuard)
                {
                    score -= 60f;
                }
            }

            return data.aiActionBias switch
            {
                EnemyAiActionBias.DefenseHeavy => score * 1.45f,
                EnemyAiActionBias.AttackHeavy => score * 0.65f,
                _ => score,
            };
        }

        private static float ScoreDebuff(
            EnemyIntent intent,
            EnemySO data,
            PlayerCombatController player,
            int actionIndex)
        {
            var due = IsDebuffDue(data, actionIndex);
            var preferredType = ResolveDebuffType(data.aiDebuffPattern, ResolveDebuffIndex(data, actionIndex));
            var candidateType = ResolveDebuffIntentType(intent);
            var score = 18f;

            if (due)
            {
                score += 85f;
            }

            if (candidateType == preferredType)
            {
                score += due ? 35f : 15f;
            }

            if (candidateType == DebuffType.Darkness)
            {
                score += 20f;
                if (player != null && player.NextTurnBoardMoveCountModifier < 0)
                {
                    score -= 25f;
                }
            }

            if (candidateType == DebuffType.Fear)
            {
                score += player != null ? player.EffectiveAttackPower * 2f : 10f;
                if (player != null && player.FearStacks > 0)
                {
                    score -= 45f;
                }
            }

            if (intent.nextBoardMoveCountModifier < 0)
            {
                score += 35f;
            }

            if (intent.nextCostGainModifier < 0 || intent.nextCostGainMultiplier < 1f)
            {
                score += 40f;
                if (player != null &&
                    (!Mathf.Approximately(player.NextTurnCostGainMultiplier, 1f) || player.NextTurnCostGainModifier != 0))
                {
                    score -= 30f;
                }
            }

            return data.aiActionBias switch
            {
                EnemyAiActionBias.AttackHeavy => score * 0.9f,
                EnemyAiActionBias.DefenseHeavy => score * 1.05f,
                _ => score,
            };
        }

        private static EnemyIntent BuildSkillIntent(EnemySO data, SkillSO skill)
        {
            var effectKind = skill.ResolveEffectKind();
            var intent = new EnemyIntent
            {
                skillId = skill.skillId,
                displayName = string.IsNullOrWhiteSpace(skill.skillName) ? skill.skillId : skill.skillName,
                skillEffectKind = effectKind,
                damageStatSource = ResolveEnemyDamageStatSource(skill),
                hpCost = skill.hpCost,
                hpCostLeavesOne = skill.hpCostLeavesOne,
                lifeStealPercent = skill.lifeStealPercent,
                nextBoardMoveCountModifier = skill.nextBoardMoveCountModifier,
                nextCostGainModifier = skill.nextCostGainModifier,
                nextCostGainMultiplier = skill.nextCostGainMultiplier,
                targetAttackModifier = skill.targetAttackModifier,
                targetDefenseModifier = skill.targetDefenseModifier,
                selfDefensePowerModifier = skill.selfDefensePowerModifier,
            };

            switch (effectKind)
            {
                case SkillEffectKind.AttackStageDown:
                    intent.intentType = EnemyIntentType.Debuff;
                    intent.targetAttackModifier = ResolveStageModifier(skill.targetAttackStageModifier, skill.targetAttackModifier, -1);
                    intent.value = Mathf.Abs(intent.targetAttackModifier);
                    return intent;
                case SkillEffectKind.DefenseStageDown:
                    intent.intentType = EnemyIntentType.Debuff;
                    intent.targetDefenseModifier = ResolveStageModifier(skill.targetDefenseStageModifier, skill.targetDefenseModifier, -1);
                    intent.value = Mathf.Abs(intent.targetDefenseModifier);
                    return intent;
                case SkillEffectKind.CostGainDown:
                    intent.intentType = EnemyIntentType.Debuff;
                    intent.value = ResolveCostGainDebuffValue(skill);
                    return intent;
                case SkillEffectKind.BoardObstacleDebuff:
                    intent.intentType = EnemyIntentType.Debuff;
                    intent.debuffType = skill.debuffType == DebuffType.None ? DebuffType.Darkness : skill.debuffType;
                    intent.value = ScaleByStrength(Mathf.Max(1, skill.debuffValue), data.aiStrength);
                    return intent;
                case SkillEffectKind.ThornGuard:
                    intent.intentType = EnemyIntentType.Defense;
                    intent.value = ScaleByStrength(skill.power, data.aiStrength);
                    intent.isThornGuard = true;
                    intent.retaliationDamage = ScaleByStrength(skill.selfThornRetaliationDamage, data.aiStrength);
                    return intent;
                case SkillEffectKind.BasicDefense:
                    intent.intentType = EnemyIntentType.Defense;
                    intent.value = ScaleByStrength(skill.power, data.aiStrength);
                    return intent;
                case SkillEffectKind.DefenseStageUp:
                    intent.intentType = EnemyIntentType.Defense;
                    intent.value = 0;
                    intent.selfDefensePowerModifier = ResolveStageModifier(skill.selfDefenseStageModifier, skill.selfDefensePowerModifier, 1);
                    return intent;
                case SkillEffectKind.ChargeAttack:
                    intent.intentType = EnemyIntentType.Attack;
                    intent.movePower = ScaleByStrength(Mathf.Max(skill.chargedPower, skill.power), data.aiStrength);
                    intent.value = intent.movePower;
                    return intent;
                default:
                    intent.intentType = EnemyIntentType.Attack;
                    intent.movePower = ScaleByStrength(skill.power, data.aiStrength);
                    intent.value = intent.movePower;
                    return intent;
            }
        }

        private static EnemyIntent BuildAttackIntent(EnemySO data)
        {
            return new EnemyIntent
            {
                intentType = EnemyIntentType.Attack,
                damageStatSource = DamageStatSource.AttackPower,
                movePower = ScaleByStrength(data.attackPower * MovePowerScale, data.aiStrength),
                value = ScaleByStrength(data.attackPower, data.aiStrength),
            };
        }

        private static EnemyIntent BuildDefenseIntent(EnemySO data)
        {
            if (CanUseSpecialActions(data) && data.canUseThornGuard)
            {
                return new EnemyIntent
                {
                    displayName = "가시 방어",
                    skillEffectKind = SkillEffectKind.ThornGuard,
                    intentType = EnemyIntentType.Defense,
                    value = ScaleByStrength(data.thornGuardShieldHp, data.aiStrength),
                    isThornGuard = true,
                    retaliationDamage = ScaleByStrength(data.thornGuardRetaliationDamage, data.aiStrength),
                };
            }

            return new EnemyIntent
            {
                intentType = EnemyIntentType.Defense,
                value = ScaleByStrength(data.defensePower, data.aiStrength),
            };
        }

        private static EnemyIntent BuildBullRushIntent(EnemySO data)
        {
            return new EnemyIntent
            {
                displayName = "황소 돌진",
                intentType = EnemyIntentType.Attack,
                damageStatSource = DamageStatSource.AttackPower,
                movePower = ScaleByStrength(data.bullRushBonusDamage * MovePowerScale, data.aiStrength),
                value = ScaleByStrength(data.attackPower + data.bullRushBonusDamage, data.aiStrength),
            };
        }

        private static EnemyIntent BuildDebuffIntent(EnemySO data, int debuffIndex)
        {
            return new EnemyIntent
            {
                intentType = EnemyIntentType.Debuff,
                debuffType = ResolveDebuffType(data.aiDebuffPattern, debuffIndex),
                value = ScaleByStrength(data.debuffPower, data.aiStrength),
            };
        }

        private static int EstimateDamage(
            EnemyIntent intent,
            EnemySO data,
            EnemyController enemy,
            PlayerCombatController player)
        {
            var attackStat = ResolveEnemyDamageStat(data, enemy, intent.damageStatSource);
            var movePower = intent.movePower > 0
                ? intent.movePower
                : Mathf.Max(0, intent.value) * MovePowerScale;
            if (attackStat <= 0 || movePower <= 0)
            {
                return 0;
            }

            var defensePower = Mathf.Max(1, player?.EffectiveDefensePower ?? 1);
            var baseDamage = (movePower / (float)MovePowerScale) * (attackStat / (float)defensePower);
            return Mathf.Max(1, Mathf.CeilToInt(baseDamage * 0.925f));
        }

        private static int ResolveEnemyDamageStat(EnemySO data, EnemyController enemy, DamageStatSource statSource)
        {
            return statSource switch
            {
                DamageStatSource.DefensePower => Mathf.Max(0, enemy?.EffectiveDefensePower ?? data.defensePower),
                DamageStatSource.ShieldHp => Mathf.Max(0, enemy?.ShieldHp ?? 0),
                _ => Mathf.Max(0, enemy?.EffectiveAttackPower ?? ScaleByStrength(data.attackPower, data.aiStrength)),
            };
        }

        private static DamageStatSource ResolveEnemyDamageStatSource(SkillSO skill)
        {
            return skill.ResolveEffectKind() switch
            {
                SkillEffectKind.ShieldScalingAttack => DamageStatSource.ShieldHp,
                SkillEffectKind.ShieldBurstAttack => DamageStatSource.ShieldHp,
                SkillEffectKind.DefenseScalingAttack => DamageStatSource.DefensePower,
                _ => skill.damageStatSource,
            };
        }

        private static bool CanLikelyKill(
            EnemyIntent intent,
            EnemySO data,
            EnemyController enemy,
            PlayerCombatController player)
        {
            if (player == null)
            {
                return false;
            }

            return Mathf.Max(0, EstimateDamage(intent, data, enemy, player) - player.ShieldHp) >= player.CurrentHp;
        }

        private static float ResolveMissingHpRatio(EnemyController enemy, EnemySO data)
        {
            var maxHp = Mathf.Max(1, enemy?.MaxHp ?? data.maxHp);
            var currentHp = Mathf.Clamp(enemy?.CurrentHp ?? maxHp, 0, maxHp);
            return 1f - currentHp / (float)maxHp;
        }

        private static bool HasNoDebuffSkillCandidate(List<EnemyIntent> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (candidate != null && candidate.intentType == EnemyIntentType.Debuff)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsDebuffDue(EnemySO data, int actionIndex)
        {
            var interval = Mathf.Max(0, data.aiDebuffInterval);
            return interval > 0 && (actionIndex + 1) % interval == 0;
        }

        private static int ResolveDebuffIndex(EnemySO data, int actionIndex)
        {
            var interval = Mathf.Max(1, data.aiDebuffInterval);
            return Mathf.Max(0, ((actionIndex + 1) / interval) - 1);
        }

        private static DebuffType ResolveDebuffIntentType(EnemyIntent intent)
        {
            if (intent.debuffType != DebuffType.None)
            {
                return intent.debuffType;
            }

            return intent.skillEffectKind switch
            {
                SkillEffectKind.AttackStageDown => DebuffType.Fear,
                SkillEffectKind.CostGainDown => DebuffType.Darkness,
                SkillEffectKind.BoardObstacleDebuff => DebuffType.Darkness,
                _ => DebuffType.None,
            };
        }

        private static DebuffType ResolveDebuffType(EnemyDebuffPattern pattern, int debuffIndex)
        {
            var even = debuffIndex % 2 == 0;
            return pattern switch
            {
                EnemyDebuffPattern.DarknessThenFear => even ? DebuffType.Darkness : DebuffType.Fear,
                _ => even ? DebuffType.Fear : DebuffType.Darkness,
            };
        }

        private static string GetIntentSignature(EnemyIntent intent)
        {
            if (!string.IsNullOrWhiteSpace(intent.skillId))
            {
                return intent.skillId;
            }

            return $"{intent.intentType}:{intent.skillEffectKind}:{intent.debuffType}:{intent.isThornGuard}";
        }

        private static int ScaleByStrength(int value, EnemyAiStrength strength)
        {
            var baseValue = Mathf.Max(0, value);
            return strength == EnemyAiStrength.Enhanced
                ? Mathf.CeilToInt(baseValue * 1.5f)
                : baseValue;
        }

        private static int ResolveStageModifier(int stageModifier, int legacyModifier, int fallback)
        {
            if (stageModifier != 0)
            {
                return stageModifier;
            }

            if (legacyModifier != 0)
            {
                return legacyModifier;
            }

            return fallback;
        }

        private static int ResolveCostGainDebuffValue(SkillSO skill)
        {
            if (skill == null)
            {
                return 0;
            }

            if (skill.nextCostGainModifier != 0)
            {
                return Mathf.Abs(skill.nextCostGainModifier);
            }

            return Mathf.RoundToInt(Mathf.Abs(1f - skill.nextCostGainMultiplier) * 100f);
        }

        private static bool CanUseSpecialActions(EnemySO data)
        {
            return data.encounterRank != EnemyEncounterRank.Normal;
        }
    }
}
