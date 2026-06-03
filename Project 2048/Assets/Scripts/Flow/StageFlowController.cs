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
        [SerializeField] private EnemySO finalBossData;
        [SerializeField] private RewardTableSO rewardTable;
        [SerializeField] private List<EnemySO> enemyPool = new();
        [SerializeField] private List<EnemySO> eliteEnemyPool = new();
        [SerializeField] private List<EnemySO> bossEnemyPool = new();
        [SerializeField] private bool randomizeEnemyOnStart = true;
        [SerializeField] private int boardMoveCount = 4;
        [SerializeField] private int finalStageIndex = 20;
        [SerializeField] private int eliteInterval = 5;
        [SerializeField] private float enemyTurnDelaySeconds = 1.2f;
        [SerializeField] private RunProgress runProgress = new();

        private int currentStageIndex = 1;
        private StageEncounterType currentEncounterType = StageEncounterType.Normal;
        private CombatResult lastCombatResult;
        private EnemySO runtimeEncounterEnemy;

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

        private void OnDestroy()
        {
            DestroyRuntimeEncounterEnemy();
        }

        public void StartStage(int stageIndex)
        {
            ResolveSceneReferences();

            if (!CanStartStage())
            {
                return;
            }

            currentStageIndex = Mathf.Max(1, stageIndex);
            currentEncounterType = ResolveEncounterType(currentStageIndex);
            lastCombatResult = null;
            DestroyRuntimeEncounterEnemy();

            ChangeState(StageFlowState.Preparing);
            OnStageFlowStarted?.Invoke(currentStageIndex);

            rewardManager.Initialize(RunProgress, rewardTable);

            var selectedEnemyData = PrepareEnemyForEncounter(
                SelectEnemyData(enemyData, currentEncounterType),
                currentEncounterType);
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
                currentEncounterType,
                currentStageIndex >= Mathf.Max(1, finalStageIndex),
                lastCombatResult,
                rewardResult));
        }

        private void FailStage()
        {
            ChangeState(StageFlowState.Failed);
            rewardManager.ClearReward(combatManager.Player);
            OnStageFailed?.Invoke();
        }

        private EnemySO SelectEnemyData(EnemySO fallback, StageEncounterType encounterType)
        {
            if (encounterType == StageEncounterType.FinalBoss && finalBossData != null)
            {
                return finalBossData;
            }

            if (!randomizeEnemyOnStart)
            {
                return fallback;
            }

            var pooledEnemy = SelectPooledEnemy(encounterType);
            return pooledEnemy != null ? pooledEnemy : fallback;
        }

        private EnemySO SelectPooledEnemy(StageEncounterType encounterType)
        {
            var pool = encounterType switch
            {
                StageEncounterType.FinalBoss => bossEnemyPool,
                StageEncounterType.Boss => bossEnemyPool,
                StageEncounterType.Elite => eliteEnemyPool,
                _ => enemyPool,
            };

            if ((pool == null || pool.Count == 0) && encounterType != StageEncounterType.Normal)
            {
                pool = enemyPool;
            }

            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            var validEnemies = new List<EnemySO>();
            foreach (var enemy in pool)
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

        private EnemySO PrepareEnemyForEncounter(EnemySO source, StageEncounterType encounterType)
        {
            if (source == null)
            {
                return null;
            }

            var desiredRank = ToEnemyRank(encounterType);
            if (source.encounterRank == desiredRank)
            {
                return source;
            }

            runtimeEncounterEnemy = Instantiate(source);
            runtimeEncounterEnemy.hideFlags = HideFlags.DontSave;
            runtimeEncounterEnemy.encounterRank = desiredRank;
            if (desiredRank != EnemyEncounterRank.Normal)
            {
                runtimeEncounterEnemy.aiStrength = EnemyAiStrength.Enhanced;
                runtimeEncounterEnemy.aiComplexity = EnemyAiComplexity.Normal;
                runtimeEncounterEnemy.actionsPerTurn = EnemySO.ResolveDefaultActionsPerTurn(runtimeEncounterEnemy.aiComplexity);
            }

            if (desiredRank == EnemyEncounterRank.FinalBoss)
            {
                runtimeEncounterEnemy.maxHp = Mathf.CeilToInt(runtimeEncounterEnemy.maxHp * 1.5f);
                runtimeEncounterEnemy.attackPower = Mathf.CeilToInt(runtimeEncounterEnemy.attackPower * 1.25f);
                runtimeEncounterEnemy.aiComplexity = EnemyAiComplexity.Complex;
                runtimeEncounterEnemy.actionsPerTurn = EnemySO.ResolveDefaultActionsPerTurn(runtimeEncounterEnemy.aiComplexity);
            }

            return runtimeEncounterEnemy;
        }

        public StageEncounterType ResolveEncounterType(int stageIndex)
        {
            var finalStage = Mathf.Max(1, finalStageIndex);
            if (stageIndex >= finalStage)
            {
                return StageEncounterType.FinalBoss;
            }

            var interval = Mathf.Max(1, eliteInterval);
            return stageIndex % interval == 0
                ? StageEncounterType.Elite
                : StageEncounterType.Normal;
        }

        private static EnemyEncounterRank ToEnemyRank(StageEncounterType encounterType)
        {
            return encounterType switch
            {
                StageEncounterType.FinalBoss => EnemyEncounterRank.FinalBoss,
                StageEncounterType.Boss => EnemyEncounterRank.Boss,
                StageEncounterType.Elite => EnemyEncounterRank.Elite,
                _ => EnemyEncounterRank.Normal,
            };
        }

        private void DestroyRuntimeEncounterEnemy()
        {
            if (runtimeEncounterEnemy == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeEncounterEnemy);
            }
            else
            {
                DestroyImmediate(runtimeEncounterEnemy);
            }

            runtimeEncounterEnemy = null;
        }

        private void ChangeState(StageFlowState nextState)
        {
            CurrentState = nextState;
        }
    }
}
