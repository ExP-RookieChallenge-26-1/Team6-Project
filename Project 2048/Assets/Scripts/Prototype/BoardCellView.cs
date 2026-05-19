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

        [Header("Tile Value Range")]
        [SerializeField] private int minTileValue = 2;
        [SerializeField] private int maxTileValue = 2048;

        [Header("Tile Color Code")]
        [SerializeField] private int brightestTileCode = 255;
        [SerializeField] private int darkestTileCode = 0;

        [Header("Number Color")]
        [SerializeField] private int blackNumberMaxValue = 16;

        [Header("TMP Face HDR")]
        [SerializeField] private int faceHdrStartValue = 32;
        [SerializeField] private float minFaceIntensity = 1f;
        [SerializeField] private float maxFaceIntensity = 3f;
        [SerializeField, Range(0f, 1f)] private float minFaceAlpha = 0.9f;
        [SerializeField, Range(0f, 1f)] private float maxFaceAlpha = 1f;

        public Image Background => background;
        public TMP_Text ValueText => valueText;
        public RectTransform RectTransform => transform as RectTransform;

        private static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");

        private Material textMaterial;
        private Coroutine mergePulseRoutine;

        private void Awake()
        {
            SetupTextMaterial();
            ApplyTextFace(0);
        }

        public void SetValue(int value, Color emptyColor, Color filledColor, Color highlightColor, Color obstacleColor)
        {
            SetupTextMaterial();

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

                valueText.color = Color.white;
            }

            if (background != null)
            {
                if (value < 0)
                {
                    background.color = obstacleColor;
                }
                else if (value == 0)
                {
                    background.color = emptyColor;
                }
                else
                {
                    background.color = GetTileColor(value);
                }
            }

            if (obstacleMarker != null)
            {
                obstacleMarker.SetActive(value < 0);
            }

            ApplyTextFace(value);
        }

        private void SetupTextMaterial()
        {
            if (valueText == null || textMaterial != null)
            {
                return;
            }

            textMaterial = Instantiate(valueText.fontMaterial);
            textMaterial.name = $"{valueText.fontMaterial.name}_{name}_Runtime";
            valueText.fontMaterial = textMaterial;
            valueText.SetMaterialDirty();
        }

        private Color GetTileColor(int value)
        {
            var t = GetNormalizedValue(value, minTileValue, maxTileValue);
            var code = Mathf.RoundToInt(Mathf.Lerp(brightestTileCode, darkestTileCode, t));
            var channel = Mathf.Clamp(code, 0, 255) / 255f;
            return new Color(channel, channel, channel, 1f);
        }

        private void ApplyTextFace(int value)
        {
            if (valueText == null || textMaterial == null || !textMaterial.HasProperty(FaceColorId))
            {
                return;
            }

            if (value <= 0)
            {
                textMaterial.SetColor(FaceColorId, new Color(0f, 0f, 0f, 0f));
                valueText.SetMaterialDirty();
                return;
            }

            var baseColor = value <= blackNumberMaxValue ? Color.black : Color.white;
            var intensity = 1f;
            var alpha = 1f;

            if (value >= faceHdrStartValue)
            {
                var t = GetNormalizedValue(value, faceHdrStartValue, maxTileValue);
                intensity = Mathf.Lerp(minFaceIntensity, maxFaceIntensity, t);
                alpha = Mathf.Lerp(minFaceAlpha, maxFaceAlpha, t);
            }

            var color = baseColor * intensity;
            color.a = alpha;

            textMaterial.SetColor(FaceColorId, color);
            valueText.SetMaterialDirty();
            valueText.SetVerticesDirty();
        }

        private static float GetNormalizedValue(int value, int minValue, int maxValue)
        {
            minValue = Mathf.Max(1, minValue);
            maxValue = Mathf.Max(minValue + 1, maxValue);
            value = Mathf.Clamp(value, minValue, maxValue);

            var minPower = Mathf.Log(minValue, 2f);
            var maxPower = Mathf.Log(maxValue, 2f);
            var currentPower = Mathf.Log(value, 2f);

            return Mathf.InverseLerp(minPower, maxPower, currentPower);
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
