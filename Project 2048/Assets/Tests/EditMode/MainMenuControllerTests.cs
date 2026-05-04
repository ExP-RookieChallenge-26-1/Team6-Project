using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace Project2048.Tests
{
    public class MainMenuControllerTests
    {
        [Test]
        public void SourceText_KeepsKoreanMenuMessagesReadable()
        {
            var source = File.ReadAllText("Assets/Scripts/UI/MainMenuController.cs");

            Assert.That(source, Does.Contain("새로 하시겠습니까?"));
            Assert.That(source, Does.Contain("이어하기 클릭됨"));
            Assert.That(source, Does.Contain("종료하시겠습니까?"));
            Assert.That(source, Does.Not.Contain("占"));
        }

        [Test]
        public void NewGameSceneName_IsEnabledInBuildSettings()
        {
            var field = typeof(global::MainMenuController).GetField(
                "GameSceneName",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(field, Is.Not.Null);

            var sceneName = (string)field.GetRawConstantValue();
            var enabledSceneNames = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                .ToArray();

            Assert.That(enabledSceneNames, Does.Contain(sceneName));
        }
    }
}
