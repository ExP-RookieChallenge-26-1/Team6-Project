using System.Collections;
using UnityEngine;

namespace Project2048.Story
{
    public class StoryLayoutView : MonoBehaviour
    {
        [SerializeField] private RectTransform dialoguePanel;
        [SerializeField] private RectTransform backgroundRoot;
        [SerializeField] private float endingDialogueMaxY = 0.55f;
        [SerializeField] private float endingLayoutDuration = 0.6f;

        public IEnumerator PlayEndingLayout()
        {
            if (dialoguePanel == null && backgroundRoot == null)
            {
                yield break;
            }

            float startDialogueMaxY = dialoguePanel != null ? dialoguePanel.anchorMax.y : endingDialogueMaxY;
            float startBackgroundMinY = backgroundRoot != null ? backgroundRoot.anchorMin.y : endingDialogueMaxY;
            float startBackgroundMaxY = backgroundRoot != null ? backgroundRoot.anchorMax.y : 1f;
            float backgroundAnchorDelta = endingDialogueMaxY - startBackgroundMinY;
            float targetBackgroundMaxY = startBackgroundMaxY + backgroundAnchorDelta;
            float safeDuration = Mathf.Max(0.01f, endingLayoutDuration);
            float elapsedTime = 0f;

            while (elapsedTime < safeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / safeDuration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                float dialogueMaxY = Mathf.Lerp(startDialogueMaxY, endingDialogueMaxY, easedT);
                float backgroundMinY = Mathf.Lerp(startBackgroundMinY, endingDialogueMaxY, easedT);
                float backgroundMaxY = Mathf.Lerp(startBackgroundMaxY, targetBackgroundMaxY, easedT);

                SetDialogueAnchors(dialogueMaxY);
                SetBackgroundAnchors(backgroundMinY, backgroundMaxY);

                yield return null;
            }

            SetDialogueAnchors(endingDialogueMaxY);
            SetBackgroundAnchors(endingDialogueMaxY, targetBackgroundMaxY);
        }

        private void SetDialogueAnchors(float maxY)
        {
            if (dialoguePanel == null)
            {
                return;
            }

            dialoguePanel.anchorMin = new Vector2(0f, 0f);
            dialoguePanel.anchorMax = new Vector2(1f, Mathf.Clamp01(maxY));
            dialoguePanel.offsetMin = Vector2.zero;
            dialoguePanel.offsetMax = Vector2.zero;
        }

        private void SetBackgroundAnchors(float minY, float maxY)
        {
            if (backgroundRoot == null)
            {
                return;
            }

            backgroundRoot.anchorMin = new Vector2(0f, minY);
            backgroundRoot.anchorMax = new Vector2(1f, maxY);
            backgroundRoot.offsetMin = Vector2.zero;
            backgroundRoot.offsetMax = Vector2.zero;
        }
    }
}
