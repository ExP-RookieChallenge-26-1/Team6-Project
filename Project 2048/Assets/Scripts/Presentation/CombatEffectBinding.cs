using System;
using UnityEngine;

namespace Project2048.Presentation
{
    [Serializable]
    public class CombatEffectBinding
    {
        public AudioClip sfxClip;
        public GameObject vfxPrefab;
        public AnimationClip animationClip;
        [Min(0f)] public float volumeScale = 1f;
        [Min(0f)] public float autoDestroySeconds = 1.25f;

        public bool HasAnyAsset => sfxClip != null || vfxPrefab != null || animationClip != null;
        public float EffectiveVolumeScale => Mathf.Max(0f, volumeScale);
        public float EffectiveAutoDestroySeconds => Mathf.Max(0f, autoDestroySeconds);
    }
}
