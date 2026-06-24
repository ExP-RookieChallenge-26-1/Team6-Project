using System.Collections.Generic;
using UnityEngine;

namespace Project2048.Stage
{
    [CreateAssetMenu(menuName = "Game/Stage/Stage Database")]
    public class StageDatabaseSO : ScriptableObject
    {
        public const int StagesPerFloor = 10;
        public const int UpperStageCount = 6;
        public const int MiddleStageCount = 8;
        public const int LowerStageCount = 6;
        public const int TotalStageCount = UpperStageCount + MiddleStageCount + LowerStageCount;

        [SerializeField] private List<StageSO> upperStages = new();
        [SerializeField] private List<StageSO> middleStages = new();
        [SerializeField] private List<StageSO> lowerStages = new();

        public bool TryGetStage(int stageIndex, out StageSO stage)
        {
            stage = null;
            if (stageIndex < 1 || stageIndex > TotalStageCount)
            {
                return false;
            }

            var stages = ResolveStages(stageIndex, out var stageIndexInFloor);
            if (stages == null || stageIndexInFloor >= stages.Count)
            {
                return false;
            }

            stage = stages[stageIndexInFloor];
            return stage != null;
        }

        public bool IsFinalStage(int stageIndex)
        {
            return stageIndex >= TotalStageCount;
        }

        private List<StageSO> ResolveStages(int stageIndex, out int stageIndexInFloor)
        {
            stageIndexInFloor = stageIndex - 1;
            if (stageIndex <= UpperStageCount)
            {
                return upperStages;
            }

            stageIndexInFloor = stageIndex - UpperStageCount - 1;
            if (stageIndex <= UpperStageCount + MiddleStageCount)
            {
                return middleStages;
            }

            stageIndexInFloor = stageIndex - UpperStageCount - MiddleStageCount - 1;
            return lowerStages;
        }

        private void OnValidate()
        {
            upperStages ??= new List<StageSO>();
            middleStages ??= new List<StageSO>();
            lowerStages ??= new List<StageSO>();
        }
    }
}
