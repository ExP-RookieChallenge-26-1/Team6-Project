using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        SetAlpha(0f);
    }

    public void FadeOut(Action onComplete)
    {
        StartCoroutine(FadeRoutine(0f, 1f, onComplete));
    }

    public void FadeIn(Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(1f, 0f, onComplete));
    }

    private IEnumerator FadeRoutine(float from, float to, Action onComplete)
    {
        fadeImage.raycastTarget = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(to);
        if (to == 0f) fadeImage.raycastTarget = false;
        onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }
}