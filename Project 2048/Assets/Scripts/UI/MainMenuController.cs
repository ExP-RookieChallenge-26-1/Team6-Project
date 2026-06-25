using Project2048.Save;
using Project2048.Stage;
using Project2048.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Button Groups")]
    [SerializeField] private GameObject mainButtonGroup;
    [SerializeField] private GameObject startButtonGroup;

    [Header("Main Buttons")]
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitButton;

    [Header("Start Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button backButton;

    [Header("Popups")]
    [SerializeField] private ConfirmPopup confirmPopup;
    [SerializeField] private SettingPopup settingPopup;
    [SerializeField] private FadeController fadeController;

    private const string GameSceneName = "BattleScene";

    private void Awake()
    {
        if (gameStartButton != null)
        {
            gameStartButton.onClick.AddListener(OnGameStartClicked);
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }

        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(OnLoadGameClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (settingButton != null)
        {
            settingButton.onClick.AddListener(OnSettingClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        ShowMainButtons();
        RefreshLoadGameButton();
    }

    private void OnEnable()
    {
        RefreshLoadGameButton();
    }

    private void Start()
    {
        RefreshLoadGameButton();
    }

    private void OnGameStartClicked()
    {
        ShowStartButtons();
    }

    private void OnBackClicked()
    {
        ShowMainButtons();
    }

    private void ShowMainButtons()
    {
        if (mainButtonGroup != null)
        {
            mainButtonGroup.SetActive(true);
        }

        if (startButtonGroup != null)
        {
            startButtonGroup.SetActive(false);
        }
    }

    private void ShowStartButtons()
    {
        if (mainButtonGroup != null)
        {
            mainButtonGroup.SetActive(false);
        }

        if (startButtonGroup != null)
        {
            startButtonGroup.SetActive(true);
        }

        RefreshLoadGameButton();
    }

    private void OnNewGameClicked()
    {
        confirmPopup.Show("새 게임을 시작하면\n기존 저장 데이터가 삭제됩니다.\n계속하시겠습니까?", StartNewGame, null);
    }

    private void StartNewGame()
    {
        fadeController.FadeOut(() => GameManager.Instance.StartNewGame());
    }

    private void OnLoadGameClicked()
    {
        var saveLoadManager = GameManager.Instance?.SaveLoadManager;
        if (saveLoadManager == null ||
            !saveLoadManager.TryGetSaveSummary(out var summary) ||
            !summary.HasActiveRun)
        {
            Debug.LogWarning("이어할 수 있는 저장 데이터가 없습니다.");
            RefreshLoadGameButton();
            return;
        }

        confirmPopup.Show(BuildLoadGameConfirmationMessage(summary), ContinueSavedGame);
    }

    private void ContinueSavedGame()
    {
        fadeController.FadeOut(() => GameManager.Instance.StartSaveGame());
    }

    private static string BuildLoadGameConfirmationMessage(SaveGameSummary summary)
    {
        var lastPlayedAtText = summary.SavedAtUtc == default
            ? "알 수 없음"
            : summary.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        var currentHpText = summary.HasCurrentHp
            ? summary.CurrentHp.ToString()
            : "기록 없음";

        return
            "저장된 게임 정보\n" +
            $"마지막 플레이 시간: {lastPlayedAtText}\n" +
            $"진행 스테이지: {BuildStageText(summary.StageIndex)}\n" +
            $"현재 HP: {currentHpText}\n" +
            "이어서 진행하시겠습니까?";
    }

    private static string BuildStageText(int stageIndex)
    {
        if (!StageDatabaseSO.TryResolveStagePosition(
                stageIndex,
                out var floor,
                out var stageNumberInFloor))
        {
            return "알 수 없음";
        }

        var floorText = floor switch
        {
            StageFloor.Upper => "상층",
            StageFloor.Middle => "중층",
            StageFloor.Lower => "하층",
            _ => "알 수 없음"
        };

        return $"{floorText} {stageNumberInFloor}스테이지";
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
