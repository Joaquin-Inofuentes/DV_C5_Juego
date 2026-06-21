using UnityEngine;
using UnityEngine.UI;
using Redes.Core;

namespace Redes.Views
{
    /// <summary>
    /// MVC - VIEW para el lobby.
    /// Muestra dos botones explícitos: "Crear Sala" y "Unirse a Sala".
    /// El GameFlowController decide cuál acción ejecutar.
    /// </summary>
    public class LobbyView : MonoBehaviour
    {
        [Header("Legacy Text refs (assigned by Tools > Redes > Link & Assign All)")]
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _playerCountText;

        [Header("Buttons (assigned by the Link tool)")]
        [SerializeField] private Button _hostButton;  // "Crear Sala"
        [SerializeField] private Button _joinButton;  // "Unirse a Sala"

        public Button HostButton => _hostButton;
        public Button JoinButton => _joinButton;

        public void ShowStatus(string message)
        {
            if (_statusText != null) _statusText.text = message;
        }

        public void ShowPlayerCount(int current)
        {
            if (_playerCountText != null)
                _playerCountText.text = $"Jugadores: {current}/{GameConstants.MIN_PLAYERS_TO_START}";
        }

        /// <summary>
        /// Muestra solo los botones de selección (estado inicial).
        /// </summary>
        public void ShowButtons()
        {
            SetButtons(host: true, join: true);
            ShowStatus("¿Crear sala o unirse?");
        }

        /// <summary>
        /// Oculta botones mientras se conecta / espera.
        /// </summary>
        public void HideButtons()
        {
            SetButtons(host: false, join: false);
        }

        public void SetVisible(bool visible)
        {
            RedesLog.Info(RedesLog.BOOT, $">> LobbyView.SetVisible({visible})");
            gameObject.SetActive(visible);
        }

        public void ShowError(string message)
        {
            SetButtons(false, false);
            ShowStatus($"[ERROR] {message}");
            RedesLog.Error(RedesLog.BOOT, $"LobbyView.ShowError: {message}");
            // Re-show buttons after a moment so user can retry
            Invoke(nameof(RestoreButtons), 3f);
        }

        private void RestoreButtons()
        {
            ShowButtons();
        }

        private void SetButtons(bool host, bool join)
        {
            if (_hostButton != null) _hostButton.gameObject.SetActive(host);
            if (_joinButton != null) _joinButton.gameObject.SetActive(join);
        }
    }
}
