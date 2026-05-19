using System.Collections;
using Project2048.Core;
using Project2048.UI;
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

        private IEnumerator Start()
        {
            yield return null;

            if (flowController == null)
            {
                Debug.LogError("FlowController is not assigned.");
                yield break;
            }

            var loadingUI = FindLoadingUI();
            while (loadingUI != null && loadingUI.IsVisible)
            {
                yield return null;
            }

            flowController.CompleteBattleSceneLoad();
        }

        private static LoadingUI FindLoadingUI()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<LoadingUI>(FindObjectsInactive.Include);
#else
            return Object.FindObjectOfType<LoadingUI>(true);
#endif
        }
    }
}
