using System.Collections.Generic;
using NUnit.Framework;
using Project2048.Board2048;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Tests
{
    public class CombatManagerTests
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
        public void RequestUseSkill_KillingLastEnemy_RaisesVictory()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var attackSkill = CreateSkill("basic-attack", SkillType.Attack, 5, 3);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 5, attackValue: 4);

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

            var victoryRaised = false;
            manager.OnCombatVictory += _ => victoryRaised = true;

            manager.ResolveBoardPhase();
            var used = manager.RequestUseSkill(attackSkill, enemy);

            Assert.That(used, Is.True);
            Assert.That(victoryRaised, Is.True);
            Assert.That(manager.CurrentPhase, Is.EqualTo(CombatPhase.Victory));
        }

        [Test]
        public void RequestEndPlayerTurn_ExecutesEnemyAttack_AndStartsNextPlayerTurn()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 4);

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
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                0);

            manager.ResolveBoardPhase();
            manager.RequestEndPlayerTurn();

            Assert.That(player.CurrentHp, Is.EqualTo(16));
            Assert.That(manager.CurrentPhase, Is.EqualTo(CombatPhase.BoardPhase));
            Assert.That(manager.TurnController.TurnCount, Is.EqualTo(2));
        }

        [Test]
        public void DefenseSkill_WithSelfDefenseBonus_AccumulatesAndAppliesToFutureBlock()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var defenseTier1 = CreateSkill("def-1", SkillType.Defense, cost: 5, power: 3);
            var defenseTier2 = CreateSkill("def-2", SkillType.Defense, cost: 5, power: 4);
            defenseTier2.selfDefenseBonus = 2;
            var playerData = CreatePlayerData(maxHp: 30, attackPower: 2);
            playerData.startingSkills = new List<SkillSO> { defenseTier1, defenseTier2 };
            var enemyData = CreateEnemyData(maxHp: 30, attackValue: 1);

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
                    { 128, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                },
                0);

            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkill(defenseTier2, null), Is.True);
            Assert.That(player.Block, Is.EqualTo(4));
            Assert.That(player.DefenseBonus, Is.EqualTo(2));

            Assert.That(manager.RequestUseSkill(defenseTier1, null), Is.True);
            Assert.That(player.Block, Is.EqualTo(4 + (3 + 2)));
            Assert.That(player.DefenseBonus, Is.EqualTo(2));
        }

        [Test]
        public void RequestEndPlayerTurn_ClearsPlayerBlock_WhenNextPlayerTurnStarts()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var defenseSkill = CreateSkill("basic-defense", SkillType.Defense, 5, 3);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            playerData.startingSkills = new List<SkillSO> { defenseSkill };
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 1);

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
            var used = manager.RequestUseSkill(defenseSkill, null);

            Assert.That(used, Is.True);
            Assert.That(player.Block, Is.EqualTo(3));

            manager.RequestEndPlayerTurn();

            Assert.That(manager.CurrentPhase, Is.EqualTo(CombatPhase.BoardPhase));
            Assert.That(player.Block, Is.EqualTo(0));
        }

        [Test]
        public void RequestEndPlayerTurn_ClearsEnemyBlock_WhenThatEnemyStartsNextOwnTurn()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Defense,
                    value = 5,
                },
                new()
                {
                    intentType = EnemyIntentType.Attack,
                    value = 0,
                },
            };

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
            manager.RequestEndPlayerTurn();

            Assert.That(enemy.Block, Is.EqualTo(5));

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
            manager.RequestEndPlayerTurn();

            Assert.That(enemy.Block, Is.EqualTo(0));
            Assert.That(manager.CurrentPhase, Is.EqualTo(CombatPhase.BoardPhase));
        }

        private T CreateGameObject<T>(string name)
            where T : Component
        {
            var gameObject = new GameObject(name);
            ownedObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private PlayerSO CreatePlayerData(int maxHp, int attackPower)
        {
            var data = ScriptableObject.CreateInstance<PlayerSO>();
            data.maxHp = maxHp;
            data.attackPower = attackPower;
            ownedObjects.Add(data);
            return data;
        }

        private EnemySO CreateEnemyData(int maxHp, int attackValue)
        {
            var data = ScriptableObject.CreateInstance<EnemySO>();
            data.maxHp = maxHp;
            data.attackPower = attackValue;
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

        private SkillSO CreateSkill(string skillId, SkillType skillType, int cost, int power)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.skillId = skillId;
            skill.skillType = skillType;
            skill.cost = cost;
            skill.power = power;
            ownedObjects.Add(skill);
            return skill;
        }
    }
}
