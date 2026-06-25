using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmPopup : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI CheckMessage;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Button backgroundButton;

    private Action onYesAction;
    private Action onNoAction;
    private bool _isInitialized;
    private bool _wantsOpen;

    public static ConfirmPopup CreateRuntime(Transform parent)
    {
        var root = CreateRectObject("ConfirmPopup", parent);
        root.SetActive(false);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var backdropImage = root.AddComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0.58f);
        backdropImage.raycastTarget = true;
        var backdropButton = root.AddComponent<Button>();
        backdropButton.targetGraphic = backdropImage;

        var panel = CreateRectObject("Panel", root.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(620f, 360f);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.10f, 0.96f);
        panelImage.raycastTarget = true;

        var messageObject = CreateRectObject("CheckMessage", panel.transform);
        var messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0f, 0.5f);
        messageRect.anchorMax = new Vector2(1f, 1f);
        messageRect.offsetMin = new Vector2(48f, -28f);
        messageRect.offsetMax = new Vector2(-48f, -42f);

        var messageText = messageObject.AddComponent<TextMeshProUGUI>();
        messageText.fontSize = 34f;
        messageText.color = Color.white;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.textWrappingMode = TextWrappingModes.Normal;
        messageText.raycastTarget = false;

        var yesButtonObject = CreateButton("YesButton", panel.transform, "예", new Vector2(-130f, -108f));
        var noButtonObject = CreateButton("NoButton", panel.transform, "아니오", new Vector2(130f, -108f));

        var popup = root.AddComponent<ConfirmPopup>();
        popup.popupRoot = root;
        popup.CheckMessage = messageText;
        popup.yesButton = yesButtonObject;
        popup.noButton = noButtonObject;
        popup.backgroundButton = backdropButton;
        return popup;
    }

    private void Awake()
    {
        InitializeIfNeeded();

        if (!_wantsOpen && popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    public void Show(string message, Action onYes, Action onNo = null)
    {
        _wantsOpen = true;
        InitializeIfNeeded();

        if (popupRoot == null)
        {
            popupRoot = gameObject;
        }

        CheckMessage.text = message;
        onYesAction = onYes;
        onNoAction = onNo;
        popupRoot.SetActive(true);
        popupRoot.transform.SetAsLastSibling();
    }

    private void InitializeIfNeeded()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (popupRoot == null || popupRoot.GetComponent<ConfirmPopup>() != this)
        {
            popupRoot = gameObject;
        }

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(OnNoClicked);
        }

        if (backgroundButton != null)
        {
            backgroundButton.onClick.AddListener(OnNoClicked);
        }
    }

    private void OnYesClicked()
    {
        _wantsOpen = false;
        popupRoot.SetActive(false);
        onYesAction?.Invoke();
    }

    private void OnNoClicked()
    {
        _wantsOpen = false;
        popupRoot.SetActive(false);
        onNoAction?.Invoke();
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition)
    {
        var buttonObject = CreateRectObject(name, parent);
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(180f, 82f);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.17f, 0.18f, 0.95f);
        image.raycastTarget = true;

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        var labelObject = CreateRectObject("Label", buttonObject.transform);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelObject.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 30f;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;
        return button;
    }

    private static GameObject CreateRectObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = parent != null ? parent.gameObject.layer : 5;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }
}
