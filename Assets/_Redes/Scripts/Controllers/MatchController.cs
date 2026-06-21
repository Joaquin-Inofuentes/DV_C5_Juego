using UnityEngine;
using Redes.Core;
using Redes.Views;

namespace Redes.Controllers
{
    /// <summary>
    /// MVC - CONTROLLER for the match outcome (win/lose).
    ///
    /// It receives the LOCAL result (already resolved per-client by the networked
    /// MatchNetworkController RPC) and drives the ResultView. It also relays the
    /// ResultView's Action so the GameFlowController can move to Finished.
    ///
    /// Logic is implemented by another agent.
    /// </summary>
    public class MatchController : MonoBehaviour
    {
        [Header("View (assigned by Tools > Redes > Link & Assign All)")]
        [SerializeField] private ResultView _resultView;

        [Header("Network Controller")]
        [SerializeField] private Gameplay.MatchNetworkController _matchNetworkController;

        public event System.Action<MatchResult> OnMatchFinished;

        private void OnEnable()
        {
            if (_resultView != null)
            {
                _resultView.OnResultNotified += HandleResultNotified;
                _resultView.OnRetryClicked += HandleRetryClicked;
            }
        }

        private void OnDisable()
        {
            if (_resultView != null)
            {
                _resultView.OnResultNotified -= HandleResultNotified;
                _resultView.OnRetryClicked -= HandleRetryClicked;
            }
        }

        private void HandleRetryClicked()
        {
            RedesLog.Info(RedesLog.MATCH, ">> MatchController.HandleRetryClicked() [IN] - No arguments");
            try
            {
                if (_resultView != null)
                {
                    RedesLog.Info(RedesLog.MATCH, "   MatchController: Disabling retry button and showing status");
                    _resultView.SetRetryButtonInteractable(false);
                    _resultView.ShowRematchStatus("Esperando al otro jugador...");
                }

                if (_matchNetworkController == null)
                {
                    RedesLog.Info(RedesLog.MATCH, "   MatchController: _matchNetworkController is NULL. Finding in scene...");
                    _matchNetworkController = FindFirstObjectByType<Gameplay.MatchNetworkController>();
                }

                if (_matchNetworkController != null)
                {
                    RedesLog.Info(RedesLog.MATCH, "   MatchController: [IN] Calling _matchNetworkController.SetLocalPlayerReady()");
                    _matchNetworkController.SetLocalPlayerReady();
                    RedesLog.Info(RedesLog.MATCH, "   MatchController: [OUT] Call to _matchNetworkController.SetLocalPlayerReady completed");
                }
                else
                {
                    RedesLog.Error(RedesLog.MATCH, "   MatchController: [ERROR] Could not find MatchNetworkController in scene!");
                }
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.MATCH, $"   MatchController: [ERROR] Exception in HandleRetryClicked: {ex.Message}\n{ex.StackTrace}");
            }
            RedesLog.Info(RedesLog.MATCH, "<< MatchController.HandleRetryClicked() [OUT]");
        }

        public void UpdateRematchStatus(string text)
        {
            RedesLog.Info(RedesLog.MATCH, $">> MatchController.UpdateRematchStatus() [IN] - text='{text}'");
            try
            {
                RedesLog.Info(RedesLog.MATCH, $"[Rematch Status Broadcast Received] '{text}'");
                if (_resultView != null)
                {
                    _resultView.ShowRematchStatus(text);
                    RedesLog.Info(RedesLog.MATCH, $"   MatchController: ResultView text successfully updated to '{text}'");
                }
                else
                {
                    RedesLog.Warn(RedesLog.MATCH, "   MatchController: _resultView is NULL, cannot update status UI text.");
                }
            }
            catch (System.Exception ex)
            {
                RedesLog.Error(RedesLog.MATCH, $"   MatchController: [ERROR] Exception in UpdateRematchStatus: {ex.Message}\n{ex.StackTrace}");
            }
            RedesLog.Info(RedesLog.MATCH, "<< MatchController.UpdateRematchStatus() [OUT]");
        }

        /// <summary>
        /// Entry point called per-client with this client's outcome.
        /// Shows the view (which also fires the Action + the required logs).
        /// </summary>
        public void NotifyResult(MatchResult result)
        {
            if (_resultView != null)
            {
                _resultView.ShowResult(result);
            }
        }

        private void HandleResultNotified(MatchResult result)
        {
            OnMatchFinished?.Invoke(result);
        }
    }
}
