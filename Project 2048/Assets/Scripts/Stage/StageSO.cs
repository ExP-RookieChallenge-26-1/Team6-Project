using System.Collections.Generic;
using Project2048.Enemy;
using UnityEngine;

namespace Project2048.Stage
{
    [CreateAssetMenu(menuName = "Game/Stage/Stage")]
    public class StageSO : ScriptableObject
    {
        [SerializeField] private StageFloor floor = StageFloor.Upper;
        [SerializeField, Range(1, StageDatabaseSO.StagesPerFloor)] private int stageNumberInFloor = 1;
        [SerializeField] private Sprite presentationBackgroundSprite;
        [SerializeField] private List<EnemySO> enemyCandidates = new();
        [SerializeField, HideInInspector] private EnemySO enemyData;

        public StageFloor Floor => floor;
        public int StageNumberInFloor => stageNumberInFloor;
        public Sprite PresentationBackgroundSprite => presentationBackgroundSprite;
        public IReadOnlyList<EnemySO> EnemyCandidates => enemyCandidates;

        public bool TrySelectEnemy(out EnemySO enemy)
        {
            enemy = null;

            if (enemyCandidates == null || enemyCandidates.Count == 0)
            {
                return false;
            }

            var validCandidateCount = 0;
            for (var index = 0; index < enemyCandidates.Count; index++)
            {
                if (enemyCandidates[index] != null)
                {
                    validCandidateCount++;
                }
            }

            if (validCandidateCount == 0)
            {
                return false;
            }

            var selectedCandidateIndex = Random.Range(0, validCandidateCount);
            for (var index = 0; index < enemyCandidates.Count; index++)
            {
                var candidate = enemyCandidates[index];
                if (candidate == null)
                {
                    continue;
                }

                if (selectedCandidateIndex == 0)
                {
                    enemy = candidate;
                    return true;
                }

                selectedCandidateIndex--;
            }

            return false;
        }

        private void OnValidate()
        {
            stageNumberInFloor = Mathf.Clamp(stageNumberInFloor, 1, StageDatabaseSO.StagesPerFloor);

            enemyCandidates ??= new List<EnemySO>();
            if (enemyData != null && !enemyCandidates.Contains(enemyData))
            {
                enemyCandidates.Add(enemyData);
            }

            enemyData = null;
        }
    }
}
