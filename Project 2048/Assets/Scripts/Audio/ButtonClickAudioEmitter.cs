using UnityEngine;
using UnityEngine.UI;

namespace Project2048.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class ButtonClickAudioEmitter : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            EnsureBound();
        }

        private void OnEnable()
        {
            EnsureBound();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayIfInteractable);
            }
        }

        public void EnsureBound()
        {
            button ??= GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(PlayIfInteractable);
            button.onClick.AddListener(PlayIfInteractable);
        }

        private void PlayIfInteractable()
        {
            button ??= GetComponent<Button>();
            if (button != null && button.IsActive() && button.IsInteractable())
            {
                ButtonClickAudioRouter.PlayGlobal();
            }
        }
    }
}
