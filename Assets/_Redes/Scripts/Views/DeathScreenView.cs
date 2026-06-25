using UnityEngine;
using UnityEngine.UI;

namespace Redes.Views
{
    public class DeathScreenView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Image _radialCircle;
        [SerializeField] private Text _countdownText;

        public void SetVisible(bool visible)
        {
            if (_panel != null)
            {
                _panel.SetActive(visible);
            }
        }

        public void UpdateProgress(float progress, float secondsLeft)
        {
            if (_radialCircle != null)
            {
                _radialCircle.fillAmount = progress;
            }
            if (_countdownText != null)
            {
                _countdownText.text = secondsLeft.ToString("F1") + "s";
            }
        }
    }
}
