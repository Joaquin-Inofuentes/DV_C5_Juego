using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using Redes.Core;

namespace Redes.Network
{
    /// <summary>
    /// CORE OF THE HOST ARCHITECTURE.
    ///
    /// Responsibilities (kept narrow on purpose - SOLID/SRP):
    ///   1. Own the Photon Fusion NetworkRunner.
    ///   2. Start it in GameMode.Host.
    ///   3. Receive every INetworkRunnerCallbacks event.
    ///   4. Translate raw Fusion events into clean C# events (Observer pattern)
    ///      that the MVC controllers subscribe to.
    ///
    /// It does NOT decide create-vs-join (delegated to ISessionListHandler) and
    /// it does NOT spawn players (delegated to PlayerSpawner). This keeps each
    /// class single-purpose.
    ///
    /// NOTE: method BODIES are intentionally left as stubs (logs + TODO).
    /// Another agent implements the actual Fusion logic.
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

        // The Fusion runner that drives the whole simulation.
        private NetworkRunner _runner;

        // SOLID: the create/join decision lives behind this abstraction.
        private ISessionListHandler _sessionHandler;

        // ---- INetworkService ----
        public bool IsRunning { get; private set; }
        public int ConnectedPlayers { get; private set; }

        public event Action OnHostStarted;
        public event Action<int> OnPlayerCountChanged;
        public event Action OnEnoughPlayersToStart;

        // Exposed so the spawner / handler can reach the runner. Read-only.
        public NetworkRunner Runner => _runner;
        public NetworkObject PlayerPrefab => _playerPrefab;

        private void Awake()
        {
            _runner = GetComponent<NetworkRunner>();
            // Default handler. Could be replaced/injected for tests (DIP).
            _sessionHandler = new RoomSessionHandler(this);
            // TODO (other agent): _runner.ProvideInput = true; _runner.AddCallbacks(this);
        }

        // ===== INetworkService implementation =====

        /// <summary>
        /// Entry point of the Host flow. Required log: "Inicio el juego".
        /// </summary>
        public void StartAsHost()
        {
            // REQUIRED LOG -> "Inicio el juego"
            RedesLog.Info(RedesLog.NET, "Inicio el juego");

            // TODO (other agent): start the runner in Host mode, e.g.:
            // _runner.AddCallbacks(this);
            // await _runner.StartGame(new StartGameArgs {
            //     GameMode = GameMode.Host,
            //     SessionName = GameConstants.DEFAULT_ROOM_NAME,
            //     PlayerCount = GameConstants.MAX_PLAYERS,
            //     SceneManager = GetComponent<NetworkSceneManagerDefault>()
            // });
            // IsRunning = true; OnHostStarted?.Invoke();
        }

        public void Shutdown()
        {
            RedesLog.Info(RedesLog.NET, "Cerrando el runner (Shutdown solicitado)");
            // TODO (other agent): _runner.Shutdown();
            IsRunning = false;
        }

        // Helper used by OnPlayerJoined/Left to keep the count + events in sync.
        private void RefreshPlayerCount()
        {
            // TODO (other agent): ConnectedPlayers = _runner.ActivePlayers.Count();
            OnPlayerCountChanged?.Invoke(ConnectedPlayers);

            if (ConnectedPlayers >= GameConstants.MIN_PLAYERS_TO_START)
            {
                // REQUIRED LOG -> "se inicio el juego por q se tienen 2 jugadores"
                RedesLog.Info(RedesLog.NET, "se inicio el juego por q se tienen 2 jugadores");
                OnEnoughPlayersToStart?.Invoke();
            }
        }

        // ===== INetworkRunnerCallbacks (Fusion 2) =====

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            int count = sessionList != null ? sessionList.Count : 0;

            // REQUIRED LOG -> "Se encontraron 0 salas" / "Se encontraron X salas"
            RedesLog.Info(RedesLog.LOBBY, $"Se encontraron {count} salas");

            // Delegate the create-vs-join decision (SOLID/SRP).
            _sessionHandler?.HandleSessionList(runner, sessionList);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $"Un jugador se unio (PlayerRef={player})");

            // Only the Host spawns objects in a Host architecture.
            if (runner.IsServer)
            {
                // TODO (other agent): _playerSpawner.SpawnPlayer(runner, player, _playerPrefab);
            }

            RefreshPlayerCount();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $"Un jugador se fue (PlayerRef={player})");
            // TODO (other agent): _playerSpawner.DespawnPlayer(runner, player);
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

        // ---- Remaining callbacks: required by the interface, no logic needed here. ----
        public void OnInput(NetworkRunner runner, NetworkInput input) { /* TODO: read input for movement/shoot/reload */ }
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
