using System;
using System.Collections.Generic;
using System.Linq;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Prototype;
using Project2048.Rewards;
using Project2048.Skills;
using Project2048.Stage;
using UnityEngine;

namespace Project2048.Flow
{
    public class StageFlowController : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private PlayerCombatController playerController;
        [SerializeField] private List<EnemyController> enemyControllers = new();
        [SerializeField] private RewardManager rewardManager;
        [SerializeField] private CombatWorldSpriteView combatWorldSpriteView;

        [Header("Stage Data")]
        [SerializeField] private StageDatabaseSO stageDatabase;
        [SerializeField] private PlayerSO playerData;
        [SerializeField] private RewardTableSO rewardTable;
        [SerializeField] private int boardMoveCount = 4;
        [SerializeField] private float enemyTurnDelaySeconds = 1.2f;
        [SerializeField] private RunProgress runProgress = new();

        private int currentStageIndex = 1;
        private CombatResult lastCombatResult;

        public StageFlowState CurrentState { get; private set; } = StageFlowState.None;
        public RunProgress RunProgress => runProgress ??= new RunProgress();

        public event Action<int> OnStageFlowStarted;
        public event Action OnCombatStarted;
        public event Action<CombatResult> OnRewardStarted;
        public event Action<StageResult> OnStageCompleted;
        public event Action OnStageFailed;

        private void Awake()
        {
            ResolveSceneReferences();
        }

