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
    ///   Awake()            → AddCallbacks + StartLobbyWatch()
    ///   StartLobbyWatch()  → runner.JoinSessionLobby() [await Photon]
    ///   OnSessionListUpdated → "RedesRoom" NOT found → OnRoomAvailabilityChanged(false) → Join DESHABILITADO
    ///   [user click CREAR SALA]
    ///   StartAsHost()      → runner.StartGame(Host) [transiciona desde lobby]
    ///   result.Ok=true     → OnHostStarted → WaitingForPlayers
    ///   OnSessionListUpdated → "RedesRoom" FOUND, open → OnRoomAvailabilityChanged(true) [User2 recibe esto]
    ///   OnPlayerJoined(p1) → SpawnPlayer(p1) + RefreshPlayerCount(1)
    ///   OnPlayerJoined(p2) → SpawnPlayer(p2) + RefreshPlayerCount(2) → OnEnoughPlayersToStart → Playing
    ///
    /// CALLSTACK USER 2 (CLIENT):
    ///   Awake()            → AddCallbacks + StartLobbyWatch()
    ///   StartLobbyWatch()  → runner.JoinSessionLobby() [await Photon]
    ///   OnSessionListUpdated → "RedesRoom" NOT found → Join DESHABILITADO [espera]
    ///   ... User 1 crea sala ...
    ///   OnSessionListUpdated → "RedesRoom" FOUND → OnRoomAvailabilityChanged(true) → Join HABILITADO
    ///   [user click UNIRSE A SALA]
    ///   StartAsClient()    → runner.StartGame(Client) [transiciona desde lobby]
    ///   result.Ok=true     → OnHostStarted → WaitingForPlayers
    ///   OnConnectedToServer→ log
    ///   OnPlayerJoined(p1), OnPlayerJoined(p2) → RefreshPlayerCount(2) → OnEnoughPlayersToStart → Playing
    /// </summary>
    [RequireComponent(typeof(NetworkRunner))]
    public class HostNetworkService : MonoBehaviour, INetworkService, INetworkRunnerCallbacks
    {
        [Header("Spawning (assigned by Tools > Redes > Link & Assign All)")]
        [SerializeField] private PlayerSpawner _playerSpawner;

        [Header("Networked Player Prefab. Assigned by the Link tool.")]
        [SerializeField] private NetworkObject _playerPrefab;

        private NetworkRunner _runner;

        // Separo "en lobby" de "en sesion de juego"
        private bool _isInSession = false;

        public bool IsRunning { get; private set; }
        public int ConnectedPlayers { get; private set; }

        public event Action         OnHostStarted;
        public event Action<int>    OnPlayerCountChanged;
        public event Action         OnEnoughPlayersToStart;
        public event Action<string> OnConnectionFailed;
        public event Action<bool>   OnRoomAvailabilityChanged;

        public NetworkRunner Runner  => _runner;
        public NetworkObject PlayerPrefab => _playerPrefab;

        // ──────────────────────────────────────────────────────────────────
        private void Awake()
        {
            RedesLog.Info(RedesLog.NET, ">> HostNetworkService.Awake()");
            _runner = GetComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
            RedesLog.Info(RedesLog.NET, "<< Awake() - iniciando lobby watch...");
            StartLobbyWatch();
        }

        // ──────────────────────────────────────────────────────────────────
        //  LOBBY WATCH — detecta si "RedesRoom" ya existe
        //  Corre automáticamente al inicio. No abre sesión, solo escucha.
        // ──────────────────────────────────────────────────────────────────
        private async void StartLobbyWatch()
        {
            RedesLog.Info(RedesLog.LOBBY, ">> StartLobbyWatch() - conectando al lobby Photon para detectar salas...");
            try
            {
                var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
                if (result.Ok)
                {
                    RedesLog.Info(RedesLog.LOBBY,
                        "<< StartLobbyWatch() OK - escuchando OnSessionListUpdated. " +
                        "Boton UNIRSE deshabilitado hasta detectar 'RedesRoom'.");
                }
                else
                {
                    RedesLog.Warn(RedesLog.LOBBY,
                        $"<< StartLobbyWatch() FALLO ({result.ShutdownReason}). " +
                        "Verifica el App ID en NetworkProjectConfig de Fusion.");
                }
            }
            catch (Exception ex)
            {
                RedesLog.Error(RedesLog.LOBBY, $"<< StartLobbyWatch() EXCEPCION: {ex.Message}");
            }
        }

        // ──────────────────────────────────────────────────────────────────
        //  JUGADOR 1 — "CREAR SALA"
        //  Transiciona el runner desde lobby → Host session.
        // ──────────────────────────────────────────────────────────────────
        public async void StartAsHost()
        {
            RedesLog.Info(RedesLog.NET, ">> StartAsHost() - creando sala");

            if (_isInSession)
            {
                RedesLog.Warn(RedesLog.NET, "<< StartAsHost() ABORTADO - ya estamos en sesion");
                return;
            }
            _isInSession = true;

            try
            {
                int buildIdx  = SceneManager.GetActiveScene().buildIndex;
                var sceneMgr  = GetComponent<NetworkSceneManagerDefault>()
                                ?? gameObject.AddComponent<NetworkSceneManagerDefault>();
                var sceneRef  = SceneRef.FromIndex(buildIdx);

                RedesLog.Info(RedesLog.LOBBY, $"   Creando sala '{GameConstants.DEFAULT_ROOM_NAME}' " +
                                               $"(maxPlayers={GameConstants.MAX_PLAYERS}, buildIdx={buildIdx})...");

                var result = await _runner.StartGame(new StartGameArgs
                {
                    GameMode    = GameMode.Host,
                    SessionName = GameConstants.DEFAULT_ROOM_NAME,
                    PlayerCount = GameConstants.MAX_PLAYERS,
                    SceneManager = sceneMgr,
                    Scene       = sceneRef
                });

                if (this == null || _runner == null) return;

                if (result.Ok)
                {
                    IsRunning = true;
                    RedesLog.Info(RedesLog.LOBBY,
                        $"   Sala '{GameConstants.DEFAULT_ROOM_NAME}' creada. " +
                        "Se esta esperando al otro jugador.");
                    RedesLog.Info(RedesLog.NET, "<< StartAsHost() OK");
                    OnHostStarted?.Invoke();
                }
                else
                {
                    _isInSession = false;
                    string reason = result.ShutdownReason.ToString();
                    RedesLog.Error(RedesLog.NET, $"<< StartAsHost() FALLO - {reason}");
                    OnConnectionFailed?.Invoke($"Crear sala fallo: {reason}");
                }
            }
            catch (Exception ex)
            {
                _isInSession = false;
                RedesLog.Error(RedesLog.NET, $"<< StartAsHost() EXCEPCION: {ex.Message}");
                OnConnectionFailed?.Invoke($"Excepcion: {ex.Message}");
            }
        }

        // ──────────────────────────────────────────────────────────────────
        //  JUGADOR 2 — "UNIRSE A SALA"
        //  Solo se llama cuando OnRoomAvailabilityChanged(true) fue disparado.
        //  Transiciona el runner desde lobby → Client session.
        // ──────────────────────────────────────────────────────────────────
        public async void StartAsClient()
        {
            RedesLog.Info(RedesLog.NET, ">> StartAsClient() - uniéndose a sala");

            if (_isInSession)
            {
                RedesLog.Warn(RedesLog.NET, "<< StartAsClient() ABORTADO - ya en sesion");
                return;
            }
            _isInSession = true;

            try
            {
                int buildIdx  = SceneManager.GetActiveScene().buildIndex;
                var sceneMgr  = GetComponent<NetworkSceneManagerDefault>()
                                ?? gameObject.AddComponent<NetworkSceneManagerDefault>();
                var sceneRef  = SceneRef.FromIndex(buildIdx);

                RedesLog.Info(RedesLog.LOBBY,
                    $"   Uniéndose a sala '{GameConstants.DEFAULT_ROOM_NAME}' (buildIdx={buildIdx})...");

                var result = await _runner.StartGame(new StartGameArgs
                {
                    GameMode    = GameMode.Client,
                    SessionName = GameConstants.DEFAULT_ROOM_NAME,
                    SceneManager = sceneMgr,
                    Scene       = sceneRef
                });

                if (this == null || _runner == null) return;

                if (result.Ok)
                {
                    IsRunning = true;
                    RedesLog.Info(RedesLog.LOBBY,
                        $"   Unido a sala '{GameConstants.DEFAULT_ROOM_NAME}' correctamente.");
                    RedesLog.Info(RedesLog.NET, "<< StartAsClient() OK");
                    OnHostStarted?.Invoke();
                }
                else
                {
                    _isInSession = false;
                    string reason = result.ShutdownReason.ToString();
                    RedesLog.Error(RedesLog.NET, $"<< StartAsClient() FALLO - {reason}");
                    OnConnectionFailed?.Invoke($"Unirse fallo: {reason}");
                }
            }
            catch (Exception ex)
            {
                _isInSession = false;
                RedesLog.Error(RedesLog.NET, $"<< StartAsClient() EXCEPCION: {ex.Message}");
                OnConnectionFailed?.Invoke($"Excepcion: {ex.Message}");
            }
        }

        public void Shutdown()
        {
            RedesLog.Info(RedesLog.NET, ">> Shutdown()");
            _runner?.Shutdown();
            IsRunning    = false;
            _isInSession = false;
            ConnectedPlayers = 0;
        }

        // ──────────────────────────────────────────────────────────────────
        //  FUSION CALLBACKS
        // ──────────────────────────────────────────────────────────────────

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            int count = sessionList?.Count ?? 0;
            RedesLog.Info(RedesLog.LOBBY, $">> OnSessionListUpdated: {count} sala(s) en el lobby");

            bool roomAvailable = false;
            if (sessionList != null)
            {
                foreach (var s in sessionList)
                {
                    bool isOurRoom = s.Name == GameConstants.DEFAULT_ROOM_NAME;
                    bool hasPlace  = s.PlayerCount < s.MaxPlayers;
                    bool isOpen    = s.IsOpen;
                    RedesLog.Info(RedesLog.LOBBY,
                        $"   Sala='{s.Name}' players={s.PlayerCount}/{s.MaxPlayers} open={isOpen} " +
                        $"[es nuestra={isOurRoom} tiene lugar={hasPlace}]");

                    if (isOurRoom && isOpen && hasPlace)
                        roomAvailable = true;
                }
            }

            RedesLog.Info(RedesLog.LOBBY,
                $"<< OnSessionListUpdated → roomAvailable={roomAvailable} " +
                $"(boton UNIRSE {(roomAvailable ? "HABILITADO" : "DESHABILITADO")})");

            OnRoomAvailabilityChanged?.Invoke(roomAvailable);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $">> OnPlayerJoined(player={player}) IsServer={runner.IsServer}");

            if (runner.IsServer)
            {
                if (_playerPrefab == null)
                    RedesLog.Error(RedesLog.NET, "   _playerPrefab NULL - asignalo con Link & Assign All");
                else if (_playerSpawner == null)
                    RedesLog.Error(RedesLog.NET, "   _playerSpawner NULL - asignalo con Link & Assign All");
                else
                    _playerSpawner.SpawnPlayer(runner, player, _playerPrefab);
            }
            else
            {
                RedesLog.Info(RedesLog.NET, $"   [CLIENT] player={player} joineó");
            }

            RefreshPlayerCount();
            RedesLog.Info(RedesLog.NET, $"<< OnPlayerJoined(player={player})");
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            RedesLog.Info(RedesLog.NET, $">> OnPlayerLeft(player={player})");
            if (runner.IsServer && _playerSpawner != null)
                _playerSpawner.DespawnPlayer(runner, player);
            RefreshPlayerCount();
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            RedesLog.Info(RedesLog.NET, ">> OnConnectedToServer() - cliente conectado al host");
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            RedesLog.Warn(RedesLog.NET, $">> OnDisconnectedFromServer(reason={reason})");
            IsRunning    = false;
            _isInSession = false;
            OnConnectionFailed?.Invoke($"Desconectado: {reason}");
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            RedesLog.Warn(RedesLog.NET, $">> OnShutdown(reason={shutdownReason})");
            IsRunning    = false;
            _isInSession = false;
            ConnectedPlayers = 0;
            if (shutdownReason != ShutdownReason.Ok && shutdownReason != ShutdownReason.GameClosed)
                OnConnectionFailed?.Invoke($"Runner apagado: {shutdownReason}");
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            RedesLog.Error(RedesLog.NET, $">> OnConnectFailed(addr={remoteAddress}, reason={reason})");
            _isInSession = false;
            OnConnectionFailed?.Invoke($"Fallo conexion: {reason}");
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData();
            data.Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float dist))
                {
                    Vector3 wp = ray.GetPoint(dist);
                    data.AimDirection = new Vector2(wp.x, wp.z);
                }
            }

            data.Buttons.Set(InputButton.Fire,   Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space));
            data.Buttons.Set(InputButton.Reload,  Input.GetKey(KeyCode.R));
            input.Set(data);
        }

        // ──────────────────────────────────────────────────────────────────
        private void RefreshPlayerCount()
        {
            ConnectedPlayers = _runner.ActivePlayers.Count();
            RedesLog.Info(RedesLog.NET, $"   RefreshPlayerCount → {ConnectedPlayers}/{GameConstants.MIN_PLAYERS_TO_START}");
            OnPlayerCountChanged?.Invoke(ConnectedPlayers);

            if (ConnectedPlayers >= GameConstants.MIN_PLAYERS_TO_START)
            {
                RedesLog.Info(RedesLog.NET, "   *** se inicio el juego porque se tienen 2 jugadores ***");
                OnEnoughPlayersToStart?.Invoke();
            }
        }

        // Stubs de la interfaz
        public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
        public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] t) { }
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
