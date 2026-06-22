using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Project2048.Enemy;
using Project2048.Stage;
using UnityEngine;

namespace Project2048.Tests
{
    public class StageDatabaseTests
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
        public void MapsUpperMiddleLowerStagesToThirtyStageRun()
        {
            var stageDatabase = CreateScriptableObject<StageDatabaseSO>();
            var upperStage = CreateStage(StageFloor.Upper, 1, CreateEnemyData(maxHp: 10, attackValue: 1));
            var middleStage = CreateStage(StageFloor.Middle, 1, CreateEnemyData(maxHp: 20, attackValue: 2));
            var lowerStage = CreateStage(StageFloor.Lower, 10, CreateEnemyData(maxHp: 30, attackValue: 3));

            SetPrivateField(stageDatabase, "upperStages", new List<StageSO> { upperStage });
            SetPrivateField(stageDatabase, "middleStages", new List<StageSO> { middleStage });
            SetPrivateField(stageDatabase, "lowerStages", new List<StageSO>
            {
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                lowerStage,
            });

            Assert.That(stageDatabase.TryGetStage(1, out var resolvedUpper), Is.True);
            Assert.That(resolvedUpper, Is.SameAs(upperStage));
            Assert.That(stageDatabase.TryGetStage(11, out var resolvedMiddle), Is.True);
            Assert.That(resolvedMiddle, Is.SameAs(middleStage));
            Assert.That(stageDatabase.TryGetStage(30, out var resolvedLower), Is.True);
            Assert.That(resolvedLower, Is.SameAs(lowerStage));
            Assert.That(stageDatabase.IsFinalStage(29), Is.False);
            Assert.That(stageDatabase.IsFinalStage(30), Is.True);
        }

        [Test]
        public void StageSelectsOneConfiguredEnemyCandidate()
        {
            var firstEnemy = CreateEnemyData(maxHp: 10, attackValue: 1);
            var secondEnemy = CreateEnemyData(maxHp: 20, attackValue: 2);
            var stage = CreateStage(StageFloor.Upper, 1, firstEnemy, secondEnemy);

            Assert.That(stage.TrySelectEnemy(out var selectedEnemy), Is.True);
            Assert.That(selectedEnemy == firstEnemy || selectedEnemy == secondEnemy, Is.True);
        }

        [Test]
        public void StageRejectsMissingEnemyCandidates()
        {
            var stage = CreateStage(StageFloor.Upper, 1, null);

            Assert.That(stage.TrySelectEnemy(out var selectedEnemy), Is.False);
            Assert.That(selectedEnemy, Is.Null);
        }

        [Test]
        public void StageExposesPresentationBackgroundSprite()
        {
            var backgroundSprite = CreateSprite("StageBackground");
            var stage = CreateStage(StageFloor.Middle, 1, CreateEnemyData(maxHp: 10, attackValue: 1));

            SetPrivateField(stage, "presentationBackgroundSprite", backgroundSprite);

            Assert.That(stage.PresentationBackgroundSprite, Is.EqualTo(backgroundSprite));
        }

        private StageSO CreateStage(StageFloor floor, int stageNumberInFloor, params EnemySO[] enemies)
        {
            var stage = CreateScriptableObject<StageSO>();
            SetPrivateField(stage, "floor", floor);
            SetPrivateField(stage, "stageNumberInFloor", stageNumberInFloor);
            SetPrivateField(stage, "enemyCandidates", new List<EnemySO>(enemies));
            return stage;
        }

        private EnemySO CreateEnemyData(int maxHp, int attackValue)
        {
            var data = CreateScriptableObject<EnemySO>();
            data.maxHp = maxHp;
            data.attackPower = attackValue;
            return data;
        }

        private T CreateScriptableObject<T>()
            where T : ScriptableObject
        {
            var data = ScriptableObject.CreateInstance<T>();
            ownedObjects.Add(data);
            return data;
        }

        private Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(2, 2);
            texture.name = $"{name}Texture";
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"{name}Sprite";
            ownedObjects.Add(texture);
            ownedObjects.Add(sprite);
            return sprite;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }
    }
}
