using UnityEngine;

namespace Project2048.Skills
{
    [System.Serializable]
    public struct SkillVfxPlacement
    {
        public SkillVfxTarget target;
        public SkillVfxVertical vertical;
        public Vector3 localOffset;
    }
}
