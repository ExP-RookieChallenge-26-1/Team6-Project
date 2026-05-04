using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project2048.Prototype
{
    public class StatusEffectTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string description;
        private Action<string, RectTransform> showTooltip;
        private Action hideTooltip;

        public void Initialize(
            string nextDescription,
            Action<string, RectTransform> nextShowTooltip,
            Action nextHideTooltip)
        {
            description = nextDescription;
            showTooltip = nextShowTooltip;
            hideTooltip = nextHideTooltip;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            showTooltip?.Invoke(description, transform as RectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hideTooltip?.Invoke();
        }
    }
}
