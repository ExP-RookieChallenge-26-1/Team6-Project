using System.Reflection;
using NUnit.Framework;
using Project2048.Combat;
using Project2048.Core;
using Project2048.Flow;
using Project2048.Save;
using UnityEngine;

namespace Project2048.Tests
{
    public class FlowControllerTests
    {
        private GameObject owner;

        [TearDown]
        public void TearDown()
        {
            if (owner != null)
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void HandleStageCompleted_RunCompleted_DeletesSaveAndRequestsMainMenu()
        {
            owner = new GameObject("FlowController");
            var flowController = owner.AddComponent<FlowController>();
            var saveLoadManager = owner.AddComponent<SaveLoadManager>();
            var context = new GameContext();
            var saveRepository = new TrackingSaveRepository();
            var didRequestMainMenu = false;
            var didStartLoading = false;

            context.SetRunActive(true);
            saveLoadManager.Initialize(context, saveRepository);
            flowController.Initialized(context, saveLoadManager);
            flowController.OnMainMenuSceneLoadRequested += () => didRequestMainMenu = true;
            flowController.OnLoadingStarted += _ => didStartLoading = true;

            InvokePrivateMethod(
                flowController,
                "HandleStageCompleted",
                new StageResult(
                    30,
                    StageEncounterType.Normal,
                    true,
                    new CombatResult(),
                    default));

            Assert.That(context.IsRunActive, Is.False);
            Assert.That(context.CurrentGameState, Is.EqualTo(GameContext.GameState.Loading));
            Assert.That(saveRepository.WasDeleted, Is.True);
            Assert.That(didStartLoading, Is.True);
            Assert.That(didRequestMainMenu, Is.True);
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(target, args);
        }

        private sealed class TrackingSaveRepository : ISaveRepository
        {
            public bool WasDeleted { get; private set; }

            public bool Exists()
            {
                return true;
            }

            public void Save(GameSaveData data)
            {
            }

            public GameSaveData Load()
            {
                return null;
            }

            public void Delete()
            {
                WasDeleted = true;
            }
        }
    }
}
