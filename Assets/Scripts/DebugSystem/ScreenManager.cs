using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace DebugSystem
{
    public class ScreenManager : MonoBehaviour
    {
        [Header("UI Panels")]
        public GameObject lobbyPanel;
        public GameObject hudPanel;
        public GameObject gameOverPanel;

        [Header("Game Over Settings")]
        public Text gameOverTitleText;
        public Text gameOverDetailsText;
        public Button restartButton;

        [Header("Lobby Settings")]
        public InputField usernameInput;
        public Button createRoomButton;
        public Button joinRoomButton;
        public Transform roomListContainer;
        public GameObject roomButtonPrefab;

        [Header("HUD Elements")]
        public Text playerHpText;
        public Text playerAmmoText;
        public Text currentStatusText;

        private void Start()
        {
            ShowLobby();

            if (createRoomButton != null)
            {
                createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            }

            if (joinRoomButton != null)
            {
                joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
            }
            
            EventBus.OnHealthSynced += UpdateHealthHUD;
            EventBus.OnAmmoUpdated += UpdateAmmoHUD;
            EventBus.OnStateCurrent += UpdateStatusHUD;
            EventBus.OnMatchFinished += ShowGameOver;

            StartCoroutine(LobbyUpdateLoop());
        }

        private void OnDestroy()
        {
            if (createRoomButton != null)
            {
                createRoomButton.onClick.RemoveListener(OnCreateRoomClicked);
            }

            if (joinRoomButton != null)
            {
                joinRoomButton.onClick.RemoveListener(OnJoinRoomClicked);
            }
            
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartGame);
            }

            EventBus.OnHealthSynced -= UpdateHealthHUD;
            EventBus.OnAmmoUpdated -= UpdateAmmoHUD;
            EventBus.OnStateCurrent -= UpdateStatusHUD;
            EventBus.OnMatchFinished -= ShowGameOver;
        }

        private System.Collections.IEnumerator LobbyUpdateLoop()
        {
            while (lobbyPanel != null && lobbyPanel.activeSelf)
            {
                UpdateLobbyUI();
                
                // Check if game has started via Host
                LocalNetworkMock.RoomData data = LocalNetworkMock.GetRoomData();
                if (data != null && data.GameStarted)
                {
                    SimulateGameStart(LocalNetworkMock.LocalPlayerName, data.HostName, data.Player2Name);
                    yield break;
                }
                
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void UpdateLobbyUI()
        {
            if (createRoomButton == null || joinRoomButton == null) return;
            
            Text createText = createRoomButton.GetComponentInChildren<Text>();
            Text joinText = joinRoomButton.GetComponentInChildren<Text>();

            if (LocalNetworkMock.IsHost)
            {
                joinRoomButton.gameObject.SetActive(false);
                createRoomButton.gameObject.SetActive(true);

                LocalNetworkMock.RoomData data = LocalNetworkMock.GetRoomData();
                if (data != null && data.Player2Ready)
                {
                    if (createText != null) createText.text = $"START GAME (Player 2: {data.Player2Name} joined)";
                    createRoomButton.interactable = true;
                }
                else
                {
                    if (createText != null) createText.text = "WAITING FOR PLAYER 2...";
                    createRoomButton.interactable = false;
                }
            }
            else
            {
                if (LocalNetworkMock.RoomExists())
                {
                    createRoomButton.gameObject.SetActive(false);
                    joinRoomButton.gameObject.SetActive(true);
                    if (joinText != null) joinText.text = "UNIRSE A SALA";
                }
                else
                {
                    createRoomButton.gameObject.SetActive(true);
                    joinRoomButton.gameObject.SetActive(false);
                    if (createText != null) createText.text = "CREAR SALA (HOST)";
                    createRoomButton.interactable = true;
                }
            }
        }

        public void ShowLobby()
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            if (hudPanel != null) hudPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        public void ShowHUD()
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(true);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        public void ShowGameOver(string result, int winningActor)
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = result == "Victory" ? "¡VICTORIA!" : "¡DERROTA!";
                gameOverTitleText.color = result == "Victory" ? Color.green : Color.red;
            }

            if (gameOverDetailsText != null)
            {
                gameOverDetailsText.text = $"El Jugador {winningActor} ha ganado la partida.";
            }

            // Host should clear room when returning to lobby or restarting
            if (LocalNetworkMock.IsHost)
            {
                LocalNetworkMock.ClearRoom();
            }
        }

        private void RestartGame()
        {
            if (LocalNetworkMock.IsHost)
            {
                LocalNetworkMock.ClearRoom();
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private string GetPlayerName()
        {
            if (usernameInput != null && !string.IsNullOrEmpty(usernameInput.text))
            {
                return usernameInput.text;
            }
            return "Player_" + UnityEngine.Random.Range(1000, 9999);
        }

        private void OnCreateRoomClicked()
        {
            string pName = GetPlayerName();

            if (LocalNetworkMock.IsHost)
            {
                // Host clicks "Start Game" after Player 2 joined
                LocalNetworkMock.StartGame();
            }
            else
            {
                // Create
                LocalNetworkMock.CreateRoom(pName);
            }
        }

        private void OnJoinRoomClicked()
        {
            string pName = GetPlayerName();
            if (LocalNetworkMock.RoomExists())
            {
                // Join
                LocalNetworkMock.JoinRoom(pName);
            }
        }

        private void SimulateGameStart(string localName, string hostName, string clientName)
        {
            Debug.Log($"[SYSTEM] Game Starting! Local Actor ID: {LocalNetworkMock.LocalActorID}");
            EventBus.TriggerNameEntered(localName);
            EventBus.TriggerRoomCreated("IPC_ROOM");
            
            // Simular entrada de jugadores
            EventBus.TriggerPlayerEntered(1, hostName);
            EventBus.TriggerPlayerEntered(2, clientName);
            
            // Cambiar estado a juego
            EventBus.TriggerStateCurrent("Playing");
            EventBus.TriggerStateTransition("Lobby", "Playing");
            
            ShowHUD();
        }

        private void UpdateHealthHUD(int actorId, float hp, float shield)
        {
            if (actorId == LocalNetworkMock.LocalActorID && playerHpText != null)
            {
                playerHpText.text = $"HP Jugador: {hp} / Escudo: {shield}";
            }
        }

        private void UpdateAmmoHUD(int actorId, string weaponName, int clip, int maxClip, int reserve)
        {
            if (actorId == LocalNetworkMock.LocalActorID && playerAmmoText != null)
            {
                playerAmmoText.text = $"Arma: {weaponName} | Munición: {clip}/{maxClip} (Reserva: {reserve})";
            }
        }

        private void UpdateStatusHUD(string state)
        {
            if (currentStatusText != null)
            {
                currentStatusText.text = $"Estado de Partida: {state}";
            }
        }
    }
}
