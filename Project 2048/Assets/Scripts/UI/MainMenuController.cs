using Project2048.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private ConfirmPopup confirmPopup;
    [SerializeField] private SettingPopup settingPopup;
    [SerializeField] private FadeController fadeController;

    private const string GameSceneName = "BattleScene";

    private void Awake()
    {
        newGameButton.onClick.AddListener(OnNewGameClicked);
        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(OnLoadGameClicked);
        }

        settingButton.onClick.AddListener(OnSettingClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        RefreshLoadGameButton();
    }

    private void OnEnable()
    {
        RefreshLoadGameButton();
    }

    private void OnNewGameClicked()
    {
        confirmPopup.Show("새 게임을 시작하면 저장 데이터가 삭제됩니다.\n계속하시겠습니까?", StartNewGame, null);
    }

    private void StartNewGame()
    {
        fadeController.FadeOut(() => GameManager.Instance.StartNewGame());
    }

    private void OnLoadGameClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager is not present.");
            return;
        }

        fadeController.FadeOut(() => GameManager.Instance.StartSaveGame());
    }

    private void RefreshLoadGameButton()
    {
        if (loadGameButton == null)
        {
            return;
        }

        var hasSave =
            GameManager.Instance != null &&
            GameManager.Instance.SaveLoadManager != null &&
            GameManager.Instance.SaveLoadManager.HasSave;

        loadGameButton.interactable = hasSave;
    }

    private void OnSettingClicked()
    {
        settingPopup.Open();
    }

    private void OnQuitClicked()
    {
        confirmPopup.Show("종료하시겠습니까?", QuitGame, null);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LateUpdate()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
