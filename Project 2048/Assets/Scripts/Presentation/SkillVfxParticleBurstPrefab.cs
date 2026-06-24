using UnityEngine;

namespace Project2048.Presentation
{
    public sealed class SkillVfxParticleBurstPrefab : MonoBehaviour
    {
        [Min(0.01f)] public float lifetimeMultiplier = 1f;
        [Min(0.01f)] public float burstMultiplier = 1f;
        [Min(0.01f)] public float speedMultiplier = 1f;
        [Min(0.01f)] public float sizeMultiplier = 1f;
        [Min(0.01f)] public float radiusMultiplier = 1f;
        public int sortingOffset = 2;
        public bool preserveAuthoredShape;

        public float EffectiveLifetimeMultiplier => Mathf.Max(0.01f, lifetimeMultiplier);
        public float EffectiveBurstMultiplier => Mathf.Max(0.01f, burstMultiplier);
        public float EffectiveSpeedMultiplier => Mathf.Max(0.01f, speedMultiplier);
        public float EffectiveSizeMultiplier => Mathf.Max(0.01f, sizeMultiplier);
        public float EffectiveRadiusMultiplier => Mathf.Max(0.01f, radiusMultiplier);
    }
}
