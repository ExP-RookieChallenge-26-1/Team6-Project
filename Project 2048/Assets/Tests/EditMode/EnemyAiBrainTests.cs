using System;
using System.Collections.Generic;
using NUnit.Framework;
using Project2048.Enemy;
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
                CreateSkill("enemy-light-shot", "빛 발사", Project2048.Skills.SkillType.Attack, Project2048.Skills.SkillEffectKind.BasicAttack, power: 5),
            };

            Assert.That(data.AssignedSkillCount, Is.EqualTo(1));
            Assert.That(data.HasMinimumSkillSlots, Is.False);

            data.skills.Add(CreateSkill("enemy-light-guard", "빛 방어", Project2048.Skills.SkillType.Defense, Project2048.Skills.SkillEffectKind.LightGuard, power: 4));

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
        public void SetNextIntent_WhenEnemyHasSkills_UsesEquippedSkillAsIntent()
        {
            var enemy = CreateEnemy("SkilledEnemy");
            var data = CreateEnemyData();
            data.aiActionBias = EnemyAiActionBias.AttackHeavy;
            data.aiDebuffInterval = 0;
            data.intentPattern.Clear();
            data.skills = new List<Project2048.Skills.SkillSO>
            {
                CreateSkill("enemy-light-shot", "빛 발사", Project2048.Skills.SkillType.Attack, Project2048.Skills.SkillEffectKind.BasicAttack, power: 5),
                CreateSkill("enemy-light-guard", "빛 방어", Project2048.Skills.SkillType.Defense, Project2048.Skills.SkillEffectKind.LightGuard, power: 4),
            };
            enemy.Init(data);

            new EnemyIntentSystem(new System.Random(1)).SetNextIntent(enemy);

            Assert.That(enemy.CurrentIntent.skillId, Is.EqualTo("enemy-light-shot"));
            Assert.That(enemy.CurrentIntent.displayName, Is.EqualTo("빛 발사"));
            Assert.That(enemy.CurrentIntent.intentType, Is.EqualTo(EnemyIntentType.Attack));
            Assert.That(enemy.CurrentIntent.value, Is.EqualTo(data.attackPower + 5));
            Assert.That(enemy.CurrentIntent.movePower, Is.EqualTo(5));
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
            ownedObjects.Add(skill);
            return skill;
        }
    }
}
