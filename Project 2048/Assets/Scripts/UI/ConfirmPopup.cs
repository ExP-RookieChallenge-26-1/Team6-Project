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

    private void Awake()
    {
        yesButton.onClick.AddListener(OnYesClicked);
        noButton.onClick.AddListener(OnNoClicked);
        backgroundButton.onClick.AddListener(OnNoClicked);
        popupRoot.SetActive(false);
    }

    public void Show(string message, Action onYes, Action onNo = null)
    {
        CheckMessage.text = message;
        onYesAction = onYes;
        onNoAction = onNo;
        popupRoot.SetActive(true);

        
        CheckMessage.text = message;
        onYesAction = onYes;
        onNoAction = onNo;
        popupRoot.SetActive(true);
       
    }

    private void OnYesClicked()
    {
        popupRoot.SetActive(false);
        onYesAction?.Invoke();
    }

    private void OnNoClicked()
    {
        popupRoot.SetActive(false);
        onNoAction?.Invoke();
    }
}