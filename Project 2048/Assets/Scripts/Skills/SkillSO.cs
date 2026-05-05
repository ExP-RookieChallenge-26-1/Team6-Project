using Project2048.Presentation;
using UnityEngine;

namespace Project2048.Skills
{
    [CreateAssetMenu(menuName = "Game/Skill")]
    public class SkillSO : ScriptableObject
    {
        public string skillId;
        public string skillName;
        public SkillType skillType;
        public int cost;
        public int power;
        public int targetAttackModifier;
        public int selfDefenseBonus;
        public Sprite icon;
        public CombatEffectBinding activationEffect = new();
        [TextArea] public string description;

        private void OnValidate()
        {
            cost = Mathf.Max(0, cost);
            power = Mathf.Max(0, power);
        }
    }
}
