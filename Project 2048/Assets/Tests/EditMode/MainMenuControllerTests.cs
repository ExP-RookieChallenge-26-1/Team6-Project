using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project2048.Tests
{
    public class MainMenuControllerTests
    {
        [Test]
        public void SourceText_KeepsKoreanMenuMessagesReadable()
        {
            var source = File.ReadAllText("Assets/Scripts/UI/MainMenuController.cs");

            Assert.That(source, Does.Contain("새로 시작하시겠습니까?"));
            Assert.That(source, Does.Contain("이어하기 클릭됨"));
            Assert.That(source, Does.Contain("종료하시겠습니까?"));
            Assert.That(source, Does.Not.Contain("醫"));
            Assert.That(source, Does.Not.Contain("댁뼱"));
            Assert.That(source, Does.Not.Contain("寃"));
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

        [Test]
        public void MainMenuReplacementScene_IsNotImported()
        {
            const string replacementScenePath = "Assets/Scenes/MainMenu1.unity";
            var replacementScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(replacementScenePath);

            Assert.That(replacementScene, Is.Null);
            Assert.That(EditorBuildSettings.scenes.Select(scene => scene.path), Does.Not.Contain(replacementScenePath));
        }

        [Test]
        public void MainMenuScene_UsesSceneAuthoredUiBindings()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var canvas = GameObject.Find("MainMenuCanvas");
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.GetComponent<Canvas>(), Is.Not.Null);

            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.001f));

            var background = canvas.transform.Find("MainMenuBackground");
            Assert.That(background, Is.Not.Null);
            Assert.That(background.gameObject.activeSelf, Is.True);
            Assert.That(background.GetSiblingIndex(), Is.EqualTo(0));

            var backgroundImage = background.GetComponent<RawImage>();
            Assert.That(backgroundImage, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(backgroundImage.texture), Is.EqualTo("Assets/Art/MainMenu/MainMenuBackgroundPlaceholder.png"));
            Assert.That(backgroundImage.raycastTarget, Is.False);

            var backgroundRect = (RectTransform)background;
            Assert.That(backgroundRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(backgroundRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(backgroundRect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(backgroundRect.offsetMax, Is.EqualTo(Vector2.zero));

            var controller = Object.FindFirstObjectByType<global::MainMenuController>(FindObjectsInactive.Include);
            Assert.That(controller, Is.Not.Null);

            var serializedController = new SerializedObject(controller);
            AssertSerializedReference(serializedController, "newGameButton");
            AssertSerializedReference(serializedController, "settingButton");
            AssertSerializedReference(serializedController, "quitButton");
            AssertSerializedReference(serializedController, "confirmPopup");
            AssertSerializedReference(serializedController, "settingPopup");
            AssertSerializedReference(serializedController, "fadeController");

            var settingPopup = canvas.transform.Find("SettingPopup");
            Assert.That(settingPopup, Is.Not.Null);
            Assert.That(settingPopup.GetComponent<global::SettingPopup>(), Is.Not.Null);
        }

        private static void AssertSerializedReference(SerializedObject serializedObject, string propertyName)
        {
            var property = serializedObject.FindProperty(propertyName);

            Assert.That(property, Is.Not.Null, propertyName);
            Assert.That(property.objectReferenceValue, Is.Not.Null, propertyName);
        }
    }
}
