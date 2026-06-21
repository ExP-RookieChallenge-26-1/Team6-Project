using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project2048.Tests
{
    public class MainMenuLayoutTests
    {
        [Test]
        public void MainMenuLayout_ArrangesTitleAndThreeButtonsLikePosterReference()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var controller = Object.FindAnyObjectByType<global::MainMenuController>(FindObjectsInactive.Include);
            Assert.That(controller, Is.Not.Null);

            var method = typeof(global::MainMenuController).GetMethod(
                "ApplyReferencePosterLayout",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(controller, null);

            var canvas = GameObject.Find("MainMenuCanvas");
            Assert.That(canvas, Is.Not.Null);

            var title = (RectTransform)canvas.transform.Find("GameTitle");
            Assert.That(title, Is.Not.Null);
            Assert.That(title.anchoredPosition, Is.EqualTo(new Vector2(0f, -430f)));
            Assert.That(title.sizeDelta, Is.EqualTo(new Vector2(960f, 260f)));

            AssertButtonRow(canvas.transform.Find("ButtonGroup/MainButtonGroup"), "OpenStartGroup", "Option", "QuitGame");
            AssertButtonRow(canvas.transform.Find("ButtonGroup/StartButtonGroup"), "StartNewGame", "StartSavedGame", "GoBack");
        }

        private static void AssertButtonRow(Transform group, params string[] buttonNames)
        {
            Assert.That(group, Is.Not.Null);
            var groupRect = (RectTransform)group;
            Assert.That(groupRect.anchoredPosition, Is.EqualTo(new Vector2(0f, -780f)));
            Assert.That(groupRect.sizeDelta, Is.EqualTo(new Vector2(1080f, 140f)));

            var expectedX = new[] { -350f, 0f, 350f };
            for (var index = 0; index < buttonNames.Length; index++)
            {
                var button = group.Find(buttonNames[index]);
                Assert.That(button, Is.Not.Null, buttonNames[index]);
                Assert.That(button.GetComponent<Button>(), Is.Not.Null, buttonNames[index]);

                var buttonRect = (RectTransform)button;
                Assert.That(buttonRect.anchoredPosition, Is.EqualTo(new Vector2(expectedX[index], 0f)));
                Assert.That(buttonRect.sizeDelta, Is.EqualTo(new Vector2(250f, 86f)));
            }
        }
    }
}
