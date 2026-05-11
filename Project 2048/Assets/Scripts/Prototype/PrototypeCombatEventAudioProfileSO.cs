using Project2048.Presentation;
using UnityEngine;

namespace Project2048.Prototype
{
    [CreateAssetMenu(menuName = "Game/Prototype Combat Event Audio Profile")]
    public class PrototypeCombatEventAudioProfileSO : ScriptableObject
    {
        public CombatEffectBinding victoryEffect = new();
        public CombatEffectBinding defeatEffect = new();
        public CombatEffectBinding restRewardEffect = new();
        public CombatEffectBinding enhanceRewardEffect = new();

        public CombatEffectBinding Resolve(PrototypeCombatEventSoundCue cue)
        {
            return cue switch
            {
                PrototypeCombatEventSoundCue.Victory => victoryEffect,
                PrototypeCombatEventSoundCue.Defeat => defeatEffect,
                PrototypeCombatEventSoundCue.RewardRest => restRewardEffect,
                PrototypeCombatEventSoundCue.RewardEnhance => enhanceRewardEffect,
                _ => null,
            };
        }
    }
}
