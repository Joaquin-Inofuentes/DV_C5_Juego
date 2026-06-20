using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using Redes.Core;

namespace Redes.Network
{
    /// <summary>
    /// CORE OF THE HOST ARCHITECTURE.
    /// </summary>
    [RequireComponent(typeof(NetworkRunner))]
    public class HostNetworkService : MonoBehaviour, INetworkService, INetworkRunnerCallbacks
    {
        [Header("Spawning (assigned by Tools > Redes > Link & Assign All)")]
        [Tooltip("Spawns the player NetworkObject when a client joins.")]
        [SerializeField] private PlayerSpawner _playerSpawner;

        [Header("Networked Player Prefab (Fusion). Assigned by the Link tool.")]
        [Tooltip("Player prefab NetworkObject. Runner.Spawn accepts it directly in Fusion 2.")]
        [SerializeField] private NetworkObject _playerPrefab;

        private NetworkRunner _runner;
        private ISessionListHandler _sessionHandler;

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
            _sessionHandler = new RoomSessionHandler(this);
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
        }

        public async void StartAsHost()
        {
            if (IsRunning || (_runner != null && _runner.IsRunning))
            {
                RedesLog.Warn(RedesLog.NET, "[HostNetworkService.StartAsHost] Ignorado: El NetworkRunner ya está corriendo.");
                return;
            }

            try
            {
                // REQUIRED LOG -> "Inicio el juego"
                RedesLog.Info(RedesLog.NET, "Inicio el juego");

                _runner.AddCallbacks(this);
                var sceneMgr = GetComponent<NetworkSceneManagerDefault>() ?? gameObject.AddComponent<NetworkSceneManagerDefault>();
                
                // Join lobby first to receive the session list and trigger the RoomSessionHandler decision.
                var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
                if (this == null || _runner == null) return;
                
                if (result.Ok)
                {
                    IsRunning = true;
                    OnHostStarted?.Invoke();
                }
                else
                {
                    RedesLog.Warn(RedesLog.NET, $"Error joining lobby: {result.ShutdownReason}");
                }
            }
            catch (Exception ex)
            {
                RedesLog.Error(RedesLog.NET, $"[HostNetworkService.StartAsHost] Excepción capturada en método. Mensaje: {ex.Message}\nCallstack:\n{ex.StackTrace}");
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

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            int count = sessionList != null ? sessionList.Count : 0;

            // REQUIRED LOG -> "Se encontraron 0 salas" / "Se encontraron X salas"
            RedesLog.Info(RedesLog.LOBBY, $"Se encontraron {count} salas");

            _sessionHandler?.HandleSessionList(runner, sessionList);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $"Un jugador se unio (PlayerRef={player})");

            if (runner.IsServer)
            {
                _playerSpawner.SpawnPlayer(runner, player, _playerPrefab);
            }

            RefreshPlayerCount();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $"Un jugador se fue (PlayerRef={player})");
            if (runner.IsServer)
            {
                _playerSpawner.DespawnPlayer(runner, player);
            }
            RefreshPlayerCount();
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            RedesLog.Info(RedesLog.NET, "Conectado al servidor (Host)");
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

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
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
