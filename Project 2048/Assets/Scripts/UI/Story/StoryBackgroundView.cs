using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Project2048.Story
{
    public class StoryBackgroundView : MonoBehaviour
    {
        [SerializeField] private Image currentBackgroundImage;
        [SerializeField] private Image nextBackgroundImage;
        [SerializeField] private float transitionDuration = 0.75f;

        private bool hasChangedBackground;

        private void Awake()
        {
            SetAlpha(currentBackgroundImage, 1f);
            SetAlpha(nextBackgroundImage, 1f);
        }

        public void ApplyStoryBackgrounds(StoryDataSO storyData)
        {
            if (storyData == null)
            {
                return;
            }

            if (storyData.currentBackgroundSprite != null && currentBackgroundImage != null)
            {
                currentBackgroundImage.sprite = storyData.currentBackgroundSprite;
            }

            if (storyData.nextBackgroundSprite != null && nextBackgroundImage != null)
            {
                nextBackgroundImage.sprite = storyData.nextBackgroundSprite;
            }

            hasChangedBackground = false;
            ResetFilledImage(currentBackgroundImage);
            ResetFilledImage(nextBackgroundImage);
            SetAlpha(currentBackgroundImage, 1f);
            SetAlpha(nextBackgroundImage, 1f);
        }

        public IEnumerator PlayTransition()
        {
            if (hasChangedBackground || currentBackgroundImage == null || nextBackgroundImage == null)
            {
                yield break;
            }

            PrepareFilledImage(currentBackgroundImage);
            SetAlpha(nextBackgroundImage, 1f);

            float safeDuration = Mathf.Max(0.01f, transitionDuration);
            float elapsedTime = 0f;

            while (elapsedTime < safeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / safeDuration);
                SetCurrentFill(1f - Mathf.SmoothStep(0f, 1f, t));

                yield return null;
            }

            SetCurrentFill(0f);
            hasChangedBackground = true;
        }

        private void SetCurrentFill(float amount)
        {
            if (currentBackgroundImage == null)
            {
                return;
            }

            currentBackgroundImage.fillAmount = amount;
        }

        private static void PrepareFilledImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
        }

        private static void ResetFilledImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.type = Image.Type.Simple;
            image.fillAmount = 1f;
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
