using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project2048.Prototype
{
    public class BoardCellView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private GameObject obstacleMarker;

        public Image Background => background;
        public TMP_Text ValueText => valueText;
        public RectTransform RectTransform => transform as RectTransform;

        private Coroutine mergePulseRoutine;

        public void SetValue(int value, Color emptyColor, Color filledColor, Color highlightColor, Color obstacleColor)
        {
            if (valueText != null)
            {
                if (value <= 0)
                {
                    valueText.text = string.Empty;
                }
                else
                {
                    valueText.text = value.ToString();
                }
            }

            if (background == null)
            {
                return;
            }

            if (value < 0)
            {
                background.color = obstacleColor;
            }
            else if (value == 0)
            {
                background.color = emptyColor;
            }
            else if (value >= 64)
            {
                background.color = highlightColor;
                if (valueText != null)
                {
                    valueText.color = Color.black;
                }
            }
            else
            {
                background.color = filledColor;
                if (valueText != null)
                {
                    valueText.color = Color.white;
                }
            }

            if (obstacleMarker != null)
            {
                obstacleMarker.SetActive(value < 0);
            }
        }

        public void PlayMergePulse()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (mergePulseRoutine != null)
            {
                StopCoroutine(mergePulseRoutine);
            }

            mergePulseRoutine = StartCoroutine(PlayMergePulseRoutine());
        }

        private IEnumerator PlayMergePulseRoutine()
        {
            var rect = RectTransform;
            if (rect == null)
            {
                yield break;
            }

            const float duration = 0.12f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var scale = t < 0.5f
                    ? Mathf.Lerp(1f, 1.12f, t * 2f)
                    : Mathf.Lerp(1.12f, 1f, (t - 0.5f) * 2f);
                rect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            rect.localScale = Vector3.one;
            mergePulseRoutine = null;
        }
    }
}
