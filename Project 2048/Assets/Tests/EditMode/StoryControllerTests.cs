using System.Reflection;
using System.Linq;
using NUnit.Framework;
using Project2048.Story;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project2048.Tests
{
    public class StoryControllerTests
    {
        [Test]
        public void StoryBackgroundView_ApplyStoryBackgrounds_ReplacesSceneImages()
        {
            var root = new GameObject("StoryBackgroundView");
            var view = root.AddComponent<StoryBackgroundView>();
            var currentImage = new GameObject("Current").AddComponent<Image>();
            var nextImage = new GameObject("Next").AddComponent<Image>();
            currentImage.transform.SetParent(root.transform);
            nextImage.transform.SetParent(root.transform);
            SetPrivateField(view, "currentBackgroundImage", currentImage);
            SetPrivateField(view, "nextBackgroundImage", nextImage);

            var currentSprite = CreateSprite("CurrentSprite");
            var nextSprite = CreateSprite("NextSprite");
            var story = ScriptableObject.CreateInstance<StoryDataSO>();
            story.currentBackgroundSprite = currentSprite;
            story.nextBackgroundSprite = nextSprite;

            view.ApplyStoryBackgrounds(story);

            Assert.That(currentImage.sprite, Is.EqualTo(currentSprite));
            Assert.That(nextImage.sprite, Is.EqualTo(nextSprite));

            Object.DestroyImmediate(story);
            Object.DestroyImmediate(currentSprite.texture);
            Object.DestroyImmediate(nextSprite.texture);
            Object.DestroyImmediate(currentSprite);
            Object.DestroyImmediate(nextSprite);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void EndingStoryData_UsesEndingBackgroundAndHasDialogue()
        {
            var story = AssetDatabase.LoadAssetAtPath<StoryDataSO>("Assets/Data/EndingStoryData.asset");

            Assert.That(story, Is.Not.Null);
            Assert.That(story.currentBackgroundSprite, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(story.currentBackgroundSprite), Is.EqualTo("Assets/Art/Source/\uAC15\uB2E4\uACBD/\uBC30\uACBD/\uC2A4\uD1A0\uB9AC \uBC30\uACBD/\u110B\u1166\u11AB\u1103\u1175\u11BC.psd"));
            Assert.That(story.steps, Is.Not.Empty);
            Assert.That(story.steps.Any(step => !string.IsNullOrWhiteSpace(step.text)), Is.True);
        }

        private static Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(4, 4);
            texture.name = name;
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(target, value);
        }
    }
}
