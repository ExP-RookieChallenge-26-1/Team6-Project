using Project2048.Core;
using UnityEngine;

namespace Project2048.Flow
{
    public class BattleSceneBinder : MonoBehaviour
    {
        [SerializeField] private FlowController flowController;

        private void Awake()
        {
            if (flowController == null && GameManager.Instance != null)
            {
                flowController = GameManager.Instance.FlowController;
            }
        }

        private void Start()
        {
            if (flowController == null)
            {
                Debug.LogError("FlowController is not assigned.");
                return;
            }

            flowController.CompleteBattleSceneLoad();
        }
    }
}
