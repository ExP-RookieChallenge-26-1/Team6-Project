using System.Collections.Generic;
using Project2048.Board2048;
using Project2048.Combat;

namespace Project2048.Enemy
{
    /// <summary>
    /// 적의 다음 행동을 미리 정하고, 적 턴에 그 행동을 실행한다.
    /// 인텐트는 UI에 공개되는 정보라서 실행 전에 EnemyController.CurrentIntent에 저장한다.
    /// </summary>
    public class EnemyIntentSystem
    {
        private readonly Dictionary<EnemyController, int> intentIndexMap = new();
        private readonly EnemyAiBrain aiBrain;

        public EnemyIntentSystem()
            : this(null)
        {
        }

        public EnemyIntentSystem(System.Random random)
        {
            aiBrain = new EnemyAiBrain(random);
        }

        public void SetNextIntent(EnemyController enemy)
        {
            if (enemy == null || enemy.Data == null)
            {
                return;
            }

            // 적마다 패턴 진행 위치를 따로 기억합니다. 같은 EnemySO를 쓰더라도 컨트롤러별 턴 순서는 분리됩니다.
            if (!intentIndexMap.ContainsKey(enemy))
            {
                intentIndexMap[enemy] = 0;
            }

            var pattern = enemy.Data.intentPattern;
            EnemyIntent nextIntent;

            if (pattern == null || pattern.Count == 0)
            {
                nextIntent = aiBrain.ChooseIntent(enemy.Data, intentIndexMap[enemy]);
            }
            else
            {
                var index = intentIndexMap[enemy] % pattern.Count;
                nextIntent = pattern[index].Clone();
            }

            enemy.SetIntent(nextIntent);
            intentIndexMap[enemy]++;
        }

        public void ExecuteIntent(
            EnemyController enemy,
            PlayerCombatController player,
            DamageCalculator damageCalculator = null,
            Board2048Manager boardManager = null)
        {
            if (enemy == null || player == null || enemy.CurrentIntent == null)
            {
                return;
            }

            damageCalculator ??= new DamageCalculator();

            switch (enemy.CurrentIntent.intentType)
            {
                case EnemyIntentType.Attack:
                    var damage = damageCalculator.CalculateEnemyDamage(enemy.CurrentIntent);
                    player.TakeDamage(damage);
                    break;
                case EnemyIntentType.Defense:
                    enemy.AddBlock(enemy.CurrentIntent.value);
                    break;
                case EnemyIntentType.Debuff:
                    ApplyDebuff(enemy.CurrentIntent, player, boardManager);
                    break;
            }
        }

        private static void ApplyDebuff(EnemyIntent intent, PlayerCombatController player, Board2048Manager boardManager)
        {
            switch (intent.debuffType)
            {
                case DebuffType.Fear:
                    if (intent.value > 0)
                    {
                        player.ApplyFear(intent.value);
                    }
                    break;
                case DebuffType.Darkness:
                    boardManager?.QueueObstacles(intent.value);
                    break;
            }
        }
    }
}
