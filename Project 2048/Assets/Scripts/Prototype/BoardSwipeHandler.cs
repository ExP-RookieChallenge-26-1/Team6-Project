using System;
using Project2048.Board2048;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project2048.Prototype
{
    /// <summary>
    /// Catches drag/swipe gestures on the 2048 board area and emits a Direction.
    /// Works with mouse drag (PC) and touch (mobile) via Unity's EventSystem.
    /// PC arrow-key input is handled by CombatUiView itself.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BoardSwipeHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float minSwipeDistance = 40f;

        public event Action<Direction> OnSwipe;

        private Vector2 startPosition;
        private bool pointerTracking;

        public void OnPointerDown(PointerEventData eventData)
        {
            StartTracking(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            FinishTracking(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            StartTracking(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // No-op: we only care about end-of-swipe direction.
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            FinishTracking(eventData);
        }

        private void StartTracking(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            startPosition = eventData.position;
            pointerTracking = true;
        }

        private void FinishTracking(PointerEventData eventData)
        {
            if (!pointerTracking || eventData == null)
            {
                return;
            }

            pointerTracking = false;
            var delta = eventData.position - startPosition;
            if (delta.magnitude < minSwipeDistance)
            {
                return;
            }

            var direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? (delta.x > 0 ? Direction.Right : Direction.Left)
                : (delta.y > 0 ? Direction.Up : Direction.Down);

            OnSwipe?.Invoke(direction);
        }
    }
}
