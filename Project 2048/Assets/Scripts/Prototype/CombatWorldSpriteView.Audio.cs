using System.Collections;
using Project2048.Audio;
using Project2048.Presentation;
using UnityEngine;

namespace Project2048.Prototype
{
    public partial class CombatWorldSpriteView
    {
        private const float WorldSfxDistance = 10000f;

        private void PlayCombatantActionAudioEffect(CombatEffectBinding effect, float extraDelaySeconds = 0f)
        {
            if (effect?.sfxClip == null)
            {
                return;
            }

            var delay = effect.EffectiveSfxDelaySeconds + Mathf.Max(0f, extraDelaySeconds);
            if (delay > 0f && isActiveAndEnabled)
            {
                StartCoroutine(PlayCombatantActionAudioEffectAfterDelay(effect, delay));
                return;
            }

            PlayCombatantActionAudioEffectNow(effect);
        }

        private IEnumerator PlayCombatantActionAudioEffectAfterDelay(CombatEffectBinding effect, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            PlayCombatantActionAudioEffectNow(effect);
        }

        private void PlayCombatantActionAudioEffectNow(CombatEffectBinding effect)
        {
            EnsureAudioSource();
            if (audioSource == null)
            {
                return;
            }

            DuckBgmForImportantSfx();
            CombatEffectAudioPlayer.PlayOneShot(audioSource, effect, 1f, transform);
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            ResolveAudioRouting();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            audioSource.mute = false;
            audioSource.loop = false;
            audioSource.maxDistance = WorldSfxDistance;
            audioSource.minDistance = WorldSfxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            if (sfxMixerGroup != null)
            {
                audioSource.outputAudioMixerGroup = sfxMixerGroup;
            }
        }

        private void ResolveAudioRouting()
        {
            var settings = Project2048AudioSettings.LoadDefault();
            if (sfxMixerGroup == null)
            {
                sfxMixerGroup = settings != null ? settings.SfxGroup : null;
            }

            if (bgmDucker == null)
            {
                bgmDucker = SimpleBgmDucker.Active != null
                    ? SimpleBgmDucker.Active
                    : FindAnyObjectByType<SimpleBgmDucker>(FindObjectsInactive.Include);
            }
        }

        private void DuckBgmForImportantSfx()
        {
            ResolveAudioRouting();
            bgmDucker?.DuckBgm();
        }
    }
}
