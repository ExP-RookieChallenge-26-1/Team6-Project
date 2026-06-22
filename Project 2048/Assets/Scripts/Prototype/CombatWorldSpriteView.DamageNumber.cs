using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Project2048.Prototype
{
    public partial class CombatWorldSpriteView
    {
        private const float DamageNumberPopupRiseDistance = 0.62f;
        private const float DamageNumberPopupMinimumWorldOffset = 0.22f;
        private const float DamageNumberPopupFallbackWorldRadius = 0.55f;
        private const float DamageNumberPopupBoundsRadiusMultiplier = 0.65f;
        private const float DamageNumberPopupWorldFontSize = 2.9f;
        private const float DamageNumberPopupUiFontSize = 40f;
        private const float DamageNumberPopupOutlineWidth = 0.22f;
        private const float DamageNumberPopupGlowInner = 0.04f;
        private const float DamageNumberPopupGlowOuter = 0.36f;
        private const float DamageNumberPopupGlowPower = 0.72f;
        private const float DamageNumberPopupGlowOffset = 0f;
        private const int DamageNumberPopupSortingOrderOffset = 32;

        private static readonly Color DamageNumberPopupTextColor = new(1f, 0.82f, 0.02f, 1f);
        private static readonly Color DamageNumberPopupOutlineColor = Color.white;
        private static readonly Color DamageNumberPopupGlowColor = new(1f, 0.72f, 0f, 0.82f);

        private RectTransform damageNumberPopupLayer;
        private readonly List<GameObject> damageNumberPopups = new();

        private void PlayDamageNumberPopupIfNeeded(int damageAmount, SpriteRenderer targetRenderer)
        {
            if (damageAmount <= 0 || targetRenderer == null)
            {
                return;
            }

            var popupLayer = ResolveDamageNumberPopupLayer();
            if (popupLayer != null)
            {
                PlayDamageNumberUiPopup(damageAmount, targetRenderer, popupLayer);
                return;
            }

            PlayDamageNumberWorldPopup(damageAmount, targetRenderer);
        }

        private void PlayDamageNumberUiPopup(int damageAmount, SpriteRenderer targetRenderer, RectTransform popupLayer)
        {
            var popupObject = new GameObject("DamageNumberPopup", typeof(RectTransform), typeof(TextMeshProUGUI));
            popupObject.transform.SetParent(popupLayer, false);
            damageNumberPopups.Add(popupObject);

            var rectTransform = popupObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(196f, 84f);
            rectTransform.anchoredPosition = ResolveDamageNumberCanvasPosition(targetRenderer, popupLayer);

            var label = popupObject.GetComponent<TextMeshProUGUI>();
            ConfigureDamageNumberLabel(label, damageAmount, DamageNumberPopupUiFontSize);
            label.raycastTarget = false;

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            StartCoroutine(DamageNumberPopupRoutine(rectTransform, label));
        }

        private void PlayDamageNumberWorldPopup(int damageAmount, SpriteRenderer targetRenderer)
        {
            var popupObject = new GameObject("DamageNumberPopup", typeof(TextMeshPro));
            popupObject.transform.SetParent(targetRenderer.transform, false);
            popupObject.transform.localPosition = targetRenderer.transform.InverseTransformPoint(ResolveDamageNumberWorldPosition(targetRenderer));
            popupObject.transform.localRotation = Quaternion.identity;
            popupObject.transform.localScale = Vector3.one;
            damageNumberPopups.Add(popupObject);

            var label = popupObject.GetComponent<TextMeshPro>();
            ConfigureDamageNumberLabel(label, damageAmount, DamageNumberPopupWorldFontSize);

            var meshRenderer = popupObject.GetComponent<MeshRenderer>();
            meshRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            meshRenderer.sortingOrder = targetRenderer.sortingOrder + DamageNumberPopupSortingOrderOffset;

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            StartCoroutine(DamageNumberPopupRoutine(popupObject.transform, label));
        }

        private static void ConfigureDamageNumberLabel(TMP_Text label, int damageAmount, float fontSize)
        {
            label.text = damageAmount.ToString();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = DamageNumberPopupTextColor;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            ConfigureDamageNumberGlow(label);
        }

        private static void ConfigureDamageNumberGlow(TMP_Text label)
        {
            var sourceMaterial = label.fontMaterial;
            if (sourceMaterial == null)
            {
                return;
            }

            var material = new Material(sourceMaterial)
            {
                name = "DamageNumberPopupMaterial",
            };

            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            if (material.HasProperty(ShaderUtilities.ID_FaceColor))
            {
                material.SetColor(ShaderUtilities.ID_FaceColor, DamageNumberPopupTextColor);
            }

            if (material.HasProperty(ShaderUtilities.ID_OutlineColor))
            {
                material.SetColor(ShaderUtilities.ID_OutlineColor, DamageNumberPopupOutlineColor);
            }

            if (material.HasProperty(ShaderUtilities.ID_OutlineWidth))
            {
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, DamageNumberPopupOutlineWidth);
            }

            if (material.HasProperty(ShaderUtilities.ID_GlowColor))
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Glow);
                material.SetColor(ShaderUtilities.ID_GlowColor, DamageNumberPopupGlowColor);
                material.SetFloat(ShaderUtilities.ID_GlowInner, DamageNumberPopupGlowInner);
                material.SetFloat(ShaderUtilities.ID_GlowOuter, DamageNumberPopupGlowOuter);
                material.SetFloat(ShaderUtilities.ID_GlowPower, DamageNumberPopupGlowPower);
                material.SetFloat(ShaderUtilities.ID_GlowOffset, DamageNumberPopupGlowOffset);
            }

            label.fontMaterial = material;
            label.UpdateMeshPadding();
        }

        private static Vector3 ResolveDamageNumberWorldPosition(SpriteRenderer targetRenderer)
        {
            var center = ResolveDamageNumberWorldCenter(targetRenderer);
            var maxRadius = ResolveDamageNumberWorldOffsetRadius(targetRenderer);
            var minRadius = Mathf.Min(DamageNumberPopupMinimumWorldOffset, maxRadius * 0.75f);
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var distance = Random.Range(minRadius, maxRadius);
            var offset = new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                0f);

            return center + offset;
        }

        private static Vector3 ResolveDamageNumberWorldCenter(SpriteRenderer targetRenderer)
        {
            if (targetRenderer == null)
            {
                return Vector3.zero;
            }

            if (targetRenderer.sprite == null)
            {
                return targetRenderer.transform.position;
            }

            return targetRenderer.bounds.center;
        }

        private static float ResolveDamageNumberWorldOffsetRadius(SpriteRenderer targetRenderer)
        {
            if (targetRenderer == null || targetRenderer.sprite == null)
            {
                return DamageNumberPopupFallbackWorldRadius;
            }

            var bounds = targetRenderer.bounds;
            var boundsRadius = Mathf.Max(bounds.extents.x, bounds.extents.y) * DamageNumberPopupBoundsRadiusMultiplier;
            return Mathf.Clamp(boundsRadius, DamageNumberPopupMinimumWorldOffset, DamageNumberPopupFallbackWorldRadius);
        }

        private static Vector2 ResolveDamageNumberCanvasPosition(SpriteRenderer targetRenderer, RectTransform popupLayer)
        {
            var worldPosition = ResolveDamageNumberWorldPosition(targetRenderer);
            var canvas = popupLayer != null ? popupLayer.GetComponentInParent<Canvas>() : null;
            var worldCamera = Camera.main;
            if (worldCamera == null)
            {
                return new Vector2(worldPosition.x * 100f, worldPosition.y * 100f);
            }

            var screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera != null ? canvas.worldCamera : worldCamera
                : null;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                popupLayer,
                screenPoint,
                eventCamera,
                out var localPoint)
                ? localPoint
                : Vector2.zero;
        }

        private RectTransform ResolveDamageNumberPopupLayer()
        {
            if (damageNumberPopupLayer != null)
            {
                damageNumberPopupLayer.SetAsLastSibling();
                return damageNumberPopupLayer;
            }

            var canvas = GameObject.Find("CombatCanvas")?.GetComponent<Canvas>()
                ?? FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.transform.Find("DamageNumberPopupLayer") as RectTransform;
            if (existing != null)
            {
                damageNumberPopupLayer = existing;
                damageNumberPopupLayer.SetAsLastSibling();
                return damageNumberPopupLayer;
            }

            var layerObject = new GameObject("DamageNumberPopupLayer", typeof(RectTransform));
            layerObject.transform.SetParent(canvas.transform, false);
            damageNumberPopupLayer = layerObject.GetComponent<RectTransform>();
            damageNumberPopupLayer.anchorMin = Vector2.zero;
            damageNumberPopupLayer.anchorMax = Vector2.one;
            damageNumberPopupLayer.offsetMin = Vector2.zero;
            damageNumberPopupLayer.offsetMax = Vector2.zero;
            damageNumberPopupLayer.SetAsLastSibling();
            return damageNumberPopupLayer;
        }

        private IEnumerator DamageNumberPopupRoutine(Transform popupTransform, TMP_Text label)
        {
            var startPosition = popupTransform.localPosition;
            var startTime = Time.realtimeSinceStartup;

            while (true)
            {
                if (popupTransform == null || label == null)
                {
                    yield break;
                }

                var elapsed = Time.realtimeSinceStartup - startTime;
                var t = Mathf.Clamp01(elapsed / DamageNumberPopupDurationSeconds);
                popupTransform.localPosition = startPosition + Vector3.up * Mathf.SmoothStep(0f, DamageNumberPopupRiseDistance, t);

                var pop = t < 0.24f
                    ? Mathf.Lerp(0.92f, 1.16f, Mathf.Clamp01(t / 0.24f))
                    : Mathf.Lerp(1.16f, 1f, Mathf.Clamp01((t - 0.24f) / 0.26f));
                popupTransform.localScale = Vector3.one * pop;

                var color = label.color;
                color.a = t < 0.62f ? 1f : Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.62f) / 0.38f));
                label.color = color;

                if (t >= 1f)
                {
                    break;
                }

                yield return null;
            }

            var popupObject = popupTransform.gameObject;
            damageNumberPopups.Remove(popupObject);
            DestroyDamageNumberPopup(popupObject);
        }

        private void ClearDamageNumberPopups()
        {
            foreach (var popup in damageNumberPopups)
            {
                if (popup == null)
                {
                    continue;
                }

                DestroyDamageNumberPopup(popup);
            }

            damageNumberPopups.Clear();
        }

        private static void DestroyDamageNumberPopup(GameObject popup)
        {
            var label = popup.GetComponent<TMP_Text>();
            var material = label != null ? label.fontMaterial : null;
            if (material != null && material.name.StartsWith("DamageNumberPopupMaterial"))
            {
                if (Application.isPlaying)
                {
                    Destroy(material);
                }
                else
                {
                    DestroyImmediate(material);
                }
            }

            if (Application.isPlaying)
            {
                Destroy(popup);
            }
            else
            {
                DestroyImmediate(popup);
            }
        }
    }
}
