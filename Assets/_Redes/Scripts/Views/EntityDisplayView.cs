using UnityEngine;
using UnityEngine.UI;

namespace Redes.Views
{
    /// <summary>
    /// MVC - VIEW representing a single UI overlay element (health bar slider).
    /// Scalable to display additional information in the future (e.g. name, ammunition).
    /// </summary>
    public class EntityDisplayView : MonoBehaviour
    {
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Text _nicknameText;
        [SerializeField] private Image _reloadFillImage;
        [SerializeField] private GameObject _reloadArea;

        public void SetHealth(float progress)
        {
            if (_healthSlider != null)
            {
                _healthSlider.value = Mathf.Clamp01(progress);
            }
        }

        public void SetNickname(string nickname)
        {
            if (_nicknameText != null)
            {
                _nicknameText.text = nickname;
            }
        }

        [Header("Positioning")]
        public float YOffset = -120f; // Ajuste para posicionar la barra debajo del personaje (4 o 5 veces el tamaño habitual hacia abajo)
        public float XOffset = 0f;

        public void SetPosition(Vector2 screenPos)
        {
            var rect = transform as RectTransform;
            if (rect != null)
            {
                rect.position = new Vector3(screenPos.x + XOffset, screenPos.y + YOffset, 0f);
            }
            else
            {
                transform.position = new Vector3(screenPos.x + XOffset, screenPos.y + YOffset, 0f);
            }
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        public void SetReloadProgress(float progress, bool isReloading)
        {
            if (_reloadArea != null)
            {
                if (_reloadArea.activeSelf != isReloading)
                {
                    _reloadArea.SetActive(isReloading);
                }
            }

            if (isReloading && _reloadFillImage != null)
            {
                _reloadFillImage.fillAmount = Mathf.Clamp01(progress);
            }
        }
    }
}
