using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace Redes.Views
{
    [RequireComponent(typeof(Button))]
    public class PlaySoundOnButtonClick : MonoBehaviour
    {
        [SerializeField] private AudioClip _clickSound;
        [SerializeField] private AudioMixerGroup _sfxGroup;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(PlayClickSound);
            }
        }

        private void PlayClickSound()
        {
            if (_clickSound == null) return;

            // Route audio through the SFX group correctly
            GameObject tempGo = new GameObject("TempUI_ClickSound");
            AudioSource source = tempGo.AddComponent<AudioSource>();
            source.clip = _clickSound;
            source.outputAudioMixerGroup = _sfxGroup;
            source.volume = 0.8f;
            source.spatialBlend = 0f; // UI is 2D
            source.Play();
            Destroy(tempGo, _clickSound.length + 0.1f);
        }
    }
}
