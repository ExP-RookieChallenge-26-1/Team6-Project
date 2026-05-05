using System.Collections.Generic;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Rewards;
using Project2048.Score;
using UnityEngine;

namespace Project2048.Prototype
{
    public class PrototypeCombatBootstrap : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private PlayerCombatController playerController;
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private CombatUiView combatUiView;
        [SerializeField] private CombatWorldSpriteView combatWorldSpriteView;
        [SerializeField] private RewardManager rewardManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private PlayerSO playerData;
        [SerializeField] private EnemySO enemyData;
        [SerializeField] private RewardTableSO rewardTable;
        [SerializeField] private List<EnemySO> enemyPool = new();
        [SerializeField] private bool randomizeEnemyOnStart = true;
        [SerializeField] private int boardMoveCount = 4;
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private float enemyTurnDelaySeconds = 1.2f;
        [SerializeField] private RunProgress runProgress = new();

        private PrototypeCombatLoadout runtimeLoadout;
        private EnemySO runtimeRandomEnemy;

        public CombatManager CombatManager => combatManager;
        public RewardManager RewardManager => rewardManager;
        public ScoreManager ScoreManager => scoreManager;
        public RunProgress RunProgress => runProgress;
        public PlayerSO PlayerData => playerData;
        public EnemySO EnemyData => enemyData;
        public IReadOnlyList<EnemySO> EnemyPool => enemyPool;
        public bool RandomizeEnemyOnStart => randomizeEnemyOnStart;

        private void Awake()
        {
            EnsureRuntimeObjects();
        }

        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartPrototypeCombat();
            }
        }

        private void OnDestroy()
        {
            runtimeLoadout?.Dispose();
            runtimeLoadout = null;
            DestroyRuntimeRandomEnemy();
        }

        public void RestartCombat()
        {
            if (combatManager != null && combatManager.CurrentPhase == CombatPhase.Defeat)
            {
                runProgress.Reset();
                scoreManager?.ResetScore();
            }

            StartPrototypeCombat();
        }

        public void StartPrototypeCombat()
        {
            EnsureRuntimeObjects();

            runtimeLoadout?.Dispose();
            runtimeLoadout = null;
            DestroyRuntimeRandomEnemy();

            var setupPlayerData = playerData;
            var setupEnemyData = enemyData;
            if (setupPlayerData == null || setupEnemyData == null)
            {
                runtimeLoadout = PrototypeCombatFactory.CreateDefaultLoadout();
                setupPlayerData = runtimeLoadout.PlayerData;
                setupEnemyData = runtimeLoadout.EnemyData;
            }
            else
            {
                setupEnemyData = SelectEnemyData(setupEnemyData);
            }

            combatManager.SetCombatants(playerController, new[] { enemyController });
            combatManager.EnemyTurnDelaySeconds = enemyTurnDelaySeconds;
            combatManager.StartCombat(new CombatSetup
            {
                playerData = setupPlayerData,
                enemyDataList = new List<EnemySO> { setupEnemyData },
                boardMoveCount = boardMoveCount,
                runProgress = runProgress,
            });
        }

        private EnemySO SelectEnemyData(EnemySO fallback)
        {
            if (!randomizeEnemyOnStart)
            {
                return fallback;
            }

            var pooledEnemy = SelectPooledEnemy();
            if (pooledEnemy != null)
            {
                return pooledEnemy;
            }

            runtimeRandomEnemy = PrototypeCombatFactory.CreateRandomPrototypeEnemy();
            if (fallback != null)
            {
                runtimeRandomEnemy.portrait = fallback.portrait;
            }

            return runtimeRandomEnemy;
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
                : validEnemies[Random.Range(0, validEnemies.Count)];
        }

        private void DestroyRuntimeRandomEnemy()
        {
            if (runtimeRandomEnemy == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeRandomEnemy);
            }
            else
            {
                DestroyImmediate(runtimeRandomEnemy);
            }

            runtimeRandomEnemy = null;
        }

        private void EnsureRuntimeObjects()
        {
            if (combatManager == null)
            {
                var managerObject = new GameObject("CombatManager");
                managerObject.transform.SetParent(transform, false);
                combatManager = managerObject.AddComponent<CombatManager>();
            }

            if (playerController == null)
            {
                var playerObject = new GameObject("Player");
                playerObject.transform.SetParent(transform, false);
                playerController = playerObject.AddComponent<PlayerCombatController>();
            }

            if (enemyController == null)
            {
                var enemyObject = new GameObject("Enemy");
                enemyObject.transform.SetParent(transform, false);
                enemyController = enemyObject.AddComponent<EnemyController>();
            }

            if (rewardManager == null)
            {
                rewardManager = GetComponentInChildren<RewardManager>(true);
            }

            if (rewardManager != null)
            {
                rewardManager.Initialize(runProgress, rewardTable);
                rewardManager.BindCombat(combatManager);
            }

            if (scoreManager == null)
            {
                scoreManager = GetComponentInChildren<ScoreManager>(true);
            }

            if (scoreManager != null)
            {
                scoreManager.BindCombat(combatManager);
            }

            if (combatUiView == null)
            {
#if UNITY_2023_1_OR_NEWER
                combatUiView = Object.FindAnyObjectByType<CombatUiView>(FindObjectsInactive.Include);
#else
                combatUiView = Object.FindObjectOfType<CombatUiView>(true);
#endif
            }

            if (combatUiView != null)
            {
                combatUiView.Initialize(this);
            }
            else
            {
                Debug.LogWarning("CombatUiView is not present in the scene; the combat will run without UI.");
            }

            if (combatWorldSpriteView == null)
            {
#if UNITY_2023_1_OR_NEWER
                combatWorldSpriteView = Object.FindAnyObjectByType<CombatWorldSpriteView>(FindObjectsInactive.Include);
#else
                combatWorldSpriteView = Object.FindObjectOfType<CombatWorldSpriteView>(true);
#endif
            }

            if (combatWorldSpriteView != null)
            {
                combatWorldSpriteView.Initialize(this);
            }
        }
    }
}