        private void OnEnable()
        {
            BindEvents();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        public void StartStage(int stageIndex)
        {
            ResolveSceneReferences();

            currentStageIndex = Mathf.Max(1, stageIndex);
            if (!CanStartStage(out var stage, out var stageEnemyData))
            {
                return;
            }

            lastCombatResult = null;

            ChangeState(StageFlowState.Preparing);
            OnStageFlowStarted?.Invoke(currentStageIndex);

            rewardManager.Initialize(RunProgress, rewardTable);
            combatWorldSpriteView?.SetStage(stage);

            combatManager.SetCombatants(playerController, enemyControllers);
            combatManager.EnemyTurnDelaySeconds = enemyTurnDelaySeconds;

            ChangeState(StageFlowState.Combat);
            OnCombatStarted?.Invoke();

            combatManager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { stageEnemyData },
                boardMoveCount = boardMoveCount,
                runProgress = RunProgress,
            });
        }

        public void ResetRunProgress()
        {
            RunProgress.Reset();
            InitializeRunProgress();
        }

        public void InitializeRunProgress()
        {
            ResolveSceneReferences();
            if (rewardManager != null)
            {
                rewardManager.Initialize(RunProgress, rewardTable);
            }
        }

        public IEnumerable<SkillSO> GetKnownSkillsForSaveRestore()
        {
            var knownSkills = new List<SkillSO>();
            AddKnownSkills(knownSkills, playerData != null ? playerData.startingSkills : null);

            if (rewardTable != null && rewardTable.rewards != null)
            {
                AddKnownSkills(
                    knownSkills,
                    rewardTable.rewards
                        .Where(reward => reward != null)
                        .Select(reward => reward.skillToLearn));
            }

            return knownSkills;
        }

        private void ResolveSceneReferences()
        {
            if (combatManager == null)
            {
                combatManager = GetComponentInChildren<CombatManager>(true);
            }

            if (playerController == null)
            {
                playerController = GetComponentInChildren<PlayerCombatController>(true);
            }

            if ((enemyControllers == null || enemyControllers.Count == 0))
            {
                enemyControllers = new List<EnemyController>(GetComponentsInChildren<EnemyController>(true));
            }

            if (rewardManager == null)
            {
                rewardManager = GetComponentInChildren<RewardManager>(true);
            }

            if (combatWorldSpriteView == null)
            {
#if UNITY_2023_1_OR_NEWER
                combatWorldSpriteView = UnityEngine.Object.FindAnyObjectByType<CombatWorldSpriteView>(FindObjectsInactive.Include);
#else
                combatWorldSpriteView = UnityEngine.Object.FindObjectOfType<CombatWorldSpriteView>(true);
#endif
            }
        }

        private bool CanStartStage(out StageSO stage, out EnemySO stageEnemyData)
        {
            stage = null;
            stageEnemyData = null;
            if (combatManager == null)
            {
                Debug.LogError("CombatManager is not assigned.");
                return false;
            }

            if (playerController == null)
            {
                Debug.LogError("PlayerCombatController is not assigned.");
                return false;
            }

            if (enemyControllers == null || enemyControllers.Count == 0)
            {
                Debug.LogError("At least one EnemyController is required.");
                return false;
            }

            if (rewardManager == null)
            {
                Debug.LogError("RewardManager is not assigned.");
                return false;
            }

            if (playerData == null)
            {
                Debug.LogError("PlayerSO is not assigned.");
                return false;
            }

            if (stageDatabase == null)
            {
                Debug.LogError("StageDatabaseSO is not assigned.");
                return false;
            }

            if (!stageDatabase.TryGetStage(currentStageIndex, out stage))
            {
                Debug.LogError($"Stage {currentStageIndex} is not configured.");
                return false;
            }

            if (!stage.TrySelectEnemy(out stageEnemyData))
            {
                Debug.LogError($"Stage {currentStageIndex} has no valid EnemySO candidate.");
                return false;
            }

            return true;
        }

        private void BindEvents()
        {
            if (combatManager != null)
            {
                combatManager.OnCombatVictory -= HandleCombatVictory;
                combatManager.OnCombatDefeat -= HandleCombatDefeat;
                combatManager.OnCombatVictory += HandleCombatVictory;
                combatManager.OnCombatDefeat += HandleCombatDefeat;
            }

            if (rewardManager != null)
            {
                rewardManager.OnRewardClaimed -= HandleRewardClaimed;
                rewardManager.OnRewardClaimed += HandleRewardClaimed;
            }
        }

        private void UnbindEvents()
        {
            if (combatManager != null)
            {
                combatManager.OnCombatVictory -= HandleCombatVictory;
                combatManager.OnCombatDefeat -= HandleCombatDefeat;
            }

            if (rewardManager != null)
            {
                rewardManager.OnRewardClaimed -= HandleRewardClaimed;
            }
        }

        private void HandleCombatVictory(CombatResult combatResult)
        {
            BeginReward(combatResult);
        }

        private void HandleCombatDefeat()
        {
            FailStage();
        }

        private void HandleRewardClaimed(RewardChoiceResult rewardResult)
        {
            CompleteStage(rewardResult);
        }

        private void BeginReward(CombatResult combatResult)
        {
            lastCombatResult = combatResult;
            ChangeState(StageFlowState.Reward);
            OnRewardStarted?.Invoke(combatResult);
            rewardManager.OfferReward(combatResult, combatManager.Player);
        }

        private void CompleteStage(RewardChoiceResult rewardResult)
        {
            ChangeState(StageFlowState.Completed);

            OnStageCompleted?.Invoke(new StageResult(
                currentStageIndex,
                StageEncounterType.Normal,
                stageDatabase != null && stageDatabase.IsFinalStage(currentStageIndex),
                lastCombatResult,
                rewardResult));
        }

        private void FailStage()
        {
            ChangeState(StageFlowState.Failed);
            rewardManager.ClearReward(combatManager.Player);
            OnStageFailed?.Invoke();
        }

        private void ChangeState(StageFlowState nextState)
        {
            CurrentState = nextState;
        }

        private static void AddKnownSkills(List<SkillSO> target, IEnumerable<SkillSO> skills)
        {
            if (target == null || skills == null)
            {
                return;
            }

            foreach (var skill in skills)
            {
                if (skill == null || string.IsNullOrWhiteSpace(skill.skillId))
                {
                    continue;
                }

                if (target.Any(existing => existing != null && existing.skillId == skill.skillId))
                {
                    continue;
                }

                target.Add(skill);
            }
        }
    }
}
