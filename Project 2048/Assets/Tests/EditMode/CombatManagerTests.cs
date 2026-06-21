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
            var attackSkill = CreateSkill("basic-attack", SkillType.Attack, 5, 30);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 4, attackValue: 4);

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
        public void RequestUseSkillById_UsesRequestedEnemyIndex()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var firstEnemy = CreateGameObject<EnemyController>("FirstEnemy");
            var secondEnemy = CreateGameObject<EnemyController>("SecondEnemy");
            var attackSkill = CreateSkill("targeted-strike", SkillType.Attack, cost: 0, power: 100);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 4);
            var firstEnemyData = CreateEnemyData(maxHp: 30, attackValue: 0);
            var secondEnemyData = CreateEnemyData(maxHp: 30, attackValue: 0);

            playerData.startingSkills = new List<SkillSO> { attackSkill };
            manager.SetCombatants(player, new[] { firstEnemy, secondEnemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { firstEnemyData, secondEnemyData },
                boardMoveCount = 1,
            });

            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkillById("targeted-strike", targetIndex: 1), Is.True);
            Assert.That(firstEnemy.CurrentHp, Is.EqualTo(firstEnemy.MaxHp));
            Assert.That(secondEnemy.CurrentHp, Is.LessThan(secondEnemy.MaxHp));
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

            Assert.That(player.CurrentHp, Is.InRange(4, 6));
            Assert.That(manager.CurrentPhase, Is.EqualTo(CombatPhase.BoardPhase));
            Assert.That(manager.TurnController.TurnCount, Is.EqualTo(2));
        }

        [Test]
        public void DamageCalculator_UsesPokemonStyleAttackTimesMovePower_WithVarianceAndCriticalMultiplier()
        {
            var playerObject = CreateGameObject<PlayerCombatController>("Player");
            var enemyObject = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 10);
            var enemyData = CreateEnemyData(maxHp: 20, attackValue: 0);
            var skill = CreateSkill("strike", SkillType.Attack, cost: 0, power: 100);

            playerData.criticalChance = 1f;
            playerData.criticalDamageMultiplier = 1.5f;
            enemyData.baseDefensePower = 10;
            playerObject.Init(playerData);
            enemyObject.Init(enemyData);

            var damage = new DamageCalculator(new System.Random(1))
                .CalculatePlayerSkillDamage(playerObject, skill, enemyObject);

            var strongerMoveDamage = new DamageCalculator(new System.Random(1))
                .CalculateMoveDamage(
                    attackPower: 10,
                    movePower: 200,
                    defensePower: 10,
                    criticalChance: 1f,
                    criticalDamageMultiplier: 1.5f);

            Assert.That(damage, Is.InRange(13, 15));
            Assert.That(strongerMoveDamage, Is.GreaterThan(damage));
        }

        [Test]
        public void ThornGuard_RetaliatesOnlyWhileShieldHpExists()
        {
            var playerObject = CreateGameObject<PlayerCombatController>("Player");
            var enemyObject = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 20, attackValue: 0);
            var skill = CreateSkill("strike", SkillType.Attack, cost: 0, power: 20);

            playerObject.Init(playerData);
            enemyObject.Init(enemyData);
            enemyObject.ApplyThornGuard(shieldHp: 1, retaliationDamage: 30);

            new SkillExecutor().Execute(skill, playerObject, enemyObject, new DamageCalculator(new System.Random(1)));

            Assert.That(playerObject.CurrentHp, Is.EqualTo(17));
            Assert.That(enemyObject.ShieldHp, Is.EqualTo(0));

            new SkillExecutor().Execute(skill, playerObject, enemyObject, new DamageCalculator(new System.Random(1)));

            Assert.That(playerObject.CurrentHp, Is.EqualTo(17));
        }

        [Test]
        public void DefenseSkill_GainsFixedShieldWithoutDefensePowerBonus()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var defenseTier1 = CreateSkill("def-1", SkillType.Defense, cost: 5, power: 30);
            var defenseTier2 = CreateSkill("def-2", SkillType.Defense, cost: 5, power: 40);
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
            Assert.That(player.Block, Is.EqualTo(40));
            Assert.That(player.DefenseBonus, Is.EqualTo(0));

            Assert.That(manager.RequestUseSkill(defenseTier1, null), Is.True);
            Assert.That(player.Block, Is.EqualTo(70));
            Assert.That(player.DefenseBonus, Is.EqualTo(0));
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

        [Test]
        public void RequestEndPlayerTurn_EnemyWithTwoActions_ExecutesBothPreviewedActions()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 4);
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Attack,
                    value = 4,
                },
                new()
                {
                    intentType = EnemyIntentType.Defense,
                    value = 5,
                },
            };
            SetEnemyActionsPerTurn(enemyData, 2);

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

            Assert.That(player.CurrentHp, Is.InRange(4, 6));
            Assert.That(enemy.Block, Is.EqualTo(5));
            Assert.That(manager.CurrentPhase, Is.EqualTo(CombatPhase.BoardPhase));
        }

        [Test]
        public void RefreshCombatantDataFromScriptableObjects_ReappliesChangedSoStatsDuringCombat()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 4);
            CombatSnapshot refreshedSnapshot = null;

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            player.TakeDamage(5);
            enemy.TakeDamage(6);
            playerData.maxHp = 12;
            playerData.attackPower = 9;
            playerData.boardMoveCountBonus = 2;
            enemyData.maxHp = 3;

            manager.OnCombatStateChanged += snapshot => refreshedSnapshot = snapshot;
            manager.RefreshCombatantDataFromScriptableObjects();

            Assert.That(player.MaxHp, Is.EqualTo(12));
            Assert.That(player.CurrentHp, Is.EqualTo(12));
            Assert.That(player.AttackPower, Is.EqualTo(9));
            Assert.That(player.BoardMoveCountBonus, Is.EqualTo(2));
            Assert.That(enemy.MaxHp, Is.EqualTo(3));
            Assert.That(enemy.CurrentHp, Is.EqualTo(3));
            Assert.That(refreshedSnapshot, Is.Not.Null);
            Assert.That(refreshedSnapshot.Player.MaxHp, Is.EqualTo(12));
            Assert.That(refreshedSnapshot.Player.AttackPower, Is.EqualTo(9));
            Assert.That(refreshedSnapshot.Enemies[0].MaxHp, Is.EqualTo(3));
        }

        [Test]
        public void StartCombat_WithoutBoardMoveOverride_UsesPlayerInitialBoardMoveCount()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);

            playerData.initialBoardMoveCount = 7;

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
            });

            Assert.That(manager.BoardManager.MoveCount, Is.EqualTo(7));
            Assert.That(manager.GetSnapshot().RemainingBoardMoves, Is.EqualTo(7));
        }

        [Test]
        public void DebuffSkill_TargetsEnemyById_AndReducesEffectiveDefense()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var fear = CreateSkill("fear", SkillType.Debuff, cost: 0, power: 0);
            fear.effectKind = SkillEffectKind.DefenseStageDown;
            fear.targetDefenseStageModifier = -1;
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            playerData.startingSkills = new List<SkillSO> { fear };
            var enemyData = CreateEnemyData(maxHp: 30, attackValue: 0);
            enemyData.baseDefensePower = 10;

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkillById("fear"), Is.True);
            Assert.That(enemy.DefenseModifier, Is.EqualTo(-1));
            Assert.That(enemy.EffectiveDefensePower, Is.EqualTo(7));
            Assert.That(manager.GetSnapshot().Enemies[0].DefensePower, Is.EqualTo(7));
        }

        [Test]
        public void DebuffAttack_WithPower_DamagesEnemyAndAppliesModifier()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 30, attackValue: 0);
            var flashStrike = CreateSkill("flash-strike", SkillType.Debuff, cost: 0, power: 50);
            flashStrike.effectKind = SkillEffectKind.AttackStageDown;
            flashStrike.targetAttackStageModifier = -1;
            player.Init(playerData);
            enemy.Init(enemyData);

            new SkillExecutor().Execute(flashStrike, player, enemy, new DamageCalculator(new System.Random(1)));

            Assert.That(enemy.CurrentHp, Is.LessThan(30));
            Assert.That(enemy.AttackModifier, Is.EqualTo(-1));
        }

        [Test]
        public void LifeStealSkill_RestoresHpFromActualHpDamage()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 30, attackValue: 0);
            var drain = CreateSkill("life-drain", SkillType.Attack, cost: 0, power: 60);
            drain.effectKind = SkillEffectKind.LifeStealAttack;
            drain.lifeStealPercent = 0.5f;
            player.Init(playerData);
            enemy.Init(enemyData);
            player.TakeDamage(10);

            new SkillExecutor().Execute(drain, player, enemy, new DamageCalculator(new System.Random(1)));

            Assert.That(enemy.CurrentHp, Is.LessThan(30));
            Assert.That(player.CurrentHp, Is.GreaterThan(10));
            Assert.That(player.CurrentHp, Is.LessThanOrEqualTo(15));
        }

        [Test]
        public void ShieldScalingAttack_UsesCurrentShieldAsAttackStatWithoutConsumingIt()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 1);
            var enemyData = CreateEnemyData(maxHp: 100, attackValue: 0);
            enemyData.baseDefensePower = 10;
            var shieldBash = CreateSkill("shield-bash", SkillType.Attack, cost: 0, power: 60);
            shieldBash.effectKind = SkillEffectKind.ShieldScalingAttack;
            shieldBash.damageStatSource = DamageStatSource.ShieldHp;
            player.Init(playerData);
            enemy.Init(enemyData);
            player.AddBlock(60);

            new SkillExecutor().Execute(shieldBash, player, enemy, new DamageCalculator(new System.Random(1)));

            Assert.That(enemy.CurrentHp, Is.LessThanOrEqualTo(69));
            Assert.That(player.ShieldHp, Is.EqualTo(60));
        }

        [Test]
        public void ShieldBurstAttack_UsesShieldAsAttackStatThenConsumesAllShield()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 1);
            var enemyData = CreateEnemyData(maxHp: 1000, attackValue: 0);
            enemyData.baseDefensePower = 10;
            var shieldBurst = CreateSkill("shield-burst", SkillType.Attack, cost: 0, power: 100);
            shieldBurst.effectKind = SkillEffectKind.ShieldBurstAttack;
            shieldBurst.damageStatSource = DamageStatSource.ShieldHp;
            player.Init(playerData);
            enemy.Init(enemyData);
            player.AddBlock(50);

            new SkillExecutor().Execute(shieldBurst, player, enemy, new DamageCalculator(new System.Random(1)));

            Assert.That(enemy.CurrentHp, Is.LessThan(1000));
            Assert.That(player.ShieldHp, Is.EqualTo(0));
        }

        [Test]
        public void DefenseScalingAttack_UsesStageModifiedDefenseAsAttackStat()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 1);
            playerData.baseDefensePower = 10;
            var enemyData = CreateEnemyData(maxHp: 100, attackValue: 0);
            enemyData.baseDefensePower = 10;
            var ironWall = CreateSkill("iron-wall", SkillType.Defense, cost: 0, power: 0);
            ironWall.effectKind = SkillEffectKind.DefenseStageUp;
            ironWall.selfDefenseStageModifier = 2;
            var bodyPress = CreateSkill("body-press", SkillType.Attack, cost: 0, power: 80);
            bodyPress.effectKind = SkillEffectKind.DefenseScalingAttack;
            bodyPress.damageStatSource = DamageStatSource.DefensePower;
            player.Init(playerData);
            enemy.Init(enemyData);
            var executor = new SkillExecutor();

            executor.Execute(ironWall, player, null, new DamageCalculator(new System.Random(1)));
            executor.Execute(bodyPress, player, enemy, new DamageCalculator(new System.Random(1)));

            Assert.That(player.DefenseStage, Is.EqualTo(2));
            Assert.That(player.EffectiveDefensePower, Is.EqualTo(20));
            Assert.That(enemy.CurrentHp, Is.LessThanOrEqualTo(86));
        }

        [Test]
        public void CounterAndEndure_RetaliateAndKeepPlayerAtOneHp()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 5, attackPower: 0);
            var enemyData = CreateEnemyData(maxHp: 20, attackValue: 10);
            player.Init(playerData);
            enemy.Init(enemyData);
            player.ApplyEndure(1);
            player.ApplyCounter(200);

            new EnemyIntentSystem(new System.Random(1)).ExecuteIntent(
                enemy,
                new EnemyIntent
                {
                    intentType = EnemyIntentType.Attack,
                    value = 10,
                },
                player,
                new DamageCalculator(new System.Random(1)));

            Assert.That(player.CurrentHp, Is.EqualTo(1));
            Assert.That(enemy.CurrentHp, Is.EqualTo(12));
        }

        [Test]
        public void BoardMovePenaltyAttack_ReducesNextPlayerTurnMoveCount()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var tentacle = CreateSkill("tentacle-strike", SkillType.Attack, cost: 0, power: 10);
            tentacle.effectKind = SkillEffectKind.BoardMovePenaltyAttack;
            tentacle.nextBoardMoveCountModifier = -1;
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 1);
            playerData.startingSkills = new List<SkillSO> { tentacle };
            var enemyData = CreateEnemyData(maxHp: 50, attackValue: 0);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 5,
            });
            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkillById("tentacle-strike", 0), Is.True);
            manager.RequestEndPlayerTurn();

            Assert.That(manager.CurrentPhase, Is.EqualTo(CombatPhase.BoardPhase));
            Assert.That(manager.BoardManager.MoveCount, Is.EqualTo(4));
        }

        [Test]
        public void EnemyCostGainDebuff_ReducesNextBoardPhaseCostOnce()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 1);
            var enemyData = CreateEnemyData(maxHp: 50, attackValue: 0);
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Debuff,
                    skillEffectKind = SkillEffectKind.CostGainDown,
                    nextCostGainMultiplier = 0.5f,
                },
            };
            var board = new[,]
            {
                { 64, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            };
            var fullCost = new CostConverter().ConvertBoardToCost(board);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.BoardManager.SetBoardState(board, 0);
            Assert.That(manager.ResolveBoardPhase(), Is.EqualTo(fullCost));

            manager.RequestEndPlayerTurn();
            manager.BoardManager.SetBoardState(board, 0);

            Assert.That(manager.ResolveBoardPhase(), Is.EqualTo(fullCost / 2));

            manager.RequestEndPlayerTurn();
            manager.BoardManager.SetBoardState(board, 0);

            Assert.That(manager.ResolveBoardPhase(), Is.EqualTo(fullCost / 2));
        }

        [Test]
        public void ChargeAttack_FiresAtNextPlayerTurnStart()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var charge = CreateSkill("gather-light", SkillType.Attack, cost: 0, power: 0);
            charge.effectKind = SkillEffectKind.ChargeAttack;
            charge.chargedPower = 160;
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            playerData.startingSkills = new List<SkillSO> { charge };
            var enemyData = CreateEnemyData(maxHp: 50, attackValue: 0);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkillById("gather-light"), Is.True);
            Assert.That(enemy.CurrentHp, Is.EqualTo(50));
            manager.RequestEndPlayerTurn();

            Assert.That(enemy.CurrentHp, Is.LessThan(50));
            Assert.That(manager.CurrentPhase, Is.EqualTo(CombatPhase.BoardPhase));
        }

        [Test]
        public void RequestEndPlayerTurn_EnemyWithThreeActions_ExecutesInPreviewOrder()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 30, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 20, attackValue: 0);
            enemyData.aiComplexity = EnemyAiComplexity.Complex;
            enemyData.actionsPerTurn = 1;
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Attack,
                    value = 4,
                },
                new()
                {
                    intentType = EnemyIntentType.Defense,
                    value = 5,
                },
                new()
                {
                    intentType = EnemyIntentType.Debuff,
                    debuffType = DebuffType.Fear,
                    value = 1,
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
            var preview = manager.GetSnapshot().Enemies[0].Intents;

            Assert.That(preview.Count, Is.EqualTo(3));

            manager.RequestEndPlayerTurn();

            Assert.That(player.CurrentHp, Is.InRange(14, 16));
            Assert.That(enemy.Block, Is.EqualTo(5));
            Assert.That(player.FearStacks, Is.EqualTo(PlayerCombatController.FearDefenseGainPenalty));
            Assert.That(manager.CurrentPhase, Is.EqualTo(CombatPhase.BoardPhase));
        }

        [Test]
        public void EnemySkillIntent_CanApplyPlayerDebuffAndSkillNameSnapshot()
        {
            var manager = CreateGameObject<CombatManager>("CombatManager");
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 30, attackPower: 8);
            var enemyData = CreateEnemyData(maxHp: 20, attackValue: 0);
            var flash = CreateSkill("enemy-flash", SkillType.Debuff, cost: 0, power: 0);
            flash.skillName = "섬광";
            flash.effectKind = SkillEffectKind.AttackStageDown;
            flash.targetAttackStageModifier = -1;
            flash.availability = SkillAvailability.Shared;
            enemyData.aiActionBias = EnemyAiActionBias.Balanced;
            enemyData.aiDebuffInterval = 1;
            var guard = CreateSkill("enemy-guard", SkillType.Defense, cost: 0, power: 2);
            guard.effectKind = SkillEffectKind.BasicDefense;
            guard.availability = SkillAvailability.Shared;
            enemyData.skills = new List<SkillSO> { flash, guard };
            enemyData.intentPattern.Clear();

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            var intent = manager.GetSnapshot().Enemies[0].Intents[0];
            Assert.That(intent.displayName, Is.EqualTo("섬광"));

            manager.ResolveBoardPhase();
            manager.RequestEndPlayerTurn();

            Assert.That(player.AttackPowerModifier, Is.EqualTo(-1));
            Assert.That(manager.GetSnapshot().Player.AttackPower, Is.EqualTo(5));
        }

        [Test]
        public void SplitAndEcho_ModifyFutureAttackDamage()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 1);
            var enemyData = CreateEnemyData(maxHp: 100, attackValue: 0);
            var split = CreateSkill("split", SkillType.Defense, cost: 0, power: 0);
            split.effectKind = SkillEffectKind.NextAttackSplit;
            split.nextAttackHitCount = 2;
            split.nextAttackHitPowerMultiplier = 0.6f;
            var echo = CreateSkill("echo", SkillType.Defense, cost: 0, power: 0);
            echo.effectKind = SkillEffectKind.NextAttackPowerMultiplier;
            echo.nextAttackPowerMultiplier = 1.3f;
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 40);
            attack.effectKind = SkillEffectKind.BasicAttack;
            player.Init(playerData);
            enemy.Init(enemyData);
            var executor = new SkillExecutor();

            executor.Execute(split, player, null, new DamageCalculator(new System.Random(1)));
            executor.Execute(echo, player, null, new DamageCalculator(new System.Random(1)));

            Assert.That(player.ExtraAttackHits, Is.EqualTo(1));
            Assert.That(player.EchoDamageBonus, Is.EqualTo(30));

            executor.Execute(attack, player, enemy, new DamageCalculator(new System.Random(1)));

            Assert.That(player.ExtraAttackHits, Is.EqualTo(0));
            Assert.That(player.EchoDamageBonus, Is.EqualTo(0));
            Assert.That(enemy.CurrentHp, Is.LessThan(100));
        }

        [Test]
        public void SacrificeAttack_SpendsHpButLeavesOne()
        {
            var player = CreateGameObject<PlayerCombatController>("Player");
            var enemy = CreateGameObject<EnemyController>("Enemy");
            var playerData = CreatePlayerData(maxHp: 10, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 20, attackValue: 0);
            var sacrifice = CreateSkill("bio", SkillType.Attack, cost: 0, power: 140);
            sacrifice.effectKind = SkillEffectKind.SacrificeAttack;
            sacrifice.hpCost = 5;
            sacrifice.hpCostLeavesOne = true;
            player.Init(playerData);
            enemy.Init(enemyData);
            player.TakeDamage(7);

            new SkillExecutor().Execute(sacrifice, player, enemy, new DamageCalculator(new System.Random(1)));

            Assert.That(player.CurrentHp, Is.EqualTo(1));
            Assert.That(enemy.CurrentHp, Is.LessThan(20));
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

        private static void SetEnemyActionsPerTurn(EnemySO data, int count)
        {
            var field = typeof(EnemySO).GetField("actionsPerTurn");
            Assert.That(field, Is.Not.Null, "EnemySO should expose actionsPerTurn for per-enemy multi-action tuning.");
            field.SetValue(data, count);
        }
    }
}
