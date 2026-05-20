using UnityEngine;

namespace Project2048.Audio
{
    public static class Project2048AudioBootstrap
    {
        private const string AudioRootName = "Project2048Audio";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapAudio()
        {
            EnsureAudioRoot();
        }

        public static GameObject EnsureAudioRoot()
        {
            var existingPlayer = Object.FindAnyObjectByType<PersistentBgmPlayer>(FindObjectsInactive.Include);
            if (existingPlayer != null)
            {
                EnsureButtonClickRouter(existingPlayer.gameObject, Project2048AudioSettings.LoadDefault());
                return existingPlayer.gameObject;
            }

            var settings = Project2048AudioSettings.LoadDefault();
            if (settings == null)
            {
                return null;
            }

            var root = new GameObject(AudioRootName);
            Object.DontDestroyOnLoad(root);

            var ducker = root.AddComponent<SimpleBgmDucker>();
            ducker.Initialize(settings);

            var bgmPlayer = root.AddComponent<PersistentBgmPlayer>();
            bgmPlayer.Initialize(settings);

            EnsureButtonClickRouter(root, settings);

            return root;
        }

        private static void EnsureButtonClickRouter(GameObject root, Project2048AudioSettings settings)
        {
            if (root == null)
            {
                return;
            }

            var buttonClickRouter = root.GetComponent<ButtonClickAudioRouter>();
            if (buttonClickRouter == null)
            {
                buttonClickRouter = root.AddComponent<ButtonClickAudioRouter>();
            }

            buttonClickRouter.Initialize(settings);
        }
    }
}
