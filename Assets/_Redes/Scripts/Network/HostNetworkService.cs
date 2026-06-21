using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using Redes.Core;

namespace Redes.Network
{
    /// <summary>
    /// CORE OF THE HOST ARCHITECTURE.
    /// StartAsHost()   -> crea sala en GameMode.Host  (botón "Crear Sala")
    /// StartAsClient() -> se une en GameMode.Client   (botón "Unirse a Sala")
    /// </summary>
    [RequireComponent(typeof(NetworkRunner))]
    public class HostNetworkService : MonoBehaviour, INetworkService, INetworkRunnerCallbacks
    {
        [Header("Spawning (assigned by Tools > Redes > Link & Assign All)")]
        [SerializeField] private PlayerSpawner _playerSpawner;

        [Header("Networked Player Prefab (Fusion). Assigned by the Link tool.")]
        [SerializeField] private NetworkObject _playerPrefab;

        private NetworkRunner _runner;

        public bool IsRunning { get; private set; }
        public int ConnectedPlayers { get; private set; }

        public event Action OnHostStarted;
        public event Action<int> OnPlayerCountChanged;
        public event Action OnEnoughPlayersToStart;

        public NetworkRunner Runner => _runner;
        public NetworkObject PlayerPrefab => _playerPrefab;

        private void Awake()
        {
            _runner = GetComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this); // Solo se registra UNA vez aquí
        }

        // ------------------------------------------------------------------ //
        //  CREAR SALA  (Player 1 - Botón "Crear Sala")
        // ------------------------------------------------------------------ //
        public async void StartAsHost()
        {
            if (IsRunning || (_runner != null && _runner.IsRunning))
            {
                RedesLog.Warn(RedesLog.NET, "[HostNetworkService.StartAsHost] Runner ya está corriendo, ignorado.");
                return;
            }

            try
            {
                RedesLog.Info(RedesLog.NET, "Inicio el juego");

                var sceneMgr = GetComponent<NetworkSceneManagerDefault>() ?? gameObject.AddComponent<NetworkSceneManagerDefault>();
                var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

                // REQUIRED LOG -> "Se creo una sala llamada X"
                RedesLog.Info(RedesLog.LOBBY, $"Se creo una sala llamada {GameConstants.DEFAULT_ROOM_NAME}");
                // REQUIRED LOG -> "Se esta esperando al otro jugador"
                RedesLog.Info(RedesLog.LOBBY, "Se esta esperando al otro jugador");

                var result = await _runner.StartGame(new StartGameArgs
                {
                    GameMode   = GameMode.Host,
                    SessionName = GameConstants.DEFAULT_ROOM_NAME,
                    PlayerCount = GameConstants.MAX_PLAYERS,
                    SceneManager = sceneMgr,
                    Scene = sceneRef
                });

                if (this == null || _runner == null) return;

                if (result.Ok)
                {
                    IsRunning = true;
                    OnHostStarted?.Invoke();
                }
                else
                {
                    RedesLog.Warn(RedesLog.NET, $"Error creando sala: {result.ShutdownReason}");
                }
            }
            catch (Exception ex)
            {
                RedesLog.Error(RedesLog.NET, $"[StartAsHost] {ex.Message}\n{ex.StackTrace}");
            }
        }

