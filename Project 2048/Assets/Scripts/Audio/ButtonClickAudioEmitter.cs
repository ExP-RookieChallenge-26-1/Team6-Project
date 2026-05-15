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
                button.onClick.RemoveListener(PlayIfButtonExists);
            }
        }

        public void EnsureBound()
        {
            button ??= GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(PlayIfButtonExists);
            button.onClick.AddListener(PlayIfButtonExists);
        }

        private void PlayIfButtonExists()
        {
            button ??= GetComponent<Button>();
            if (button != null)
            {
                ButtonClickAudioRouter.PlayGlobal();
            }
        }
    }
}
