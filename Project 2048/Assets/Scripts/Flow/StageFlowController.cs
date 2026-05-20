using System;
using System.Collections.Generic;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Rewards;
using UnityEngine;

namespace Project2048.Flow
{
    public class StageFlowController : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private PlayerCombatController playerController;
        [SerializeField] private List<EnemyController> enemyControllers = new();
        [SerializeField] private RewardManager rewardManager;

        [Header("Prototype Stage Data")]
        [SerializeField] private PlayerSO playerData;
        [SerializeField] private EnemySO enemyData;
        [SerializeField] private RewardTableSO rewardTable;
        [SerializeField] private List<EnemySO> enemyPool = new();
        [SerializeField] private bool randomizeEnemyOnStart = true;
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

            if (!CanStartStage())
            {
                return;
            }

            currentStageIndex = Mathf.Max(1, stageIndex);
            lastCombatResult = null;

            ChangeState(StageFlowState.Preparing);
            OnStageFlowStarted?.Invoke(currentStageIndex);

            rewardManager.Initialize(RunProgress, rewardTable);

            var selectedEnemyData = SelectEnemyData(enemyData);
            var combatEnemies = new List<EnemySO> { selectedEnemyData };

            combatManager.SetCombatants(playerController, enemyControllers);
            combatManager.EnemyTurnDelaySeconds = enemyTurnDelaySeconds;

            ChangeState(StageFlowState.Combat);
            OnCombatStarted?.Invoke();

            combatManager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = combatEnemies,
                boardMoveCount = boardMoveCount,
                runProgress = RunProgress,
            });

            Debug.Log("StageContoller StartCombat");
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
        }

        private bool CanStartStage()
        {
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

            if (enemyData == null && (enemyPool == null || enemyPool.Count == 0))
            {
                Debug.LogError("EnemySO or enemy pool is required.");
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
            lastCombatResult = combatResult;
            ChangeState(StageFlowState.Reward);
            OnRewardStarted?.Invoke(combatResult);
            rewardManager.OfferReward(combatResult, combatManager.Player);
        }

        private void HandleCombatDefeat()
        {
            ChangeState(StageFlowState.Failed);
            rewardManager.ClearReward(combatManager.Player);
            OnStageFailed?.Invoke();
        }

        private void HandleRewardClaimed(RewardChoiceResult rewardResult)
        {
            ChangeState(StageFlowState.Completed);

            OnStageCompleted?.Invoke(new StageResult(
                currentStageIndex,
                lastCombatResult,
                rewardResult));

            Debug.Log("StageController EndStage");
        }

        private EnemySO SelectEnemyData(EnemySO fallback)
        {
            if (!randomizeEnemyOnStart)
            {
                return fallback;
            }

            var pooledEnemy = SelectPooledEnemy();
            return pooledEnemy != null ? pooledEnemy : fallback;
        }

        private EnemySO SelectPooledEnemy()
        {
            if (enemyPool == null || enemyPool.Count == 0)
            {
                return null;
            }

            var validEnemies = new List<EnemySO>();
            foreach (var enemy in enemyPool)
            {
                if (enemy != null)
                {
                    validEnemies.Add(enemy);
                }
            }

            return validEnemies.Count == 0
                ? null
                : validEnemies[UnityEngine.Random.Range(0, validEnemies.Count)];
        }

        private void ChangeState(StageFlowState nextState)
        {
            CurrentState = nextState;
        }
    }
}
