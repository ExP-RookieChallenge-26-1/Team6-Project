using System.Collections.Generic;
using Project2048.Enemy;

namespace Project2048.Combat
{
    [System.Serializable]
    public class CombatSetup
    {
        public PlayerSO playerData;
        public List<EnemySO> enemyDataList = new();
        public int boardMoveCount = 4;
    }
}
