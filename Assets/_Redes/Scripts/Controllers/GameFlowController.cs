using UnityEngine;
using Redes.Core;
using Redes.Models;
using Redes.Network;
using Redes.Views;

namespace Redes.Controllers
{
    /// <summary>
    /// MVC - CONTROLLER for the overall flow (boot -> lobby -> playing -> finished).
    ///
    /// It is the glue: it owns the GameStateModel, listens to the INetworkService
    /// (host events) and updates the LobbyView / HUD accordingly.
    /// Depends on the INetworkService ABSTRACTION (DIP), referenced via the
    /// concrete HostNetworkService for the inspector.
    ///
    /// Logic is implemented by another agent; here is the wiring + structure.
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

        // MVC Model owned by this controller (created at runtime).
        private GameStateModel _model;

        // Treat the concrete service through its abstraction.
        private INetworkService NetworkService => _hostService;

        private void Awake()
        {
            _model = new GameStateModel();
            _model.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnEnable()
        {
            if (NetworkService != null)
            {
                NetworkService.OnHostStarted        += HandleHostStarted;
                NetworkService.OnPlayerCountChanged += HandlePlayerCountChanged;
                NetworkService.OnEnoughPlayersToStart += HandleEnoughPlayers;
            }
            if (_lobbyView != null && _lobbyView.HostButton != null)
            {
                _lobbyView.HostButton.onClick.AddListener(StartHost);
            }
            if (_matchController != null)
            {
                _matchController.OnMatchFinished += HandleMatchFinished;
            }
        }

        private void OnDisable()
        {
            if (NetworkService != null)
            {
                NetworkService.OnHostStarted        -= HandleHostStarted;
                NetworkService.OnPlayerCountChanged -= HandlePlayerCountChanged;
                NetworkService.OnEnoughPlayersToStart -= HandleEnoughPlayers;
            }
            if (_lobbyView != null && _lobbyView.HostButton != null)
            {
                _lobbyView.HostButton.onClick.RemoveListener(StartHost);
            }
            if (_matchController != null)
            {
                _matchController.OnMatchFinished -= HandleMatchFinished;
            }
        }

        // ---- Intent: user pressed "Host" ----
        public void StartHost()
        {
            NetworkService.StartAsHost();
            _model.SetPhase(GamePhase.SearchingSession);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Booting:
                case GamePhase.SearchingSession:
                    if (_lobbyView != null)
                    {
                        _lobbyView.SetVisible(true);
                        _lobbyView.ShowStatus("Buscando sala...");
                    }
                    if (_gameHudView != null) _gameHudView.SetVisible(false);
                    break;
                case GamePhase.WaitingForPlayers:
                    if (_lobbyView != null)
                    {
                        _lobbyView.SetVisible(true);
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

        // ---- Handlers ----
        private void HandleHostStarted()
        {
            _model.SetPhase(GamePhase.WaitingForPlayers);
            if (_lobbyView != null) _lobbyView.ShowStatus("Esperando jugadores...");
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
