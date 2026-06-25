using UnityEngine;
using UnityEngine.UI;

public sealed class PausePopup : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button backgroundButton;

    private bool initialized;
    private bool wantsOpen;

    private void Awake()
    {
        InitializeIfNeeded();
        if (!wantsOpen && popupRoot != null)
        {
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

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(Close);
        }

        if (backgroundButton != null)
        {
            backgroundButton.onClick.AddListener(Close);
        }
    }
}