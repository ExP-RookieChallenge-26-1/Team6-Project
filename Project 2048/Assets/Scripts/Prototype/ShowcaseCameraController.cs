using UnityEngine;
using UnityEngine.InputSystem;

namespace Project2048.Prototype
{
    [RequireComponent(typeof(Camera))]
    public sealed class ShowcaseCameraController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 8f;
        [SerializeField, Min(1f)] private float fastMoveMultiplier = 1.8f;
        [SerializeField, Min(0.1f)] private float zoomSpeed = 1.25f;
        [SerializeField, Min(0.5f)] private float minOrthographicSize = 3.2f;
        [SerializeField, Min(0.5f)] private float maxOrthographicSize = 9f;

        private Camera targetCamera;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var move = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                move.x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                move.x += 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                move.y += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                move.y -= 1f;
            }

            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            var speedMultiplier = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
                ? fastMoveMultiplier
                : 1f;
            transform.position += new Vector3(move.x, move.y, 0f) * (moveSpeed * speedMultiplier * Time.unscaledDeltaTime);

            var zoomInput = 0f;
            if (keyboard.qKey.isPressed)
            {
                zoomInput += 1f;
            }

            if (keyboard.eKey.isPressed)
            {
                zoomInput -= 1f;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                zoomInput -= mouse.scroll.ReadValue().y * 0.01f;
            }

            if (!Mathf.Approximately(zoomInput, 0f))
            {
                targetCamera ??= GetComponent<Camera>();
                targetCamera.orthographicSize = Mathf.Clamp(
                    targetCamera.orthographicSize + zoomInput * zoomSpeed * Time.unscaledDeltaTime,
                    minOrthographicSize,
                    maxOrthographicSize);
            }
        }
    }
}
