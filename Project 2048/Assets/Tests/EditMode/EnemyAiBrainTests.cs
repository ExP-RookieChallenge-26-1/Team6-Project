using System;
using System.Collections.Generic;
using NUnit.Framework;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Tests
{
    public class EnemyAiBrainTests
    {
        private readonly List<UnityEngine.Object> ownedObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var ownedObject in ownedObjects)
            {
                if (ownedObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(ownedObject);
                }
            }

            ownedObjects.Clear();
        }

        [Test]
        public void SetNextIntent_UsesExplicitIntentPatternBeforeAiBrain()
        {
            var enemy = CreateEnemy("PatternEnemy");
            var data = CreateEnemyData();
            data.aiActionBias = EnemyAiActionBias.DefenseHeavy;
            data.intentPattern = new List<EnemyIntent>
            {
                new EnemyIntent
                {
                    intentType = EnemyIntentType.Attack,
                    value = 9,
                },
            };

            enemy.Init(data);

            new EnemyIntentSystem(new System.Random(1)).SetNextIntent(enemy);

            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Attack));
            Assert.That(enemy.CurrentIntent.value, Is.EqualTo(9));
        }

        [Test]
        public void Reset_RestartsPatternForReusedController()
        {
            var enemy = CreateEnemy("ReusedEnemy");
            var data = CreateEnemyData();
            data.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Attack,
                    value = 9,
                },
                new()
                {
                    intentType = EnemyIntentType.Defense,
                    value = 5,
                },
            };
            enemy.Init(data);

            var system = new EnemyIntentSystem(new System.Random(1));
            system.SetNextIntent(enemy);
            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Attack));

            system.SetNextIntent(enemy);
            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Defense));

            system.Reset();
            system.SetNextIntent(enemy);

            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Attack));
            Assert.That(enemy.CurrentIntent.value, Is.EqualTo(9));
        }

        [Test]
        public void SetNextIntent_WhenPatternIsEmpty_InsertsDebuffsByConfiguredPattern()
        {
            var enemy = CreateEnemy("BrainEnemy");
            var data = CreateEnemyData();
            data.aiActionBias = EnemyAiActionBias.Balanced;
            data.aiDebuffPattern = EnemyDebuffPattern.FearThenDarkness;
            data.aiDebuffInterval = 2;
            data.debuffPower = 3;
            data.intentPattern.Clear();
            enemy.Init(data);

            var system = new EnemyIntentSystem(new System.Random(2));

            system.SetNextIntent(enemy);
            Assert.That(enemy.CurrentIntent.intentType, Is.Not.EqualTo(EnemyIntentType.Debuff));

            system.SetNextIntent(enemy);
            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Debuff));
            Assert.That(enemy.CurrentIntent.debuffType, Is.EqualTo(DebuffType.Fear));
            Assert.That(enemy.CurrentIntent.value, Is.EqualTo(3));

            system.SetNextIntent(enemy);
            Assert.That(enemy.CurrentIntent.intentType, Is.Not.EqualTo(EnemyIntentType.Debuff));

            system.SetNextIntent(enemy);
            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Debuff));
            Assert.That(enemy.CurrentIntent.debuffType, Is.EqualTo(DebuffType.Darkness));
        }

        [Test]
        public void SetNextIntent_AttackAndDefenseBiasesChangeWeightedSelection()
        {
            var attackEnemy = CreateEnemy("AttackEnemy");
            var attackData = CreateEnemyData();
            attackData.aiActionBias = EnemyAiActionBias.AttackHeavy;
            attackData.aiDebuffInterval = 0;
            attackData.intentPattern.Clear();
            attackEnemy.Init(attackData);

            var defenseEnemy = CreateEnemy("DefenseEnemy");
            var defenseData = CreateEnemyData();
            defenseData.aiActionBias = EnemyAiActionBias.DefenseHeavy;
            defenseData.aiDebuffInterval = 0;
            defenseData.intentPattern.Clear();
            defenseEnemy.Init(defenseData);

            var attackSystem = new EnemyIntentSystem(new System.Random(11));
            var defenseSystem = new EnemyIntentSystem(new System.Random(11));
            var attackCounts = CountActions(attackSystem, attackEnemy, 200);
            var defenseCounts = CountActions(defenseSystem, defenseEnemy, 200);

            Assert.That(attackCounts.Attacks, Is.GreaterThan(attackCounts.Defenses));
            Assert.That(defenseCounts.Defenses, Is.GreaterThan(defenseCounts.Attacks));
        }

        [Test]
        public void SetNextIntent_EnhancedAiStrengthIncreasesGeneratedIntentValues()
        {
            var normalEnemy = CreateEnemy("NormalEnemy");
            var normalData = CreateEnemyData();
            normalData.aiActionBias = EnemyAiActionBias.AttackHeavy;
            normalData.aiDebuffInterval = 0;
            normalData.aiStrength = EnemyAiStrength.Normal;
            normalData.attackPower = 4;
            normalData.intentPattern.Clear();
            normalEnemy.Init(normalData);

            var enhancedEnemy = CreateEnemy("EnhancedEnemy");
            var enhancedData = CreateEnemyData();
            enhancedData.aiActionBias = EnemyAiActionBias.AttackHeavy;
            enhancedData.aiDebuffInterval = 0;
            enhancedData.aiStrength = EnemyAiStrength.Enhanced;
            enhancedData.attackPower = 4;
            enhancedData.intentPattern.Clear();
            enhancedEnemy.Init(enhancedData);

            new EnemyIntentSystem(new System.Random(0)).SetNextIntent(normalEnemy);
            new EnemyIntentSystem(new System.Random(0)).SetNextIntent(enhancedEnemy);

            Assert.That(normalEnemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Attack));
            Assert.That(enhancedEnemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Attack));
            Assert.That(enhancedEnemy.CurrentIntent.value, Is.GreaterThan(normalEnemy.CurrentIntent.value));
        }

        [Test]
        public void SetNextIntent_EliteThornGuardCreatesRetaliatingShield()
        {
            var enemy = CreateEnemy("EliteHedgehog");
            var data = CreateEnemyData();
            data.encounterRank = EnemyEncounterRank.Elite;
            data.aiStrength = EnemyAiStrength.Enhanced;
            data.aiActionBias = EnemyAiActionBias.DefenseHeavy;
            data.aiDebuffInterval = 0;
            data.canUseThornGuard = true;
            data.thornGuardShieldHp = 5;
            data.thornGuardRetaliationDamage = 2;
            data.intentPattern.Clear();
            enemy.Init(data);

            new EnemyIntentSystem(new System.Random(0)).SetNextIntent(enemy);

            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Defense));
            Assert.That(enemy.CurrentIntent.isThornGuard, Is.True);
            Assert.That(enemy.CurrentIntent.value, Is.EqualTo(8));
            Assert.That(enemy.CurrentIntent.retaliationDamage, Is.EqualTo(3));
        }

        [Test]
        public void SetNextIntent_EliteBullRushOverridesNormalAttackOnInterval()
        {
            var enemy = CreateEnemy("EliteBull");
            var data = CreateEnemyData();
            data.encounterRank = EnemyEncounterRank.Elite;
            data.aiStrength = EnemyAiStrength.Enhanced;
            data.attackPower = 4;
            data.aiDebuffInterval = 0;
            data.canUseBullRush = true;
            data.bullRushInterval = 1;
            data.bullRushBonusDamage = 3;
            data.intentPattern.Clear();
            enemy.Init(data);

            new EnemyIntentSystem(new System.Random(99)).SetNextIntent(enemy);

            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Attack));
            Assert.That(enemy.CurrentIntent.value, Is.EqualTo(11));
        }

        [Test]
        public void SetNextIntents_UsesConsecutivePatternEntriesForPreview()
        {
            var enemy = CreateEnemy("MultiActionEnemy");
            var data = CreateEnemyData();
            data.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Attack,
                    value = 9,
                },
                new()
                {
                    intentType = EnemyIntentType.Defense,
                    value = 5,
                },
                new()
                {
                    intentType = EnemyIntentType.Debuff,
                    debuffType = DebuffType.Fear,
                    value = 2,
                },
            };
            SetEnemyActionsPerTurn(data, 2);
            enemy.Init(data);

            var system = new EnemyIntentSystem(new System.Random(1));
            var setNextIntents = typeof(EnemyIntentSystem).GetMethod(
                "SetNextIntents",
                new[] { typeof(EnemyController), typeof(int) });
            Assert.That(setNextIntents, Is.Not.Null, "EnemyIntentSystem should expose SetNextIntents for multi-action previews.");

            setNextIntents.Invoke(system, new object[] { enemy, 2 });
            var firstPreview = GetCurrentIntents(enemy);

            Assert.That(firstPreview.Count, Is.EqualTo(2));
            Assert.That(firstPreview[0].intentType, Is.EqualTo(EnemyIntentType.Attack));
            Assert.That(firstPreview[0].value, Is.EqualTo(9));
            Assert.That(firstPreview[1].intentType, Is.EqualTo(EnemyIntentType.Defense));
            Assert.That(firstPreview[1].value, Is.EqualTo(5));
            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Attack));

            setNextIntents.Invoke(system, new object[] { enemy, 2 });
            var secondPreview = GetCurrentIntents(enemy);

            Assert.That(secondPreview.Count, Is.EqualTo(2));
            Assert.That(secondPreview[0].intentType, Is.EqualTo(EnemyIntentType.Debuff));
            Assert.That(secondPreview[0].debuffType, Is.EqualTo(DebuffType.Fear));
            Assert.That(secondPreview[1].intentType, Is.EqualTo(EnemyIntentType.Attack));
        }

        [Test]
        public void SetNextIntents_SkipsDuplicateOncePerTurnPatternSkills()
        {
            var enemy = CreateEnemy("OncePerTurnPatternEnemy");
            var data = CreateEnemyData();
            data.aiComplexity = EnemyAiComplexity.Complex;
            data.actionsPerTurn = 1;
            data.intentPattern = new List<EnemyIntent>
            {
                new() { skillId = "endure", skillEffectKind = SkillEffectKind.Endure, intentType = EnemyIntentType.Defense },
                new() { skillId = "endure", skillEffectKind = SkillEffectKind.Endure, intentType = EnemyIntentType.Defense },
                new() { skillId = "strike", skillEffectKind = SkillEffectKind.BasicAttack, intentType = EnemyIntentType.Attack, value = 5 },
            };
            enemy.Init(data);

            new EnemyIntentSystem(new System.Random(1)).SetNextIntents(enemy, data.ActionsPerTurn);
            var preview = GetCurrentIntents(enemy);

            Assert.That(preview.Count, Is.EqualTo(3));
            Assert.That(CountEffectKind(preview, SkillEffectKind.Endure), Is.EqualTo(1));
            Assert.That(CountIntentType(preview, EnemyIntentType.Attack), Is.EqualTo(2));
        }

        [Test]
        public void SetNextIntents_SkipsEndureWhenAlreadyActive()
        {
            var enemy = CreateEnemy("EnduringEnemy");
            var player = CreatePlayer("Player", maxHp: 30, attackPower: 4, defensePower: 1);
            var data = CreateEnemyData();
            data.aiActionBias = EnemyAiActionBias.DefenseHeavy;
            data.aiDebuffInterval = 0;
            data.intentPattern = new List<EnemyIntent>
            {
                new() { skillId = "endure", skillEffectKind = SkillEffectKind.Endure, intentType = EnemyIntentType.Defense },
                new() { skillId = "strike", skillEffectKind = SkillEffectKind.BasicAttack, intentType = EnemyIntentType.Attack, value = 5 },
            };
            enemy.Init(data);
            enemy.ApplyEndure(1);

            new EnemyIntentSystem(new System.Random(1)).SetNextIntents(enemy, 1, player);

            Assert.That(enemy.CurrentIntent.skillId, Is.EqualTo("strike"));

            var aiEnemy = CreateEnemy("GeneratedEnduringEnemy");
            var aiData = CreateEnemyData();
            aiData.aiActionBias = EnemyAiActionBias.DefenseHeavy;
            aiData.aiDebuffInterval = 0;
            aiData.intentPattern.Clear();
            var endure = CreateSkill("endure", "Endure", SkillType.Defense, SkillEffectKind.Endure, power: 0);
            endure.selfEndureTurns = 1;
            var strike = CreateSkill("strike", "Strike", SkillType.Attack, SkillEffectKind.BasicAttack, power: 50);
            aiData.skills = new List<SkillSO> { endure, strike };
            aiEnemy.Init(aiData);
            aiEnemy.ApplyEndure(1);

            new EnemyIntentSystem(new System.Random(1)).SetNextIntents(aiEnemy, 2, player);
            var generatedPreview = GetCurrentIntents(aiEnemy);

            Assert.That(CountSkillId(generatedPreview, "endure"), Is.Zero);
        }

        [Test]
        public void ActionsPerTurn_UsesComplexityDefaultsUpToThreeActions()
        {
            var simple = CreateEnemyData();
            simple.aiComplexity = EnemyAiComplexity.Simple;
            simple.actionsPerTurn = 1;
            var normal = CreateEnemyData();
            normal.aiComplexity = EnemyAiComplexity.Normal;
            normal.actionsPerTurn = 1;
            var complex = CreateEnemyData();
            complex.aiComplexity = EnemyAiComplexity.Complex;
            complex.actionsPerTurn = 1;

            Assert.That(simple.ActionsPerTurn, Is.EqualTo(1));
            Assert.That(normal.ActionsPerTurn, Is.EqualTo(2));
            Assert.That(complex.ActionsPerTurn, Is.EqualTo(3));
        }

        [Test]
        public void EnemySO_AssignedSkillCount_RequiresAtLeastTwoSkills()
        {
            var data = CreateEnemyData();
            data.skills = new List<Project2048.Skills.SkillSO>
            {
                CreateSkill("quick-stab", "빠른 찌르기", Project2048.Skills.SkillType.Attack, Project2048.Skills.SkillEffectKind.BasicAttack, power: 50),
            };

            Assert.That(data.AssignedSkillCount, Is.EqualTo(1));
            Assert.That(data.HasMinimumSkillSlots, Is.False);

            data.skills.Add(CreateSkill("low-stance", "낮은 자세", Project2048.Skills.SkillType.Defense, Project2048.Skills.SkillEffectKind.BasicDefense, power: 4));

            Assert.That(data.AssignedSkillCount, Is.EqualTo(EnemySO.MinEquippedSkillSlots));
            Assert.That(data.HasMinimumSkillSlots, Is.True);
        }

        [Test]
        public void SetNextIntents_ComplexEnemyPreviewsThreeActions()
        {
            var enemy = CreateEnemy("ComplexEnemy");
            var data = CreateEnemyData();
            data.aiComplexity = EnemyAiComplexity.Complex;
            data.actionsPerTurn = 1;
            data.intentPattern = new List<EnemyIntent>
            {
                new() { intentType = EnemyIntentType.Attack, value = 4 },
                new() { intentType = EnemyIntentType.Defense, value = 5 },
                new() { intentType = EnemyIntentType.Debuff, debuffType = DebuffType.Fear, value = 1 },
                new() { intentType = EnemyIntentType.Attack, value = 6 },
            };
            enemy.Init(data);

            new EnemyIntentSystem(new System.Random(1)).SetNextIntents(enemy, data.ActionsPerTurn);
            var preview = GetCurrentIntents(enemy);

            Assert.That(preview.Count, Is.EqualTo(3));
            Assert.That(preview[0].intentType, Is.EqualTo(EnemyIntentType.Attack));
            Assert.That(preview[1].intentType, Is.EqualTo(EnemyIntentType.Defense));
            Assert.That(preview[2].intentType, Is.EqualTo(EnemyIntentType.Debuff));
        }

        [Test]
        public void SetNextIntents_CapsRequestedCountAtMaximumActionsPerTurn()
        {
            var enemy = CreateEnemy("CappedEnemy");
            var data = CreateEnemyData();
            data.intentPattern.Clear();
            enemy.Init(data);

            new EnemyIntentSystem(new System.Random(1)).SetNextIntents(enemy, 99);
            var preview = GetCurrentIntents(enemy);

            Assert.That(preview.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(preview.Count, Is.LessThanOrEqualTo(EnemySO.MaximumActionsPerTurn));
        }

        [Test]
        public void SetNextIntents_WithPlayerContext_AttackHeavyRepeatsLethalAttacks()
        {
            var enemy = CreateEnemy("Executioner");
            var data = CreateEnemyData();
            data.aiActionBias = EnemyAiActionBias.AttackHeavy;
            data.aiDebuffInterval = 0;
            data.attackPower = 8;
            data.intentPattern.Clear();
            enemy.Init(data);

            var player = CreatePlayer("LowHpPlayer", maxHp: 6, attackPower: 2, defensePower: 1);

            new EnemyIntentSystem(new System.Random(1)).SetNextIntents(enemy, 3, player);
            var preview = GetCurrentIntents(enemy);

            Assert.That(preview.Count, Is.EqualTo(3));
            Assert.That(CountIntentType(preview, EnemyIntentType.Attack), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void SetNextIntents_WithPlayerContext_DefenseHeavyLowHpPrefersThornGuard()
        {
            var enemy = CreateEnemy("LowHpGuardian");
            var data = CreateEnemyData();
            data.encounterRank = EnemyEncounterRank.Elite;
            data.aiActionBias = EnemyAiActionBias.DefenseHeavy;
            data.aiDebuffInterval = 0;
            data.canUseThornGuard = true;
            data.thornGuardShieldHp = 6;
            data.thornGuardRetaliationDamage = 3;
            data.intentPattern.Clear();
            enemy.Init(data);
            enemy.TakeDamage(16);

            var player = CreatePlayer("ThreateningPlayer", maxHp: 30, attackPower: 10, defensePower: 1);

            new EnemyIntentSystem(new System.Random(2)).SetNextIntents(enemy, 2, player);
            var preview = GetCurrentIntents(enemy);

            Assert.That(preview[0].intentType, Is.EqualTo(EnemyIntentType.Defense));
            Assert.That(preview[0].isThornGuard, Is.True);
        }

        [Test]
        public void SetNextIntents_DebuffIntervalUsesConfiguredDarknessPattern()
        {
            var enemy = CreateEnemy("DarkPlanner");
            var data = CreateEnemyData();
            data.aiDebuffInterval = 1;
            data.aiDebuffPattern = EnemyDebuffPattern.DarknessThenFear;
            data.intentPattern.Clear();
            var darkness = CreateSkill(
                "darkness",
                "암흑",
                SkillType.Debuff,
                SkillEffectKind.BoardObstacleDebuff,
                power: 0);
            darkness.availability = SkillAvailability.EnemyOnly;
            darkness.debuffType = DebuffType.Darkness;
            darkness.debuffValue = 1;
            data.skills = new List<SkillSO> { darkness };
            enemy.Init(data);

            var player = CreatePlayer("Player", maxHp: 30, attackPower: 5, defensePower: 1);

            new EnemyIntentSystem(new System.Random(3)).SetNextIntents(enemy, 1, player);
            var preview = GetCurrentIntents(enemy);

            Assert.That(preview[0].intentType, Is.EqualTo(EnemyIntentType.Debuff));
            Assert.That(preview[0].debuffType, Is.EqualTo(DebuffType.Darkness));
        }

        [Test]
        public void SetNextIntents_LowHpEnemyPrefersEquippedLifeStealAttack()
        {
            var enemy = CreateEnemy("Leech");
            var data = CreateEnemyData();
            data.aiActionBias = EnemyAiActionBias.AttackHeavy;
            data.aiDebuffInterval = 0;
            data.intentPattern.Clear();
            var lifeSteal = CreateSkill(
                "life-drain",
                "생명 흡수",
                SkillType.Attack,
                SkillEffectKind.LifeStealAttack,
                power: 60);
            lifeSteal.lifeStealPercent = 0.5f;
            var heavyStrike = CreateSkill(
                "heavy-strike",
                "강타",
                SkillType.Attack,
                SkillEffectKind.BasicAttack,
                power: 80);
            data.skills = new List<SkillSO> { lifeSteal, heavyStrike };
            enemy.Init(data);
            enemy.TakeDamage(15);

            var player = CreatePlayer("Player", maxHp: 40, attackPower: 4, defensePower: 1);

            new EnemyIntentSystem(new System.Random(4)).SetNextIntents(enemy, 1, player);

            Assert.That(enemy.CurrentIntent.skillId, Is.EqualTo("life-drain"));
            Assert.That(enemy.CurrentIntent.lifeStealPercent, Is.EqualTo(0.5f));
        }

        [Test]
        public void SetNextIntent_WhenEnemyHasSkills_UsesEquippedSkillAsIntent()
        {
            var enemy = CreateEnemy("SkilledEnemy");
            var data = CreateEnemyData();
            data.aiActionBias = EnemyAiActionBias.AttackHeavy;
            data.aiDebuffInterval = 0;
            data.intentPattern.Clear();
            data.skills = new List<Project2048.Skills.SkillSO>
            {
                CreateSkill("quick-stab", "빠른 찌르기", Project2048.Skills.SkillType.Attack, Project2048.Skills.SkillEffectKind.BasicAttack, power: 50),
                CreateSkill("low-stance", "낮은 자세", Project2048.Skills.SkillType.Defense, Project2048.Skills.SkillEffectKind.BasicDefense, power: 4),
            };
            enemy.Init(data);

            new EnemyIntentSystem(new System.Random(1)).SetNextIntent(enemy);

            Assert.That(enemy.CurrentIntent.skillId, Is.EqualTo("quick-stab"));
            Assert.That(enemy.CurrentIntent.displayName, Is.EqualTo("빠른 찌르기"));
            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Attack));
            Assert.That(enemy.CurrentIntent.value, Is.EqualTo(50));
            Assert.That(enemy.CurrentIntent.movePower, Is.EqualTo(50));
        }

        [Test]
        public void SetNextIntent_IgnoresPlayerOnlySkillsOnEnemyData()
        {
            var enemy = CreateEnemy("MisconfiguredEnemy");
            var data = CreateEnemyData();
            data.aiActionBias = EnemyAiActionBias.AttackHeavy;
            data.aiDebuffInterval = 0;
            data.intentPattern.Clear();
            var playerLifeDrain = CreateSkill(
                "life-drain",
                "생명 흡수",
                Project2048.Skills.SkillType.Attack,
                Project2048.Skills.SkillEffectKind.LifeStealAttack,
                power: 60);
            playerLifeDrain.availability = Project2048.Skills.SkillAvailability.PlayerOnly;
            playerLifeDrain.lifeStealPercent = 0.5f;
            data.skills = new List<Project2048.Skills.SkillSO> { playerLifeDrain };
            enemy.Init(data);

            new EnemyIntentSystem(new System.Random(1)).SetNextIntent(enemy);

            Assert.That(enemy.CurrentIntent.skillId, Is.Null.Or.Empty);
            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Attack));
            Assert.That(enemy.CurrentIntent.movePower, Is.EqualTo(data.attackPower * 10));
            Assert.That(enemy.CurrentIntent.lifeStealPercent, Is.EqualTo(0f));
        }

        [Test]
        public void SetNextIntent_EnemyOnlyDarknessSkillCreatesDarknessDebuff()
        {
            var enemy = CreateEnemy("DarknessEnemy");
            var data = CreateEnemyData();
            data.aiDebuffInterval = 1;
            data.intentPattern.Clear();
            var darkness = CreateSkill(
                "deep-darkness",
                "깊은 암흑",
                Project2048.Skills.SkillType.Debuff,
                Project2048.Skills.SkillEffectKind.BoardObstacleDebuff,
                power: 0);
            darkness.availability = Project2048.Skills.SkillAvailability.EnemyOnly;
            darkness.debuffType = DebuffType.Darkness;
            darkness.debuffValue = 2;
            data.skills = new List<Project2048.Skills.SkillSO> { darkness };
            enemy.Init(data);

            new EnemyIntentSystem(new System.Random(1)).SetNextIntent(enemy);

            Assert.That(enemy.CurrentIntent.skillId, Is.EqualTo("deep-darkness"));
            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Debuff));
            Assert.That(enemy.CurrentIntent.debuffType, Is.EqualTo(DebuffType.Darkness));
            Assert.That(enemy.CurrentIntent.value, Is.EqualTo(2));
        }

        private EnemyController CreateEnemy(string name)
        {
            var gameObject = new GameObject(name);
            ownedObjects.Add(gameObject);
            return gameObject.AddComponent<EnemyController>();
        }

        private EnemySO CreateEnemyData()
        {
            var data = ScriptableObject.CreateInstance<EnemySO>();
            data.maxHp = 20;
            data.attackPower = 4;
            data.defensePower = 3;
            data.debuffPower = 1;
            ownedObjects.Add(data);
            return data;
        }

        private PlayerCombatController CreatePlayer(string name, int maxHp, int attackPower, int defensePower)
        {
            var gameObject = new GameObject(name);
            ownedObjects.Add(gameObject);
            var player = gameObject.AddComponent<PlayerCombatController>();
            var data = ScriptableObject.CreateInstance<PlayerSO>();
            data.maxHp = maxHp;
            data.attackPower = attackPower;
            data.baseDefensePower = defensePower;
            ownedObjects.Add(data);
            player.Init(data);
            return player;
        }

        private static (int Attacks, int Defenses) CountActions(
            EnemyIntentSystem system,
            EnemyController enemy,
            int count)
        {
            var attacks = 0;
            var defenses = 0;

            for (var i = 0; i < count; i++)
            {
                system.SetNextIntent(enemy);
                if (enemy.CurrentIntent.intentType == EnemyIntentType.Attack)
                {
                    attacks++;
                }
                else if (enemy.CurrentIntent.intentType == EnemyIntentType.Defense)
                {
                    defenses++;
                }
            }

            return (attacks, defenses);
        }

        private static void SetEnemyActionsPerTurn(EnemySO data, int count)
        {
            var field = typeof(EnemySO).GetField("actionsPerTurn");
            Assert.That(field, Is.Not.Null, "EnemySO should expose actionsPerTurn for per-enemy multi-action tuning.");
            field.SetValue(data, count);
        }

        private static IReadOnlyList<EnemyIntent> GetCurrentIntents(EnemyController enemy)
        {
            var property = typeof(EnemyController).GetProperty("CurrentIntents");
            Assert.That(property, Is.Not.Null, "EnemyController should expose all previewed intents.");
            return (IReadOnlyList<EnemyIntent>)property.GetValue(enemy);
        }

        private static int CountIntentType(IReadOnlyList<EnemyIntent> intents, EnemyIntentType intentType)
        {
            var count = 0;
            foreach (var intent in intents)
            {
                if (intent != null && intent.intentType == intentType)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEffectKind(IReadOnlyList<EnemyIntent> intents, SkillEffectKind effectKind)
        {
            var count = 0;
            foreach (var intent in intents)
            {
                if (intent != null && intent.skillEffectKind == effectKind)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSkillId(IReadOnlyList<EnemyIntent> intents, string skillId)
        {
            var count = 0;
            foreach (var intent in intents)
            {
                if (intent != null && intent.skillId == skillId)
                {
                    count++;
                }
            }

            return count;
        }

        private Project2048.Skills.SkillSO CreateSkill(
            string skillId,
            string skillName,
            Project2048.Skills.SkillType skillType,
            Project2048.Skills.SkillEffectKind effectKind,
            int power)
        {
            var skill = ScriptableObject.CreateInstance<Project2048.Skills.SkillSO>();
            skill.skillId = skillId;
            skill.skillName = skillName;
            skill.skillType = skillType;
            skill.effectKind = effectKind;
            skill.power = power;
            skill.availability = Project2048.Skills.SkillAvailability.Shared;
            ownedObjects.Add(skill);
            return skill;
        }
    }
}
