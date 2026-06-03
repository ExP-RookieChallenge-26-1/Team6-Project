using System.Reflection;
using NUnit.Framework;
using Project2048.Combat;
using Project2048.Core;
using Project2048.Flow;
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
        public void HandleStageCompleted_RunCompleted_RequestsEndingScene()
        {
            owner = new GameObject("FlowController");
            var flowController = owner.AddComponent<FlowController>();
            var context = new GameContext();
            var didRequestEnding = false;
            var didStartLoading = false;

            context.SetRunActive(true);
            flowController.Initialized(context);
            flowController.OnEndingSceneLoadRequested += () => didRequestEnding = true;
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
            Assert.That(context.CurrentGameState, Is.EqualTo(GameContext.GameState.Ending));
            Assert.That(didStartLoading, Is.True);
            Assert.That(didRequestEnding, Is.True);
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(target, args);
        }
    }
}
