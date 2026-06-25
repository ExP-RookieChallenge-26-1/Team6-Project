using System.Collections.Generic;
using UnityEngine;

namespace Project2048.Stage
{
    [CreateAssetMenu(menuName = "Game/Stage/Stage Database")]
    public class StageDatabaseSO : ScriptableObject
    {
        public const int StagesPerFloor = 10;
        public const int UpperStageCount = 6;
        public const int MiddleStageCount = 7;
        public const int LowerStageCount = 7;
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

        public static bool TryResolveStagePosition(
            int stageIndex,
            out StageFloor floor,
            out int stageNumberInFloor)
        {
            floor = StageFloor.Upper;
            stageNumberInFloor = 0;

            if (stageIndex < 1 || stageIndex > TotalStageCount)
            {
                return false;
            }

            if (stageIndex <= UpperStageCount)
            {
                stageNumberInFloor = stageIndex;
                return true;
            }

            if (stageIndex <= UpperStageCount + MiddleStageCount)
            {
                floor = StageFloor.Middle;
                stageNumberInFloor = stageIndex - UpperStageCount;
                return true;
            }

            floor = StageFloor.Lower;
            stageNumberInFloor = stageIndex - UpperStageCount - MiddleStageCount;
            return true;
        }

        private List<StageSO> ResolveStages(int stageIndex, out int stageIndexInFloor)
        {
            if (!TryResolveStagePosition(stageIndex, out var floor, out var stageNumberInFloor))
            {
                stageIndexInFloor = -1;
                return null;
            }

            stageIndexInFloor = stageNumberInFloor - 1;
            switch (floor)
            {
                case StageFloor.Upper:
                    return upperStages;
                case StageFloor.Middle:
                    return middleStages;
                case StageFloor.Lower:
                    return lowerStages;
                default:
                    return null;
            }
        }

        private void OnValidate()
        {
            upperStages ??= new List<StageSO>();
            middleStages ??= new List<StageSO>();
            lowerStages ??= new List<StageSO>();
        }
    }
}
