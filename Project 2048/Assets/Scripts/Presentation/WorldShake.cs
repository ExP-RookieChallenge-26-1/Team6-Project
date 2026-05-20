using System.Collections;
using UnityEngine;

namespace Project2048.Presentation
{
    public class WorldShake : MonoBehaviour
    {
        [SerializeField] private float defaultDuration = 0.15f;
        [SerializeField] private float defaultMagnitude = 0.08f;
        [SerializeField] private bool useUnscaledTime = true;

        private Vector3 originalLocalPosition;
        private Coroutine shakeCoroutine;

        public bool IsShaking => shakeCoroutine != null;

        private void Awake()
        {
            ResetRestPosition();
        }

        private void OnDisable()
        {
            StopShake(restorePosition: true);
        }

        public void ResetRestPosition()
        {
            originalLocalPosition = transform.localPosition;
        }

        public void Shake()
        {
            Shake(defaultDuration, defaultMagnitude);
        }

        public void Shake(float duration, float magnitude)
        {
            if (shakeCoroutine != null)
            {
                StopShake(restorePosition: true);
            }
            else
            {
                ResetRestPosition();
            }

            if (!Application.isPlaying || !isActiveAndEnabled || duration <= 0f || magnitude <= 0f)
            {
                RestorePosition();
                return;
            }

            shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        public void StopShake(bool restorePosition = true)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }

            if (restorePosition)
            {
                RestorePosition();
            }
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                var progress = Mathf.Clamp01(elapsed / duration);
                var damping = 1f - progress;
                transform.localPosition = originalLocalPosition + CreateShakeOffset(magnitude, damping);

                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            RestorePosition();
            shakeCoroutine = null;
        }

        private void RestorePosition()
        {
            transform.localPosition = originalLocalPosition;
        }

        private static Vector3 CreateShakeOffset(float magnitude, float damping)
        {
            var sample = Random.insideUnitCircle;
            if (sample.sqrMagnitude <= 0.0001f)
            {
                sample = Vector2.right;
            }

            sample = sample.normalized * Random.Range(0.5f, 1f);
            return new Vector3(sample.x, sample.y, 0f) * magnitude * damping;
        }
    }
}
