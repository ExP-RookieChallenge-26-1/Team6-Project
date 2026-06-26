using UnityEngine;
using Project2048.Flow;
using Project2048.Save;

namespace Project2048.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField]
        private GameContext gameContext;

        [SerializeField] private FlowController flowController;
        [SerializeField] private SaveLoadManager saveLoadManager;

        public FlowController FlowController => flowController;
        public SaveLoadManager SaveLoadManager => saveLoadManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            gameContext = new GameContext();

            flowController ??= GetComponent<FlowController>();
            saveLoadManager ??= GetComponent<SaveLoadManager>();
            if (flowController == null)
            {
                Debug.LogError("FlowController is not assigned.");
                return;
            }

            if (saveLoadManager == null)
            {
                Debug.LogError("SaveLoadManager is not assigned.");
                return;
            }

            saveLoadManager.Initialize(gameContext);
            flowController.Initialized(gameContext, saveLoadManager);
        }

        public void StartNewGame()
        {
            if (flowController == null)
            {
                Debug.LogError("FlowController is not assigned.");
                return;
            }

            flowController.SetNewGame();
        }

        public void StartSaveGame()
        {
            if (flowController == null)
            {
                Debug.LogError("FlowController is not assigned.");
                return;
            }

            flowController.ContinueGame();
        }
    }
}
