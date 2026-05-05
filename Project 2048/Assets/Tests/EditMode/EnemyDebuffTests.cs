using NUnit.Framework;
using Project2048.Board2048;
using Project2048.Combat;
using Project2048.Enemy;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project2048.Tests
{
    public class EnemyDebuffTests
    {
        [Test]
        public void FearIntent_ReducesFutureBlockGainByFixedAmount()
        {
            var playerObject = new GameObject("Player");
            try
            {
                var player = playerObject.AddComponent<PlayerCombatController>();
                var playerData = ScriptableObject.CreateInstance<PlayerSO>();
                playerData.maxHp = 20;
                playerData.attackPower = 1;
                player.Init(playerData);

                var enemyObject = new GameObject("Enemy");
                try
                {
                    var enemy = enemyObject.AddComponent<EnemyController>();
                    var enemyData = ScriptableObject.CreateInstance<EnemySO>();
                    enemyData.maxHp = 10;
                    enemyData.attackPower = 1;
                    enemy.Init(enemyData);
                    enemy.SetIntent(new EnemyIntent
                    {
                        intentType = EnemyIntentType.Debuff,
                        debuffType = DebuffType.Fear,
                        value = 2,
                    });

                    new EnemyIntentSystem().ExecuteIntent(enemy, player);

                    Assert.That(player.FearStacks, Is.EqualTo(6));
                    Assert.That(player.GainBlockWithBonus(8), Is.EqualTo(2));
                    Assert.That(player.Block, Is.EqualTo(2));
                    Assert.That(player.GainBlockWithBonus(5), Is.EqualTo(0));
                    Assert.That(player.Block, Is.EqualTo(2));

                    Object.DestroyImmediate(enemyData);
                }
                finally
                {
                    Object.DestroyImmediate(enemyObject);
                }

                Object.DestroyImmediate(playerData);
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void DarknessIntent_QueuesObstacles_OnBoardManager()
        {
            var playerObject = new GameObject("Player");
            try
            {
                var player = playerObject.AddComponent<PlayerCombatController>();
                var playerData = ScriptableObject.CreateInstance<PlayerSO>();
                playerData.maxHp = 20;
                playerData.attackPower = 1;
                player.Init(playerData);

                var enemyObject = new GameObject("Enemy");
                try
                {
                    var enemy = enemyObject.AddComponent<EnemyController>();
                    var enemyData = ScriptableObject.CreateInstance<EnemySO>();
                    enemyData.maxHp = 10;
                    enemyData.attackPower = 1;
                    enemy.Init(enemyData);
                    enemy.SetIntent(new EnemyIntent
                    {
                        intentType = EnemyIntentType.Debuff,
                        debuffType = DebuffType.Darkness,
                        value = 3,
                    });

                    var board = new Board2048Manager();
                    new EnemyIntentSystem().ExecuteIntent(enemy, player, null, board);

                    Assert.That(board.PendingObstacleCount, Is.EqualTo(3));

                    board.InitBoard(2, spawnInitialTiles: false);
                    Assert.That(board.PendingObstacleCount, Is.EqualTo(0));

                    var obstacles = 0;
                    var snapshot = board.GetBoardSnapshot();
                    for (var row = 0; row < 4; row++)
                    {
                        for (var col = 0; col < 4; col++)
                        {
                            if (Board2048Manager.IsObstacle(snapshot[row, col]))
                            {
                                obstacles++;
                            }
                        }
                    }

                    Assert.That(obstacles, Is.EqualTo(3));

                    Object.DestroyImmediate(enemyData);
                }
                finally
                {
                    Object.DestroyImmediate(enemyObject);
                }

                Object.DestroyImmediate(playerData);
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }
        }
    }
}
