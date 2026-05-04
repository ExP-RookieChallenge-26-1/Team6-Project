using UnityEngine;
using Project2048.Flow;

namespace Project2048.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private GameContext gameContext;

        [SerializeField] private FlowController flowController;

        public FlowController FlowController => flowController;

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
            if (flowController == null)
            {
                Debug.LogError("FlowController is not assigned.");
                return;
            }

            flowController.Initialized(gameContext);
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

        }
    }
}
