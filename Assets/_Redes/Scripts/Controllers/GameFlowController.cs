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
            // TODO (other agent): subscribe to _model.OnPhaseChanged -> update views.
        }

        private void OnEnable()
        {
            // TODO (other agent): subscribe to network events:
            // NetworkService.OnHostStarted        += HandleHostStarted;
            // NetworkService.OnPlayerCountChanged += HandlePlayerCountChanged;
            // NetworkService.OnEnoughPlayersToStart += HandleEnoughPlayers;
            // _lobbyView.HostButton.onClick.AddListener(StartHost);
        }

        private void OnDisable()
        {
            // TODO (other agent): unsubscribe everything subscribed in OnEnable.
        }

        // ---- Intent: user pressed "Host" ----
        public void StartHost()
        {
            // Required "Inicio el juego" log is emitted inside HostNetworkService.StartAsHost().
            NetworkService.StartAsHost();
            _model.SetPhase(GamePhase.SearchingSession);
        }

        // ---- Handlers (stubs) ----
        private void HandleHostStarted()
        {
            _model.SetPhase(GamePhase.WaitingForPlayers);
            // TODO (other agent): _lobbyView.ShowStatus("Esperando jugadores...");
        }

        private void HandlePlayerCountChanged(int count)
        {
            _model.SetPlayers(count);
            // TODO (other agent): _lobbyView.ShowPlayerCount(count);
        }

        private void HandleEnoughPlayers()
        {
            _model.SetPhase(GamePhase.Playing);
            // TODO (other agent): _lobbyView.SetVisible(false); _gameHudView.SetVisible(true);
        }
    }
}
