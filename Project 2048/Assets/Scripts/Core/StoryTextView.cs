using TMPro;
using UnityEngine;

namespace Project2048.Story
{
    public class StoryTextView : MonoBehaviour
    {
        [SerializeField] private StoryController storyController;
        [SerializeField] private TMP_Text storyText;

        private void Awake()
        {
            storyController ??= GetComponentInParent<StoryController>();
            storyText ??= GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (storyController == null)
            {
                return;
            }

            storyController.OnStoryStepChanged += HandleStoryStepChanged;
        }

        private void OnDisable()
        {
            if (storyController == null)
            {
                return;
            }

            storyController.OnStoryStepChanged -= HandleStoryStepChanged;
        }

        private void HandleStoryStepChanged(StoryStep step)
        {
            SetText(step != null ? step.text : string.Empty);
        }

        public void SetText(string text)
        {
            if (storyText == null)
            {
                return;
            }

            storyText.text = text;
        }
    }
}
