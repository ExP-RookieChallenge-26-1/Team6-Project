using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MainMenuController
{
    private const float ReferenceButtonGroupY = -780f;
    private const float ReferenceButtonSpacing = 350f;
    private const float ReferenceButtonWidth = 250f;
    private const float ReferenceButtonHeight = 86f;
    private const float ReferenceTitleY = -430f;
    private const float ReferenceTitleWidth = 960f;
    private const float ReferenceTitleHeight = 260f;

    private static readonly Color ReferenceButtonColor = new(0.47f, 0.47f, 0.45f, 0.96f);
    private static readonly Color ReferenceButtonTextColor = new(0.13f, 0.13f, 0.13f, 1f);
    private static readonly Color ReferenceButtonOutlineColor = new(0.02f, 0.02f, 0.02f, 1f);

    private void ApplyReferencePosterLayout()
    {
        var canvasRoot = ResolveMainMenuCanvasRoot();
        if (canvasRoot == null)
        {
            return;
        }

        LayoutBackground(canvasRoot);
        LayoutTitle(canvasRoot);
        LayoutButtonGroup(mainButtonGroup, gameStartButton, settingButton, quitButton);
        LayoutButtonGroup(startButtonGroup, newGameButton, loadGameButton, backButton);
    }

    private RectTransform ResolveMainMenuCanvasRoot()
    {
        var canvas = mainButtonGroup != null
            ? mainButtonGroup.GetComponentInParent<Canvas>(true)
            : null;

        if (canvas != null)
        {
            return canvas.transform as RectTransform;
        }

        var canvasObject = GameObject.Find("MainMenuCanvas");
        return canvasObject != null ? canvasObject.transform as RectTransform : null;
    }

    private static void LayoutBackground(RectTransform canvasRoot)
    {
        var background = canvasRoot.Find("MainMenuBackground") as RectTransform;
        if (background == null)
        {
            return;
        }

        background.SetAsFirstSibling();
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;
        background.pivot = new Vector2(0.5f, 0.5f);

        if (background.TryGetComponent<RawImage>(out var rawImage))
        {
            rawImage.raycastTarget = false;
        }
    }

    private static void LayoutTitle(RectTransform canvasRoot)
    {
        var title = canvasRoot.Find("GameTitle") as RectTransform;
        if (title == null)
        {
            return;
        }

        SetCenteredRect(title, new Vector2(0f, ReferenceTitleY), new Vector2(ReferenceTitleWidth, ReferenceTitleHeight));

        var titleText = title.GetComponent<TMP_Text>();
        if (titleText == null)
        {
            return;
        }

        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 88f;
        titleText.fontSizeMax = 190f;
        titleText.characterSpacing = 0f;
        titleText.raycastTarget = false;
    }

    private static void LayoutButtonGroup(GameObject group, params Button[] buttons)
    {
        if (group == null)
        {
            return;
        }

        if (group.transform is RectTransform groupRect)
        {
            SetCenteredRect(groupRect, new Vector2(0f, ReferenceButtonGroupY), new Vector2(1080f, 140f));
        }

        for (var index = 0; index < buttons.Length; index++)
        {
            LayoutPosterButton(buttons[index], (index - 1) * ReferenceButtonSpacing);
        }
    }

    private static void LayoutPosterButton(Button button, float xPosition)
    {
        if (button == null || button.transform is not RectTransform rectTransform)
        {
            return;
        }

        SetCenteredRect(
            rectTransform,
            new Vector2(xPosition, 0f),
            new Vector2(ReferenceButtonWidth, ReferenceButtonHeight));

        if (button.targetGraphic is Image targetImage)
        {
            targetImage.color = ReferenceButtonColor;
            targetImage.type = Image.Type.Sliced;
            targetImage.raycastTarget = true;
        }

        var outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = ReferenceButtonOutlineColor;
        outline.effectDistance = new Vector2(4f, -4f);
        outline.useGraphicAlpha = false;

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            return;
        }

        label.alignment = TextAlignmentOptions.Center;
        label.color = ReferenceButtonTextColor;
        label.enableAutoSizing = true;
        label.fontSizeMin = 24f;
        label.fontSizeMax = 42f;
        label.characterSpacing = 0f;
        label.margin = new Vector4(10f, 4f, 10f, 4f);
    }

    private static void SetCenteredRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }
}
