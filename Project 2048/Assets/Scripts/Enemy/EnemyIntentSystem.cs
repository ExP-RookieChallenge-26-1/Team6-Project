using System.Collections.Generic;
using Project2048.Board2048;
using Project2048.Combat;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Enemy
{
    /// <summary>
    /// 적의 다음 행동을 미리 정하고, 적 턴에 그 행동을 실행한다.
    /// 인텐트는 UI에 공개되는 정보라서 실행 전에 EnemyController.CurrentIntent에 저장한다.
    /// </summary>
    public class EnemyIntentSystem
    {
        private readonly Dictionary<EnemyController, int> intentIndexMap = new();
        private readonly EnemyAiBrain aiBrain;

        public EnemyIntentSystem()
            : this(null)
        {
        }

        public EnemyIntentSystem(System.Random random)
        {
            aiBrain = new EnemyAiBrain(random);
        }

        public void SetNextIntent(EnemyController enemy)
        {
            SetNextIntents(enemy, 1);
        }

        public void SetNextIntents(EnemyController enemy, int count)
        {
            if (enemy == null || enemy.Data == null)
            {
                return;
            }

            // 적마다 패턴 진행 위치를 따로 기억합니다. 같은 EnemySO를 쓰더라도 컨트롤러별 턴 순서는 분리됩니다.
            if (!intentIndexMap.ContainsKey(enemy))
            {
                intentIndexMap[enemy] = 0;
            }

            var intentCount = System.Math.Max(1, System.Math.Min(count, EnemySO.MaximumActionsPerTurn));
            var nextIntents = new List<EnemyIntent>(intentCount);
            for (var index = 0; index < intentCount; index++)
            {
                var nextIntent = ResolveNextIntent(enemy);
                if (nextIntent != null)
                {
                    nextIntents.Add(nextIntent);
                }

                intentIndexMap[enemy]++;
            }

            enemy.SetIntents(nextIntents);
        }

        public void ExecuteIntent(
            EnemyController enemy,
            PlayerCombatController player,
            DamageCalculator damageCalculator = null,
            Board2048Manager boardManager = null)
        {
            ExecuteIntent(enemy, enemy?.CurrentIntent, player, damageCalculator, boardManager);
        }

        public void ExecuteIntent(
            EnemyController enemy,
            EnemyIntent intent,
            PlayerCombatController player,
            DamageCalculator damageCalculator = null,
            Board2048Manager boardManager = null)
        {
            if (enemy == null || player == null || intent == null)
            {
                return;
            }

            damageCalculator ??= new DamageCalculator();

            switch (intent.intentType)
            {
                case EnemyIntentType.Attack:
                    enemy.SpendHp(intent.hpCost, intent.hpCostLeavesOne);
                    var damage = damageCalculator.CalculateEnemyDamage(enemy, intent, player);
                    var shieldBeforeHit = player.ShieldHp;
                    var shouldRetaliate = shieldBeforeHit > 0 && player.ThornRetaliationDamage > 0;
                    var retaliationPower = player.ThornRetaliationDamage;
                    var hpDamage = player.TakeDamage(damage);
                    if (intent.lifeStealPercent > 0f && hpDamage > 0)
                    {
                        enemy.RestoreHp(Mathf.CeilToInt(hpDamage * Mathf.Clamp01(intent.lifeStealPercent)));
                    }

                    if (intent.nextBoardMoveCountModifier != 0)
                    {
                        player.ApplyNextTurnBoardMoveCountModifier(intent.nextBoardMoveCountModifier);
                    }

                    if (shouldRetaliate && retaliationPower > 0)
                    {
                        var retaliationDamage = damageCalculator.CalculatePlayerSkillDamageFromStat(
                            shieldBeforeHit,
                            retaliationPower,
                            enemy,
                            player.CriticalChance,
                            player.CriticalDamageMultiplier);
                        enemy.TakeDamage(retaliationDamage);
                    }

                    var counterDamage = player.CalculateCounterDamage(hpDamage);
                    if (counterDamage > 0)
                    {
                        enemy.TakeDamage(counterDamage);
                    }
                    break;
                case EnemyIntentType.Defense:
                    if (intent.isThornGuard)
                    {
                        enemy.ApplyThornGuard(intent.value, intent.retaliationDamage);
                    }
                    else
                    {
                        enemy.AddBlock(intent.value);
                    }

                    if (intent.selfDefensePowerModifier != 0)
                    {
                        enemy.ApplyDefenseModifier(intent.selfDefensePowerModifier);
                    }
                    break;
                case EnemyIntentType.Debuff:
                    ApplyDebuff(intent, player, boardManager);
                    break;
            }
        }

        private EnemyIntent ResolveNextIntent(EnemyController enemy)
        {
            var pattern = enemy.Data.intentPattern;
            if (pattern == null || pattern.Count == 0)
            {
                return aiBrain.ChooseIntent(enemy.Data, intentIndexMap[enemy]);
            }

            var index = intentIndexMap[enemy] % pattern.Count;
            return pattern[index]?.Clone();
        }

        private static void ApplyDebuff(EnemyIntent intent, PlayerCombatController player, Board2048Manager boardManager)
        {
            if (intent.skillEffectKind == SkillEffectKind.AttackStageDown)
            {
                player.ApplyAttackPowerModifier(intent.targetAttackModifier != 0
                    ? intent.targetAttackModifier
                    : -Mathf.Max(0, intent.value));
                return;
            }

            if (intent.skillEffectKind == SkillEffectKind.DefenseStageDown)
            {
                player.ApplyDefensePowerModifier(intent.targetDefenseModifier != 0
                    ? intent.targetDefenseModifier
                    : -Mathf.Max(0, intent.value));
                return;
            }

            switch (intent.debuffType)
            {
                case DebuffType.Fear:
                    if (intent.value > 0)
                    {
                        player.ApplyFear(intent.value);
                    }
                    break;
                case DebuffType.Darkness:
                    boardManager?.QueueObstacles(intent.value);
                    break;
            }
        }
    }
}
