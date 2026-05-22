using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project2048.Prototype
{
    public class StatusEffectTooltipTarget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public const float LongPressDelaySeconds = 0.42f;

        private string description;
        private Action<string, RectTransform> showTooltip;
        private Action hideTooltip;
        private Coroutine longPressCoroutine;
        private Coroutine restoreSelectableCoroutine;
        private Selectable pressedSelectable;
        private bool isPressing;
        private bool isTooltipVisible;
        private bool wasSelectableInteractable;

        public void Initialize(
            string nextDescription,
            Action<string, RectTransform> nextShowTooltip,
            Action nextHideTooltip)
        {
            description = nextDescription;
            showTooltip = nextShowTooltip;
            hideTooltip = nextHideTooltip;
        }

        private void OnDisable()
        {
            CancelPress(hideVisibleTooltip: true, suppressClick: false);
            RestoreSelectableImmediately();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            CancelPress(hideVisibleTooltip: true, suppressClick: false);
            if (string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            isPressing = true;
            pressedSelectable = GetComponent<Selectable>();
            wasSelectableInteractable = pressedSelectable != null && pressedSelectable.interactable;
            longPressCoroutine = StartCoroutine(ShowAfterLongPress());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CancelPress(hideVisibleTooltip: true, suppressClick: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelPress(hideVisibleTooltip: true, suppressClick: true);
        }

        private IEnumerator ShowAfterLongPress()
        {
            yield return new WaitForSecondsRealtime(LongPressDelaySeconds);
            longPressCoroutine = null;
            if (!isPressing || string.IsNullOrWhiteSpace(description))
            {
                yield break;
            }

            isTooltipVisible = true;
            showTooltip?.Invoke(description, transform as RectTransform);
        }

        private void CancelPress(bool hideVisibleTooltip, bool suppressClick)
        {
            isPressing = false;
            if (longPressCoroutine != null)
            {
                StopCoroutine(longPressCoroutine);
                longPressCoroutine = null;
            }

            var shouldSuppressClick = suppressClick
                && isTooltipVisible
                && pressedSelectable != null
                && wasSelectableInteractable
                && pressedSelectable.interactable;

            if (hideVisibleTooltip && isTooltipVisible)
            {
                hideTooltip?.Invoke();
                isTooltipVisible = false;
            }

            if (shouldSuppressClick)
            {
                SuppressSelectableClickForCurrentFrame(pressedSelectable);
            }

            pressedSelectable = null;
            wasSelectableInteractable = false;
        }

        private void SuppressSelectableClickForCurrentFrame(Selectable selectable)
        {
            if (restoreSelectableCoroutine != null)
            {
                StopCoroutine(restoreSelectableCoroutine);
            }

            selectable.interactable = false;
            restoreSelectableCoroutine = StartCoroutine(RestoreSelectableNextFrame(selectable));
        }

        private IEnumerator RestoreSelectableNextFrame(Selectable selectable)
        {
            yield return null;
            if (selectable != null)
            {
                selectable.interactable = true;
            }

            restoreSelectableCoroutine = null;
        }

        private void RestoreSelectableImmediately()
        {
            if (restoreSelectableCoroutine != null)
            {
                StopCoroutine(restoreSelectableCoroutine);
                restoreSelectableCoroutine = null;
            }

            if (pressedSelectable != null && wasSelectableInteractable)
            {
                pressedSelectable.interactable = true;
            }

            pressedSelectable = null;
            wasSelectableInteractable = false;
        }
    }
}
