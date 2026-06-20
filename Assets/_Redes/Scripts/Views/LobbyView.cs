using UnityEngine;
using UnityEngine.UI; // Legacy Text (Text component), as requested.
using Redes.Core;

namespace Redes.Views
{
    /// <summary>
    /// MVC - VIEW for the lobby / connection state. ONLY draws UI.
    /// Uses legacy UnityEngine.UI.Text (requirement).
    /// The Link tool assigns these Text references from the scene Canvas.
    /// </summary>
    public class LobbyView : MonoBehaviour
    {
        [Header("Legacy Text refs (assigned by Tools > Redes > Link & Assign All)")]
        [SerializeField] private Text _statusText;     // "Esperando jugadores...", etc.
        [SerializeField] private Text _playerCountText; // "Jugadores: 1/2"

        [Header("Buttons (optional, assigned by Link tool)")]
        [SerializeField] private Button _hostButton;   // Starts the Host flow.

        /// <summary>Exposed so GameFlowController can wire the click. Logic by other agent.</summary>
        public Button HostButton => _hostButton;

        public void ShowStatus(string message)
        {
            if (_statusText != null) _statusText.text = message;
            // TODO (other agent): any animation / show-hide.
        }

        public void ShowPlayerCount(int current)
        {
            if (_playerCountText != null)
                _playerCountText.text = $"Jugadores: {current}/{GameConstants.MIN_PLAYERS_TO_START}";
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
