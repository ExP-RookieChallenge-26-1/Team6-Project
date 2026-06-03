using System.Collections.Generic;
using UnityEngine;

namespace Project2048.Stage
{
    [CreateAssetMenu(menuName = "Game/Stage/Stage Database")]
    public class StageDatabaseSO : ScriptableObject
    {
        public const int StagesPerFloor = 10;
        public const int TotalStageCount = StagesPerFloor * 3;

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

            var zeroBasedIndex = stageIndex - 1;
            var floorIndex = zeroBasedIndex / StagesPerFloor;
            var stageIndexInFloor = zeroBasedIndex % StagesPerFloor;
            var stages = ResolveStages(floorIndex);
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

        private List<StageSO> ResolveStages(int floorIndex)
        {
            return floorIndex switch
            {
                0 => upperStages,
                1 => middleStages,
                _ => lowerStages,
            };
        }

        private void OnValidate()
        {
            upperStages ??= new List<StageSO>();
            middleStages ??= new List<StageSO>();
            lowerStages ??= new List<StageSO>();
        }
    }
}
