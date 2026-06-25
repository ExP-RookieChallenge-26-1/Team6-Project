using Project2048.Core;
using UnityEngine;
using UnityEngine.UI;

public sealed class PausePopup : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private GameObject mainmenuPopup;
    [SerializeField] private Button PopupQuitButton;
    [SerializeField] private Button backgroundButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button backToMainMenuButton;
    [SerializeField] private Button noButton;

    private bool initialized;
    private bool wantsOpen;

    private void Awake()
    {
        InitializeIfNeeded();
        if (!wantsOpen && popupRoot != null)
        {
            mainmenuPopup.SetActive(false);
            popupRoot.SetActive(false);
        }
    }

    public void Open()
    {
        wantsOpen = true;
        InitializeIfNeeded();
        popupRoot.SetActive(true);
    }

    public void Close()
    {
        wantsOpen = false;
        InitializeIfNeeded();
        popupRoot.SetActive(false);
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        if (popupRoot == null)
        {
            popupRoot = gameObject;
        }

        if (PopupQuitButton != null)
        {
            PopupQuitButton.onClick.AddListener(Close);
        }

        if (backgroundButton != null)
        {
            backgroundButton.onClick.AddListener(Close);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OpenBackToMainMenuPopup);
        }

        if(backToMainMenuButton != null)
        {
            backToMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(CloseBackToMainMenu);
        }
    }

    public void OpenBackToMainMenuPopup()
    {
        mainmenuPopup.SetActive(true);
    }

    private void ReturnToMainMenu()
    {
        var flowController = GameManager.Instance != null
            ? GameManager.Instance.FlowController
            : null;

        if (flowController == null)
        {
            Debug.LogError("FlowController is not available.");
            return;
        }

        flowController.RequestMainMenu();
    }

    private void CloseBackToMainMenu()
    {
        mainmenuPopup.SetActive(false);
    }
}
