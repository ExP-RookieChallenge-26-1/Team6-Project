using System;
using UnityEngine;

namespace Project2048.Skills
{
    [Serializable]
    public sealed class SkillVfxTuning
    {
        public static readonly Vector3 DefaultLocalOffset = new(0f, 0.16f, 0f);

        public SkillVfxFamily family;
        public Sprite primarySprite;
        public GameObject primaryPrefab;
        public GameObject projectilePrefab;
        public Sprite secondarySprite;
        public GameObject secondaryPrefab;
        public ParticleSystem particlePrefab;
        public Material particleMaterial;
        public Vector3 localOffset = DefaultLocalOffset;
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

        public bool HasAnySetting =>
            family != SkillVfxFamily.None ||
            HasAuthoredVisual ||
            particleMaterial != null ||
            localOffset != DefaultLocalOffset ||
            !Mathf.Approximately(radiusMultiplier, 1f) ||
            lifetimeSeconds > 0f ||
            sortingOffset != 12 ||
            !Mathf.Approximately(tintWhiteBlend, 0.18f) ||
            !Mathf.Approximately(alpha, 0.9f) ||
            !Mathf.Approximately(rotationDegrees, -12f);

        public void Sanitize()
        {
            radiusMultiplier = Mathf.Max(0.01f, radiusMultiplier);
            if (lifetimeSeconds != -1f)
            {
                lifetimeSeconds = Mathf.Max(0f, lifetimeSeconds);
            }

            alpha = Mathf.Clamp01(alpha);
            tintWhiteBlend = Mathf.Clamp01(tintWhiteBlend);
        }
    }
}
