using UnityEngine;
using Redes.Core;
using Redes.Models;
using Redes.Network;
using Redes.Views;

namespace Redes.Controllers
{
    /// <summary>
    /// CALLSTACK COMPLETO:
    ///
    /// ── INICIO (ambos jugadores) ──────────────────────────────────────────
    /// Awake()  → crea GameStateModel, subscribe OnPhaseChanged
    /// OnEnable()→ subscribe eventos de red + listeners de botones
    /// Start()  → HandlePhaseChanged(Booting) → LobbyView visible, botones visibles
    ///
    /// ── USER 1: CREAR SALA ───────────────────────────────────────────────
    /// CreateRoom()
    ///   → LobbyView.HideButtons(), ShowStatus("Creando sala...")
    ///   → NetworkService.StartAsHost()          [async, espera Photon]
    ///   → _model.SetPhase(SearchingSession)
    /// ¿StartGame OK?
    ///   SÍ  → OnHostStarted → HandleHostStarted → WaitingForPlayers → "Esperando..."
    ///   NO  → OnConnectionFailed → HandleConnectionFailed → Booting → ShowError
    /// OnPlayerCountChanged(1) → LobbyView.ShowPlayerCount(1)
    /// OnEnoughPlayersToStart  → HandleEnoughPlayers → Playing → HUD visible
    ///
    /// ── USER 2: UNIRSE ───────────────────────────────────────────────────
    /// JoinRoom()
    ///   → LobbyView.HideButtons(), ShowStatus("Uniéndose...")
    ///   → NetworkService.StartAsClient()         [async, busca sesion "RedesRoom"]
    ///   → _model.SetPhase(SearchingSession)
    /// ¿StartGame OK?
    ///   SÍ  → OnHostStarted → HandleHostStarted → WaitingForPlayers
    ///   NO  → OnConnectionFailed → vuelve a Booting + error
    /// OnEnoughPlayersToStart  → Playing
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("Network (assigned by Tools > Redes > Link & Assign All)")]
        [SerializeField] private HostNetworkService _hostService;

        [Header("Views (assigned by the Link tool)")]
        [SerializeField] private LobbyView _lobbyView;
        [SerializeField] private GameHudView _gameHudView;

        [Header("Sub-controllers (assigned by the Link tool)")]
        [SerializeField] private MatchController _matchController;

        private GameStateModel _model;
        private INetworkService NetworkService => _hostService;

        private static string _pendingStatusMessage = null;

        // ──────────────────────────────────────────────────────────────────
        private void Awake()
        {
            RedesLog.Info(RedesLog.BOOT, ">> GameFlowController.Awake()");
            _model = new GameStateModel();
            _model.OnPhaseChanged += HandlePhaseChanged;
            RedesLog.Info(RedesLog.BOOT, $"<< GameFlowController.Awake() - fase inicial={_model.Phase}");
        }

        private void Start()
        {
            RedesLog.Info(RedesLog.BOOT, ">> GameFlowController.Start()");
            LogReferenceStatus();
            
            if (_hostService != null && _hostService.IsRunning && _hostService.ConnectedPlayers >= GameConstants.MIN_PLAYERS_TO_START)
            {
                _model.SetPhase(GamePhase.Playing);
            }
            else
            {
                HandlePhaseChanged(_model.Phase);
                
                // Show any pending message from previous session failure
                if (!string.IsNullOrEmpty(_pendingStatusMessage))
                {
                    if (_lobbyView != null)
                    {
                        _lobbyView.ShowStatus(_pendingStatusMessage);
                    }
                    _pendingStatusMessage = null;
                }
            }

            // Ensure lobby watch is started if we are in booting (and after a scene reload)
            if (_hostService != null && _model.Phase == GamePhase.Booting)
            {
                _hostService.StartLobbyWatch();
            }

            RedesLog.Info(RedesLog.BOOT, "<< GameFlowController.Start()");
        }

        private void OnEnable()
        {
            RedesLog.Info(RedesLog.BOOT, ">> GameFlowController.OnEnable()");
            
            if (HostNetworkService.Instance != null)
            {
                _hostService = HostNetworkService.Instance;
                RedesLog.Info(RedesLog.BOOT, "   GameFlowController: Re-linked _hostService to persistent HostNetworkService Instance successfully.");
            }

            if (NetworkService != null)
            {
                NetworkService.OnHostStarted             += HandleHostStarted;
                NetworkService.OnPlayerCountChanged      += HandlePlayerCountChanged;
                NetworkService.OnEnoughPlayersToStart    += HandleEnoughPlayers;
                NetworkService.OnConnectionFailed        += HandleConnectionFailed;
                NetworkService.OnRoomListUpdated         += HandleRoomListUpdated;
                RedesLog.Info(RedesLog.BOOT, "   eventos de red registrados (incl. OnRoomListUpdated)");
            }
            else
            {
                RedesLog.Error(RedesLog.BOOT, "   _hostService es NULL! Asignalo con Link & Assign All");
            }

            if (_lobbyView != null)
            {
                if (_lobbyView.HostButton != null)
                    _lobbyView.HostButton.onClick.AddListener(CreateRoom);
                RedesLog.Info(RedesLog.BOOT, "   listeners de botones registrados");
            }
            else
            {
                RedesLog.Error(RedesLog.BOOT, "   _lobbyView es NULL!");
            }

            if (_matchController != null)
            {
                _matchController.OnMatchFinished += HandleMatchFinished;
                _matchController.OnLobbyClicked += HandleReturnToLobbyClicked;
            }

            RedesLog.Info(RedesLog.BOOT, "<< GameFlowController.OnEnable()");
        }

        private void OnDisable()
        {
            if (NetworkService != null)
            {
                NetworkService.OnHostStarted             -= HandleHostStarted;
                NetworkService.OnPlayerCountChanged      -= HandlePlayerCountChanged;
                NetworkService.OnEnoughPlayersToStart    -= HandleEnoughPlayers;
                NetworkService.OnConnectionFailed        -= HandleConnectionFailed;
                NetworkService.OnRoomListUpdated         -= HandleRoomListUpdated;
            }
            if (_lobbyView != null)
            {
                if (_lobbyView.HostButton != null) _lobbyView.HostButton.onClick.RemoveListener(CreateRoom);
                // JoinRoom is handled dynamically by LobbyView now
            }
            if (_matchController != null)
            {
                _matchController.OnMatchFinished -= HandleMatchFinished;
                _matchController.OnLobbyClicked -= HandleReturnToLobbyClicked;
            }
        }

        private void HandleReturnToLobbyClicked()
        {
            RedesLog.Trace(RedesLog.LOBBY, "GameFlowController", "HandleReturnToLobbyClicked", null, "Shutting down network service and returning to lobby");
            TriggerReturnToLobby();
        }

        public void TriggerReturnToLobby()
        {
            RedesLog.Trace(RedesLog.LOBBY, "GameFlowController", "TriggerReturnToLobby [IN]", null, "Performing return to lobby procedure");
            try
            {
                if (_hostService != null)
                {
                    _hostService.Shutdown();
                }
                
                var resultView = FindFirstObjectByType<ResultView>();
                if (resultView != null)
                {
                    resultView.HideResult();
                }
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.LOBBY, $"   GameFlowController: [ERROR] Exception in TriggerReturnToLobby: {ex.Message}");
            }
            _model.SetPhase(GamePhase.Booting);
            
            RedesLog.Trace(RedesLog.LOBBY, "GameFlowController", "TriggerReturnToLobby", null, "Reloading scene to ensure clean state...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);

            RedesLog.Trace(RedesLog.LOBBY, "GameFlowController", "TriggerReturnToLobby [OUT]", null, "Return to lobby completed");
        }

        public static string LocalUsername = "Player";

        // ──────────────────────────────────────────────────────────────────
        //  ACCIONES DE BOTONES
        // ──────────────────────────────────────────────────────────────────

        public void CreateRoom()
        {
            RedesLog.Info(RedesLog.LOBBY, ">> CreateRoom() - Jugador 1 quiere crear sala");
            if (NetworkService == null) { RedesLog.Error(RedesLog.LOBBY, "   NetworkService NULL"); return; }

            string username = _lobbyView != null ? _lobbyView.Username : "";
            if (string.IsNullOrWhiteSpace(username) || username.Trim() == "Username" || username.Trim() == "")
            {
                if (_lobbyView != null)
                {
                    _lobbyView.ShowStatus("¡Error: Introduce un nombre de usuario válido!");
                    _lobbyView.ShowButtons();
                }
                return;
            }
            LocalUsername = username.Trim();

            if (_lobbyView != null)
            {
                _lobbyView.HideButtons();
                _lobbyView.ShowStatus("Creando sala...");
            }
            NetworkService.StartAsHost(LocalUsername + "'s Room");
            _model.SetPhase(GamePhase.SearchingSession);
            RedesLog.Info(RedesLog.LOBBY, "<< CreateRoom() - fase=SearchingSession, esperando StartGame...");
        }

        public void JoinRoom(string sessionName)
        {
            RedesLog.Info(RedesLog.LOBBY, $">> JoinRoom({sessionName}) - Jugador quiere unirse");
            if (NetworkService == null) { RedesLog.Error(RedesLog.LOBBY, "   NetworkService NULL"); return; }

            string username = _lobbyView != null ? _lobbyView.Username : "";
            if (string.IsNullOrWhiteSpace(username) || username.Trim() == "Username" || username.Trim() == "")
            {
                if (_lobbyView != null)
                {
                    _lobbyView.ShowStatus("¡Error: Introduce un nombre de usuario válido primero!");
                }
                return;
            }
            LocalUsername = username.Trim();

            if (_lobbyView != null)
            {
                _lobbyView.HideButtons();
                _lobbyView.ShowStatus($"Uniéndose a {sessionName}...");
            }
            NetworkService.StartAsClient(sessionName);
            _model.SetPhase(GamePhase.SearchingSession);
            RedesLog.Info(RedesLog.LOBBY, "<< JoinRoom() - fase=SearchingSession, esperando StartGame...");
        }

        // ──────────────────────────────────────────────────────────────────
        //  HANDLERS DE FASE
        // ──────────────────────────────────────────────────────────────────

        private void HandlePhaseChanged(GamePhase phase)
        {
            RedesLog.Info(RedesLog.BOOT, $">> HandlePhaseChanged({phase})");
            switch (phase)
            {
                case GamePhase.Booting:
                    if (_lobbyView != null) { _lobbyView.SetVisible(true); _lobbyView.ShowButtons(); }
                    if (_gameHudView != null) _gameHudView.SetVisible(false);
                    RedesLog.Info(RedesLog.BOOT, "   BOOTING → lobby visible, botones visibles");
                    break;

                case GamePhase.SearchingSession:
                    if (_lobbyView != null) { _lobbyView.SetVisible(true); _lobbyView.HideButtons(); _lobbyView.ShowStatus("Conectando..."); }
                    if (_gameHudView != null) _gameHudView.SetVisible(false);
                    RedesLog.Info(RedesLog.BOOT, "   SEARCHING → 'Conectando...'");
                    break;

                case GamePhase.WaitingForPlayers:
                    if (_lobbyView != null) { _lobbyView.SetVisible(true); _lobbyView.HideButtons(); _lobbyView.ShowStatus("Esperando jugadores..."); }
                    if (_gameHudView != null) _gameHudView.SetVisible(false);
                    RedesLog.Info(RedesLog.BOOT, "   WAITING → 'Esperando jugadores...'");
                    break;

                case GamePhase.Playing:
                    if (_lobbyView != null) _lobbyView.SetVisible(false);
                    if (_gameHudView != null) _gameHudView.SetVisible(true);
                    RedesLog.Info(RedesLog.BOOT, "   PLAYING → lobby oculto, HUD visible");
                    break;

                case GamePhase.Finished:
                    if (_lobbyView != null) _lobbyView.SetVisible(false);
                    if (_gameHudView != null) _gameHudView.SetVisible(false);
                    RedesLog.Info(RedesLog.BOOT, "   FINISHED → todo oculto");
                    break;
            }
            RedesLog.Info(RedesLog.BOOT, $"<< HandlePhaseChanged({phase})");
        }

        // ──────────────────────────────────────────────────────────────────
        //  HANDLERS DE EVENTOS DE RED
        // ──────────────────────────────────────────────────────────────────

        private void HandleHostStarted()
        {
            RedesLog.Info(RedesLog.NET, ">> HandleHostStarted() - runner activo, esperando jugadores");
            _model.SetPhase(GamePhase.WaitingForPlayers);
            RedesLog.Info(RedesLog.NET, "<< HandleHostStarted()");
        }

        private void HandlePlayerCountChanged(int count)
        {
            RedesLog.Info(RedesLog.NET, $">> HandlePlayerCountChanged(count={count})");
            _model.SetPlayers(count);
            if (_lobbyView != null) _lobbyView.ShowPlayerCount(count);
            RedesLog.Info(RedesLog.NET, $"<< HandlePlayerCountChanged(count={count})");
        }

        private void HandleEnoughPlayers()
        {
            RedesLog.Info(RedesLog.MATCH, ">> HandleEnoughPlayers() - 2 jugadores conectados, INICIANDO JUEGO");
            _model.SetPhase(GamePhase.Playing);
            if (_lobbyView != null) _lobbyView.SetVisible(false);
            if (_gameHudView != null) _gameHudView.SetVisible(true);
            RedesLog.Info(RedesLog.MATCH, "<< HandleEnoughPlayers() - fase=Playing");
        }

        private void HandleRoomListUpdated(System.Collections.Generic.List<Fusion.SessionInfo> sessions)
        {
            RedesLog.Info(RedesLog.LOBBY,
                $">> HandleRoomListUpdated: {sessions?.Count} salas");

            if (_lobbyView != null && _model.Phase == GamePhase.Booting)
            {
                _lobbyView.PopulateRooms(sessions, JoinRoom);
                _lobbyView.ShowStatus(sessions != null && sessions.Count > 0
                    ? "Salas disponibles. ¿Crear o unirse?"
                    : "Buscando salas...");
            }
        }

        private void HandleConnectionFailed(string reason)
        {
            RedesLog.Error(RedesLog.NET, $">> HandleConnectionFailed(reason={reason})");
            _model.SetPhase(GamePhase.Booting);
            if (_lobbyView != null)
            {
                _lobbyView.SetVisible(true);
                _lobbyView.ShowButtons();           // restaura host (join sigue deshabilitado)
                
                // Friendly message if normal game end/shutdown from server
                string msg = "";
                if (reason.Contains("DisconnectedByPluginLogic") || reason.Contains("ShutdownReason.Ok"))
                {
                    msg = "Partida finalizada. De vuelta al lobby.";
                }
                else
                {
                    msg = $"Error: {reason}";
                }
                
                _lobbyView.ShowStatus(msg);
                _pendingStatusMessage = msg;
            }
            RedesLog.Error(RedesLog.NET, "<< HandleConnectionFailed() - vuelto a Booting");
        }

        private void HandleMatchFinished(MatchResult result)
        {
            RedesLog.Info(RedesLog.MATCH, $">> HandleMatchFinished(result={result})");
            _model.SetPhase(GamePhase.Finished);
            RedesLog.Info(RedesLog.MATCH, "<< HandleMatchFinished()");
        }

        // ──────────────────────────────────────────────────────────────────
        private void LogReferenceStatus()
        {
            RedesLog.Info(RedesLog.BOOT, "=== REFERENCIAS GameFlowController ===");
            RedesLog.Info(RedesLog.BOOT, $"  _hostService:   {(_hostService   != null ? "OK" : "NULL ⚠️")}");
            RedesLog.Info(RedesLog.BOOT, $"  _lobbyView:     {(_lobbyView     != null ? "OK" : "NULL ⚠️")}");
            RedesLog.Info(RedesLog.BOOT, $"  _gameHudView:   {(_gameHudView   != null ? "OK" : "NULL ⚠️")}");
            RedesLog.Info(RedesLog.BOOT, $"  _matchController:{(_matchController != null ? "OK" : "NULL ⚠️")}");
            if (_lobbyView != null)
            {
                RedesLog.Info(RedesLog.BOOT, $"  _lobbyView._hostButton: {(_lobbyView.HostButton != null ? "OK" : "NULL ⚠️")}");
                RedesLog.Info(RedesLog.BOOT, $"  _lobbyView._joinButton: {(_lobbyView.JoinButton != null ? "OK" : "NULL ⚠️")}");
            }
            if (_hostService != null)
            {
                RedesLog.Info(RedesLog.BOOT, $"  _hostService._playerPrefab:  {(_hostService.PlayerPrefab  != null ? "OK" : "NULL ⚠️")}");
                RedesLog.Info(RedesLog.BOOT, $"  _hostService._playerSpawner: (ver log NET)");
            }
            RedesLog.Info(RedesLog.BOOT, "======================================");
        }
    }
}
