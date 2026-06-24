using UnityEngine;

namespace Project2048.Skills
{
    [System.Serializable]
    public sealed class SkillVfxCue
    {
        public SkillVfxTrigger trigger;
        public GameObject prefab;
        public SkillVfxPlacement placement;
        [Min(0f)] public float delaySeconds;
        [Min(0f)] public float scale = 1f;          // 1 = use prefab as-is
        public Color tint = Color.clear;            // clear = use prefab as-is
        public float lifetimeOverride = -1f;        // <=0 = prefab/self lifetime

        public bool HasPrefab => prefab != null;
    }
}
