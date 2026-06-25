using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project2048.Prototype
{
    public class BoardCellView : MonoBehaviour
    {
        private static readonly Color EmptyCellFixedColor = new(26f / 255f, 26f / 255f, 26f / 255f, 1f);
        private static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");

        [SerializeField] private Image background;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private GameObject edge;
        [SerializeField] private GameObject obstacleMarker;

        [Header("Tile Face HDR Bloom")]
        [SerializeField] private int faceHdrStartValue = 8;
        [SerializeField] private int maxTileValue = 2048;
        [SerializeField] private float minFaceIntensity = 1f;
        [SerializeField] private float maxFaceIntensity = 3f;
        [SerializeField] private float minFaceAlpha = 0.9f;
        [SerializeField] private float maxFaceAlpha = 1f;

        public Image Background => background;
        public TMP_Text ValueText => valueText;
        public RectTransform RectTransform => transform as RectTransform;

        private Coroutine mergePulseRoutine;

        private void Awake()
        {
            ResolveEdge();
        }

        public void SetValue(int value, Color emptyColor, Color filledColor, Color highlightColor, Color obstacleColor)
        {
            ResolveEdge();
            var hasValue = value > 0;
            if (valueText != null)
            {
                valueText.text = hasValue ? value.ToString() : string.Empty;
                valueText.color = Color.white;
                ApplyTextFace(hasValue ? value : 0);
            }

            if (edge != null)
            {
                edge.SetActive(hasValue);
            }

            if (background != null)
            {
                background.color = value switch
                {
                    < 0 => obstacleColor,
                    0 => EmptyCellFixedColor,
                    _ => Color.black,
                };
            }

            if (obstacleMarker != null)
            {
                obstacleMarker.SetActive(value < 0);
            }
        }

        private void ResolveEdge()
        {
            if (edge != null)
            {
                return;
            }

            foreach (var rectTransform in GetComponentsInChildren<RectTransform>(true))
            {
                if (rectTransform != null && rectTransform.gameObject.name == "Edge")
                {
                    edge = rectTransform.gameObject;
                    return;
                }
            }
        }

        private void ApplyTextFace(int value)
        {
            if (valueText == null)
            {
                return;
            }

            var material = valueText.fontMaterial;
            if (material == null)
            {
                return;
            }

            material.SetColor(FaceColorId, GetFaceColor(value));
        }

        // 숫자가 클수록 face HDR 세기를 올려 흰 블룸이 비례해서 강해지게 함.
        private Color GetFaceColor(int value)
        {
            if (value < faceHdrStartValue)
            {
                return Color.white;
            }

            var t = GetNormalizedValue(value, faceHdrStartValue, maxTileValue);
            var intensity = Mathf.Lerp(minFaceIntensity, maxFaceIntensity, t);
            var alpha = Mathf.Lerp(minFaceAlpha, maxFaceAlpha, t);
            var color = Color.white * intensity;
            color.a = alpha;
            return color;
        }

        private static float GetNormalizedValue(int value, int start, int max)
        {
            if (max <= start)
            {
                return 1f;
            }

            var logStart = Mathf.Log(start, 2f);
            var logMax = Mathf.Log(max, 2f);
            var logValue = Mathf.Log(Mathf.Max(value, start), 2f);
            var t = (logValue - logStart) / (logMax - logStart);
            return Mathf.Clamp01(t);
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
