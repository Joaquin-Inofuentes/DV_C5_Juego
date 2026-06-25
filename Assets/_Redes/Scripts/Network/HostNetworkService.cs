using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using Redes.Core;
using Redes.Controllers;
using Redes.Views;

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
            // Debug.Log($"[CONNECTION_DEBUG] StartLobbyWatch called. _isInSession={_isInSession}, _lobbyRunner exists={_lobbyRunner != null}");
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
            // Debug.Log($"[CONNECTION_DEBUG] Created LobbyRunner. Attempting JoinSessionLobby...");

            try
            {
                var result = await _lobbyRunner.JoinSessionLobby(SessionLobby.ClientServer);
                if (this == null) return;

                // Debug.Log($"[CONNECTION_DEBUG] JoinSessionLobby completed. Result Ok={result.Ok}");

                if (result.Ok)
                    RedesLog.Info(RedesLog.LOBBY, "<< StartLobbyWatch() OK - escuchando salas. Boton UNIRSE espera a detectar 'RedesRoom'.");
                else
                    RedesLog.Warn(RedesLog.LOBBY, $"<< StartLobbyWatch() FALLO ({result.ShutdownReason}). Verifica App ID en NetworkProjectConfig.");
            }
            catch (Exception ex)
            {
                // Debug.LogError($"[CONNECTION_DEBUG] Exception in JoinSessionLobby: {ex}");
                RedesLog.Error(RedesLog.LOBBY, $"<< StartLobbyWatch() EXCEPCION: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task ShutdownLobbyRunnerAsync()
        {
            if (_lobbyRunner == null)
            {
                // Debug.Log("[CONNECTION_DEBUG] ShutdownLobbyRunnerAsync() called but _lobbyRunner is already null.");
                return;
            }
            RedesLog.Info(RedesLog.LOBBY, "   ShutdownLobbyRunner() - dejamos de mirar el lobby");
            _lobbyShutdownIntentional = true;
            var r = _lobbyRunner;
            _lobbyRunner = null;
            // Debug.Log($"[CONNECTION_DEBUG] >>> ShutdownLobbyRunnerAsync() - Calling await r.Shutdown() on {r.name}...");
            await r.Shutdown();
            // Debug.Log($"[CONNECTION_DEBUG] >>> ShutdownLobbyRunnerAsync() - Shutdown completed. Destroying runner GameObject...");
            DestroyRunnerGameObject(r);
            await System.Threading.Tasks.Task.Yield();
            // Debug.Log($"[CONNECTION_DEBUG] <<< ShutdownLobbyRunnerAsync() - finished successfully.");
        }

        // ──────────────────────────────────────────────────────────────────
        //  JUGADOR 1 — "CREAR SALA"
        // ──────────────────────────────────────────────────────────────────
        public async void StartAsHost(string sessionName)
        {
            // Debug.Log($"[CONNECTION_DEBUG] >>> [1] Inicia StartAsHost(). sessionName={sessionName}, _isInSession={_isInSession}");
            RedesLog.Info(RedesLog.NET, $">> StartAsHost() - crear sala {sessionName}");
            if (_isInSession) { 
                // Debug.Log($"[CONNECTION_DEBUG] <<< [1] StartAsHost() ABORTADO porque _isInSession=true");
                RedesLog.Warn(RedesLog.NET, "<< StartAsHost ABORTADO - ya en sesion"); 
                return; 
            }

            _isInSession = true;
            // Debug.Log($"[CONNECTION_DEBUG] >>> [2] _isInSession puesto en true. Apagando LobbyRunner...");
            await ShutdownLobbyRunnerAsync();                       // soltamos el lobby con await!
            
            // Debug.Log($"[CONNECTION_DEBUG] >>> [3] Creando GameRunner (provideInput=true)...");
            _gameRunner = CreateRunner("GameRunner", provideInput: true);
            
            if (_gameRunner == null) {
                // Debug.LogError($"[CONNECTION_DEBUG] <<< [3] ERROR FATAL: _gameRunner es nulo después de CreateRunner.");
                return;
            }

            // Debug.Log($"[CONNECTION_DEBUG] >>> [4] GameRunner creado con éxito (InstanceID: {_gameRunner.GetInstanceID()}). Configurando StartGameArgs...");

            int buildIdx = 0;
            try
            {
                var activeScene = SceneManager.GetActiveScene();
                // Debug.Log($"[CONNECTION_DEBUG] >>> [4A] Escena activa obtenida: name={activeScene.name}, buildIndex={activeScene.buildIndex}");
                
                buildIdx = activeScene.buildIndex;
                if (buildIdx < 0)
                {
                    // Debug.LogWarning($"[CONNECTION_DEBUG] >>> [4B] Active scene '{activeScene.name}' is not in Build Settings (index -1). Falling back to index 0.");
                    buildIdx = 0;
                }
                
                // Debug.Log($"[CONNECTION_DEBUG] >>> [4C] Usando buildIdx={buildIdx} para SceneRef.");
                
                // Debug.Log($"[CONNECTION_DEBUG] >>> [4D] Añadiendo NetworkSceneManagerDefault a _gameRunner...");
                var sceneMgr = _gameRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
                if (sceneMgr == null) // Debug.LogError("[CONNECTION_DEBUG] ERROR: NetworkSceneManagerDefault no se pudo añadir!");
                
                RedesLog.Info(RedesLog.LOBBY, $"   Creando sala '{sessionName}' (maxPlayers={GameConstants.MAX_PLAYERS})...");

                var args = new StartGameArgs
                {
                    GameMode     = GameMode.Host,
                    SessionName  = sessionName,
                    PlayerCount  = GameConstants.MAX_PLAYERS,
                    SceneManager = sceneMgr,
                    Scene        = SceneRef.FromIndex(buildIdx)
                };
                
                // Debug.Log($"[CONNECTION_DEBUG] >>> [5] StartGameArgs configurado: GameMode={args.GameMode}, SessionName={args.SessionName}, PlayerCount={args.PlayerCount}, SceneHasValue={args.Scene.HasValue}");
                // Debug.Log($"[CONNECTION_DEBUG] >>> [6] Llamando a await _gameRunner.StartGame(args)...");

                var result = await _gameRunner.StartGame(args);
                
                if (this == null || _gameRunner == null) {
                    // Debug.LogWarning($"[CONNECTION_DEBUG] <<< [6] HostNetworkService o _gameRunner destruido mientras se esperaba StartGame.");
                    return;
                }

                // Debug.Log($"[CONNECTION_DEBUG] >>> [7] StartGame() finalizado. Resultado Ok={result.Ok}, ShutdownReason={result.ShutdownReason}");

                if (!result.Ok && result.ShutdownReason == ShutdownReason.IncompatibleConfiguration)
                {
                    Debug.LogError("========================================================================\n" +
                                   "❌ ERROR CRÍTICO DE FUSION: IncompatibleConfiguration\n" +
                                   "========================================================================\n" +
                                   "Tu AppID de Photon Fusion actual está configurado como 'Shared Mode' únicamente en el Dashboard de Photon. " +
                                   "El modo Host-Client NO está permitido por Photon con este AppID.\n\n" +
                                   "CÓMO SOLUCIONARLO:\n" +
                                   "1. Ve al Dashboard de Photon: https://dashboard.photonengine.com/\n" +
                                   "2. Crea una nueva aplicación (o edita la existente) y asegúrate de elegir:\n" +
                                   "   - SDK: 'Photon Fusion'\n" +
                                   "   - Fusion Topology (Modo de Juego): Selecciona 'Client/Server' o 'All' (¡NO elijas 'Shared'!).\n" +
                                   "3. Copia el nuevo AppID de Fusion generado.\n" +
                                   "4. Abre Unity, ve a 'Assets/Photon/Fusion/Resources/PhotonAppSettings.asset' y pega el nuevo AppID en el campo 'AppIdFusion'.\n" +
                                   "========================================================================");
                }

                if (result.Ok)
                {
                    IsRunning = true;
                    Debug.Log($"Se creo sala {sessionName}");
                    // Debug.Log($"[CONNECTION_DEBUG] <<< [8] Host started successfully. Waiting for Player 2...");
                    RedesLog.Info(RedesLog.LOBBY, $"   Sala '{sessionName}' creada. Esperando al otro jugador.");
                    RedesLog.Info(RedesLog.NET, "<< StartAsHost() OK");
                    OnHostStarted?.Invoke();
                }
                else
                {
                    // Debug.LogError($"[CONNECTION_DEBUG] <<< [8] Host start failed con razón: {result.ShutdownReason}");
                    RedesLog.Error(RedesLog.NET, $"<< StartAsHost() FALLO - {result.ShutdownReason}");
                    FailAndReturnToLobby($"Crear sala fallo: {result.ShutdownReason}");
                }
            }
            catch (Exception ex)
            {
                // Debug.LogError($"[CONNECTION_DEBUG] <<< [EXCEPCIÓN] Exception in StartAsHost: {ex}");
                RedesLog.Error(RedesLog.NET, $"<< StartAsHost() EXCEPCION: {ex.Message}");
                FailAndReturnToLobby($"Excepcion al crear sala: {ex.Message}");
            }
        }

        // ──────────────────────────────────────────────────────────────────
        //  JUGADOR 2 — "UNIRSE A SALA"
        // ──────────────────────────────────────────────────────────────────
        public async void StartAsClient(string sessionName)
        {
            // Debug.Log($"[CONNECTION_DEBUG] >>> [1] Inicia StartAsClient(). sessionName={sessionName}, _isInSession={_isInSession}");
            RedesLog.Info(RedesLog.NET, $">> StartAsClient() - unirse a sala {sessionName}");
            if (_isInSession) { 
                // Debug.Log($"[CONNECTION_DEBUG] <<< [1] StartAsClient() ABORTADO porque _isInSession=true");
                RedesLog.Warn(RedesLog.NET, "<< StartAsClient ABORTADO - ya en sesion"); 
                return; 
            }

            _isInSession = true;
            // Debug.Log($"[CONNECTION_DEBUG] >>> [2] _isInSession puesto en true. Apagando LobbyRunner...");
            await ShutdownLobbyRunnerAsync();
            
            // Debug.Log($"[CONNECTION_DEBUG] >>> [3] Creando GameRunner (provideInput=true)...");
            _gameRunner = CreateRunner("GameRunner", provideInput: true);
            
            if (_gameRunner == null) {
                // Debug.LogError($"[CONNECTION_DEBUG] <<< [3] ERROR FATAL: _gameRunner es nulo después de CreateRunner.");
                return;
            }

            // Debug.Log($"[CONNECTION_DEBUG] >>> [4] GameRunner creado con éxito (InstanceID: {_gameRunner.GetInstanceID()}). Configurando StartGameArgs...");

            int buildIdx = 0;
            try
            {
                var activeScene = SceneManager.GetActiveScene();
                // Debug.Log($"[CONNECTION_DEBUG] >>> [4A] Escena activa obtenida: name={activeScene.name}, buildIndex={activeScene.buildIndex}");
                
                buildIdx = activeScene.buildIndex;
                if (buildIdx < 0)
                {
                    // Debug.LogWarning($"[CONNECTION_DEBUG] >>> [4B] Active scene '{activeScene.name}' is not in Build Settings (index -1). Falling back to index 0.");
                    buildIdx = 0;
                }
                
                // Debug.Log($"[CONNECTION_DEBUG] >>> [4C] Usando buildIdx={buildIdx} (no se usará explicitamente en el args porque es cliente, pero se verifica).");
                
                // Debug.Log($"[CONNECTION_DEBUG] >>> [4D] Añadiendo NetworkSceneManagerDefault a _gameRunner...");
                var sceneMgr = _gameRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
                if (sceneMgr == null) // Debug.LogError("[CONNECTION_DEBUG] ERROR: NetworkSceneManagerDefault no se pudo añadir!");

                RedesLog.Info(RedesLog.LOBBY, $"   Uniéndose a sala '{sessionName}'...");

                var args = new StartGameArgs
                {
                    GameMode     = GameMode.Client,
                    SessionName  = sessionName,
                    SceneManager = sceneMgr
                };
                
                // Debug.Log($"[CONNECTION_DEBUG] >>> [5] StartGameArgs configurado: GameMode={args.GameMode}, SessionName={args.SessionName}");
                // Debug.Log($"[CONNECTION_DEBUG] >>> [6] Llamando a await _gameRunner.StartGame(args)...");

                var result = await _gameRunner.StartGame(args);
                
                if (this == null || _gameRunner == null) {
                    // Debug.LogWarning($"[CONNECTION_DEBUG] <<< [6] HostNetworkService o _gameRunner destruido mientras se esperaba StartGame.");
                    return;
                }

                // Debug.Log($"[CONNECTION_DEBUG] >>> [7] StartGame() finalizado. Resultado Ok={result.Ok}, ShutdownReason={result.ShutdownReason}");

                if (!result.Ok && result.ShutdownReason == ShutdownReason.IncompatibleConfiguration)
                {
                    Debug.LogError("========================================================================\n" +
                                   "❌ ERROR CRÍTICO DE FUSION: IncompatibleConfiguration\n" +
                                   "========================================================================\n" +
                                   "Tu AppID de Photon Fusion actual está configurado como 'Shared Mode' únicamente en el Dashboard de Photon. " +
                                   "El modo Host-Client NO está permitido por Photon con este AppID.\n\n" +
                                   "CÓMO SOLUCIONARLO:\n" +
                                   "1. Ve al Dashboard de Photon: https://dashboard.photonengine.com/\n" +
                                   "2. Crea una nueva aplicación (o edita la existente) y asegúrate de elegir:\n" +
                                   "   - SDK: 'Photon Fusion'\n" +
                                   "   - Fusion Topology (Modo de Juego): Selecciona 'Client/Server' o 'All' (¡NO elijas 'Shared'!).\n" +
                                   "3. Copia el nuevo AppID de Fusion generado.\n" +
                                   "4. Abre Unity, ve a 'Assets/Photon/Fusion/Resources/PhotonAppSettings.asset' y pega el nuevo AppID en el campo 'AppIdFusion'.\n" +
                                   "========================================================================");
                }

                if (result.Ok)
                {
                    IsRunning = true;
                    // Debug.Log($"[CONNECTION_DEBUG] <<< [8] Client started successfully. Joined room {sessionName}.");
                    RedesLog.Info(RedesLog.LOBBY, $"   Unido a sala '{sessionName}'!");
                    RedesLog.Info(RedesLog.NET, "<< StartAsClient() OK");
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
            if (_playerSpawner != null)
            {
                _playerSpawner.ClearSpawned();
            }
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
            bool isShared = runner.Topology == Topologies.Shared;
            RedesLog.Info(RedesLog.NET, $">> OnPlayerJoined(player={player}) IsServer={runner.IsServer} IsShared={isShared}");

            bool shouldSpawn = isShared ? (player == runner.LocalPlayer) : runner.IsServer;

            if (shouldSpawn)
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
            
            bool isShared = runner.Topology == Topologies.Shared;
            bool shouldDespawn = isShared ? (player == runner.LocalPlayer) : runner.IsServer;

            if (shouldDespawn && _playerSpawner != null)
                _playerSpawner.DespawnPlayer(runner, player);
            
            RefreshPlayerCount();

            // Fallback: If a player leaves and the match was already finished (ResultView active), return to lobby automatically
            var resultView = FindFirstObjectByType<ResultView>();
            if (resultView != null && resultView.gameObject.activeInHierarchy)
            {
                RedesLog.Info(RedesLog.NET, "   OnPlayerLeft Fallback: Match results are showing and player left. Triggering return to lobby.");
                var flow = FindFirstObjectByType<GameFlowController>();
                if (flow != null)
                {
                    flow.TriggerReturnToLobby();
                }
            }
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
            bool isShared = runner.Topology == Topologies.Shared;
            RedesLog.Info(RedesLog.NET, $">> OnSceneLoadDone. IsServer={runner.IsServer} IsShared={isShared} ActivePlayers={activeCount}");

            if (isShared)
            {
                if (_playerSpawner != null && !_playerSpawner.IsPlayerSpawned(runner.LocalPlayer))
                {
                    _playerSpawner.SpawnPlayer(runner, runner.LocalPlayer, _playerPrefab);
                }
            }
            else if (runner.IsServer)
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
