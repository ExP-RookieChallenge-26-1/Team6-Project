using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private ConfirmPopup confirmPopup;
    [SerializeField] private SettingPopup settingPopup;
    [SerializeField] private FadeController fadeController;

    private const string GameSceneName = "SampleScene";

    private void Awake()
    {
        newGameButton.onClick.AddListener(OnNewGameClicked);
        loadGameButton.onClick.AddListener(OnLoadGameClicked);
        settingButton.onClick.AddListener(OnSettingClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnNewGameClicked()
    {
        confirmPopup.Show("새로 하시겠습니까?", StartNewGame, null);
    }

    private void StartNewGame()
    {
        fadeController.FadeOut(() => SceneManager.LoadScene(GameSceneName));
    }

    private void OnLoadGameClicked()
    {
        Debug.Log("이어하기 클릭됨");
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