using UnityEngine;
using UnityEngine.Audio;

namespace Project2048.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class PersistentBgmPlayer : MonoBehaviour
    {
        [SerializeField] private Project2048AudioSettings audioSettings;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip mainThemeClip;
        [SerializeField] private AudioMixerGroup bgmMixerGroup;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool persistAcrossScenes = true;

        private static PersistentBgmPlayer instance;

        public AudioSource Source => audioSource;
        public AudioClip MainThemeClip => mainThemeClip;
        public AudioMixerGroup BgmMixerGroup => bgmMixerGroup;

        public void Initialize(Project2048AudioSettings settings)
        {
            audioSettings = settings;
            ResolveSettings();
            EnsureAudioSource();
            if (playOnStart && Application.isPlaying)
            {
                Play();
            }
        }

        public bool Play(AudioClip clipOverride = null)
        {
            ResolveSettings();
            EnsureAudioSource();

            var clip = clipOverride != null ? clipOverride : mainThemeClip;
            if (audioSource == null || clip == null)
            {
                return false;
            }

            if (audioSource.clip != clip)
            {
                audioSource.clip = clip;
            }

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }

            return true;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            ResolveSettings();
            EnsureAudioSource();
        }

        private void Start()
        {
            if (playOnStart && Application.isPlaying)
            {
                Play();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void ResolveSettings()
        {
            if (audioSettings == null)
            {
                audioSettings = Project2048AudioSettings.LoadDefault();
            }

            if (audioSettings == null)
            {
                return;
            }

            if (mainThemeClip == null)
            {
                mainThemeClip = audioSettings.MainThemeClip;
            }

            if (bgmMixerGroup == null)
            {
                bgmMixerGroup = audioSettings.BgmGroup;
            }
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

            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            audioSource.mute = false;
            if (bgmMixerGroup != null)
            {
                audioSource.outputAudioMixerGroup = bgmMixerGroup;
            }
        }
    }
}
