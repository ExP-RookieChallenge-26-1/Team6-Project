using UnityEngine;
using UnityEngine.UI;

namespace Project2048
{
    public class TopBarButtonsController : MonoBehaviour
    {
        [SerializeField]
        Button settingsButton;
        [SerializeField]
        Button pauseButton;

        [SerializeField]
        GameObject settingsPopup;
        [SerializeField]
        GameObject pausePopup;

        private PausePopup pausePopupComponent;
        private SettingPopup settingsPopupComponent;

        private void Awake()
        {
            pausePopupComponent = pausePopup.GetComponent<PausePopup>();
            settingsPopupComponent = settingsPopup.GetComponent<SettingPopup>();

            settingsButton.onClick.AddListener(OnClickSettings);
            pauseButton.onClick.AddListener(OnClickPause);
        }

        public void OnClickPause()
        {
            pausePopupComponent.Open();
        }

        public void OnClickSettings()
        {
            settingsPopupComponent.Open();
        }
    }
}
