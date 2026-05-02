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
    }
}
