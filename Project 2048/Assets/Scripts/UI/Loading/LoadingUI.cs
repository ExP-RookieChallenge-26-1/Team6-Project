using System.Collections;
using Project2048.Flow;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Project2048.UI
{
    public class LoadingUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Graphic backgroundGraphic;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private int sortingOrder = 10000;
        [SerializeField] private float progressFillSpeed = 1.5f;
        [SerializeField] private float fadeDuration = 0.6f;
        [SerializeField] private float minimumOpaqueSeconds = 0.6f;

        private float targetProgress;
        private float displayedProgress;
        private Coroutine hideRoutine;
        private Coroutine fadeRoutine;
        private LoadingPresentationMode presentationMode = LoadingPresentationMode.Progress;

        public bool IsVisible => GetRoot().activeInHierarchy;
        public bool IsFadeOnlyPresentation => presentationMode == LoadingPresentationMode.FadeOnly;

        private void Awake()
        {
            root ??= gameObject;
            backgroundGraphic ??= GetRoot().GetComponent<Graphic>();
            loadingCanvas ??= GetComponentInParent<Canvas>();
            if (loadingCanvas != null)
            {
                loadingCanvas.overrideSorting = true;
                loadingCanvas.sortingOrder = sortingOrder;
            }

            Hide();
        }

        private void Update()
        {
            if (!GetRoot().activeSelf)
            {
                return;
            }

            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                targetProgress,
                progressFillSpeed * Time.unscaledDeltaTime);

            ApplyProgress(displayedProgress);
        }

        public void Show(LoadingPresentationMode mode)
        {
            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            presentationMode = mode;
            targetProgress = 0f;
            displayedProgress = 0f;
            ApplyProgress(displayedProgress);
            SetProgressVisible(mode == LoadingPresentationMode.Progress);
            GetRoot().SetActive(true);

            if (mode == LoadingPresentationMode.FadeOnly)
            {
                SetBackgroundAlpha(0f);
                fadeRoutine = StartCoroutine(FadeBackgroundTo(1f));
            }
            else
            {
                SetBackgroundAlpha(1f);
            }
        }

        public void Hide()
        {
            targetProgress = 1f;
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (!isActiveAndEnabled)
            {
                displayedProgress = 1f;
                ApplyProgress(displayedProgress);
                SetBackgroundAlpha(0f);
                GetRoot().SetActive(false);
                return;
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HideWhenProgressCompletes());
        }

        public IEnumerator WaitForSceneLoadStart()
        {
            if (presentationMode != LoadingPresentationMode.FadeOnly)
            {
                yield break;
            }

            while (fadeRoutine != null)
            {
                yield return null;
            }

            SetBackgroundAlpha(1f);
        }

        public IEnumerator CompleteSceneLoadPresentation()
        {
            if (presentationMode != LoadingPresentationMode.FadeOnly)
            {
                Hide();
                yield break;
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            SetProgressVisible(false);
            SetBackgroundAlpha(1f);

            if (minimumOpaqueSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(minimumOpaqueSeconds);
            }

            fadeRoutine = StartCoroutine(FadeBackgroundTo(0f));
            while (fadeRoutine != null)
            {
                yield return null;
            }

            GetRoot().SetActive(false);
        }

        public void SetProgress(float progress)
        {
            targetProgress = Mathf.Clamp01(progress);
        }

        private void ApplyProgress(float progress)
        {
            if (progressSlider == null)
            {
                UpdateProgressText(progress);
                return;
            }

            progressSlider.value = progress;
            UpdateProgressText(progress);
        }

        private void UpdateProgressText(float progress)
        {
            if (progressText == null)
            {
                return;
            }

            progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        private void SetProgressVisible(bool isVisible)
        {
            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(isVisible);
            }

            if (progressSlider != null)
            {
                progressSlider.gameObject.SetActive(isVisible);
            }

            if (progressText != null)
            {
                progressText.gameObject.SetActive(isVisible);
            }
        }

        private IEnumerator FadeBackgroundTo(float targetAlpha)
        {
            float startAlpha = backgroundGraphic != null ? backgroundGraphic.color.a : targetAlpha;
            float safeDuration = Mathf.Max(0.01f, fadeDuration);
            float elapsedTime = 0f;

            while (elapsedTime < safeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsedTime / safeDuration);
                SetBackgroundAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }

            SetBackgroundAlpha(targetAlpha);
            fadeRoutine = null;
        }

        private void SetBackgroundAlpha(float alpha)
        {
            if (backgroundGraphic == null)
            {
                return;
            }

            Color color = backgroundGraphic.color;
            color.a = alpha;
            backgroundGraphic.color = color;
        }

        private GameObject GetRoot()
        {
            root ??= gameObject;
            return root;
        }

        private IEnumerator HideWhenProgressCompletes()
        {
            while (displayedProgress < 0.999f)
            {
                displayedProgress = Mathf.MoveTowards(
                    displayedProgress,
                    1f,
                    progressFillSpeed * Time.unscaledDeltaTime);

                ApplyProgress(displayedProgress);
                yield return null;
            }

            displayedProgress = 1f;
            ApplyProgress(displayedProgress);
            hideRoutine = null;
            GetRoot().SetActive(false);
        }
    }
}
