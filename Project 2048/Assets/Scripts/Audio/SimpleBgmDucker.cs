using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Project2048.Audio
{
    public class SimpleBgmDucker : MonoBehaviour
    {
        public const float DefaultBaseVolumeDb = -14f;
        public const float DefaultDuckedVolumeDb = -20f;
        public const float DefaultAttackSeconds = 0.05f;
        public const float DefaultHoldSeconds = 0.15f;
        public const float DefaultReleaseSeconds = 0.35f;

        [SerializeField] private Project2048AudioSettings audioSettings;
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string bgmVolumeParameter = Project2048AudioSettings.BgmVolumeParameterName;
        [SerializeField] private float baseVolumeDb = DefaultBaseVolumeDb;
        [SerializeField] private float duckedVolumeDb = DefaultDuckedVolumeDb;
        [SerializeField] private float attackSeconds = DefaultAttackSeconds;
        [SerializeField] private float holdSeconds = DefaultHoldSeconds;
        [SerializeField] private float releaseSeconds = DefaultReleaseSeconds;

        private Coroutine duckRoutine;

        public static SimpleBgmDucker Active { get; private set; }
        public float BaseVolumeDb => baseVolumeDb;
        public float DuckedVolumeDb => duckedVolumeDb;
        public float AttackSeconds => attackSeconds;
        public float HoldSeconds => holdSeconds;
        public float ReleaseSeconds => releaseSeconds;
        public float CurrentBgmVolumeDb { get; private set; } = DefaultBaseVolumeDb;

        public void Initialize(Project2048AudioSettings settings)
        {
            audioSettings = settings;
            ResolveMixer();
            ApplyBaseVolume();
        }

        public void DuckBgm()
        {
            ResolveMixer();
            if (duckRoutine != null)
            {
                StopCoroutine(duckRoutine);
            }

            if (!isActiveAndEnabled)
            {
                ApplyBgmVolume(duckedVolumeDb);
                ApplyBaseVolume();
                duckRoutine = null;
                return;
            }

            duckRoutine = StartCoroutine(DuckRoutine(CurrentBgmVolumeDb));
        }

        public void ApplyBaseVolume()
        {
            ApplyBgmVolume(baseVolumeDb);
        }

        public static float EvaluateVolumeDb(
            float elapsedSeconds,
            float baseVolumeDb = DefaultBaseVolumeDb,
            float duckedVolumeDb = DefaultDuckedVolumeDb,
            float attackSeconds = DefaultAttackSeconds,
            float holdSeconds = DefaultHoldSeconds,
            float releaseSeconds = DefaultReleaseSeconds)
        {
            var elapsed = Mathf.Max(0f, elapsedSeconds);
            var attack = Mathf.Max(0f, attackSeconds);
            var hold = Mathf.Max(0f, holdSeconds);
            var release = Mathf.Max(0f, releaseSeconds);

            if (attack <= 0f)
            {
                if (elapsed <= hold)
                {
                    return duckedVolumeDb;
                }
            }
            else if (elapsed < attack)
            {
                return Mathf.Lerp(baseVolumeDb, duckedVolumeDb, elapsed / attack);
            }

            var holdEnd = attack + hold;
            if (elapsed < holdEnd)
            {
                return duckedVolumeDb;
            }

            if (release <= 0f)
            {
                return baseVolumeDb;
            }

            var releaseT = Mathf.Clamp01((elapsed - holdEnd) / release);
            return Mathf.Lerp(duckedVolumeDb, baseVolumeDb, releaseT);
        }

        private void Awake()
        {
            ResolveMixer();
            ApplyBaseVolume();
        }

        private void OnEnable()
        {
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }

            if (duckRoutine != null)
            {
                StopCoroutine(duckRoutine);
                duckRoutine = null;
            }

            ApplyBaseVolume();
        }

        private IEnumerator DuckRoutine(float startVolumeDb)
        {
            var attack = Mathf.Max(0f, attackSeconds);
            var release = Mathf.Max(0f, releaseSeconds);

            if (attack > 0f)
            {
                var elapsed = 0f;
                while (elapsed < attack)
                {
                    elapsed += Time.unscaledDeltaTime;
                    ApplyBgmVolume(Mathf.Lerp(startVolumeDb, duckedVolumeDb, Mathf.Clamp01(elapsed / attack)));
                    yield return null;
                }
            }

            ApplyBgmVolume(duckedVolumeDb);

            var holdEnd = Time.unscaledTime + Mathf.Max(0f, holdSeconds);
            while (Time.unscaledTime < holdEnd)
            {
                yield return null;
            }

            if (release > 0f)
            {
                var elapsed = 0f;
                while (elapsed < release)
                {
                    elapsed += Time.unscaledDeltaTime;
                    ApplyBgmVolume(Mathf.Lerp(duckedVolumeDb, baseVolumeDb, Mathf.Clamp01(elapsed / release)));
                    yield return null;
                }
            }

            ApplyBaseVolume();
            duckRoutine = null;
        }

        private void ResolveMixer()
        {
            if (audioSettings == null)
            {
                audioSettings = Project2048AudioSettings.LoadDefault();
            }

            if (audioSettings == null)
            {
                return;
            }

            if (audioMixer == null)
            {
                audioMixer = audioSettings.MasterMixer;
            }

            bgmVolumeParameter = audioSettings.BgmVolumeParameter;
        }

        private void ApplyBgmVolume(float volumeDb)
        {
            CurrentBgmVolumeDb = volumeDb;
            if (audioMixer != null && !string.IsNullOrWhiteSpace(bgmVolumeParameter))
            {
                audioMixer.SetFloat(bgmVolumeParameter, volumeDb);
            }
        }
    }
}
