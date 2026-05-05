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
        [Min(0.01f)] public float minPitch = 1f;
        [Min(0.01f)] public float maxPitch = 1f;
        public Vector3 localOffset;
        [Min(0f)] public float autoDestroySeconds = 1.25f;

        public bool HasAnyAsset => sfxClip != null || vfxPrefab != null || animationClip != null;
        public float EffectiveVolumeScale => Mathf.Max(0f, volumeScale);
        public float EffectiveMinPitch => Mathf.Max(0.01f, Mathf.Min(minPitch, maxPitch));
        public float EffectiveMaxPitch => Mathf.Max(0.01f, Mathf.Max(minPitch, maxPitch));
        public float EffectiveAutoDestroySeconds => Mathf.Max(0f, autoDestroySeconds);

        public float ResolvePitch()
        {
            var min = EffectiveMinPitch;
            var max = EffectiveMaxPitch;
            return Mathf.Approximately(min, max) ? min : UnityEngine.Random.Range(min, max);
        }
    }

    public static class CombatEffectAudioPlayer
    {
        public static bool PlayOneShot(
            AudioSource template,
            CombatEffectBinding effect,
            float volumeMultiplier,
            Transform owner)
        {
            if (template == null || effect?.sfxClip == null)
            {
                return false;
            }

            var pitch = effect.ResolvePitch();
            var volumeScale = Mathf.Max(0f, volumeMultiplier) * effect.EffectiveVolumeScale;
            if (Mathf.Approximately(pitch, 1f))
            {
                template.PlayOneShot(effect.sfxClip, volumeScale);
                return true;
            }

            var tempObject = new GameObject("CombatEffectAudio");
            tempObject.transform.SetParent(owner != null ? owner : template.transform, false);

            var source = tempObject.AddComponent<AudioSource>();
            CopyAudioSourceSettings(template, source);
            source.pitch = pitch;
            source.PlayOneShot(effect.sfxClip, volumeScale);

            var lifetime = Mathf.Max(0.1f, effect.sfxClip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)));
            UnityEngine.Object.Destroy(tempObject, lifetime);
            return true;
        }

        private static void CopyAudioSourceSettings(AudioSource source, AudioSource target)
        {
            target.outputAudioMixerGroup = source.outputAudioMixerGroup;
            target.playOnAwake = false;
            target.spatialBlend = source.spatialBlend;
            target.volume = source.volume;
            target.mute = source.mute;
            target.loop = false;
            target.priority = source.priority;
            target.minDistance = source.minDistance;
            target.maxDistance = source.maxDistance;
            target.rolloffMode = source.rolloffMode;
            target.dopplerLevel = source.dopplerLevel;
        }
    }
}
