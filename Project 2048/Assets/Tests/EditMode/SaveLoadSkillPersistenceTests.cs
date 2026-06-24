using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Project2048.Core;
using Project2048.Rewards;
using Project2048.Save;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Tests
{
    public class SaveLoadSkillPersistenceTests
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
        public void GameSaveData_RestoresEquippedSkillsBySavedIds()
        {
            var lightShot = CreateSkill("light-shot");
            var lowStance = CreateSkill("low-stance");
            var fireball = CreateSkill("fireball");
            var context = new GameContext();
            var progress = new RunProgress();

            context.SetRunActive(true);
            context.SetStageIndex(3);
            progress.CapturePlayerSkills(new[] { lightShot, lowStance, fireball });

            var saveData = GameSaveData.From(context, progress);
            var restoredProgress = new RunProgress();
            saveData.ApplyTo(restoredProgress, new[] { fireball, lightShot, lowStance });

            Assert.That(saveData.equippedSkillIds, Is.EqualTo(new[] { "light-shot", "low-stance", "fireball" }));
            Assert.That(
                restoredProgress.EquippedSkills.Select(skill => skill.skillId),
                Is.EqualTo(new[] { "light-shot", "low-stance", "fireball" }));
        }

        private SkillSO CreateSkill(string skillId)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.skillId = skillId;
            ownedObjects.Add(skill);
            return skill;
        }
    }
}
