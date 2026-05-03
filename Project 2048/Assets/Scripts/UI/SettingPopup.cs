using UnityEngine;
using UnityEngine.UI;

public class SettingPopup : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button backgroundButton;

    private void Awake()
    {
        exitButton.onClick.AddListener(Close);
        backgroundButton.onClick.AddListener(Close);
        popupRoot.SetActive(false);
    }

    public void Open()
    {
        popupRoot.SetActive(true);
    }

    public void Close()
    {
        popupRoot.SetActive(false);
    }
}