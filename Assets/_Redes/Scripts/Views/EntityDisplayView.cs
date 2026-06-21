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

        public void SetPosition(Vector2 screenPos)
        {
            var rect = transform as RectTransform;
            if (rect != null)
            {
                rect.position = screenPos;
            }
            else
            {
                transform.position = screenPos;
            }
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
