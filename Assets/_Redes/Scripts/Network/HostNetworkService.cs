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
    /// CALLSTACK USER 1 (HOST):
    ///   Awake → AddCallbacks
    ///   StartAsHost() → StartGame(Host) → [await Photon]
    ///     OK  → OnHostStarted fired → GameFlowController.HandleHostStarted → WaitingForPlayers
    ///     FAIL→ OnConnectionFailed fired → GameFlowController vuelve a Booting + muestra error
    ///   OnPlayerJoined(p1=host)  → SpawnPlayer(p1) → RefreshPlayerCount(1) → aun esperando
    ///   OnPlayerJoined(p2=client)→ SpawnPlayer(p2) → RefreshPlayerCount(2) → OnEnoughPlayersToStart
    ///
    /// CALLSTACK USER 2 (CLIENT):
    ///   Awake → AddCallbacks
    ///   StartAsClient() → StartGame(Client) → [await Photon, find session "RedesRoom"]
    ///     OK  → OnHostStarted fired → WaitingForPlayers
    ///     FAIL→ OnConnectionFailed fired → vuelve a Booting
    ///   OnConnectedToServer → log
    ///   OnPlayerJoined(p1), OnPlayerJoined(p2) → RefreshPlayerCount → OnEnoughPlayersToStart
    /// </summary>
    [RequireComponent(typeof(NetworkRunner))]
    public class HostNetworkService : MonoBehaviour, INetworkService, INetworkRunnerCallbacks
    {
        [Header("Spawning (assigned by Tools > Redes > Link & Assign All)")]
        [SerializeField] private PlayerSpawner _playerSpawner;

        [Header("Networked Player Prefab. Assigned by the Link tool.")]
        [SerializeField] private NetworkObject _playerPrefab;

        private NetworkRunner _runner;

        public bool IsRunning { get; private set; }
        public int ConnectedPlayers { get; private set; }

        public event Action OnHostStarted;
        public event Action<int> OnPlayerCountChanged;
        public event Action OnEnoughPlayersToStart;
        public event Action<string> OnConnectionFailed;

        public NetworkRunner Runner => _runner;
        public NetworkObject PlayerPrefab => _playerPrefab;

        // ──────────────────────────────────────────────────────────────────
        private void Awake()
        {
            RedesLog.Info(RedesLog.NET, ">> HostNetworkService.Awake()");
            _runner = GetComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this); // una sola vez
            RedesLog.Info(RedesLog.NET, "<< HostNetworkService.Awake() - callbacks registrados");
        }

        // ──────────────────────────────────────────────────────────────────
        //  JUGADOR 1 — "CREAR SALA"
        // ──────────────────────────────────────────────────────────────────
        public async void StartAsHost()
        {
            RedesLog.Info(RedesLog.NET, ">> StartAsHost() - intentando crear sala");

            if (IsRunning || (_runner != null && _runner.IsRunning))
            {
                RedesLog.Warn(RedesLog.NET, "<< StartAsHost() ABORTADO - runner ya corriendo");
                return;
            }

            try
            {
                var sceneMgr = GetComponent<NetworkSceneManagerDefault>()
                               ?? gameObject.AddComponent<NetworkSceneManagerDefault>();
                int buildIdx = SceneManager.GetActiveScene().buildIndex;
                var sceneRef = SceneRef.FromIndex(buildIdx);

                RedesLog.Info(RedesLog.LOBBY, $"Se creara una sala llamada '{GameConstants.DEFAULT_ROOM_NAME}' (buildIdx={buildIdx})");

                var args = new StartGameArgs
                {
                    GameMode    = GameMode.Host,
                    SessionName = GameConstants.DEFAULT_ROOM_NAME,
                    PlayerCount = GameConstants.MAX_PLAYERS,
                    SceneManager = sceneMgr,
                    Scene       = sceneRef
                };
                RedesLog.Info(RedesLog.NET, $"   StartGame(Host, session={args.SessionName}, maxPlayers={args.PlayerCount})...");

                var result = await _runner.StartGame(args);

                if (this == null || _runner == null)
                {
                    RedesLog.Warn(RedesLog.NET, "<< StartAsHost() - objeto destruido durante await, ignorado");
                    return;
                }

                if (result.Ok)
                {
                    IsRunning = true;
                    RedesLog.Info(RedesLog.LOBBY, $"Se creo la sala '{GameConstants.DEFAULT_ROOM_NAME}' correctamente. Se esta esperando al otro jugador.");
                    RedesLog.Info(RedesLog.NET, "<< StartAsHost() OK - OnHostStarted fired");
                    OnHostStarted?.Invoke();
                }
                else
                {
                    string reason = result.ShutdownReason.ToString();
                    RedesLog.Error(RedesLog.NET, $"<< StartAsHost() FALLO - ShutdownReason={reason}");
                    OnConnectionFailed?.Invoke($"Crear sala fallo: {reason}");
                }
            }
            catch (Exception ex)
            {
                RedesLog.Error(RedesLog.NET, $"<< StartAsHost() EXCEPCION: {ex.Message}\n{ex.StackTrace}");
                OnConnectionFailed?.Invoke($"Excepcion: {ex.Message}");
            }
        }

        // ──────────────────────────────────────────────────────────────────
        //  JUGADOR 2 — "UNIRSE A SALA"
        // ──────────────────────────────────────────────────────────────────
        public async void StartAsClient()
        {
            RedesLog.Info(RedesLog.NET, ">> StartAsClient() - intentando unirse a sala");

            if (IsRunning || (_runner != null && _runner.IsRunning))
            {
                RedesLog.Warn(RedesLog.NET, "<< StartAsClient() ABORTADO - runner ya corriendo");
                return;
            }

            try
            {
                var sceneMgr = GetComponent<NetworkSceneManagerDefault>()
                               ?? gameObject.AddComponent<NetworkSceneManagerDefault>();
                int buildIdx = SceneManager.GetActiveScene().buildIndex;
                var sceneRef = SceneRef.FromIndex(buildIdx);

                RedesLog.Info(RedesLog.LOBBY, $"Buscando sala '{GameConstants.DEFAULT_ROOM_NAME}' para unirse (buildIdx={buildIdx})...");

                var args = new StartGameArgs
                {
                    GameMode    = GameMode.Client,
                    SessionName = GameConstants.DEFAULT_ROOM_NAME,
                    SceneManager = sceneMgr,
                    Scene       = sceneRef
                };
                RedesLog.Info(RedesLog.NET, $"   StartGame(Client, session={args.SessionName})...");

                var result = await _runner.StartGame(args);

                if (this == null || _runner == null)
                {
                    RedesLog.Warn(RedesLog.NET, "<< StartAsClient() - objeto destruido durante await");
                    return;
                }

                if (result.Ok)
                {
                    IsRunning = true;
                    RedesLog.Info(RedesLog.LOBBY, $"Se unio a la sala '{GameConstants.DEFAULT_ROOM_NAME}' correctamente.");
                    RedesLog.Info(RedesLog.NET, "<< StartAsClient() OK - OnHostStarted fired");
                    OnHostStarted?.Invoke();
                }
                else
                {
                    string reason = result.ShutdownReason.ToString();
                    RedesLog.Error(RedesLog.NET, $"<< StartAsClient() FALLO - ShutdownReason={reason}");
                    OnConnectionFailed?.Invoke($"Unirse fallo: {reason} (la sala existe? el Host inicio primero?)");
                }
            }
            catch (Exception ex)
            {
                RedesLog.Error(RedesLog.NET, $"<< StartAsClient() EXCEPCION: {ex.Message}\n{ex.StackTrace}");
                OnConnectionFailed?.Invoke($"Excepcion: {ex.Message}");
            }
        }

        public void Shutdown()
        {
            RedesLog.Info(RedesLog.NET, ">> Shutdown() solicitado");
            if (_runner != null) _runner.Shutdown();
            IsRunning = false;
            ConnectedPlayers = 0;
            RedesLog.Info(RedesLog.NET, "<< Shutdown() OK");
        }

        // ──────────────────────────────────────────────────────────────────
        //  FUSION CALLBACKS
        // ──────────────────────────────────────────────────────────────────

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $">> OnPlayerJoined(player={player}) IsServer={runner.IsServer}");

            if (runner.IsServer)
            {
                if (_playerPrefab == null)
                {
                    RedesLog.Error(RedesLog.NET, "   _playerPrefab es NULL. Asignalo con Tools > Redes > 3. Link & Assign All");
                }
                else if (_playerSpawner == null)
                {
                    RedesLog.Error(RedesLog.NET, "   _playerSpawner es NULL. Asignalo con Tools > Redes > 3. Link & Assign All");
                }
                else
                {
                    RedesLog.Info(RedesLog.NET, $"   [HOST] Spawneando player para PlayerRef={player}");
                    _playerSpawner.SpawnPlayer(runner, player, _playerPrefab);
                }
            }
            else
            {
                RedesLog.Info(RedesLog.NET, $"   [CLIENT] player={player} joineó, no spawneamos (solo el host spawna)");
            }

            RefreshPlayerCount();
            RedesLog.Info(RedesLog.NET, $"<< OnPlayerJoined(player={player}) - count={ConnectedPlayers}");
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $">> OnPlayerLeft(player={player})");
            if (runner.IsServer && _playerSpawner != null)
                _playerSpawner.DespawnPlayer(runner, player);
            RefreshPlayerCount();
            RedesLog.Info(RedesLog.NET, $"<< OnPlayerLeft(player={player}) - count={ConnectedPlayers}");
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            RedesLog.Info(RedesLog.NET, ">> OnConnectedToServer() - cliente conectado al servidor de Fusion");
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            RedesLog.Warn(RedesLog.NET, $">> OnDisconnectedFromServer(reason={reason})");
            IsRunning = false;
            OnConnectionFailed?.Invoke($"Desconectado: {reason}");
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            RedesLog.Warn(RedesLog.NET, $">> OnShutdown(reason={shutdownReason})");
            IsRunning = false;
            ConnectedPlayers = 0;
            if (shutdownReason != ShutdownReason.Ok && shutdownReason != ShutdownReason.GameClosed)
                OnConnectionFailed?.Invoke($"Runner apagado: {shutdownReason}");
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            RedesLog.Error(RedesLog.NET, $">> OnConnectFailed(addr={remoteAddress}, reason={reason})");
            OnConnectionFailed?.Invoke($"Fallo de conexion: {reason}");
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData();
            data.Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float dist))
                {
                    Vector3 worldPos = ray.GetPoint(dist);
                    data.AimDirection = new Vector2(worldPos.x, worldPos.z);
                }
            }

            data.Buttons.Set(InputButton.Fire,   Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space));
            data.Buttons.Set(InputButton.Reload,  Input.GetKey(KeyCode.R));
            input.Set(data);
        }

        // ──────────────────────────────────────────────────────────────────
        private void RefreshPlayerCount()
        {
            RedesLog.Info(RedesLog.NET, ">> RefreshPlayerCount()");
            ConnectedPlayers = _runner.ActivePlayers.Count();
            RedesLog.Info(RedesLog.NET, $"   ConnectedPlayers={ConnectedPlayers} / MIN={GameConstants.MIN_PLAYERS_TO_START}");
            OnPlayerCountChanged?.Invoke(ConnectedPlayers);

            if (ConnectedPlayers >= GameConstants.MIN_PLAYERS_TO_START)
            {
                RedesLog.Info(RedesLog.NET, "   se inicio el juego porque se tienen 2 jugadores");
                OnEnoughPlayersToStart?.Invoke();
            }
            RedesLog.Info(RedesLog.NET, "<< RefreshPlayerCount()");
        }

        // Stubs de la interfaz
        public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> list)
        {
            RedesLog.Info(RedesLog.LOBBY, $"OnSessionListUpdated: {list?.Count ?? 0} salas encontradas");
        }
        public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
        public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr m) { }
        public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> d) { }
        public void OnHostMigration(NetworkRunner r, HostMigrationToken t) { }
        public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, ArraySegment<byte> d) { }
        public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float prog) { }
        public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
        public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
        public void OnSceneLoadDone(NetworkRunner r) { RedesLog.Info(RedesLog.NET, "OnSceneLoadDone"); }
        public void OnSceneLoadStart(NetworkRunner r) { RedesLog.Info(RedesLog.NET, "OnSceneLoadStart"); }
    }
}
