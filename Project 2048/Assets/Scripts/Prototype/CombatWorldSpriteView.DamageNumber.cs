using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Project2048.Prototype
{
    public partial class CombatWorldSpriteView
    {
        private const float DamageNumberPopupRiseDistance = 0.62f;
        private const float DamageNumberPopupWorldRadius = 0.4f;
        private const float DamageNumberPopupWorldFontSize = 2.9f;
        private const float DamageNumberPopupOutlineWidth = 0.22f;
        private const float DamageNumberPopupGlowInner = 0.04f;
        private const float DamageNumberPopupGlowOuter = 0.36f;
        private const float DamageNumberPopupGlowPower = 0.72f;
        private const float DamageNumberPopupGlowOffset = 0f;
        private const int DamageNumberPopupSortingOrderOffset = 32;

        private static readonly Color DamageNumberPopupNormalTextColor = Color.white;
        private static readonly Color DamageNumberPopupCriticalTextColor = new(1f, 0.82f, 0.02f, 1f);
        private static readonly Color DamageNumberPopupOutlineColor = new(0.04f, 0.04f, 0.04f, 1f);
        private static readonly Color DamageNumberPopupNormalGlowColor = new(1f, 1f, 1f, 0.58f);
        private static readonly Color DamageNumberPopupCriticalGlowColor = new(1f, 0.72f, 0f, 0.82f);

        private readonly List<GameObject> damageNumberPopups = new();

        private void PlayDamageNumberPopupIfNeeded(int damageAmount, SpriteRenderer targetRenderer, bool isCritical = false)
        {
            if (damageAmount <= 0 || targetRenderer == null)
            {
                return;
            }

            PlayDamageNumberWorldPopup(damageAmount, targetRenderer, isCritical);
        }

        private void PlayDamageNumberWorldPopup(int damageAmount, SpriteRenderer targetRenderer, bool isCritical)
        {
            var popupObject = new GameObject("DamageNumberPopup", typeof(TextMeshPro));
            popupObject.transform.SetParent(transform, true);
            popupObject.transform.position = ResolveDamageNumberWorldPosition(targetRenderer);
            popupObject.transform.rotation = Quaternion.identity;
            popupObject.transform.localScale = Vector3.one;
            damageNumberPopups.Add(popupObject);

            var label = popupObject.GetComponent<TextMeshPro>();
            ConfigureDamageNumberLabel(label, damageAmount, DamageNumberPopupWorldFontSize, isCritical);

            var meshRenderer = popupObject.GetComponent<MeshRenderer>();
            meshRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            meshRenderer.sortingOrder = targetRenderer.sortingOrder + DamageNumberPopupSortingOrderOffset;

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            StartCoroutine(DamageNumberPopupRoutine(popupObject.transform, label));
        }

        private static void ConfigureDamageNumberLabel(TMP_Text label, int damageAmount, float fontSize, bool isCritical)
        {
            var textColor = isCritical ? DamageNumberPopupCriticalTextColor : DamageNumberPopupNormalTextColor;
            label.text = damageAmount.ToString();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = textColor;
            label.outlineColor = DamageNumberPopupOutlineColor;
            label.outlineWidth = DamageNumberPopupOutlineWidth;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            ConfigureDamageNumberGlow(
                label,
                textColor,
                isCritical ? DamageNumberPopupCriticalGlowColor : DamageNumberPopupNormalGlowColor);
        }

        private static void ConfigureDamageNumberGlow(TMP_Text label, Color textColor, Color glowColor)
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
                material.SetColor(ShaderUtilities.ID_FaceColor, textColor);
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
                material.SetColor(ShaderUtilities.ID_GlowColor, glowColor);
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
            var offset2D = Random.insideUnitCircle * DamageNumberPopupWorldRadius;
            var offset = new Vector3(offset2D.x, offset2D.y, 0f);

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
