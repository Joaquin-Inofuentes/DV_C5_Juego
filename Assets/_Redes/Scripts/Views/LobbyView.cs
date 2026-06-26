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
        [SerializeField] private Font _buttonFont;

        [Header("Buttons (assigned by the Link tool)")]
        [SerializeField] private Button _hostButton;  // "Crear Sala"

        [Header("Generic Name Buttons")]
        [SerializeField] private Button _ramboButton;
        [SerializeField] private Button _t600Button;
        [SerializeField] private Button _lionButton;

        public Button HostButton => _hostButton;
        
        public string Username => _usernameInput != null ? _usernameInput.text : "Player";

        private void Start()
        {
            // Relying on persistent events linked via editor tools
        }

        public void ApplyButtonStyle(Button button)
        {
            if (button == null) return;
            var img = button.GetComponent<Image>();
            if (img == null) img = button.gameObject.AddComponent<Image>();

            // Obtener el sprite de fondo del _ramboButton si está asignado
            Sprite btnSprite = null;
            if (_ramboButton != null)
            {
                var ramboImg = _ramboButton.GetComponent<Image>();
                if (ramboImg != null) btnSprite = ramboImg.sprite;
            }
            if (btnSprite != null)
            {
                img.sprite = btnSprite;
            }

            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.54f;

            button.targetGraphic = img;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock cb = button.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 1f);
            cb.highlightedColor = new Color(1f, 0.2f, 0.2f, 1f);
            cb.pressedColor = new Color(0.08f, 0.1f, 0.16f, 1f);
            cb.selectedColor = new Color(1f, 0.2f, 0.2f, 1f);
            cb.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.1f;
            button.colors = cb;
            
            // Asignar el sonido del click
            var soundClick = button.GetComponent<PlaySoundOnButtonClick>();
            if (soundClick == null)
            {
                soundClick = button.gameObject.AddComponent<PlaySoundOnButtonClick>();
                if (_ramboButton != null)
                {
                    var ramboClick = _ramboButton.GetComponent<PlaySoundOnButtonClick>();
                    if (ramboClick != null)
                    {
                        soundClick.Initialize(ramboClick.ClickSound, ramboClick.SfxGroup);
                    }
                }
            }

            // Aplicar tamaño automático (BestFit/AutoSizing), anchors de estiramiento completo y alineación al centro
            var childTexts = button.GetComponentsInChildren<Text>(true);
            foreach (var txt in childTexts)
            {
                if (_buttonFont != null) txt.font = _buttonFont;
                var rt = txt.rectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = Vector2.zero;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                }
                txt.resizeTextForBestFit = true;
                txt.resizeTextMinSize = 10;
                txt.resizeTextMaxSize = 40;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
            }

            var childTmpTexts = button.GetComponentsInChildren<TMPro.TMP_Text>(true);
            foreach (var tmp in childTmpTexts)
            {
                var rt = tmp.rectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = Vector2.zero;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                }
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 10f;
                tmp.fontSizeMax = 40f;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
            }
        }

        public void SetUsername(string name)
        {
            if (_usernameInput != null)
            {
                _usernameInput.text = name;
            }
        }

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

        public void SetJoinButtonEnabled(bool enabled)
        {
            // JoinButton is dynamic under RoomList now, not a static button.
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
                text.font = _buttonFont != null ? _buttonFont : (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf"));
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
                text.font = _buttonFont != null ? _buttonFont : (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf"));
                text.fontSize = 20;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;

                // Aplicar estilo al botón dinámico (después de agregar el texto hijo)
                ApplyButtonStyle(btn);
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
        }
    }
}
