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

        [Header("Dynamic Room List (assigned by the Link tool)")]
        [SerializeField] private InputField _usernameInput;
        [SerializeField] private Transform _roomListContainer;
        [SerializeField] private GameObject _roomButtonPrefab; // Can be assigned or dynamically created

        [Header("Buttons (assigned by the Link tool)")]
        [SerializeField] private Button _hostButton;  // "Crear Sala"
        [SerializeField] private Button _joinButton;  // "Unirse a Sala"

        public Button HostButton => _hostButton;
        public Button JoinButton => _joinButton;
        
        public string Username => _usernameInput != null ? _usernameInput.text : "Player";

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
            // Join siempre arranca deshabilitado hasta que OnRoomAvailabilityChanged(true) llegue
            SetJoinButtonEnabled(false);
            ShowStatus("Buscando salas...");
        }

        /// <summary>
        /// Habilita o deshabilita el botón "Unirse a Sala".
        /// Llamado por GameFlowController cuando OnRoomAvailabilityChanged llega.
        /// </summary>
        public void SetJoinButtonEnabled(bool enabled)
        {
            if (_joinButton == null) return;
            _joinButton.interactable = enabled;
            RedesLog.Info(RedesLog.LOBBY,
                $"   LobbyView: boton UNIRSE {(enabled ? "HABILITADO ✓ (sala disponible)" : "DESHABILITADO (no hay sala)")}");
        }

        /// <summary>
        /// Oculta botones mientras se conecta / espera.
        /// </summary>
        public void HideButtons()
        {
            SetButtons(host: false, join: false);
            if (_roomListContainer != null) _roomListContainer.gameObject.SetActive(false);
        }

        public void PopulateRooms(System.Collections.Generic.List<Fusion.SessionInfo> sessions, System.Action<string> onJoinClicked)
        {
            if (_roomListContainer == null) return;
            
            _roomListContainer.gameObject.SetActive(true);

            // Clean up old children except the first one if it's a template? No, we will just destroy all text/buttons
            foreach (Transform child in _roomListContainer)
            {
                Destroy(child.gameObject);
            }

            if (sessions == null || sessions.Count == 0)
            {
                var noRoomsObj = new GameObject("NoRoomsText", typeof(RectTransform));
                noRoomsObj.transform.SetParent(_roomListContainer, false);
                var text = noRoomsObj.AddComponent<Text>();
                text.text = "No existen salas. Debes crear una.";
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 24;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                SetJoinButtonEnabled(false);
                return;
            }

            bool hasValidRoom = false;

            foreach (var session in sessions)
            {
                if (!session.IsOpen || !session.IsVisible) continue;
                
                hasValidRoom = true;

                var btnObj = new GameObject("RoomButton", typeof(RectTransform));
                btnObj.transform.SetParent(_roomListContainer, false);
                var btnRt = btnObj.GetComponent<RectTransform>();
                btnRt.sizeDelta = new Vector2(300, 40);
                
                var img = btnObj.AddComponent<Image>();
                img.color = new Color(0.2f, 0.6f, 0.3f);
                
                var btn = btnObj.AddComponent<Button>();
                string sessionName = session.Name;
                btn.onClick.AddListener(() => onJoinClicked?.Invoke(sessionName));

                var txtObj = new GameObject("Text", typeof(RectTransform));
                txtObj.transform.SetParent(btnObj.transform, false);
                var txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;
                
                var text = txtObj.AddComponent<Text>();
                text.text = $"Entrar a: {session.Name} ({session.PlayerCount}/{session.MaxPlayers})";
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 20;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
            }
            
            SetJoinButtonEnabled(hasValidRoom);
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
