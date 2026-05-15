using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project2048.Audio
{
    [DisallowMultipleComponent]
    public class ButtonClickAudioRouter : MonoBehaviour
    {
        private const float ButtonRefreshIntervalSeconds = 0.5f;
        public const float DefaultMinPitch = 0.96f;
        public const float DefaultMaxPitch = 1.04f;

        [SerializeField] private Project2048AudioSettings audioSettings;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;
        [SerializeField, Range(0.5f, 1.5f)] private float minPitch = DefaultMinPitch;
        [SerializeField, Range(0.5f, 1.5f)] private float maxPitch = DefaultMaxPitch;

        private float nextButtonRefreshTime;

        public static ButtonClickAudioRouter Active { get; private set; }
        public static event System.Action ButtonClickPlayed;
        public AudioSource Source => audioSource;
        public AudioClip ButtonClickClip => buttonClickClip;
        public float MinPitch => Mathf.Min(minPitch, maxPitch);
        public float MaxPitch => Mathf.Max(minPitch, maxPitch);

        public void Initialize(Project2048AudioSettings settings)
        {
            audioSettings = settings;
            EnsureAudioSource();
            ResolveButtonClickClip();
            RefreshButtons();
        }

        public int RefreshButtons()
        {
            nextButtonRefreshTime = Time.unscaledTime + ButtonRefreshIntervalSeconds;
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            var count = 0;
            foreach (var button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                var emitter = button.GetComponent<ButtonClickAudioEmitter>();
                if (emitter == null)
                {
                    emitter = button.gameObject.AddComponent<ButtonClickAudioEmitter>();
                }

                emitter.EnsureBound();
                count++;
            }

            return count;
        }

        public static void PlayGlobal()
        {
            Active?.PlayButtonClick();
        }

        public void PlayButtonClick()
        {
            EnsureAudioSource();
            ResolveButtonClickClip();
            if (audioSource != null && buttonClickClip != null)
            {
                audioSource.pitch = EvaluatePitch(Random.value, MinPitch, MaxPitch);
                audioSource.PlayOneShot(buttonClickClip, volumeScale);
                ButtonClickPlayed?.Invoke();
            }
        }

        public static float EvaluatePitch(
            float random01,
            float minPitch = DefaultMinPitch,
            float maxPitch = DefaultMaxPitch)
        {
            var low = Mathf.Min(minPitch, maxPitch);
            var high = Mathf.Max(minPitch, maxPitch);
            return Mathf.Lerp(low, high, Mathf.Clamp01(random01));
        }

        private void Awake()
        {
            ResolveSettings();
            EnsureAudioSource();
            ResolveButtonClickClip();
        }

        private void OnEnable()
        {
            Active = this;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            RefreshButtons();
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextButtonRefreshTime)
            {
                RefreshButtons();
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshButtons();
        }

        private void ResolveSettings()
        {
            if (audioSettings == null)
            {
                audioSettings = Project2048AudioSettings.LoadDefault();
            }
        }

        private void ResolveButtonClickClip()
        {
            ResolveSettings();
            if (buttonClickClip == null && audioSettings != null)
            {
                buttonClickClip = audioSettings.ButtonClickClip;
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                var bgmSource = GetComponent<PersistentBgmPlayer>()?.Source;
                foreach (var source in GetComponents<AudioSource>())
                {
                    if (source != null && source != bgmSource)
                    {
                        audioSource = source;
                        break;
                    }
                }
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            ResolveSettings();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.loop = false;
            audioSource.volume = 1f;
            audioSource.mute = false;
            audioSettings?.ApplyOutputGroup(audioSource, Project2048AudioChannel.UI);
        }
    }
}
