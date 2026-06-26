using UnityEngine;
using UnityEngine.UI;

namespace Redes.Controllers
{
    /// <summary>
    /// Singleton Screen Manager that provides a full-screen loading overlay to block inputs
    /// during network connection, scene restarting, and match ending.
    /// </summary>
    public class RedesLoadingScreen : MonoBehaviour
    {
        private static RedesLoadingScreen _instance;
        public static RedesLoadingScreen Instance => _instance;

        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private Text _loadingText;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        public void ShowLoading(string message)
        {
            Debug.Log($"[SCREEN_MANAGER] ShowLoading: {message}");
            if (_loadingPanel != null)
            {
                _loadingPanel.SetActive(true);
            }
            if (_loadingText != null)
            {
                _loadingText.text = message;
            }
        }

        public void HideLoading()
        {
            Debug.Log("[SCREEN_MANAGER] HideLoading");
            if (_loadingPanel != null)
            {
                _loadingPanel.SetActive(false);
            }
        }
    }
}
