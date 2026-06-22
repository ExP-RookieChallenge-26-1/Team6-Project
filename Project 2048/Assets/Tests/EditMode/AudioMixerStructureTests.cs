using NUnit.Framework;
using Project2048.Audio;
using Project2048.Prototype;
using UnityEditor;
using UnityEngine;

namespace Project2048.Tests
{
    public class AudioMixerStructureTests
    {
        private readonly System.Collections.Generic.List<Object> ownedObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var ownedObject in ownedObjects)
            {
                if (ownedObject != null)
                {
                    Object.DestroyImmediate(ownedObject);
                }
            }

            ownedObjects.Clear();
            Project2048AudioPreferences.DeleteSavedVolumes();
            Project2048AudioPreferences.ApplySavedVolumes(LoadAudioSettings());
            PlayerPrefs.Save();
        }

        [Test]
        public void SimpleBgmDucker_DefaultsMatchSfxClarityTuning()
        {
            var gameObject = CreateOwnedGameObject("Ducker");
            var ducker = gameObject.AddComponent<SimpleBgmDucker>();

            Assert.That(ducker.BaseVolumeDb, Is.EqualTo(-14f).Within(0.001f));
            Assert.That(ducker.DuckedVolumeDb, Is.EqualTo(-20f).Within(0.001f));
            Assert.That(ducker.AttackSeconds, Is.EqualTo(0.05f).Within(0.001f));
            Assert.That(ducker.HoldSeconds, Is.EqualTo(0.15f).Within(0.001f));
            Assert.That(ducker.ReleaseSeconds, Is.EqualTo(0.35f).Within(0.001f));
        }

        [Test]
        public void SimpleBgmDucker_EvaluateVolumeCurve_DucksQuicklyThenReturnsNaturally()
        {
            Assert.That(SimpleBgmDucker.EvaluateVolumeDb(0f), Is.EqualTo(-14f).Within(0.001f));
            Assert.That(SimpleBgmDucker.EvaluateVolumeDb(0.05f), Is.EqualTo(-20f).Within(0.001f));
            Assert.That(SimpleBgmDucker.EvaluateVolumeDb(0.12f), Is.EqualTo(-20f).Within(0.001f));
            Assert.That(SimpleBgmDucker.EvaluateVolumeDb(0.375f), Is.EqualTo(-17f).Within(0.001f));
            Assert.That(SimpleBgmDucker.EvaluateVolumeDb(0.55f), Is.EqualTo(-14f).Within(0.001f));
        }

        [Test]
        public void Project2048AudioSettings_DefaultAssetDefinesMixerGroupsAndMainTheme()
        {
            var settings = LoadAudioSettings();
            var serializedSettings = SerializedObjectFor(settings);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.MasterMixer, Is.Not.Null);
            Assert.That(settings.MainThemeClip, Is.Not.Null);
            Assert.That(serializedSettings.FindProperty("buttonClickClip").objectReferenceValue, Is.Not.Null);
            Assert.That(settings.BgmGroup?.name, Is.EqualTo("BGM"));
            Assert.That(settings.SfxGroup?.name, Is.EqualTo("SFX"));
            Assert.That(settings.UiGroup?.name, Is.EqualTo("UI"));
            Assert.That(settings.AmbienceGroup?.name, Is.EqualTo("Ambience"));
            Assert.That(settings.BgmVolumeParameter, Is.EqualTo(Project2048AudioSettings.BgmVolumeParameterName));
        }

        [Test]
        public void RuntimeAudioRouting_AssignsKnownCombatSourcesToSfxMixerGroup()
        {
            var settings = LoadAudioSettings();
            var duckerObject = CreateOwnedGameObject("Ducker");
            var ducker = duckerObject.AddComponent<SimpleBgmDucker>();
            ducker.Initialize(settings);
            var combatUiObject = CreateOwnedGameObject("CombatUi");
            var combatUiSource = combatUiObject.AddComponent<AudioSource>();
            var combatUi = combatUiObject.AddComponent<CombatUiView>();
            var worldObject = CreateOwnedGameObject("WorldSprites");
            var worldSource = worldObject.AddComponent<AudioSource>();
            var worldSprites = worldObject.AddComponent<CombatWorldSpriteView>();
            var eventObject = CreateOwnedGameObject("CombatEventAudio");
            var eventSource = eventObject.AddComponent<AudioSource>();
            var eventAudio = eventObject.AddComponent<PrototypeCombatEventAudioPlayer>();

            Assert.That(settings, Is.Not.Null);
            combatUi.Initialize(null);
            InvokePrivate(worldSprites, "EnsureAudioSource");
            InvokePrivate(eventAudio, "EnsureAudioDefaults");

            Assert.That(combatUiSource.outputAudioMixerGroup, Is.SameAs(settings.SfxGroup));
            Assert.That(worldSource.outputAudioMixerGroup, Is.SameAs(settings.SfxGroup));
            Assert.That(eventSource.outputAudioMixerGroup, Is.SameAs(settings.SfxGroup));
            Assert.That(SerializedObjectFor(combatUi).FindProperty("bgmDucker").objectReferenceValue, Is.SameAs(ducker));
            Assert.That(SerializedObjectFor(worldSprites).FindProperty("bgmDucker").objectReferenceValue, Is.SameAs(ducker));
            Assert.That(SerializedObjectFor(eventAudio).FindProperty("bgmDucker").objectReferenceValue, Is.SameAs(ducker));
        }

        [Test]
        public void ButtonClickAudioRouter_AttachesEmitterToSceneButtonsAndRoutesToUiGroup()
        {
            var settings = LoadAudioSettings();
            var routerType = System.Type.GetType("Project2048.Audio.ButtonClickAudioRouter, Game.Core");
            var emitterType = System.Type.GetType("Project2048.Audio.ButtonClickAudioEmitter, Game.Core");
            var root = CreateOwnedGameObject("ButtonAudioRoot");
            var source = root.AddComponent<AudioSource>();
            var buttonObject = CreateOwnedGameObject("Button");
            buttonObject.AddComponent<UnityEngine.UI.Image>();
            buttonObject.AddComponent<UnityEngine.UI.Button>();

            Assert.That(routerType, Is.Not.Null);
            Assert.That(emitterType, Is.Not.Null);
            var router = (MonoBehaviour)root.AddComponent(routerType);
            routerType.GetMethod("Initialize")?.Invoke(router, new object[] { settings });
            routerType.GetMethod("RefreshButtons")?.Invoke(router, null);

            Assert.That(source.outputAudioMixerGroup, Is.SameAs(settings.UiGroup));
            Assert.That(buttonObject.GetComponent(emitterType), Is.Not.Null);
        }

        [Test]
        public void ButtonClickAudioRouter_DefaultPitchVariationStaysSubtle()
        {
            Assert.That(ButtonClickAudioRouter.DefaultMinPitch, Is.EqualTo(0.995f).Within(0.001f));
            Assert.That(ButtonClickAudioRouter.DefaultMaxPitch, Is.EqualTo(1.005f).Within(0.001f));
            Assert.That(ButtonClickAudioRouter.EvaluatePitch(0f), Is.EqualTo(0.995f).Within(0.001f));
            Assert.That(ButtonClickAudioRouter.EvaluatePitch(0.5f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(ButtonClickAudioRouter.EvaluatePitch(1f), Is.EqualTo(1.005f).Within(0.001f));
            Assert.That(ButtonClickAudioRouter.EvaluatePitch(0.5f, 1.005f, 0.995f), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void ButtonClickAudioRouter_RebindsAfterRuntimeButtonListenersAreReplaced()
        {
            var settings = LoadAudioSettings();
            var root = CreateOwnedGameObject("ButtonAudioRoot");
            var router = root.AddComponent<ButtonClickAudioRouter>();
            var buttonObject = CreateOwnedGameObject("RuntimeButton");
            buttonObject.AddComponent<UnityEngine.UI.Image>();
            var button = buttonObject.AddComponent<UnityEngine.UI.Button>();
            var playCount = 0;

            ButtonClickAudioRouter.ButtonClickPlayed += CountPlay;
            try
            {
                router.Initialize(settings);
                router.RefreshButtons();
                button.onClick.Invoke();

                button.onClick.RemoveAllListeners();
                router.RefreshButtons();
                button.onClick.Invoke();
            }
            finally
            {
                ButtonClickAudioRouter.ButtonClickPlayed -= CountPlay;
            }

            Assert.That(playCount, Is.EqualTo(2));

            void CountPlay()
            {
                playCount++;
            }
        }

        [Test]
        public void ButtonClickAudioEmitter_PlaysWhenEarlierHandlerDisablesButton()
        {
            var settings = LoadAudioSettings();
            var root = CreateOwnedGameObject("ButtonAudioRoot");
            root.AddComponent<AudioSource>();
            var router = root.AddComponent<ButtonClickAudioRouter>();
            var buttonObject = CreateOwnedGameObject("SelfDisablingButton");
            buttonObject.AddComponent<UnityEngine.UI.Image>();
            var button = buttonObject.AddComponent<UnityEngine.UI.Button>();
            var playCount = 0;

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.ButtonClickClip, Is.Not.Null);

            button.onClick.AddListener(() => buttonObject.SetActive(false));
            router.Initialize(settings);

            ButtonClickAudioRouter.ButtonClickPlayed += CountPlay;
            try
            {
                button.onClick.Invoke();
            }
            finally
            {
                ButtonClickAudioRouter.ButtonClickPlayed -= CountPlay;
            }

            Assert.That(playCount, Is.EqualTo(1));

            void CountPlay()
            {
                playCount++;
            }
        }

        [Test]
        public void Project2048AudioBootstrap_CreatesPersistentBgmPlayerAndDucker()
        {
            var settings = LoadAudioSettings();

            var root = Project2048AudioBootstrap.EnsureAudioRoot();
            ownedObjects.Add(root);

            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponent<SimpleBgmDucker>(), Is.Not.Null);
            Assert.That(root.GetComponent(System.Type.GetType("Project2048.Audio.ButtonClickAudioRouter, Game.Core")), Is.Not.Null);

            var player = root.GetComponent<PersistentBgmPlayer>();
            Assert.That(player, Is.Not.Null);
            Assert.That(player.MainThemeClip, Is.SameAs(settings.MainThemeClip));
            Assert.That(player.Source.outputAudioMixerGroup, Is.SameAs(settings.BgmGroup));
        }

        [Test]
        public void Project2048AudioPreferences_ConvertsNormalizedVolumeWithChannelDefaults()
        {
            Assert.That(
                Project2048AudioPreferences.VolumeToDb(Project2048AudioChannel.BGM, 1f),
                Is.EqualTo(-14f).Within(0.001f));
            Assert.That(
                Project2048AudioPreferences.VolumeToDb(Project2048AudioChannel.SFX, 1f),
                Is.EqualTo(0f).Within(0.001f));
            Assert.That(
                Project2048AudioPreferences.VolumeToDb(Project2048AudioChannel.SFX, 0.5f),
                Is.EqualTo(-6.0206f).Within(0.001f));
            Assert.That(
                Project2048AudioPreferences.VolumeToDb(Project2048AudioChannel.UI, 0f),
                Is.EqualTo(Project2048AudioSettings.MinVolumeDb).Within(0.001f));
            Assert.That(
                Project2048AudioPreferences.OffsetDb(Project2048AudioSettings.MinVolumeDb, -6f),
                Is.EqualTo(Project2048AudioSettings.MinVolumeDb).Within(0.001f));
        }

        [Test]
        public void Project2048AudioPreferences_SetNormalizedVolumePersistsAndAppliesMixer()
        {
            var settings = LoadAudioSettings();
            const float normalizedVolume = 0.25f;

            Project2048AudioPreferences.SetNormalizedVolume(
                settings,
                Project2048AudioChannel.SFX,
                normalizedVolume,
                saveImmediately: false);

            Assert.That(
                Project2048AudioPreferences.GetNormalizedVolume(Project2048AudioChannel.SFX),
                Is.EqualTo(normalizedVolume).Within(0.001f));
            Assert.That(settings.MasterMixer.GetFloat(settings.SfxVolumeParameter, out var sfxVolumeDb), Is.True);
            Assert.That(sfxVolumeDb, Is.EqualTo(-12.0412f).Within(0.001f));
        }

        [Test]
        public void SimpleBgmDucker_ApplyBaseVolumeUsesSavedPreference()
        {
            var settings = LoadAudioSettings();
            const float normalizedVolume = 0.5f;
            Project2048AudioPreferences.SetNormalizedVolume(
                settings,
                Project2048AudioChannel.BGM,
                normalizedVolume,
                saveImmediately: false);

            var gameObject = CreateOwnedGameObject("Ducker");
            var ducker = gameObject.AddComponent<SimpleBgmDucker>();
            ducker.Initialize(settings);

            Assert.That(
                ducker.CurrentBgmVolumeDb,
                Is.EqualTo(Project2048AudioPreferences.VolumeToDb(Project2048AudioChannel.BGM, normalizedVolume))
                    .Within(0.001f));
            Assert.That(ducker.BaseVolumeDb, Is.EqualTo(-14f).Within(0.001f));
            Assert.That(ducker.DuckedVolumeDb, Is.EqualTo(-20f).Within(0.001f));
        }

        private GameObject CreateOwnedGameObject(string name)
        {
            var gameObject = new GameObject(name);
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private static Project2048AudioSettings LoadAudioSettings()
        {
            return AssetDatabase.LoadAssetAtPath<Project2048AudioSettings>(
                "Assets/Resources/Audio/Project2048AudioSettings.asset");
        }

        private static SerializedObject SerializedObjectFor(Object target)
        {
            Assert.That(target, Is.Not.Null);
            return new SerializedObject(target);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(target, null);
        }
    }
}
