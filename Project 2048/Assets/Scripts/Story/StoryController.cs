using System;
using System.Collections;
using Project2048.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Project2048.Story
{
    public class StoryController : MonoBehaviour
    {
        [SerializeField] private StoryDataSO openingStory;
        [SerializeField] private StoryDataSO endingStory;
        [SerializeField] private StoryTextView storyTextView;
        [SerializeField] private StoryBackgroundView storyBackgroundView;
        [SerializeField] private StoryLayoutView storyLayoutView;
        [SerializeField] private Button nextButton;

        public event Action<StoryStep> OnStoryStepChanged;
        public event Action OnStoryFinished;

        private StoryDataSO currentStory;
        private int currentIndex;
        private Coroutine storyRoutine;
        private bool isSequencePlaying;

        private void Awake()
        {
            storyTextView ??= GetComponentInChildren<StoryTextView>(true);
            storyBackgroundView ??= GetComponentInChildren<StoryBackgroundView>(true);
            storyLayoutView ??= GetComponentInChildren<StoryLayoutView>(true);
            nextButton ??= GetComponentInChildren<Button>(true);
        }

        private void Start()
        {
            StartStory(ResolveInitialStory());
        }

        private StoryDataSO ResolveInitialStory()
        {
            var flowController = GameManager.Instance != null
                ? GameManager.Instance.FlowController
                : null;
            if (flowController != null &&
                flowController.CurrentGameState == GameContext.GameState.Ending)
            {
                return endingStory;
            }

            return openingStory;
        }

        public void StartStory(StoryDataSO storyData)
        {
            currentStory = storyData;
            currentIndex = 0;
            storyBackgroundView?.ApplyStoryBackgrounds(storyData);

            if (storyData == null || storyData.steps.Count == 0)
            {
                FinishStory();
                return;
            }

            PlayCurrentStep();
        }

        public void Next()
        {
            if (isSequencePlaying)
            {
                return;
            }

            if (currentStory == null || currentStory.steps == null || currentStory.steps.Count == 0)
            {
                FinishStory();
                return;
            }

            currentIndex++;

            if (currentIndex >= currentStory.steps.Count)
            {
                FinishStory();
                return;
            }

            PlayCurrentStep();
        }

        public void ShowCurrentStep()
        {
            PlayCurrentStep();
        }

        public void Skip()
        {
            if (storyRoutine != null)
            {
                StopCoroutine(storyRoutine);
                storyRoutine = null;
            }

            isSequencePlaying = false;
            FinishStory();
        }

        private void PlayCurrentStep()
        {
            if (storyRoutine != null)
            {
                StopCoroutine(storyRoutine);
            }

            storyRoutine = StartCoroutine(ShowCurrentStepRoutine());
        }

        private IEnumerator ShowCurrentStepRoutine()
        {
            if (currentStory == null ||
                currentStory.steps == null ||
                currentIndex < 0 ||
                currentIndex >= currentStory.steps.Count)
            {
                yield break;
            }

            isSequencePlaying = true;

            StoryStep step = currentStory.steps[currentIndex];

            if (step.shouldChangeBackground && storyBackgroundView != null)
            {
                SetNextButtonVisible(false);
                storyTextView?.Clear();
                yield return storyBackgroundView.PlayTransition();
            }

            OnStoryStepChanged?.Invoke(step);
            SetNextButtonVisible(true);
            isSequencePlaying = false;
            storyRoutine = null;
        }

        private void FinishStory()
        {
            if (storyRoutine != null)
            {
                StopCoroutine(storyRoutine);
            }

            storyRoutine = StartCoroutine(FinishStoryRoutine());
        }

        private IEnumerator FinishStoryRoutine()
        {
            isSequencePlaying = true;
            SetNextButtonVisible(false);
            storyTextView?.Clear();

            if (storyLayoutView != null)
            {
                yield return storyLayoutView.PlayEndingLayout();
            }

            isSequencePlaying = false;
            storyRoutine = null;
            OnStoryFinished?.Invoke();
        }

        private void SetNextButtonVisible(bool isVisible)
        {
            if (nextButton == null)
            {
                return;
            }

            nextButton.gameObject.SetActive(isVisible);
        }
    }
}
