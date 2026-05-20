using UnityEngine;
using UnityEngine.Audio;

namespace Project2048.Audio
{
    [CreateAssetMenu(fileName = "Project2048AudioSettings", menuName = "Project 2048/Audio Settings")]
    public class Project2048AudioSettings : ScriptableObject
    {
        public const string ResourcePath = "Audio/Project2048AudioSettings";
        public const string BgmVolumeParameterName = "BGMVolume";
        public const string SfxVolumeParameterName = "SFXVolume";
        public const string UiVolumeParameterName = "UIVolume";
        public const string AmbienceVolumeParameterName = "AmbienceVolume";

        [SerializeField] private AudioClip mainThemeClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup uiGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;
        [SerializeField] private string bgmVolumeParameter = BgmVolumeParameterName;
        [SerializeField] private string sfxVolumeParameter = SfxVolumeParameterName;
        [SerializeField] private string uiVolumeParameter = UiVolumeParameterName;
        [SerializeField] private string ambienceVolumeParameter = AmbienceVolumeParameterName;

        public AudioClip MainThemeClip => mainThemeClip;
        public AudioClip ButtonClickClip => buttonClickClip;
        public AudioMixer MasterMixer => masterMixer;
        public AudioMixerGroup BgmGroup => bgmGroup;
        public AudioMixerGroup SfxGroup => sfxGroup;
        public AudioMixerGroup UiGroup => uiGroup;
        public AudioMixerGroup AmbienceGroup => ambienceGroup;
        public string BgmVolumeParameter => ResolveParameterName(bgmVolumeParameter, BgmVolumeParameterName);
        public string SfxVolumeParameter => ResolveParameterName(sfxVolumeParameter, SfxVolumeParameterName);
        public string UiVolumeParameter => ResolveParameterName(uiVolumeParameter, UiVolumeParameterName);
        public string AmbienceVolumeParameter => ResolveParameterName(
            ambienceVolumeParameter,
            AmbienceVolumeParameterName);

        public static Project2048AudioSettings LoadDefault()
        {
            return Resources.Load<Project2048AudioSettings>(ResourcePath);
        }

        public AudioMixerGroup GetGroup(Project2048AudioChannel channel)
        {
            return channel switch
            {
                Project2048AudioChannel.BGM => bgmGroup,
                Project2048AudioChannel.SFX => sfxGroup,
                Project2048AudioChannel.UI => uiGroup,
                Project2048AudioChannel.Ambience => ambienceGroup,
                _ => null,
            };
        }

        public string GetVolumeParameter(Project2048AudioChannel channel)
        {
            return channel switch
            {
                Project2048AudioChannel.BGM => BgmVolumeParameter,
                Project2048AudioChannel.SFX => SfxVolumeParameter,
                Project2048AudioChannel.UI => UiVolumeParameter,
                Project2048AudioChannel.Ambience => AmbienceVolumeParameter,
                _ => string.Empty,
            };
        }

        public bool ApplyOutputGroup(AudioSource source, Project2048AudioChannel channel)
        {
            if (source == null)
            {
                return false;
            }

            var group = GetGroup(channel);
            if (group == null)
            {
                return false;
            }

            source.outputAudioMixerGroup = group;
            return true;
        }

        public bool TrySetVolume(Project2048AudioChannel channel, float volumeDb)
        {
            if (masterMixer == null)
            {
                return false;
            }

            var parameter = GetVolumeParameter(channel);
            return !string.IsNullOrWhiteSpace(parameter) && masterMixer.SetFloat(parameter, volumeDb);
        }

        private static string ResolveParameterName(string configuredName, string fallbackName)
        {
            return string.IsNullOrWhiteSpace(configuredName) ? fallbackName : configuredName;
        }
    }
}
