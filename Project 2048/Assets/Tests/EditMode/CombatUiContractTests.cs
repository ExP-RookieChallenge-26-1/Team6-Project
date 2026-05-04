using System.Collections.Generic;
using NUnit.Framework;
using Project2048.Board2048;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Tests
{
    public class CombatUiContractTests
    {
        private readonly List<Object> ownedObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var ownedObject in ownedObjects)
            {
                if (ownedObject != null)
                {
                    Object.DestroyImmediate(ownedObject);
                }
            }

            ownedObjects.Clear();
        }

        [Test]
        public void GetSnapshot_AfterStartCombat_ContainsUiFacingState()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var attackSkill = CreateSkill("basic-attack", "Basic Attack", SkillType.Attack, 5, 3);
            var defenseSkill = CreateSkill("basic-defense", "Basic Defense", SkillType.Defense, 5, 4);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2, attackSkill, defenseSkill);
            var enemyData = CreateEnemyData("Slime", maxHp: 10, attackValue: 4);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 2,
            });

            var snapshot = manager.GetSnapshot();

            Assert.That(snapshot.Phase, Is.EqualTo(CombatPhase.BoardPhase));
            Assert.That(snapshot.Player.CurrentHp, Is.EqualTo(20));
            Assert.That(snapshot.Enemies.Count, Is.EqualTo(1));
            Assert.That(snapshot.Enemies[0].DisplayName, Is.EqualTo("Slime"));
            Assert.That(snapshot.Enemies[0].AiProfileLabel, Is.EqualTo("AI: 밸런스 / 공포->암흑 / 일반"));
            Assert.That(snapshot.Skills.Count, Is.EqualTo(2));
            Assert.That(snapshot.Board.GetLength(0), Is.EqualTo(4));
            Assert.That(snapshot.Board.GetLength(1), Is.EqualTo(4));
            Assert.That(snapshot.LastActionDescription, Is.EqualTo("2048 진행"));
        }

        [Test]
        public void RequestBoardMove_FinishingBoardPhase_TransitionsToActionPhaseWithCost()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData("Slime", maxHp: 10, attackValue: 4);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            manager.BoardManager.SetBoardState(
                new[,]
                {
                    { 0, 0, 0, 64 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                1);

            var moved = manager.RequestBoardMove(Direction.Left);
            var snapshot = manager.GetSnapshot();

            Assert.That(moved, Is.True);
            Assert.That(snapshot.Phase, Is.EqualTo(CombatPhase.ActionPhase));
            Assert.That(snapshot.CurrentCost, Is.InRange(14, 15));
            Assert.That(snapshot.RemainingBoardMoves, Is.EqualTo(0));
        }

        [Test]
        public void RequestUseSkillById_EmitsUpdatedSnapshot_WithoutUiHoldingRuntimeReferences()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var attackSkill = CreateSkill("basic-attack", "Basic Attack", SkillType.Attack, 5, 3);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2, attackSkill);
            var enemyData = CreateEnemyData("Slime", maxHp: 10, attackValue: 4);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            manager.BoardManager.SetBoardState(
                new[,]
                {
                    { 64, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                0);
            manager.ResolveBoardPhase();

            CombatSnapshot latestSnapshot = null;
            manager.OnCombatStateChanged += snapshot => latestSnapshot = snapshot;

            var used = manager.RequestUseSkillById("basic-attack", 0);

            Assert.That(used, Is.True);
            Assert.That(latestSnapshot, Is.Not.Null);
            Assert.That(latestSnapshot.CurrentCost, Is.EqualTo(8));
            Assert.That(latestSnapshot.Enemies[0].CurrentHp, Is.EqualTo(5));
            Assert.That(latestSnapshot.LastActionDescription, Is.EqualTo("플레이어: Basic Attack"));
        }

        [Test]
        public void RequestEndPlayerTurn_EmitsEnemyActionDescription()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData("Slime", maxHp: 10, attackValue: 4);

            manager.SetCombatants(player, new[] { enemy });
            manager.EnemyTurnDelaySeconds = 0f;
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            manager.BoardManager.SetBoardState(
                new[,]
                {
                    { 64, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                0);
            manager.ResolveBoardPhase();

            CombatSnapshot latestSnapshot = null;
            manager.OnCombatStateChanged += snapshot => latestSnapshot = snapshot;

            manager.RequestEndPlayerTurn();

            Assert.That(latestSnapshot, Is.Not.Null);
            Assert.That(latestSnapshot.LastActionDescription, Is.EqualTo("Slime: 공격"));
        }

        [Test]
        public void RequestEndPlayerTurn_DebuffIntent_EmitsSpecificIntentName()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData("Slime", maxHp: 10, attackValue: 4);
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new EnemyIntent
                {
                    intentType = EnemyIntentType.Debuff,
                    debuffType = DebuffType.Darkness,
                    value = 2,
                },
            };

            manager.SetCombatants(player, new[] { enemy });
            manager.EnemyTurnDelaySeconds = 0f;
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            manager.BoardManager.SetBoardState(
                new[,]
                {
                    { 64, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                0);
            manager.ResolveBoardPhase();

            CombatSnapshot latestSnapshot = null;
            manager.OnCombatStateChanged += snapshot => latestSnapshot = snapshot;

            manager.RequestEndPlayerTurn();

            Assert.That(latestSnapshot, Is.Not.Null);
            Assert.That(latestSnapshot.LastActionDescription, Is.EqualTo("Slime: 암흑"));
        }

        [Test]
        public void RequestEndPlayerTurn_PreparesNextIntentForNextPlayerTurnSnapshot()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData("Slime", maxHp: 10, attackValue: 4);
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new EnemyIntent
                {
                    intentType = EnemyIntentType.Attack,
                    value = 4,
                },
                new EnemyIntent
                {
                    intentType = EnemyIntentType.Defense,
                    value = 3,
                },
            };

            manager.SetCombatants(player, new[] { enemy });
            manager.EnemyTurnDelaySeconds = 0f;
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            manager.BoardManager.SetBoardState(
                new[,]
                {
                    { 64, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                0);
            manager.ResolveBoardPhase();

            CombatSnapshot latestSnapshot = null;
            manager.OnCombatStateChanged += snapshot => latestSnapshot = snapshot;

            manager.RequestEndPlayerTurn();

            Assert.That(latestSnapshot, Is.Not.Null);
            Assert.That(latestSnapshot.Phase, Is.EqualTo(CombatPhase.BoardPhase));
            Assert.That(latestSnapshot.Enemies[0].Intent.intentType, Is.EqualTo(EnemyIntentType.Defense));
            Assert.That(latestSnapshot.Enemies[0].Intent.value, Is.EqualTo(3));
        }

        [Test]
        public void RequestEndPlayerTurn_FearDebuff_EmitsVfxCueAndHalvesDefenseGain()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var defenseSkill = CreateSkill("guard", "Guard", SkillType.Defense, cost: 0, power: 5);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            playerData.startingSkills = new List<SkillSO> { defenseSkill };
            var enemyData = CreateEnemyData("Slime", maxHp: 10, attackValue: 4);
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new EnemyIntent
                {
                    intentType = EnemyIntentType.Debuff,
                    debuffType = DebuffType.Fear,
                    value = 2,
                },
            };

            manager.SetCombatants(player, new[] { enemy });
            manager.EnemyTurnDelaySeconds = 0f;
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.BoardManager.SetBoardState(new[,]
            {
                { 64, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            }, 0);
            manager.ResolveBoardPhase();

            CombatSnapshot latestSnapshot = null;
            manager.OnCombatStateChanged += snapshot => latestSnapshot = snapshot;

            manager.RequestEndPlayerTurn();

            Assert.That(latestSnapshot, Is.Not.Null);
            Assert.That(latestSnapshot.LastVfxCue, Is.Not.Null);
            Assert.That(latestSnapshot.LastVfxCue.DebuffType, Is.EqualTo(DebuffType.Fear));
            Assert.That(latestSnapshot.LastVfxCue.Value, Is.EqualTo(2));
            Assert.That(latestSnapshot.LastVfxCue.Sequence, Is.GreaterThan(0));
            Assert.That(latestSnapshot.Player.StatusEffects, Has.Some.Matches<CombatStatusEffectSnapshot>(
                effect => effect.Id == "fear" && effect.Description.Contains("절반")));

            manager.ResolveBoardPhase();
            Assert.That(manager.RequestUseSkill(defenseSkill, null), Is.True);
            Assert.That(player.Block, Is.EqualTo(3));
        }

        [Test]
        public void RequestEndPlayerTurn_DarknessDebuff_EmitsVfxCueAndPlacesObstaclesOnNextBoard()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData("Slime", maxHp: 10, attackValue: 4);
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new EnemyIntent
                {
                    intentType = EnemyIntentType.Debuff,
                    debuffType = DebuffType.Darkness,
                    value = 2,
                },
            };

            manager.SetCombatants(player, new[] { enemy });
            manager.EnemyTurnDelaySeconds = 0f;
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.BoardManager.SetBoardState(new[,]
            {
                { 64, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            }, 0);
            manager.ResolveBoardPhase();

            CombatSnapshot latestSnapshot = null;
            manager.OnCombatStateChanged += snapshot => latestSnapshot = snapshot;

            manager.RequestEndPlayerTurn();

            Assert.That(latestSnapshot, Is.Not.Null);
            Assert.That(latestSnapshot.LastVfxCue, Is.Not.Null);
            Assert.That(latestSnapshot.LastVfxCue.DebuffType, Is.EqualTo(DebuffType.Darkness));
            Assert.That(latestSnapshot.LastVfxCue.Value, Is.EqualTo(2));
            Assert.That(CountObstacles(latestSnapshot.Board), Is.EqualTo(2));
        }

        [Test]
        public void GetSnapshot_ExposesBuffAndDebuffStatusEffectsForHpUi()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData("Slime", maxHp: 10, attackValue: 4);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            player.ApplyFear(2);
            enemy.ApplyAttackModifier(3);
            manager.BoardManager.SetBoardState(new[,]
            {
                { Board2048Manager.ObstacleValue, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            }, 1);

            var snapshot = manager.GetSnapshot();

            Assert.That(snapshot.Player.StatusEffects, Has.Some.Matches<CombatStatusEffectSnapshot>(
                effect => effect.Id == "fear" &&
                          effect.DisplayName == "공포" &&
                          !effect.IsBuff &&
                          effect.Value == 2 &&
                          effect.Description.Contains("절반")));
            Assert.That(snapshot.Player.StatusEffects, Has.Some.Matches<CombatStatusEffectSnapshot>(
                effect => effect.Id == "darkness" &&
                          effect.DisplayName == "암흑" &&
                          !effect.IsBuff &&
                          effect.Value == 1));
            Assert.That(snapshot.Enemies[0].StatusEffects, Has.Some.Matches<CombatStatusEffectSnapshot>(
                effect => effect.Id == "attack-up" &&
                          effect.DisplayName == "공격 강화" &&
                          effect.IsBuff &&
                          effect.Value == 3));
        }

        private T CreateGameObject<T>(string name)
            where T : Component
        {
            var gameObject = new GameObject(name);
            ownedObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private PlayerSO CreatePlayerData(int maxHp, int attackPower, params SkillSO[] skills)
        {
            var data = ScriptableObject.CreateInstance<PlayerSO>();
            data.maxHp = maxHp;
            data.attackPower = attackPower;
            data.startingSkills = new List<SkillSO>(skills);
            ownedObjects.Add(data);
            return data;
        }

        private EnemySO CreateEnemyData(string enemyName, int maxHp, int attackValue)
        {
            var data = ScriptableObject.CreateInstance<EnemySO>();
            data.enemyName = enemyName;
            data.maxHp = maxHp;
            data.attackPower = attackValue;
            data.aiActionBias = EnemyAiActionBias.Balanced;
            data.aiDebuffPattern = EnemyDebuffPattern.FearThenDarkness;
            data.aiStrength = EnemyAiStrength.Normal;
            data.intentPattern = new List<EnemyIntent>
            {
                new EnemyIntent
                {
                    intentType = EnemyIntentType.Attack,
                    value = attackValue,
                },
            };
            ownedObjects.Add(data);
            return data;
        }

        private SkillSO CreateSkill(string skillId, string skillName, SkillType skillType, int cost, int power)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.skillId = skillId;
            skill.skillName = skillName;
            skill.skillType = skillType;
            skill.cost = cost;
            skill.power = power;
            ownedObjects.Add(skill);
            return skill;
        }

        private static int CountObstacles(int[,] board)
        {
            var count = 0;
            for (var row = 0; row < 4; row++)
            {
                for (var col = 0; col < 4; col++)
                {
                    if (Board2048Manager.IsObstacle(board[row, col]))
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
