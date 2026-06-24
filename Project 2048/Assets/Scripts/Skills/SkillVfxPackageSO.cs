using UnityEngine;

namespace Project2048.Skills
{
    public sealed class SkillVfxPackageSO : ScriptableObject
    {
        public SkillVfxFamily family;
        public Sprite primarySprite;
        public GameObject primaryPrefab;
        public GameObject projectilePrefab;
        public Sprite secondarySprite;
        public GameObject secondaryPrefab;
        public ParticleSystem particlePrefab;
        public Material particleMaterial;
        public Vector3 localOffset = new(0f, 0.16f, 0f);
        public float radiusMultiplier = 1f;
        public float lifetimeSeconds = -1f;
        public int sortingOffset = 12;
        public float tintWhiteBlend = 0.18f;
        public float alpha = 0.9f;
        public float rotationDegrees = -12f;

        public bool HasAuthoredVisual =>
            primarySprite != null ||
            primaryPrefab != null ||
            projectilePrefab != null ||
            secondarySprite != null ||
            secondaryPrefab != null ||
            particlePrefab != null;
    }
}
