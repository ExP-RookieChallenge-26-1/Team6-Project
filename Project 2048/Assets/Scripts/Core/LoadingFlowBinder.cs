using Project2048.UI;
using UnityEngine;

namespace Project2048.Flow
{
    public class LoadingFlowBinder : MonoBehaviour
    {
        [SerializeField] private FlowController flowController;
        [SerializeField] private SceneFlowManager sceneFlowManager;
        [SerializeField] private LoadingUI loadingUI;

        private void Awake()
        {
            flowController ??= GetComponent<FlowController>();
            sceneFlowManager ??= GetComponent<SceneFlowManager>();
            loadingUI ??= GetComponentInChildren<LoadingUI>(true);
        }

        private void OnEnable()
        {
            if (flowController != null && loadingUI != null)
            {
                flowController.OnLoadingStarted += loadingUI.Show;
            }

            if (sceneFlowManager != null && loadingUI != null)
            {
                sceneFlowManager.OnLoadProgressChanged += loadingUI.SetProgress;
                sceneFlowManager.OnSceneLoadCompleted += loadingUI.Hide;
            }
        }

        private void OnDisable()
        {
            if (flowController != null && loadingUI != null)
            {
                flowController.OnLoadingStarted -= loadingUI.Show;
            }

            if (sceneFlowManager != null && loadingUI != null)
            {
                sceneFlowManager.OnLoadProgressChanged -= loadingUI.SetProgress;
                sceneFlowManager.OnSceneLoadCompleted -= loadingUI.Hide;
            }
        }
    }
}
