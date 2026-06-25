using UnityEngine;
using UnityEngine.Audio;

namespace Project2048.Audio
{
    [CreateAssetMenu(fileName = "Project2048AudioSettings", menuName = "Project 2048/Audio Settings")]
    public class Project2048AudioSettings : ScriptableObject
    {
        public const string ResourcePath = "Audio/Project2048AudioSettings";
        public const string MasterVolumeParameterName = "MasterVolume";
        public const string BgmVolumeParameterName = "BGMVolume";
        public const string SfxVolumeParameterName = "SFXVolume";
        public const float DefaultBgmVolumeDb = -14f;
        public const float MinVolumeDb = -80f;
        public const float MaxVolumeDb = 0f;

        [SerializeField] private AudioClip mainThemeClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private string masterVolumeParameter = MasterVolumeParameterName;
        [SerializeField] private string bgmVolumeParameter = BgmVolumeParameterName;
        [SerializeField] private string sfxVolumeParameter = SfxVolumeParameterName;

        public AudioClip MainThemeClip => mainThemeClip;
        public AudioClip ButtonClickClip => buttonClickClip;
        public AudioMixer MasterMixer => masterMixer;
        public AudioMixerGroup BgmGroup => bgmGroup;
        public AudioMixerGroup SfxGroup => sfxGroup;
        public string MasterVolumeParameter => ResolveParameterName(masterVolumeParameter, MasterVolumeParameterName);
        public string BgmVolumeParameter => ResolveParameterName(bgmVolumeParameter, BgmVolumeParameterName);
        public string SfxVolumeParameter => ResolveParameterName(sfxVolumeParameter, SfxVolumeParameterName);

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
                _ => null,
            };
        }

        public string GetVolumeParameter(Project2048AudioChannel channel)
        {
            return channel switch
            {
                Project2048AudioChannel.Master => MasterVolumeParameter,
                Project2048AudioChannel.BGM => BgmVolumeParameter,
                Project2048AudioChannel.SFX => SfxVolumeParameter,
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

    public static class Project2048AudioPreferences
    {
        public const float DefaultNormalizedVolume = 1f;

        private const string VolumeKeyPrefix = "Project2048.Audio.Volume.";
        private const string MutedKeyPrefix = "Project2048.Audio.Muted.";

        public static float GetNormalizedVolume(Project2048AudioChannel channel)
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(GetVolumeKey(channel), DefaultNormalizedVolume));
        }

        public static bool IsMuted(Project2048AudioChannel channel)
        {
            return PlayerPrefs.GetInt(GetMutedKey(channel), 0) != 0;
        }

        public static void SetNormalizedVolume(
            Project2048AudioSettings settings,
            Project2048AudioChannel channel,
            float normalizedVolume,
            bool saveImmediately = true)
        {
            var clampedVolume = Mathf.Clamp01(normalizedVolume);
            PlayerPrefs.SetFloat(GetVolumeKey(channel), clampedVolume);
            ApplyVolume(settings, channel, clampedVolume);

            if (saveImmediately)
            {
                PlayerPrefs.Save();
            }
        }

        public static void SetMuted(
            Project2048AudioSettings settings,
            Project2048AudioChannel channel,
            bool isMuted,
            bool saveImmediately = true)
        {
            PlayerPrefs.SetInt(GetMutedKey(channel), isMuted ? 1 : 0);
            ApplyVolume(settings, channel, GetNormalizedVolume(channel));

            if (saveImmediately)
            {
                PlayerPrefs.Save();
            }
        }

        public static void ApplySavedVolumes(Project2048AudioSettings settings)
        {
            if (settings == null)
            {
                settings = Project2048AudioSettings.LoadDefault();
            }

            foreach (Project2048AudioChannel channel in System.Enum.GetValues(typeof(Project2048AudioChannel)))
            {
                ApplyVolume(settings, channel, GetNormalizedVolume(channel));
            }
        }

        public static void DeleteSavedVolumes()
        {
            foreach (Project2048AudioChannel channel in System.Enum.GetValues(typeof(Project2048AudioChannel)))
            {
                PlayerPrefs.DeleteKey(GetVolumeKey(channel));
                PlayerPrefs.DeleteKey(GetMutedKey(channel));
            }
        }

        public static float GetEffectiveVolumeDb(Project2048AudioChannel channel)
        {
            return VolumeToDb(channel, IsMuted(channel) ? 0f : GetNormalizedVolume(channel));
        }

        public static float VolumeToDb(Project2048AudioChannel channel, float normalizedVolume)
        {
            return VolumeToDb(normalizedVolume, GetChannelDefaultDb(channel));
        }

        public static float VolumeToDb(float normalizedVolume, float fullScaleDb)
        {
            var clampedVolume = Mathf.Clamp01(normalizedVolume);
            if (clampedVolume <= 0f)
            {
                return Project2048AudioSettings.MinVolumeDb;
            }

            return Mathf.Clamp(
                fullScaleDb + 20f * Mathf.Log10(clampedVolume),
                Project2048AudioSettings.MinVolumeDb,
                Project2048AudioSettings.MaxVolumeDb);
        }

        public static float OffsetDb(float volumeDb, float offsetDb)
        {
            if (volumeDb <= Project2048AudioSettings.MinVolumeDb)
            {
                return Project2048AudioSettings.MinVolumeDb;
            }

            return Mathf.Clamp(
                volumeDb + offsetDb,
                Project2048AudioSettings.MinVolumeDb,
                Project2048AudioSettings.MaxVolumeDb);
        }

        public static float GetChannelDefaultDb(Project2048AudioChannel channel)
        {
            return channel == Project2048AudioChannel.BGM
                ? Project2048AudioSettings.DefaultBgmVolumeDb
                : Project2048AudioSettings.MaxVolumeDb;
        }

        private static void ApplyVolume(
            Project2048AudioSettings settings,
            Project2048AudioChannel channel,
            float normalizedVolume)
        {
            if (settings == null)
            {
                return;
            }

            if (channel == Project2048AudioChannel.BGM && SimpleBgmDucker.Active != null)
            {
                SimpleBgmDucker.Active.ApplyBaseVolume();
                return;
            }

            var effectiveVolume = IsMuted(channel) ? 0f : normalizedVolume;
            settings.TrySetVolume(channel, VolumeToDb(channel, effectiveVolume));
        }

        private static string GetVolumeKey(Project2048AudioChannel channel)
        {
            return VolumeKeyPrefix + channel;
        }

        private static string GetMutedKey(Project2048AudioChannel channel)
        {
            return MutedKeyPrefix + channel;
        }
    }
}
