using System.Collections.Generic;
using NUnit.Framework;
using Project2048.Board2048;
using Project2048.Combat;
using Project2048.Cost;
using Project2048.Enemy;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Tests
{
    public class SkillStatusEffectTests
    {
        private readonly List<Object> ownedObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var ownedObject in ownedObjects)
            {
                if (ownedObject != null)
                {
                    Object.DestroyImmediate(ownedObject);
                }
            }

            ownedObjects.Clear();
        }

        [Test]
        public void StatusSkills_ApplyBleedPoisonBrandAndOpenWound()
        {
            var player = CreatePlayer(maxHp: 100, attackPower: 10);
            var enemy = CreateEnemy(maxHp: 220, attackPower: 0);
            var executor = new SkillExecutor();

            executor.Execute(CreateSkill("bleeding-cut", SkillType.Attack, SkillEffectKind.BleedAttack, power: 0, statusDuration: 2, statusDamage: 20), player, enemy, new DamageCalculator(new System.Random(1)));
            executor.Execute(CreateSkill("poison-coat", SkillType.Attack, SkillEffectKind.PoisonAttack, power: 0, statusDuration: 3, statusMaxHpDamagePercent: 0.05f), player, enemy, new DamageCalculator(new System.Random(1)));

            Assert.That(enemy.BleedTurns, Is.EqualTo(2));
            Assert.That(enemy.PoisonTurns, Is.EqualTo(3));

            executor.Execute(CreateSkill("open-wound", SkillType.Attack, SkillEffectKind.OpenWoundAttack, power: 0, conditionalPowerBonus: 50), player, enemy, new DamageCalculator(new System.Random(1)));

            Assert.That(enemy.BleedTurns, Is.EqualTo(3));
            Assert.That(enemy.PoisonTurns, Is.EqualTo(4));

            executor.Execute(CreateSkill("crack-brand", SkillType.Debuff, SkillEffectKind.CrackBrand, statusDamage: 40), player, enemy, new DamageCalculator(new System.Random(1)));
            player.ApplyNextAttackSplit(2, 1f);
            var hpBefore = enemy.CurrentHp;

            executor.Execute(CreateSkill("basic", SkillType.Attack, SkillEffectKind.BasicAttack, power: 0), player, enemy, new DamageCalculator(new System.Random(1)));

            Assert.That(enemy.BrandDamage, Is.Zero);
            Assert.That(enemy.CurrentHp, Is.LessThanOrEqualTo(hpBefore - 60));
        }

        [Test]
        public void ConditionalAndCostSkills_AdjustDamageShieldAndCost()
        {
            var player = CreatePlayer(maxHp: 100, attackPower: 10);
            var enemy = CreateEnemy(maxHp: 100, attackPower: 0);
            var executor = new SkillExecutor();

            enemy.SpendHp(95, leaveOne: false);
            executor.Execute(CreateSkill("execute", SkillType.Attack, SkillEffectKind.ExecuteAttack, power: 40, conditionalHpThreshold: 0.3f), player, enemy, new DamageCalculator(new System.Random(1)));
            Assert.That(enemy.IsDead, Is.True);

            enemy = CreateEnemy(maxHp: 100, attackPower: 0);
            enemy.AddBlock(100);

            executor.Execute(CreateSkill("piercing-hit-test", SkillType.Attack, SkillEffectKind.BasicAttack, power: 40, shieldPiercePercent: 50), player, enemy, new DamageCalculator(new System.Random(1)));
            Assert.That(enemy.CurrentHp, Is.LessThan(100));
            Assert.That(enemy.Block, Is.LessThan(100));

            var wallet = new ActionCostWallet();
            wallet.SetCost(4);
            executor.Execute(
                CreateSkill("overburn", SkillType.Attack, SkillEffectKind.OverburnAttack, power: 50, extraPowerPerConsumedCost: 10),
                player,
                enemy,
                new DamageCalculator(new System.Random(1)),
                new SkillExecutionContext { CostWallet = wallet });

            Assert.That(wallet.CurrentCost, Is.Zero);
        }

        [Test]
        public void ControlAndBoardSkills_ApplySealTauntCarryCleanseAndCorrosion()
        {
            var player = CreatePlayer(maxHp: 100, attackPower: 10);
            var enemy = CreateEnemy(maxHp: 100, attackPower: 0);
            var executor = new SkillExecutor();

            enemy.RecordUsedIntent(new EnemyIntent
            {
                skillId = "enemy-guard",
                skillType = SkillType.Defense,
                skillEffectKind = SkillEffectKind.BasicDefense,
                intentType = EnemyIntentType.Defense,
            });
            executor.Execute(CreateSkill("seal-skill", SkillType.Debuff, SkillEffectKind.SealSkill, statusDuration: 1), player, enemy, new DamageCalculator(new System.Random(1)));
            Assert.That(enemy.IsSkillIdSealed("enemy-guard"), Is.True);

            executor.Execute(CreateSkill("taunt", SkillType.Debuff, SkillEffectKind.Taunt, targetAttackStageModifier: 2, statusDuration: 1), player, enemy, new DamageCalculator(new System.Random(1)));
            Assert.That(enemy.IsTaunted, Is.True);
            Assert.That(enemy.AttackModifier, Is.EqualTo(2));

            executor.Execute(CreateSkill("afterglow-save", SkillType.Defense, SkillEffectKind.CostCarry, maxCostCarry: 4), player, enemy, new DamageCalculator(new System.Random(1)));
            Assert.That(player.CaptureCostCarry(7), Is.EqualTo(4));
            Assert.That(player.ConsumeCarriedCost(), Is.EqualTo(4));

            var board = new Board2048Manager();
            board.SetBoardState(
                new[,]
                {
                    { Board2048Manager.ObstacleValue, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                0);
            var wallet = new ActionCostWallet();
            executor.Execute(
                CreateSkill("cleanse-hand", SkillType.Defense, SkillEffectKind.DarknessCleanse, costRefund: 2),
                player,
                enemy,
                new DamageCalculator(new System.Random(1)),
                new SkillExecutionContext { BoardManager = board, CostWallet = wallet });

            Assert.That(board.GetBoardSnapshot()[0, 0], Is.Zero);
            Assert.That(wallet.CurrentCost, Is.EqualTo(2));

            new EnemyIntentSystem().ExecuteIntent(
                enemy,
                new EnemyIntent
                {
                    skillType = SkillType.Debuff,
                    skillEffectKind = SkillEffectKind.CostGainDown,
                    intentType = EnemyIntentType.Debuff,
                    nextCostGainModifier = -3,
                },
                player);

            Assert.That(player.ApplyAndConsumeNextTurnCostGainModifiers(10), Is.EqualTo(7));
        }

        [Test]
        public void EnemyAi_RespectsTauntAndSealRestrictions()
        {
            var enemyData = ScriptableObject.CreateInstance<EnemySO>();
            ownedObjects.Add(enemyData);
            enemyData.maxHp = 100;
            enemyData.attackPower = 10;
            enemyData.skills = new List<SkillSO>
            {
                CreateSkill("enemy-guard", SkillType.Defense, SkillEffectKind.BasicDefense, power: 40),
                CreateSkill("enemy-strike", SkillType.Attack, SkillEffectKind.BasicAttack, power: 40),
            };
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    skillId = "enemy-guard",
                    skillType = SkillType.Defense,
                    skillEffectKind = SkillEffectKind.BasicDefense,
                    intentType = EnemyIntentType.Defense,
                    value = 40,
                },
            };

            var player = CreatePlayer(maxHp: 100, attackPower: 10);
            var enemy = CreateGameObject<EnemyController>("Enemy");
            enemy.Init(enemyData);
            enemy.ApplyTaunt(1);

            var intentSystem = new EnemyIntentSystem(new System.Random(1));
            intentSystem.SetNextIntents(enemy, 1, player);

            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Attack));
            Assert.That(enemy.CurrentIntent.skillId, Is.EqualTo("enemy-strike"));

            enemy.RecordUsedIntent(new EnemyIntent
            {
                skillId = "enemy-guard",
                skillType = SkillType.Defense,
                skillEffectKind = SkillEffectKind.BasicDefense,
                intentType = EnemyIntentType.Defense,
            });
            enemy.ApplySealFromLastUsedSkill(1);
            enemy.ConsumeTurnRestrictions();
            enemy.ApplySealFromLastUsedSkill(1);
            intentSystem.SetNextIntents(enemy, 1, player);

            Assert.That(enemy.CurrentIntent.skillId, Is.Not.EqualTo("enemy-guard"));
        }

        [Test]
        public void RankStageModifiers_PersistUntilCombatReset()
        {
            var player = CreatePlayer(maxHp: 100, attackPower: 10);
            var enemy = CreateEnemy(maxHp: 100, attackPower: 10);
            var executor = new SkillExecutor();

            executor.Execute(
                CreateSkill("flash", SkillType.Debuff, SkillEffectKind.AttackStageDown, targetAttackStageModifier: -1),
                player,
                enemy,
                new DamageCalculator(new System.Random(1)));
            executor.Execute(
                CreateSkill("howl", SkillType.Debuff, SkillEffectKind.DefenseStageDown, targetDefenseStageModifier: -1),
                player,
                enemy,
                new DamageCalculator(new System.Random(1)));
            executor.Execute(
                CreateSkill("iron-wall", SkillType.Defense, SkillEffectKind.DefenseStageUp, selfDefenseStageModifier: 2),
                player,
                enemy,
                new DamageCalculator(new System.Random(1)));
            player.ApplyCriticalStageModifier(1);
            enemy.ApplyTaunt(1);

            enemy.ConsumeTurnRestrictions();
            player.ClearTurnLimitedSkillEffects();
            player.ResolveEndOfTurnStatuses();
            enemy.ResolveEndOfTurnStatuses();

            Assert.That(enemy.AttackModifier, Is.EqualTo(-1));
            Assert.That(enemy.DefenseModifier, Is.EqualTo(-1));
            Assert.That(player.DefenseStage, Is.EqualTo(2));
            Assert.That(player.CriticalStage, Is.EqualTo(1));

            player.Init(player.Data);
            enemy.Init(enemy.Data);

            Assert.That(enemy.AttackModifier, Is.Zero);
            Assert.That(enemy.DefenseModifier, Is.Zero);
            Assert.That(player.AttackStage, Is.Zero);
            Assert.That(player.DefenseStage, Is.Zero);
            Assert.That(player.CriticalStage, Is.Zero);
        }

        private PlayerCombatController CreatePlayer(int maxHp, int attackPower)
        {
            var controller = CreateGameObject<PlayerCombatController>("Player");
            var data = ScriptableObject.CreateInstance<PlayerSO>();
            ownedObjects.Add(data);
            data.maxHp = maxHp;
            data.attackPower = attackPower;
            data.baseDefensePower = 10;
            data.criticalChance = 0f;
            data.criticalDamageMultiplier = 1f;
            controller.Init(data);
            return controller;
        }

        private EnemyController CreateEnemy(int maxHp, int attackPower)
        {
            var controller = CreateGameObject<EnemyController>("Enemy");
            var data = ScriptableObject.CreateInstance<EnemySO>();
            ownedObjects.Add(data);
            data.maxHp = maxHp;
            data.attackPower = attackPower;
            data.baseDefensePower = 10;
            data.criticalChance = 0f;
            data.criticalDamageMultiplier = 1f;
            controller.Init(data);
            return controller;
        }

        private SkillSO CreateSkill(
            string skillId,
            SkillType skillType,
            SkillEffectKind effectKind,
            int cost = 0,
            int power = 0,
            int targetAttackStageModifier = 0,
            int targetDefenseStageModifier = 0,
            int selfDefenseStageModifier = 0,
            int statusDuration = 0,
            int statusDamage = 0,
            float statusMaxHpDamagePercent = 0f,
            int conditionalPowerBonus = 0,
            float conditionalHpThreshold = 0f,
            int shieldPiercePercent = 0,
            int extraPowerPerConsumedCost = 0,
            int maxCostCarry = 0,
            int costRefund = 0)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            ownedObjects.Add(skill);
            skill.skillId = skillId;
            skill.skillType = skillType;
            skill.effectKind = effectKind;
            skill.cost = cost;
            skill.power = power;
            skill.targetAttackStageModifier = targetAttackStageModifier;
            skill.targetDefenseStageModifier = targetDefenseStageModifier;
            skill.selfDefenseStageModifier = selfDefenseStageModifier;
            skill.statusDuration = statusDuration;
            skill.statusDamage = statusDamage;
            skill.statusMaxHpDamagePercent = statusMaxHpDamagePercent;
            skill.conditionalPowerBonus = conditionalPowerBonus;
            skill.conditionalHpThreshold = conditionalHpThreshold;
            skill.shieldPiercePercent = shieldPiercePercent;
            skill.extraPowerPerConsumedCost = extraPowerPerConsumedCost;
            skill.maxCostCarry = maxCostCarry;
            skill.costRefund = costRefund;
            return skill;
        }

        private T CreateGameObject<T>(string name)
            where T : Component
        {
            var gameObject = new GameObject(name);
            ownedObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }
    }
}
