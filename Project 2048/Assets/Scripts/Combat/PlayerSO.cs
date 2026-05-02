using System.Collections.Generic;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Combat
{
    [CreateAssetMenu(menuName = "Game/Player")]
    public class PlayerSO : ScriptableObject
    {
        public int maxHp = 30;
        public int attackPower = 3;
        public int boardMoveCountBonus;
        public Sprite portrait;
        public List<SkillSO> startingSkills = new();

        private void OnValidate()
        {
            maxHp = Mathf.Max(1, maxHp);
            attackPower = Mathf.Max(0, attackPower);
            boardMoveCountBonus = Mathf.Max(0, boardMoveCountBonus);
        }
    }
}
