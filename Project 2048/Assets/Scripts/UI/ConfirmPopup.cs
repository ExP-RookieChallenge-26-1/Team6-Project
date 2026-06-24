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

        CheckMessage.text = message;
        onYesAction = onYes;
        onNoAction = onNo;
        popupRoot.SetActive(true);
    }

    private void InitializeIfNeeded()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

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
}