        // ------------------------------------------------------------------ //
        //  UNIRSE A SALA  (Player 2 - Botón "Unirse a Sala")
        // ------------------------------------------------------------------ //
        public async void StartAsClient()
        {
            if (IsRunning || (_runner != null && _runner.IsRunning))
            {
                RedesLog.Warn(RedesLog.NET, "[HostNetworkService.StartAsClient] Runner ya está corriendo, ignorado.");
                return;
            }

            try
            {
                RedesLog.Info(RedesLog.NET, "Uniéndose a sala como cliente");

                var sceneMgr = GetComponent<NetworkSceneManagerDefault>() ?? gameObject.AddComponent<NetworkSceneManagerDefault>();
                var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

                // REQUIRED LOG -> "Se unio a la sala de X nombre"
                RedesLog.Info(RedesLog.LOBBY,
                    $"Se unio a la sala de {GameConstants.DEFAULT_ROOM_NAME} nombre y modo Client");

                var result = await _runner.StartGame(new StartGameArgs
                {
                    GameMode    = GameMode.Client,
                    SessionName = GameConstants.DEFAULT_ROOM_NAME,
                    SceneManager = sceneMgr,
                    Scene = sceneRef
                });

                if (this == null || _runner == null) return;

                if (result.Ok)
                {
                    IsRunning = true;
                    OnHostStarted?.Invoke();
                }
                else
                {
                    RedesLog.Warn(RedesLog.NET, $"Error uniéndose a sala: {result.ShutdownReason}");
                }
            }
            catch (Exception ex)
            {
                RedesLog.Error(RedesLog.NET, $"[StartAsClient] {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void Shutdown()
        {
            RedesLog.Info(RedesLog.NET, "Cerrando el runner (Shutdown solicitado)");
            _runner.Shutdown();
            IsRunning = false;
        }

        private void RefreshPlayerCount()
        {
            ConnectedPlayers = _runner.ActivePlayers.Count();
            OnPlayerCountChanged?.Invoke(ConnectedPlayers);

            if (ConnectedPlayers >= GameConstants.MIN_PLAYERS_TO_START)
            {
                // REQUIRED LOG -> "se inicio el juego por q se tienen 2 jugadores"
                RedesLog.Info(RedesLog.NET, "se inicio el juego por q se tienen 2 jugadores");
                OnEnoughPlayersToStart?.Invoke();
            }
        }

        // ---- Fusion Callbacks ----

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $"Un jugador se unio (PlayerRef={player})");

            if (runner.IsServer)
            {
                if (_playerPrefab == null)
                {
                    RedesLog.Error(RedesLog.NET, "[OnPlayerJoined] _playerPrefab es null. Asignalo con Tools > Redes > 3. Link & Assign All");
                    return;
                }
                if (_playerSpawner == null)
                {
                    RedesLog.Error(RedesLog.NET, "[OnPlayerJoined] _playerSpawner es null. Asignalo con Tools > Redes > 3. Link & Assign All");
                    return;
                }
                _playerSpawner.SpawnPlayer(runner, player, _playerPrefab);
            }

            RefreshPlayerCount();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $"Un jugador se fue (PlayerRef={player})");
            if (runner.IsServer && _playerSpawner != null)
            {
                _playerSpawner.DespawnPlayer(runner, player);
            }
            RefreshPlayerCount();
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            RedesLog.Info(RedesLog.NET, "Conectado al servidor");
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            RedesLog.Warn(RedesLog.NET, $"Desconectado del servidor: {reason}");
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            RedesLog.Warn(RedesLog.NET, $"Runner apagado: {shutdownReason}");
            IsRunning = false;
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData();

            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            data.Move = new Vector2(moveX, moveY);

            Vector2 aimDir = Vector2.zero;
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float rayDistance))
                {
                    Vector3 worldPos = ray.GetPoint(rayDistance);
                    aimDir = new Vector2(worldPos.x, worldPos.z);
                }
            }
            data.AimDirection = aimDir;

            data.Buttons.Set(InputButton.Fire, Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space));
            data.Buttons.Set(InputButton.Reload, Input.GetKey(KeyCode.R));

            input.Set(data);
        }

        // Unused / stub callbacks (Fusion interface requirement)
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            int count = sessionList != null ? sessionList.Count : 0;
            RedesLog.Info(RedesLog.LOBBY, $"Se encontraron {count} salas");
        }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            RedesLog.Warn(RedesLog.NET, $"Fallo al conectar a {remoteAddress}: {reason}");
        }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
    }
}
