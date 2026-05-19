using System;
using UnityEngine;

namespace Project2048.Presentation
{
    [Serializable]
    public class CombatParticleEffectBinding
    {
        public string objectName;
        public ParticleSystem particlePrefab;
        public Material particleMaterial;
        public bool useParticleColor;
        public Color particleColor = Color.white;
        [Min(0.05f)] public float lifetimeSeconds = 0.8f;
        [Min(1)] public int burstCount = 16;
        [Min(0f)] public float startSpeed = 0.6f;
        [Min(0.01f)] public float startSize = 0.12f;
        public bool swirl;

        public bool HasParticleVisual => particlePrefab != null || particleMaterial != null || useParticleColor;
        public float EffectiveLifetimeSeconds => Mathf.Max(0.05f, lifetimeSeconds);
        public int EffectiveBurstCount => Mathf.Max(1, burstCount);
        public float EffectiveStartSpeed => Mathf.Max(0f, startSpeed);
        public float EffectiveStartSize => Mathf.Max(0.01f, startSize);

        public string ResolveObjectName(string fallback)
        {
            return string.IsNullOrWhiteSpace(objectName) ? fallback : objectName.Trim();
        }

        public Color ResolveColor(Color fallback)
        {
            return useParticleColor ? particleColor : fallback;
        }
    }
}
