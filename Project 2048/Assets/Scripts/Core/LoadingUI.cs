using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Project2048.UI
{
    public class LoadingUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private int sortingOrder = 10000;
        [SerializeField] private float progressFillSpeed = 1.5f;

        private float targetProgress;
        private float displayedProgress;
        private Coroutine hideRoutine;

        private void Awake()
        {
            root ??= gameObject;
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

        public void Show()
        {
            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }

            targetProgress = 0f;
            displayedProgress = 0f;
            ApplyProgress(displayedProgress);
            GetRoot().SetActive(true);
        }

        public void Hide()
        {
            targetProgress = 1f;
            if (!isActiveAndEnabled)
            {
                displayedProgress = 1f;
                ApplyProgress(displayedProgress);
                GetRoot().SetActive(false);
                return;
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HideWhenProgressCompletes());
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
            GetRoot().SetActive(false);
            hideRoutine = null;
        }
    }
}
