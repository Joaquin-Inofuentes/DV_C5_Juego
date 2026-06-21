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
    /// ARQUITECTURA DE DOS RUNNERS (robusta).
    ///
    /// PROBLEMA QUE RESUELVE:
    ///   En Fusion 2 un NetworkRunner que hizo Shutdown queda MUERTO y no se puede
    ///   reutilizar. El log mostraba que el runner se apagaba solo tras conectarse
    ///   al lobby (DisconnectByClientLogic), y cualquier StartGame posterior tiraba
    ///   excepcion vacia -> ni Host ni Unirse funcionaban.
    ///
    /// SOLUCION:
    ///   - _lobbyRunner: runner DEDICADO solo a escuchar la lista de salas.
    ///     Si se cae mientras seguimos en el menu, se RECREA automaticamente.
    ///   - _gameRunner:  runner FRESCO creado en el momento de Crear/Unirse.
    ///     Nunca se reutiliza uno muerto.
    ///
    /// CALLSTACK USER 1 (HOST):
    ///   Awake → StartLobbyWatch() → CreateRunner("LobbyRunner") → JoinSessionLobby
    ///   OnSessionListUpdated(lobbyRunner) → 0 salas → OnRoomAvailabilityChanged(false)
    ///   [click CREAR SALA] StartAsHost()
    ///     → ShutdownLobbyRunner() (intencional)
    ///     → CreateRunner("GameRunner") → StartGame(Host)
    ///     → OK → OnHostStarted → WaitingForPlayers
    ///   OnPlayerJoined(gameRunner, p1=host) → SpawnPlayer → count=1
    ///   OnPlayerJoined(gameRunner, p2)      → SpawnPlayer → count=2 → OnEnoughPlayersToStart
    ///
    /// CALLSTACK USER 2 (CLIENT):
    ///   Awake → StartLobbyWatch() → JoinSessionLobby
    ///   OnSessionListUpdated → 0 salas → Join DESHABILITADO
    ///   ...User1 crea sala...
    ///   OnSessionListUpdated → "RedesRoom" open → OnRoomAvailabilityChanged(true) → Join HABILITADO
    ///   [click UNIRSE] StartAsClient()
    ///     → ShutdownLobbyRunner()
    ///     → CreateRunner("GameRunner") → StartGame(Client)
    ///     → OK → OnHostStarted → OnConnectedToServer → ambos players spawneados por el host
    /// </summary>
    public class HostNetworkService : MonoBehaviour, INetworkService, INetworkRunnerCallbacks
    {
        [Header("Spawning (assigned by Tools > Redes > Link & Assign All)")]
        [SerializeField] private PlayerSpawner _playerSpawner;

        [Header("Networked Player Prefab. Assigned by the Link tool.")]
        [SerializeField] private NetworkObject _playerPrefab;

        private NetworkRunner _lobbyRunner;
        private NetworkRunner _gameRunner;

        private bool _isInSession = false;          // true cuando entramos a Crear/Unirse
        private bool _lobbyShutdownIntentional = false;
        private bool _lastRoomAvailable = false;

        public bool IsRunning { get; private set; }
        public int ConnectedPlayers { get; private set; }

        public event Action         OnHostStarted;
        public event Action<int>    OnPlayerCountChanged;
        public event Action         OnEnoughPlayersToStart;
        public event Action<string> OnConnectionFailed;
        public event Action<List<SessionInfo>> OnRoomListUpdated;

        public NetworkRunner Runner       => _gameRunner;
        public NetworkObject PlayerPrefab => _playerPrefab;

        // ──────────────────────────────────────────────────────────────────
        public static HostNetworkService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            RedesLog.Info(RedesLog.NET, ">> HostNetworkService.Awake() - arquitectura 2 runners");
            StartLobbyWatch();
            RedesLog.Info(RedesLog.NET, "<< HostNetworkService.Awake()");
        }

        // ──────────────────────────────────────────────────────────────────
        //  Helpers de creación / destrucción de runners
        // ──────────────────────────────────────────────────────────────────
        private NetworkRunner CreateRunner(string goName, bool provideInput)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            var runner = go.AddComponent<NetworkRunner>();
            runner.ProvideInput = provideInput;
            runner.AddCallbacks(this);
            RedesLog.Info(RedesLog.NET, $"   CreateRunner('{goName}', provideInput={provideInput})");
            return runner;
        }

        private void DestroyRunnerGameObject(NetworkRunner runner)
        {
            if (runner == null) return;
            var go = runner.gameObject;
            if (go != null) Destroy(go);
        }

        // ──────────────────────────────────────────────────────────────────
        //  LOBBY WATCH — solo escucha la lista de salas. Auto-reconecta.
        // ──────────────────────────────────────────────────────────────────
        public async void StartLobbyWatch()
        {
            if (_isInSession)
            {
                RedesLog.Info(RedesLog.LOBBY, "   StartLobbyWatch omitido (estamos en sesion de juego)");
                return;
            }
            if (_lobbyRunner != null)
            {
                RedesLog.Info(RedesLog.LOBBY, "   StartLobbyWatch omitido (lobbyRunner ya existe)");
                return;
            }

            RedesLog.Info(RedesLog.LOBBY, ">> StartLobbyWatch() - creando lobbyRunner y conectando al lobby...");
            _lobbyShutdownIntentional = false;
            _lobbyRunner = CreateRunner("LobbyRunner", provideInput: false);

            try
            {
                var result = await _lobbyRunner.JoinSessionLobby(SessionLobby.ClientServer);
                if (this == null) return;

                if (result.Ok)
                    RedesLog.Info(RedesLog.LOBBY, "<< StartLobbyWatch() OK - escuchando salas. Boton UNIRSE espera a detectar 'RedesRoom'.");
                else
                    RedesLog.Warn(RedesLog.LOBBY, $"<< StartLobbyWatch() FALLO ({result.ShutdownReason}). Verifica App ID en NetworkProjectConfig.");
            }
            catch (Exception ex)
            {
                RedesLog.Error(RedesLog.LOBBY, $"<< StartLobbyWatch() EXCEPCION: {ex.Message}");
            }
        }

        private void ShutdownLobbyRunner()
        {
            if (_lobbyRunner == null) return;
            RedesLog.Info(RedesLog.LOBBY, "   ShutdownLobbyRunner() - dejamos de mirar el lobby");
            _lobbyShutdownIntentional = true;
            var r = _lobbyRunner;
            _lobbyRunner = null;
            r.Shutdown();
            DestroyRunnerGameObject(r);
        }

        // ──────────────────────────────────────────────────────────────────
        //  JUGADOR 1 — "CREAR SALA"
        // ──────────────────────────────────────────────────────────────────
        public async void StartAsHost(string sessionName)
        {
            RedesLog.Info(RedesLog.NET, $">> StartAsHost() - crear sala {sessionName}");
            if (_isInSession) { RedesLog.Warn(RedesLog.NET, "<< StartAsHost ABORTADO - ya en sesion"); return; }

            _isInSession = true;
            ShutdownLobbyRunner();                       // soltamos el lobby
            _gameRunner = CreateRunner("GameRunner", provideInput: true);

            try
            {
                int buildIdx = SceneManager.GetActiveScene().buildIndex;
                var sceneMgr = _gameRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();

                RedesLog.Info(RedesLog.LOBBY, $"   Creando sala '{sessionName}' (maxPlayers={GameConstants.MAX_PLAYERS})...");

                var result = await _gameRunner.StartGame(new StartGameArgs
                {
                    GameMode     = GameMode.Host,
                    SessionName  = sessionName,
                    PlayerCount  = GameConstants.MAX_PLAYERS,
                    SceneManager = sceneMgr,
                    Scene        = SceneRef.FromIndex(buildIdx)
                });
                if (this == null || _gameRunner == null) return;

                if (result.Ok)
                {
                    IsRunning = true;
                    RedesLog.Info(RedesLog.LOBBY, $"   Sala '{sessionName}' creada. Esperando al otro jugador.");
                    RedesLog.Info(RedesLog.NET, "<< StartAsHost() OK");
                    OnHostStarted?.Invoke();
                }
                else
                {
                    RedesLog.Error(RedesLog.NET, $"<< StartAsHost() FALLO - {result.ShutdownReason}");
                    FailAndReturnToLobby($"Crear sala fallo: {result.ShutdownReason}");
                }
            }
            catch (Exception ex)
            {
                RedesLog.Error(RedesLog.NET, $"<< StartAsHost() EXCEPCION: {ex.Message}");
                FailAndReturnToLobby($"Excepcion al crear sala: {ex.Message}");
            }
        }

        // ──────────────────────────────────────────────────────────────────
        //  JUGADOR 2 — "UNIRSE A SALA"
        // ──────────────────────────────────────────────────────────────────
        public async void StartAsClient(string sessionName)
        {
            RedesLog.Info(RedesLog.NET, $">> StartAsClient() - unirse a sala {sessionName}");
            if (_isInSession) { RedesLog.Warn(RedesLog.NET, "<< StartAsClient ABORTADO - ya en sesion"); return; }

            _isInSession = true;
            ShutdownLobbyRunner();
            _gameRunner = CreateRunner("GameRunner", provideInput: true);

            try
            {
                int buildIdx = SceneManager.GetActiveScene().buildIndex;
                var sceneMgr = _gameRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();

                RedesLog.Info(RedesLog.LOBBY, $"   Uniéndose a sala '{sessionName}'...");

                var result = await _gameRunner.StartGame(new StartGameArgs
                {
                    GameMode     = GameMode.Client,
                    SessionName  = sessionName,
                    SceneManager = sceneMgr,
                    Scene        = SceneRef.FromIndex(buildIdx)
                });
                if (this == null || _gameRunner == null) return;

                if (result.Ok)
                {
                    IsRunning = true;
                    RedesLog.Info(RedesLog.LOBBY, $"   Unido a sala '{sessionName}' correctamente.");
                    RedesLog.Info(RedesLog.NET, "<< StartAsClient() OK");
                    OnHostStarted?.Invoke();
                }
                else
                {
                    RedesLog.Error(RedesLog.NET, $"<< StartAsClient() FALLO - {result.ShutdownReason}");
                    FailAndReturnToLobby($"Unirse fallo: {result.ShutdownReason}");
                }
            }
            catch (Exception ex)
            {
                RedesLog.Error(RedesLog.NET, $"<< StartAsClient() EXCEPCION: {ex.Message}");
                FailAndReturnToLobby($"Excepcion al unirse: {ex.Message}");
            }
        }

        /// <summary>Limpia el runner de juego fallido y vuelve a mirar el lobby.</summary>
        private void FailAndReturnToLobby(string reason)
        {
            IsRunning = false;
            _isInSession = false;
            if (_gameRunner != null)
            {
                var r = _gameRunner;
                _gameRunner = null;
                r.Shutdown();
                DestroyRunnerGameObject(r);
            }
            OnConnectionFailed?.Invoke(reason);
            // Reintentar mirar el lobby para que el boton Unirse vuelva a funcionar
            Invoke(nameof(StartLobbyWatch), 0.5f);
        }

        public void Shutdown()
        {
            RedesLog.Info(RedesLog.NET, ">> Shutdown() total");
            _lobbyShutdownIntentional = true;
            if (_gameRunner != null)  { _gameRunner.Shutdown();  DestroyRunnerGameObject(_gameRunner);  _gameRunner = null; }
            if (_lobbyRunner != null) { _lobbyRunner.Shutdown(); DestroyRunnerGameObject(_lobbyRunner); _lobbyRunner = null; }
            IsRunning = false;
            _isInSession = false;
            ConnectedPlayers = 0;
        }

        // ──────────────────────────────────────────────────────────────────
        //  FUSION CALLBACKS  (se distinguen por el parámetro 'runner')
        // ──────────────────────────────────────────────────────────────────

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (runner != _lobbyRunner) return; // solo nos interesa la del lobby
            int count = sessionList?.Count ?? 0;
            RedesLog.Info(RedesLog.LOBBY, $">> OnSessionListUpdated: {count} sala(s)");

            OnRoomListUpdated?.Invoke(sessionList);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner != _gameRunner) return;
            RedesLog.Info(RedesLog.NET, $">> OnPlayerJoined(player={player}) IsServer={runner.IsServer}");

            if (runner.IsServer)
            {
                if (_playerPrefab == null)
                    RedesLog.Error(RedesLog.NET, "   _playerPrefab NULL - corre Link & Assign All");
                else if (_playerSpawner == null)
                    RedesLog.Error(RedesLog.NET, "   _playerSpawner NULL - corre Link & Assign All");
                else
                {
                    if (!_playerSpawner.IsPlayerSpawned(player))
                    {
                        _playerSpawner.SpawnPlayer(runner, player, _playerPrefab);
                    }
                    else
                    {
                        RedesLog.Info(RedesLog.NET, $"   OnPlayerJoined: Jugador {player} ya estaba spawneado. Omitiendo spawn.");
                    }
                }
            }

            RefreshPlayerCount();
            RedesLog.Info(RedesLog.NET, $"<< OnPlayerJoined(player={player})");
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner != _gameRunner) return;
            RedesLog.Info(RedesLog.NET, $">> OnPlayerLeft(player={player})");
            if (runner.IsServer && _playerSpawner != null)
                _playerSpawner.DespawnPlayer(runner, player);
            RefreshPlayerCount();
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            if (runner != _gameRunner) return;
            RedesLog.Info(RedesLog.NET, ">> OnConnectedToServer() - cliente conectado al host");
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            if (runner != _gameRunner) return;
            RedesLog.Warn(RedesLog.NET, $">> OnDisconnectedFromServer(reason={reason})");
            FailAndReturnToLobby($"Desconectado: {reason}");
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            RedesLog.Warn(RedesLog.NET, $">> OnShutdown(reason={shutdownReason}) runner={runner?.gameObject.name}");

            if (runner == _lobbyRunner)
            {
                _lobbyRunner = null;
                if (!_lobbyShutdownIntentional && !_isInSession)
                {
                    RedesLog.Info(RedesLog.LOBBY, "   lobbyRunner cayo solo → reconectando en 1s");
                    Invoke(nameof(StartLobbyWatch), 1f);
                }
                _lobbyShutdownIntentional = false;
            }
            else if (runner == _gameRunner)
            {
                RedesLog.Trace(RedesLog.NET, "HostNetworkService", "OnShutdown", null, $"Game runner shut down. Reason={shutdownReason}. Triggering local return to lobby.");
                IsRunning = false;
                _isInSession = false;
                _gameRunner = null;
                
                var flow = FindFirstObjectByType<GameFlowController>();
                if (flow != null)
                {
                    flow.TriggerReturnToLobby();
                }

                if (shutdownReason != ShutdownReason.Ok && shutdownReason != ShutdownReason.GameClosed)
                    OnConnectionFailed?.Invoke($"Sesion terminada: {shutdownReason}");
            }
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            if (runner != _gameRunner) return;
            RedesLog.Error(RedesLog.NET, $">> OnConnectFailed(reason={reason})");
            FailAndReturnToLobby($"Fallo conexion: {reason}");
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (runner != _gameRunner) return;
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

            data.Buttons.Set(InputButton.Fire,  Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space));
            data.Buttons.Set(InputButton.Reload, Input.GetKey(KeyCode.R));
            input.Set(data);
        }

        // ──────────────────────────────────────────────────────────────────
        private void RefreshPlayerCount()
        {
            ConnectedPlayers = _gameRunner != null ? _gameRunner.ActivePlayers.Count() : 0;
            RedesLog.Info(RedesLog.NET, $"   RefreshPlayerCount → {ConnectedPlayers}/{GameConstants.MIN_PLAYERS_TO_START}");
            OnPlayerCountChanged?.Invoke(ConnectedPlayers);

            if (ConnectedPlayers >= GameConstants.MIN_PLAYERS_TO_START)
            {
                RedesLog.Info(RedesLog.NET, "   *** se inicio el juego porque se tienen 2 jugadores ***");
                OnEnoughPlayersToStart?.Invoke();
            }
        }

        // Stubs
        public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
        public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] t) { }
        public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr m) { }
        public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> d) { }
        public void OnHostMigration(NetworkRunner r, HostMigrationToken t) { }
        public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, ArraySegment<byte> d) { }
        public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float prog) { }
        public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
        public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
        public void OnSceneLoadDone(NetworkRunner runner)
        {
            if (runner != _gameRunner) return;
            int activeCount = runner.ActivePlayers.Count();
            RedesLog.Info(RedesLog.NET, $">> OnSceneLoadDone. IsServer={runner.IsServer} ActivePlayers={activeCount}");

            if (runner.IsServer)
            {
                if (_playerSpawner != null)
                {
                    _playerSpawner.DespawnAllActivePlayers(runner);
                    
                    foreach (var playerRef in runner.ActivePlayers)
                    {
                        _playerSpawner.SpawnPlayer(runner, playerRef, _playerPrefab);
                    }
                }
            }

            // Force evaluate player count and trigger transition for both host and clients!
            OnPlayerCountChanged?.Invoke(activeCount);
            if (activeCount >= GameConstants.MIN_PLAYERS_TO_START)
            {
                RedesLog.Info(RedesLog.NET, $"   OnSceneLoadDone: {activeCount} jugadores conectados. Iniciando juego.");
                OnEnoughPlayersToStart?.Invoke();
            }
        }
        public void OnSceneLoadStart(NetworkRunner r) { }
    }
}
