using UnityEngine;
using Redes.Core;
using Redes.Models;
using Redes.Network;
using Redes.Views;

namespace Redes.Controllers
{
    /// <summary>
    /// MVC - CONTROLLER para el flujo general (boot → lobby → playing → finished).
    ///
    /// Boot:  muestra lobby con botones "Crear Sala" / "Unirse".
    /// Crear: llama StartAsHost() → espera jugadores → Playing al llegar 2.
    /// Unirse: llama StartAsClient() → Playing cuando entra.
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

        private void Awake()
        {
            _model = new GameStateModel();
            _model.OnPhaseChanged += HandlePhaseChanged;
        }

        private void Start()
        {
            // Inicializa la UI con la fase actual (Booting → muestra lobby con botones)
            HandlePhaseChanged(_model.Phase);
        }

        private void OnEnable()
        {
            if (NetworkService != null)
            {
                NetworkService.OnHostStarted          += HandleHostStarted;
                NetworkService.OnPlayerCountChanged   += HandlePlayerCountChanged;
                NetworkService.OnEnoughPlayersToStart += HandleEnoughPlayers;
            }
            if (_lobbyView != null)
            {
                if (_lobbyView.HostButton != null)
                    _lobbyView.HostButton.onClick.AddListener(CreateRoom);
                if (_lobbyView.JoinButton != null)
                    _lobbyView.JoinButton.onClick.AddListener(JoinRoom);
            }
            if (_matchController != null)
                _matchController.OnMatchFinished += HandleMatchFinished;
        }

        private void OnDisable()
        {
            if (NetworkService != null)
            {
                NetworkService.OnHostStarted          -= HandleHostStarted;
                NetworkService.OnPlayerCountChanged   -= HandlePlayerCountChanged;
                NetworkService.OnEnoughPlayersToStart -= HandleEnoughPlayers;
            }
            if (_lobbyView != null)
            {
                if (_lobbyView.HostButton != null)
                    _lobbyView.HostButton.onClick.RemoveListener(CreateRoom);
                if (_lobbyView.JoinButton != null)
                    _lobbyView.JoinButton.onClick.RemoveListener(JoinRoom);
            }
            if (_matchController != null)
                _matchController.OnMatchFinished -= HandleMatchFinished;
        }

        // ---- Acciones de botones ----

        public void CreateRoom()
        {
            if (_lobbyView != null)
            {
                _lobbyView.HideButtons();
                _lobbyView.ShowStatus("Creando sala...");
            }
            NetworkService.StartAsHost();
            _model.SetPhase(GamePhase.SearchingSession);
        }

        public void JoinRoom()
        {
            if (_lobbyView != null)
            {
                _lobbyView.HideButtons();
                _lobbyView.ShowStatus("Uniéndose a sala...");
            }
            NetworkService.StartAsClient();
            _model.SetPhase(GamePhase.SearchingSession);
        }

        // ---- Phase handler ----

        private void HandlePhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Booting:
                    if (_lobbyView != null)
                    {
                        _lobbyView.SetVisible(true);
                        _lobbyView.ShowButtons();
                    }
                    if (_gameHudView != null) _gameHudView.SetVisible(false);
                    break;

                case GamePhase.SearchingSession:
                    if (_lobbyView != null)
                    {
                        _lobbyView.SetVisible(true);
                        _lobbyView.HideButtons();
                        _lobbyView.ShowStatus("Conectando...");
                    }
                    if (_gameHudView != null) _gameHudView.SetVisible(false);
                    break;

                case GamePhase.WaitingForPlayers:
                    if (_lobbyView != null)
                    {
                        _lobbyView.SetVisible(true);
                        _lobbyView.HideButtons();
                        _lobbyView.ShowStatus("Esperando jugadores...");
                    }
                    if (_gameHudView != null) _gameHudView.SetVisible(false);
                    break;

                case GamePhase.Playing:
                    if (_lobbyView != null) _lobbyView.SetVisible(false);
                    if (_gameHudView != null) _gameHudView.SetVisible(true);
                    break;

                case GamePhase.Finished:
                    if (_lobbyView != null) _lobbyView.SetVisible(false);
                    if (_gameHudView != null) _gameHudView.SetVisible(false);
                    break;
            }
        }

        // ---- Network event handlers ----

        private void HandleHostStarted()
        {
            _model.SetPhase(GamePhase.WaitingForPlayers);
        }

        private void HandlePlayerCountChanged(int count)
        {
            _model.SetPlayers(count);
            if (_lobbyView != null) _lobbyView.ShowPlayerCount(count);
        }

        private void HandleEnoughPlayers()
        {
            _model.SetPhase(GamePhase.Playing);
            if (_lobbyView != null) _lobbyView.SetVisible(false);
            if (_gameHudView != null) _gameHudView.SetVisible(true);
        }

        private void HandleMatchFinished(MatchResult result)
        {
            _model.SetPhase(GamePhase.Finished);
        }
    }
}
