using System;
using UnityEngine;

namespace Project2048.Story
{
    public class StoryController : MonoBehaviour
    {
        [SerializeField] private StoryDataSO openingStory;

        public event Action<StoryStep> OnStoryStepChanged;
        public event Action OnStoryFinished;

        private StoryDataSO currentStory;
        private int currentIndex;   // 스토리 진행 인덱스

        private void Start()
        {
            StartStory(openingStory);
        }

        public void StartStory(StoryDataSO storyData)
        {
            // storyData 기준으로 첫 단계 표시
            currentStory = storyData;
            currentIndex = 0;

            
            if (storyData == null || storyData.steps.Count == 0)
            {
                FinishStory();
                return;
            }

            ShowCurrentStep();
           
        }

        public void Next()
        {
            // UI에서 다음 클릭 시 다음 대사로 이동
            // 마지막이면 FinishStory()
            if (currentStory == null || currentStory.steps == null || currentStory.steps.Count == 0)
            {
                FinishStory();
                return;
            }

            currentIndex++;
            
            if (currentIndex >= openingStory.steps.Count)
            {
                FinishStory();
                return;
            }

            ShowCurrentStep();
            
        }

        public void ShowCurrentStep()
        {
            if (currentStory == null ||
                currentStory.steps == null ||
                currentIndex < 0 ||
                currentIndex >= currentStory.steps.Count)
            {
                return;
            }

            OnStoryStepChanged?.Invoke(currentStory.steps[currentIndex]);
        }

        public void Skip()
        {
            FinishStory();
        }

        private void FinishStory()
        {
            OnStoryFinished?.Invoke();
        }
    }
}
