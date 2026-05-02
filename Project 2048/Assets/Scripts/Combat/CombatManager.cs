using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project2048.Board2048;
using Project2048.Cost;
using Project2048.Enemy;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Combat
{
    /// <summary>
    /// 전투의 중심 진입점이다. UI는 내부 컨트롤러를 직접 만지지 않고
    /// 이 클래스의 command 메서드와 snapshot 이벤트만 바라보면 된다.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        [SerializeField] private PlayerCombatController player;
        [SerializeField] private List<EnemyController> enemies = new();
        [SerializeField] private float enemyTurnDelaySeconds;
        public float EnemyTurnDelaySeconds
        {
            get => enemyTurnDelaySeconds;
            set => enemyTurnDelaySeconds = Mathf.Max(0f, value);
        }

        private CombatSetup currentSetup;
        private SkillExecutor skillExecutor;
        private EnemyIntentSystem enemyIntentSystem;
        private CostConverter costConverter;
        private DamageCalculator damageCalculator;
        private bool boardEventsBound;
        private bool costEventsBound;
        private bool playerEventsBound;
        private bool suppressStateNotifications;
        // UI에 보여줄 최근 행동 문구다. 실제 전투 규칙은 CombatManager 안에 남기고 UI는 문자열만 렌더한다.
        private string lastActionDescription = "대기";
        private CombatVfxCue lastVfxCue;
        private int vfxCueSequence;

        public CombatPhase CurrentPhase { get; private set; } = CombatPhase.None;
        public TurnController TurnController { get; } = new();
        public Board2048Manager BoardManager { get; private set; } = new();
        public ActionCostWallet CostWallet { get; } = new();
        public IReadOnlyList<EnemyController> Enemies => enemies;
        public PlayerCombatController Player => player;

        public event Action<CombatPhase> OnPhaseChanged;
        public event Action<CombatResult> OnCombatVictory;
        public event Action OnCombatDefeat;
        public event Action<int> OnCostChanged;
        public event Action<CombatSnapshot> OnCombatStateChanged;

        private void Awake()
        {
            EnsureRuntimeState();
        }

        private void OnDestroy()
        {
            if (boardEventsBound && BoardManager != null)
            {
                BoardManager.OnBoardFinished -= HandleBoardFinished;
                BoardManager.OnBoardChanged -= HandleBoardChanged;
                BoardManager.OnMoveCountChanged -= HandleBoardMoveCountChanged;
                boardEventsBound = false;
            }

            if (costEventsBound)
            {
                CostWallet.OnCostChanged -= HandleCostChanged;
                costEventsBound = false;
            }

            UnbindEntityEvents();
        }

        public void SetCombatants(PlayerCombatController playerController, IEnumerable<EnemyController> enemyControllers)
        {
            player = playerController;
            enemies = enemyControllers?.Where(enemy => enemy != null).ToList() ?? new List<EnemyController>();
        }

        public void StartCombat(CombatSetup setup)
        {
            EnsureRuntimeState();
            currentSetup = setup ?? throw new ArgumentNullException(nameof(setup));
            if (player == null)
            {
                throw new InvalidOperationException("CombatManager requires a player combat controller.");
            }

            if (currentSetup.enemyDataList == null || currentSetup.enemyDataList.Count == 0)
            {
                throw new InvalidOperationException("Combat setup requires at least one enemy definition.");
            }

            if (enemies.Count < currentSetup.enemyDataList.Count)
            {
                throw new InvalidOperationException("CombatManager needs at least as many enemy controllers as enemy definitions.");
            }

            // 시작 중에는 phase, HP, intent가 연달아 바뀐다. 중간 snapshot을 UI에 여러 번 보내지 않고
            // 모든 초기화가 끝난 뒤 완성된 snapshot 하나만 발행한다.
            suppressStateNotifications = true;
            lastActionDescription = "2048 진행";
            lastVfxCue = null;
            vfxCueSequence = 0;
            TurnController.Reset();
            CostWallet.Clear();
            UnbindEntityEvents();

            player.Init(currentSetup.playerData);
            BindPlayerEvents();

            for (var index = 0; index < currentSetup.enemyDataList.Count; index++)
            {
                enemies[index].Init(currentSetup.enemyDataList[index]);
                BindEnemyEvents(enemies[index]);
            }

            ChangePhase(CombatPhase.CombatStart);
            PrepareEnemyIntents();
            StartPlayerTurn();
            suppressStateNotifications = false;
            NotifyStateChanged();
        }

        public CombatSnapshot GetSnapshot()
        {
            return new CombatSnapshot
            {
                Phase = CurrentPhase,
                CurrentCost = CostWallet.CurrentCost,
                RemainingBoardMoves = BoardManager.MoveCount,
                LastActionDescription = lastActionDescription,
                LastVfxCue = lastVfxCue?.Clone(),
                Board = BoardManager.GetBoardSnapshot(),
                Player = BuildPlayerSnapshot(),
                Enemies = BuildEnemySnapshots(),
                Skills = BuildSkillSnapshots(),
            };
        }

        public int ResolveBoardPhase()
        {
            EnsureRuntimeState();
            if (CurrentPhase != CombatPhase.BoardPhase)
            {
                return CostWallet.CurrentCost;
            }

            var cost = costConverter.ConvertBoardToCost(BoardManager.GetBoardSnapshot());
            lastActionDescription = $"코스트 획득: {cost}";
            CostWallet.SetCost(cost);
            ChangePhase(CombatPhase.ActionPhase);
            return cost;
        }

        public bool RequestUseSkill(SkillSO skill, EnemyController target = null)
        {
            EnsureRuntimeState();
            if (CurrentPhase != CombatPhase.ActionPhase || skill == null)
            {
                return false;
            }

            if (skill.skillType == SkillType.Attack && (target == null || target.IsDead))
            {
                return false;
            }

            if (!CostWallet.CanSpend(skill.cost))
            {
                return false;
            }

            lastActionDescription = $"플레이어: {GetSkillDisplayName(skill)}";
            CostWallet.Spend(skill.cost);
            skillExecutor.Execute(skill, player, target, damageCalculator);
            CheckVictory();
            return true;
        }

        public bool RequestUseSkillById(string skillId, int targetIndex = -1)
        {
            EnsureRuntimeState();
            if (player == null || string.IsNullOrWhiteSpace(skillId))
            {
                return false;
            }

            // 외부 UI는 ScriptableObject 참조를 몰라도 된다. 버튼은 skillId와 targetIndex만 넘기면 된다.
            var skill = player.Skills.FirstOrDefault(candidate => candidate != null && candidate.skillId == skillId);
            if (skill == null)
            {
                return false;
            }

            EnemyController target = null;
            if (skill.skillType == SkillType.Attack)
            {
                if (targetIndex < 0 || targetIndex >= enemies.Count)
                {
                    return false;
                }

                target = enemies[targetIndex];
            }

            return RequestUseSkill(skill, target);
        }

        public bool RequestBoardMove(Direction direction)
        {
            EnsureRuntimeState();
            if (CurrentPhase != CombatPhase.BoardPhase)
            {
                return false;
            }

            return BoardManager.Move(direction);
        }

        public void RequestEndPlayerTurn()
        {
            EnsureRuntimeState();
            if (CurrentPhase != CombatPhase.ActionPhase)
            {
                return;
            }

            // 플레이 화면에서는 적 턴 패널을 잠깐 보여주고, EditMode 테스트처럼 비활성 상태에서는 즉시 해결한다.
            if (enemyTurnDelaySeconds > 0f && isActiveAndEnabled)
            {
                lastActionDescription = "적 턴 시작";
                BeginEnemyTurn();
                StartCoroutine(DelayedResolveEnemyTurn());
            }
            else
            {
                StartEnemyTurn();
            }
        }

        private void StartPlayerTurn()
        {
            // 방어도는 턴마다 사라지고, 방어 보너스/디버프는 별도 값으로 유지된다.
            player.ClearBlock();
            TurnController.StartPlayerTurn();
            ChangePhase(CombatPhase.PlayerTurnStart);
            ChangePhase(CombatPhase.BoardPhase);

            // 이번 턴에 2048을 움직일 수 있는 횟수다. 0이 되면 보드 전체가 코스트로 바뀐다.
            var moveCount = Mathf.Max(0, currentSetup.boardMoveCount + player.BoardMoveCountBonus);
            BoardManager.InitBoard(moveCount);
        }

        private void StartEnemyTurn()
        {
            BeginEnemyTurn();
            ResolveEnemyTurn();
        }

        private void BeginEnemyTurn()
        {
            TurnController.StartEnemyTurn();
            ChangePhase(CombatPhase.EnemyTurn);
        }

        private void ResolveEnemyTurn()
        {
            foreach (var enemy in enemies.Where(enemy => enemy != null && !enemy.IsDead))
            {
                if (ResolveEnemyAction(enemy))
                {
                    return;
                }
            }

            PrepareEnemyIntents();
            if (!CheckVictory())
            {
                StartPlayerTurn();
            }
        }

        private void ExecuteEnemyIntent(EnemyController enemy, EnemyIntent intent)
        {
            var wasSuppressing = suppressStateNotifications;
            suppressStateNotifications = true;
            enemyIntentSystem.ExecuteIntent(enemy, player, damageCalculator, BoardManager);
            suppressStateNotifications = wasSuppressing;

            if (intent != null &&
                intent.intentType == EnemyIntentType.Debuff &&
                intent.debuffType != DebuffType.None &&
                intent.value > 0)
            {
                lastVfxCue = new CombatVfxCue
                {
                    Sequence = ++vfxCueSequence,
                    DebuffType = intent.debuffType,
                    Value = intent.value,
                    SourceName = GetEnemyDisplayName(enemy),
                    TargetName = "플레이어",
                };
            }

            NotifyStateChanged();
        }

        private IEnumerator DelayedResolveEnemyTurn()
        {
            yield return new WaitForSeconds(enemyTurnDelaySeconds);
            foreach (var enemy in enemies.Where(enemy => enemy != null && !enemy.IsDead))
            {
                if (ResolveEnemyAction(enemy))
                {
                    yield break;
                }

                if (enemyTurnDelaySeconds > 0f)
                {
                    yield return new WaitForSeconds(enemyTurnDelaySeconds);
                }
            }

            PrepareEnemyIntents();
            if (!CheckVictory())
            {
                StartPlayerTurn();
            }
        }

        private bool ResolveEnemyAction(EnemyController enemy)
        {
            var intent = enemy.CurrentIntent?.Clone();
            lastActionDescription = $"{GetEnemyDisplayName(enemy)}: {FormatEnemyIntent(intent)}";
            ExecuteEnemyIntent(enemy, intent);
            return CheckDefeat();
        }

        private void PrepareEnemyIntents()
        {
            foreach (var enemy in enemies.Where(enemy => enemy != null && !enemy.IsDead))
            {
                enemyIntentSystem.SetNextIntent(enemy);
            }
        }

        private bool CheckVictory()
        {
            if (enemies.Any(enemy => enemy != null && !enemy.IsDead))
            {
                return false;
            }

            ChangePhase(CombatPhase.Victory);
            OnCombatVictory?.Invoke(BuildCombatResult());
            return true;
        }

        private bool CheckDefeat()
        {
            if (player != null && !player.IsDead)
            {
                return false;
            }

            ChangePhase(CombatPhase.Defeat);
            OnCombatDefeat?.Invoke();
            return true;
        }

        private CombatResult BuildCombatResult()
        {
            return new CombatResult
            {
                turnCount = TurnController.TurnCount,
                remainingMoveCount = BoardManager.MoveCount,
                overCost = CostWallet.CurrentCost,
                enemyDifficultyScore = enemies
                    .Where(enemy => enemy != null && enemy.Data != null)
                    .Sum(enemy => enemy.Data.difficultyScore),
            };
        }

        private void ChangePhase(CombatPhase nextPhase)
        {
            CurrentPhase = nextPhase;
            OnPhaseChanged?.Invoke(nextPhase);
            NotifyStateChanged();
        }

        private void HandleCostChanged(int currentCost)
        {
            OnCostChanged?.Invoke(currentCost);
            NotifyStateChanged();
        }

        private void HandleBoardFinished()
        {
            ResolveBoardPhase();
        }

        private void EnsureRuntimeState()
        {
            // MonoBehaviour 생명주기와 EditMode 테스트 양쪽에서 안전하게 부를 수 있게, 런타임 의존성과 이벤트 연결을
            // 한 번만 준비한다.
            skillExecutor ??= new SkillExecutor();
            enemyIntentSystem ??= new EnemyIntentSystem();
            costConverter ??= new CostConverter();
            damageCalculator ??= new DamageCalculator();
            BoardManager ??= new Board2048Manager();

            if (!boardEventsBound)
            {
                BoardManager.OnBoardFinished += HandleBoardFinished;
                BoardManager.OnBoardChanged += HandleBoardChanged;
                BoardManager.OnMoveCountChanged += HandleBoardMoveCountChanged;
                boardEventsBound = true;
            }

            if (!costEventsBound)
            {
                CostWallet.OnCostChanged += HandleCostChanged;
                costEventsBound = true;
            }
        }

        private void HandleBoardChanged(int[,] _)
        {
            NotifyStateChanged();
        }

        private void HandleBoardMoveCountChanged(int _)
        {
            NotifyStateChanged();
        }

        private void BindPlayerEvents()
        {
            if (player == null || playerEventsBound)
            {
                return;
            }

            player.OnHpChanged += HandlePlayerHpChanged;
            player.OnBlockChanged += HandlePlayerBlockChanged;
            player.OnDefenseBonusChanged += HandlePlayerDefenseBonusChanged;
            playerEventsBound = true;
        }

        private void BindEnemyEvents(EnemyController enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.OnHpChanged -= HandleEnemyHpChanged;
            enemy.OnIntentChanged -= HandleEnemyIntentChanged;
            enemy.OnHpChanged += HandleEnemyHpChanged;
            enemy.OnIntentChanged += HandleEnemyIntentChanged;
        }

        private void UnbindEntityEvents()
        {
            if (player != null && playerEventsBound)
            {
                player.OnHpChanged -= HandlePlayerHpChanged;
                player.OnBlockChanged -= HandlePlayerBlockChanged;
                player.OnDefenseBonusChanged -= HandlePlayerDefenseBonusChanged;
                playerEventsBound = false;
            }

            foreach (var enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                enemy.OnHpChanged -= HandleEnemyHpChanged;
                enemy.OnIntentChanged -= HandleEnemyIntentChanged;
            }
        }

        private void HandlePlayerHpChanged(int _, int __)
        {
            NotifyStateChanged();
        }

        private void HandlePlayerBlockChanged(int _)
        {
            NotifyStateChanged();
        }

        private void HandlePlayerDefenseBonusChanged(int _)
        {
            NotifyStateChanged();
        }

        private void HandleEnemyHpChanged(int _, int __)
        {
            NotifyStateChanged();
        }

        private void HandleEnemyIntentChanged(EnemyIntent _)
        {
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            if (suppressStateNotifications)
            {
                return;
            }

            OnCombatStateChanged?.Invoke(GetSnapshot());
        }

        private PlayerCombatSnapshot BuildPlayerSnapshot()
        {
            if (player == null)
            {
                return null;
            }

            return new PlayerCombatSnapshot
            {
                CurrentHp = player.CurrentHp,
                MaxHp = player.MaxHp,
                AttackPower = player.AttackPower,
                Block = player.Block,
                DefenseBonus = player.DefenseBonus,
            };
        }

        private List<EnemyCombatSnapshot> BuildEnemySnapshots()
        {
            var snapshots = new List<EnemyCombatSnapshot>(enemies.Count);

            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy == null)
                {
                    continue;
                }

                snapshots.Add(new EnemyCombatSnapshot
                {
                    EnemyIndex = index,
                    DisplayName = enemy.Data != null && !string.IsNullOrWhiteSpace(enemy.Data.enemyName)
                        ? enemy.Data.enemyName
                        : enemy.name,
                    CurrentHp = enemy.CurrentHp,
                    MaxHp = enemy.MaxHp,
                    Block = enemy.Block,
                    IsDead = enemy.IsDead,
                    AiProfileLabel = enemy.Data != null ? enemy.Data.GetAiProfileLabel() : string.Empty,
                    Intent = enemy.CurrentIntent?.Clone(),
                });
            }

            return snapshots;
        }

        private List<SkillSnapshot> BuildSkillSnapshots()
        {
            var snapshots = new List<SkillSnapshot>();
            if (player == null)
            {
                return snapshots;
            }

            foreach (var skill in player.Skills)
            {
                if (skill == null)
                {
                    continue;
                }

                snapshots.Add(new SkillSnapshot
                {
                    SkillId = skill.skillId,
                    DisplayName = string.IsNullOrWhiteSpace(skill.skillName) ? skill.skillId : skill.skillName,
                    SkillType = skill.skillType,
                    Cost = skill.cost,
                    Power = skill.power,
                });
            }

            return snapshots;
        }

        private static string GetSkillDisplayName(SkillSO skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(skill.skillName) ? skill.skillId : skill.skillName;
        }

        private static string GetEnemyDisplayName(EnemyController enemy)
        {
            if (enemy == null)
            {
                return string.Empty;
            }

            return enemy.Data != null && !string.IsNullOrWhiteSpace(enemy.Data.enemyName)
                ? enemy.Data.enemyName
                : enemy.name;
        }

        private static string FormatEnemyIntent(EnemyIntent intent)
        {
            if (intent == null)
            {
                return "행동 없음";
            }

            return intent.intentType switch
            {
                EnemyIntentType.Defense => "방어",
                EnemyIntentType.Attack => "공격",
                EnemyIntentType.Debuff => intent.debuffType switch
                {
                    DebuffType.Darkness => "암흑",
                    DebuffType.Fear => "공포",
                    _ => "디버프",
                },
                _ => intent.intentType.ToString(),
            };
        }
    }
}
