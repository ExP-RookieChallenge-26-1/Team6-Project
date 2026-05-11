using Project2048.Combat;
using Project2048.Presentation;
using Project2048.Rewards;
using UnityEngine;

namespace Project2048.Prototype
{
    /// <summary>
    /// Plays prototype battle-result and reward-selection sounds from domain events,
    /// separate from button/UI click feedback.
    /// </summary>
    public class PrototypeCombatEventAudioPlayer : MonoBehaviour
    {
        private const float EventSfxDistance = 10000f;

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private PrototypeCombatEventAudioProfileSO eventAudioProfile;
        [SerializeField] private float volumeScale = 1f;

        private CombatManager combatManager;
        private RewardManager rewardManager;

        public PrototypeCombatEventSoundCue LastPlayedCue { get; private set; }

        private void Awake()
        {
            EnsureAudioDefaults();
        }

        private void OnDestroy()
        {
            UnbindCombat();
            UnbindReward();
        }

        public void Initialize(PrototypeCombatBootstrap owner)
        {
            EnsureAudioDefaults();
            BindCombat(owner != null ? owner.CombatManager : null);
            BindReward(owner != null ? owner.RewardManager : null);
        }

        private void BindCombat(CombatManager manager)
        {
            if (combatManager == manager)
            {
                return;
            }

            UnbindCombat();
            combatManager = manager;
            if (combatManager == null)
            {
                return;
            }

            combatManager.OnCombatVictory += HandleCombatVictory;
            combatManager.OnCombatDefeat += HandleCombatDefeat;
        }

        private void BindReward(RewardManager manager)
        {
            if (rewardManager == manager)
            {
                return;
            }

            UnbindReward();
            rewardManager = manager;
            if (rewardManager == null)
            {
                return;
            }

            rewardManager.OnRewardClaimed += HandleRewardClaimed;
        }

        private void UnbindCombat()
        {
            if (combatManager == null)
            {
                return;
            }

            combatManager.OnCombatVictory -= HandleCombatVictory;
            combatManager.OnCombatDefeat -= HandleCombatDefeat;
            combatManager = null;
        }

        private void UnbindReward()
        {
            if (rewardManager == null)
            {
                return;
            }

            rewardManager.OnRewardClaimed -= HandleRewardClaimed;
            rewardManager = null;
        }

        private void HandleCombatVictory(CombatResult _)
        {
            PlayCue(PrototypeCombatEventSoundCue.Victory);
        }

        private void HandleCombatDefeat()
        {
            PlayCue(PrototypeCombatEventSoundCue.Defeat);
        }

        private void HandleRewardClaimed(RewardChoiceResult result)
        {
            switch (result.Kind)
            {
                case RewardChoiceKind.Rest:
                    PlayCue(PrototypeCombatEventSoundCue.RewardRest);
                    break;
                case RewardChoiceKind.Enhance:
                    PlayCue(PrototypeCombatEventSoundCue.RewardEnhance);
                    break;
            }
        }

        private void PlayCue(PrototypeCombatEventSoundCue cue)
        {
            LastPlayedCue = cue;
            var effect = eventAudioProfile != null ? eventAudioProfile.Resolve(cue) : null;
            if (effect?.sfxClip != null && audioSource != null)
            {
                CombatEffectAudioPlayer.PlayOneShot(audioSource, effect, volumeScale, transform);
            }
        }

        private void EnsureAudioDefaults()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            audioSource.mute = false;
            audioSource.loop = false;
            audioSource.minDistance = EventSfxDistance;
            audioSource.maxDistance = EventSfxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            if (volumeScale <= 0f)
            {
                volumeScale = 1f;
            }
        }
    }
}
