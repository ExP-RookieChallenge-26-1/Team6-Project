using Project2048.Core;
using Project2048.Flow;
using UnityEngine;

namespace Project2048.Story
{
    public class StoryFlowBinder : MonoBehaviour
    {
        [SerializeField] private StoryController storyController;

        private FlowController flowController;

        private void Awake()
        {
            storyController ??= GetComponent<StoryController>();

            if (GameManager.Instance != null)
            {
                flowController = GameManager.Instance.FlowController;
            }
        }

        private void OnEnable()
        {
            if (storyController == null || flowController == null)
            {
                return;
            }

            storyController.OnStoryFinished += flowController.StartGameAfterStory;
        }

        private void OnDisable()
        {
            if (storyController == null || flowController == null)
            {
                return;
            }

            storyController.OnStoryFinished -= flowController.StartGameAfterStory;
        }
    }
}
