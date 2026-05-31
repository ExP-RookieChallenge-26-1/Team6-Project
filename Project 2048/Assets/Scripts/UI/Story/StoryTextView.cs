using TMPro;
using UnityEngine;

namespace Project2048.Story
{
    public class StoryTextView : MonoBehaviour
    {
        [SerializeField] private StoryController storyController;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text storyText;

        private void Awake()
        {
            storyController ??= GetComponentInParent<StoryController>();
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
            SetDialogue(
                step != null ? step.speakerName : string.Empty,
                step != null ? step.text : string.Empty);
        }

        public void SetDialogue(string speakerName, string text)
        {
            if (speakerNameText != null)
            {
                speakerNameText.text = speakerName;
            }

            if (storyText != null)
            {
                storyText.text = text;
            }
        }

        public void Clear()
        {
            SetDialogue(string.Empty, string.Empty);
        }
    }
}
